using Origin.Source.Model.Map.Light;
using Origin.Source.Model.NewWorld;
using Origin.Source.Model.NewWorld.Map;
using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;
using Tile = Origin.Source.Model.NewWorld.Tile;

namespace Origin.Source.Model.NewWorld.Systems.Light
{
    internal class SystemUpdateArtificialLight : TickSystem
    {
        // Базові параметри моделі штучного освітлення.
        private const byte MaxLightLevel = 7;
        private const int MaxEmitterPower = 7;
        private const int TailSteps = MaxLightLevel - 1;
        private const int MaxEffectiveDistance = MaxEmitterPower + TailSteps;
        private const int BlockSize = BlockBase.BLOCK_SIZE;
        private const double LightRoundness = 0.7; // 0.0 = ромб (манхеттен), 1.0 = округле (евклід)

        // Патерни сусідів та попередньо підрахований коефіцієнт округлості.
        private static readonly Point3[] RecastNeighbours = WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(true);
        private static readonly double LightRoundnessK = Math.Clamp(LightRoundness, 0.0, 1.0);

        // Dirty-набір змінених позицій після конструкцій.
        private readonly HashSet<Point3> dirtyPositions = [];
        // Реєстр емітерів: позиція -> потужність.
        private readonly Dictionary<Point3, byte> emitterPowers = [];
        // Індекс емітерів по блоках для швидкого локального пошуку.
        private readonly Dictionary<Point3, HashSet<Point3>> emittersByBlock = [];

        // Сусіди для підсвітки блокерів поруч із освітленим повітрям.
        private readonly Point3[] plusNeighbours = WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false);
        // Сусіди для поширення хвилі світла.
        private readonly Point3[] propagationNeighbours =
        [
            new(1, 0, 0),
            new(0, 1, 0),
            new(-1, 0, 0),
            new(0, -1, 0),
            new(-1, -1, 0),
            new(-1, 1, 0),
            new(1, -1, 0),
            new(1, 1, 0),
            Point3.Up,
            Point3.Down
        ];

        // Буфери, які перевикористовуються між апдейтами (мінімум алокацій).
        private readonly Dictionary<Point3, byte> bestBuffer = [];
        private readonly HashSet<Point3> overlapBuffer = [];
        private readonly Queue<LightWaveNode> waveQueue = [];
        private readonly HashSet<Point3> visitedBuffer = [];
        private bool recastDirty = false;

        // Вузол BFS-хвилі.
        private readonly record struct LightWaveNode(Point3 Pos);

        public SystemUpdateArtificialLight(Site site) : base(site)
        {
            site.ArtificialLightSystem = this;
        }

        // Повна ініціалізація: очистка стану, побудова реєстру емітерів, повний перерахунок.
        public override void Initialize()
        {
            dirtyPositions.Clear();
            emitterPowers.Clear();
            emittersByBlock.Clear();
            BuildEmitterRegistry();

            RecalculateAllLight();
            _site.LightControl.bufferDirty = true;
        }

        public override void LoadInit()
        {
            base.LoadInit();
            Initialize();
        }

        public void OnConstructionPlaced(Point3 pos)
        {
            MarkForRecast(pos);
        }

        public void OnConstructionRemoved(Point3 pos)
        {
            MarkForRecast(pos);
        }

        public override void Update(in ulong t)
        {
            if (!recastDirty)
                return;

            RecalculatePartialLight();
            ClearRecastPlan();
            recastDirty = false;
            _site.LightControl.bufferDirty = true;
        }

        // Відмічає локальну зону для часткового перерахунку після змін у світі.
        private void MarkForRecast(Point3 pos)
        {
            foreach (var n in RecastNeighbours)
            {
                Point3 target = pos + n;
                if (!target.InBounds(Point3.Zero, _site.Size))
                    continue;

                dirtyPositions.Add(target);
            }

            UpdateEmitterAt(pos);
            recastDirty = true;
        }

        private void ClearRecastPlan()
        {
            dirtyPositions.Clear();
        }

        // Перебудова індексу емітерів по всій мапі.
        private void BuildEmitterRegistry()
        {
            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        byte power = GetEmitterPower(pos);
                        if (power > 0)
                            RegisterEmitter(pos, power);
                    }
                }
            }
        }

        // Оновлює запис емітера лише в одній позиції після place/remove конструкції.
        private void UpdateEmitterAt(Point3 pos)
        {
            UnregisterEmitter(pos);

            byte power = GetEmitterPower(pos);
            if (power > 0)
                RegisterEmitter(pos, power);
        }

        // Реєструє емітер у загальному словнику та block-індексі.
        private void RegisterEmitter(Point3 pos, byte power)
        {
            emitterPowers[pos] = power;
            Point3 blockPos = ToBlockPos(pos);
            if (!emittersByBlock.TryGetValue(blockPos, out var set))
            {
                set = [];
                emittersByBlock[blockPos] = set;
            }

            set.Add(pos);
        }

        // Видаляє емітер із загального словника та block-індексу.
        private void UnregisterEmitter(Point3 pos)
        {
            if (!emitterPowers.Remove(pos))
                return;

            Point3 blockPos = ToBlockPos(pos);
            if (!emittersByBlock.TryGetValue(blockPos, out var set))
                return;

            set.Remove(pos);
            if (set.Count == 0)
                emittersByBlock.Remove(blockPos);
        }

        private static Point3 ToBlockPos(Point3 pos)
        {
            return new Point3(pos.X / BlockSize, pos.Y / BlockSize, pos.Z);
        }

        // Повний перерахунок штучного світла по всій мапі.
        private void RecalculateAllLight()
        {
            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                        RefreshBlockerState(pos, ref pl);
                        pl.LightLevel = 0;
                        pl.HasMultipleLightSources = false;
                    }
                }
            }

            bestBuffer.Clear();
            overlapBuffer.Clear();

            foreach (var emitter in emitterPowers)
                PropagateFromSource(emitter.Key, emitter.Value, null, null, bestBuffer, overlapBuffer);

            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                        pl.LightLevel = bestBuffer.TryGetValue(pos, out var level) ? level : (byte)0;
                        pl.HasMultipleLightSources = overlapBuffer.Contains(pos);
                    }
                }
            }
        }

        // Частковий перерахунок: dirty-позиції -> релевантні емітери -> локальна зона.
        private void RecalculatePartialLight()
        {
            if (dirtyPositions.Count == 0)
                return;

            HashSet<Point3> recastEmitters = CollectEmittersForDirtyPositions();
            HashSet<Point3> area = BuildAffectedArea(dirtyPositions, recastEmitters);
            if (area.Count == 0)
                return;

            ExpandEmittersFromAreaBounds(area, recastEmitters);

            int totalCells = _site.Size.X * _site.Size.Y * _site.Size.Z;
            if (area.Count >= totalCells / 2)
            {
                RecalculateAllLight();
                return;
            }

            foreach (var pos in area)
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                RefreshBlockerState(pos, ref pl);
                pl.LightLevel = 0;
                pl.HasMultipleLightSources = false;
            }

            bestBuffer.Clear();
            overlapBuffer.Clear();

            foreach (var emitterPos in recastEmitters)
            {
                if (emitterPowers.TryGetValue(emitterPos, out var power))
                    PropagateFromSource(emitterPos, power, null, area, bestBuffer, overlapBuffer);
            }

            foreach (var pos in area)
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                pl.LightLevel = bestBuffer.TryGetValue(pos, out var level) ? level : (byte)0;
                pl.HasMultipleLightSources = overlapBuffer.Contains(pos);
            }
        }

        // Розширює список емітерів за bounds локальної зони, щоб уникнути «затінення старих джерел».
        private void ExpandEmittersFromAreaBounds(HashSet<Point3> area, HashSet<Point3> emitters)
        {
            bool initialized = false;
            int minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;

            foreach (var pos in area)
            {
                if (!initialized)
                {
                    minX = maxX = pos.X;
                    minY = maxY = pos.Y;
                    minZ = maxZ = pos.Z;
                    initialized = true;
                    continue;
                }

                if (pos.X < minX) minX = pos.X;
                if (pos.X > maxX) maxX = pos.X;
                if (pos.Y < minY) minY = pos.Y;
                if (pos.Y > maxY) maxY = pos.Y;
                if (pos.Z < minZ) minZ = pos.Z;
                if (pos.Z > maxZ) maxZ = pos.Z;
            }

            if (!initialized)
                return;

            minX = Math.Max(0, minX - MaxEffectiveDistance);
            minY = Math.Max(0, minY - MaxEffectiveDistance);
            minZ = Math.Max(0, minZ - MaxEffectiveDistance);

            maxX = Math.Min(_site.Size.X - 1, maxX + MaxEffectiveDistance);
            maxY = Math.Min(_site.Size.Y - 1, maxY + MaxEffectiveDistance);
            maxZ = Math.Min(_site.Size.Z - 1, maxZ + MaxEffectiveDistance);

            int minBX = minX / BlockSize;
            int maxBX = maxX / BlockSize;
            int minBY = minY / BlockSize;
            int maxBY = maxY / BlockSize;

            for (int bz = minZ; bz <= maxZ; bz++)
            {
                for (int bx = minBX; bx <= maxBX; bx++)
                {
                    for (int by = minBY; by <= maxBY; by++)
                    {
                        Point3 blockPos = new(bx, by, bz);
                        if (!emittersByBlock.TryGetValue(blockPos, out var blockEmitters))
                            continue;

                        foreach (var emitterPos in blockEmitters)
                            emitters.Add(emitterPos);
                    }
                }
            }
        }

        // Збирає емітери, які потенційно можуть впливати на dirty-позиції.
        private HashSet<Point3> CollectEmittersForDirtyPositions()
        {
            HashSet<Point3> result = [];

            foreach (var dirtyPos in dirtyPositions)
            {
                int blockRadius = (MaxEffectiveDistance * 2 + BlockSize - 1) / BlockSize;
                Point3 centerBlock = ToBlockPos(dirtyPos);

                int minBZ = Math.Max(0, dirtyPos.Z - MaxEffectiveDistance * 2);
                int maxBZ = Math.Min(_site.Size.Z - 1, dirtyPos.Z + MaxEffectiveDistance * 2);

                for (int bz = minBZ; bz <= maxBZ; bz++)
                {
                    for (int bx = Math.Max(0, centerBlock.X - blockRadius); bx <= Math.Min((_site.Size.X - 1) / BlockSize, centerBlock.X + blockRadius); bx++)
                    {
                        for (int by = Math.Max(0, centerBlock.Y - blockRadius); by <= Math.Min((_site.Size.Y - 1) / BlockSize, centerBlock.Y + blockRadius); by++)
                        {
                            Point3 blockPos = new(bx, by, bz);
                            if (!emittersByBlock.TryGetValue(blockPos, out var blockEmitters))
                                continue;

                            foreach (var emitterPos in blockEmitters)
                            {
                                byte power = emitterPowers[emitterPos];
                                int emitterRange = GetEmitterRange(power);

                                int distance = Math.Abs(emitterPos.X - dirtyPos.X)
                                    + Math.Abs(emitterPos.Y - dirtyPos.Y)
                                    + Math.Abs(emitterPos.Z - dirtyPos.Z);

                                if (distance <= emitterRange + MaxEffectiveDistance)
                                    result.Add(emitterPos);
                            }
                        }
                    }
                }
            }

            return result;
        }

        // Будує фінальну локальну зону перерахунку (dirty + область впливу релевантних емітерів).
        private HashSet<Point3> BuildAffectedArea(HashSet<Point3> dirty, HashSet<Point3> recastEmitters)
        {
            HashSet<Point3> area = [];

            foreach (var pos in dirty)
                AddInfluenceArea(pos, MaxEffectiveDistance, area);

            foreach (var emitterPos in recastEmitters)
            {
                if (!emitterPowers.TryGetValue(emitterPos, out var power))
                    continue;

                AddInfluenceArea(emitterPos, GetEmitterRange(power), area);
            }

            return area;
        }

        // Оцінка максимальної дальності променя для поточної потужності емітера.
        private static int GetEmitterRange(byte emitterPower)
        {
            byte attenuation = GetAttenuationStep(emitterPower);
            int tail = (int)Math.Ceiling((MaxLightLevel - 1) / (double)attenuation);
            return emitterPower + tail;
        }

        // BFS-поширення світла від одного емітера в межах заданих обмежень.
        private void PropagateFromSource(
            Point3 sourcePos,
            byte emitterPower,
            HashSet<Point3> propagationArea,
            HashSet<Point3> targetArea,
            Dictionary<Point3, byte> best,
            HashSet<Point3> overlap)
        {
            if (emitterPower == 0)
                return;

            if (propagationArea != null && !propagationArea.Contains(sourcePos))
                return;

            byte attenuationStep = GetAttenuationStep(emitterPower);
            int maxRange = GetEmitterRange(emitterPower);
            int maxRangeSq = maxRange * maxRange;

            waveQueue.Clear();
            visitedBuffer.Clear();

            waveQueue.Enqueue(new LightWaveNode(sourcePos));
            visitedBuffer.Add(sourcePos);

            while (waveQueue.Count > 0)
            {
                var node = waveQueue.Dequeue();
                Point3 pos = node.Pos;

                int dx = pos.X - sourcePos.X;
                int dy = pos.Y - sourcePos.Y;
                int dz = pos.Z - sourcePos.Z;
                int distSq = dx * dx + dy * dy + dz * dz;
                if (distSq > maxRangeSq)
                    continue;

                double distance = ComputeDistanceWithRoundness(dx, dy, dz);
                byte level = ComputeLightLevelByDistance(emitterPower, attenuationStep, distance);
                if (level == 0)
                    continue;

                if (targetArea == null || targetArea.Contains(pos))
                    PutBestLight(pos, level, best, overlap);

                if (!IsBlockingTile(pos))
                    LightAdjacentBlockers(pos, level, targetArea, best, overlap);
                else
                    continue;

                foreach (var step in propagationNeighbours)
                {
                    if (!CanMoveOutwardFromSource(sourcePos, pos, step))
                        continue;

                    Point3 nextPos = pos + step;
                    if (!nextPos.InBounds(Point3.Zero, _site.Size))
                        continue;

                    if (propagationArea != null && !propagationArea.Contains(nextPos))
                        continue;

                    if (IsBlockingTile(nextPos))
                        continue;

                    if (!visitedBuffer.Add(nextPos))
                        continue;

                    waveQueue.Enqueue(new LightWaveNode(nextPos));
                }
            }
        }

        // Змішана метрика відстані для керування формою світлової плями.
        private static double ComputeDistanceWithRoundness(int dx, int dy, int dz)
        {
            double euclidean = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            double manhattan = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
            return manhattan + (euclidean - manhattan) * LightRoundnessK;
        }

        // Розрахунок рівня світла за відстанню: повне освітлення в зоні power, далі спад.
        private static byte ComputeLightLevelByDistance(byte emitterPower, byte attenuationStep, double distance)
        {
            if (distance <= emitterPower)
                return MaxLightLevel;

            int extraSteps = (int)Math.Ceiling(distance - emitterPower);
            int level = MaxLightLevel - extraSteps * attenuationStep;
            return level > 0 ? (byte)level : (byte)0;
        }

        // Забороняє «рух назад» до джерела, щоб зменшити протікання світла через геометрію.
        private static bool CanMoveOutwardFromSource(Point3 sourcePos, Point3 currentPos, Point3 step)
        {
            int dx = currentPos.X - sourcePos.X;
            int dy = currentPos.Y - sourcePos.Y;
            int dz = currentPos.Z - sourcePos.Z;

            if (dx > 0 && step.X < 0) return false;
            if (dx < 0 && step.X > 0) return false;
            if (dy > 0 && step.Y < 0) return false;
            if (dy < 0 && step.Y > 0) return false;
            if (dz > 0 && step.Z < 0) return false;
            if (dz < 0 && step.Z > 0) return false;

            return true;
        }

        // Крок згасання після повної зони: обернено пропорційний потужності емітера.
        private static byte GetAttenuationStep(byte emitterPower)
        {
            if (emitterPower == 0)
                return MaxLightLevel;

            return (byte)Math.Max(1, (int)Math.Ceiling(MaxEmitterPower / (double)emitterPower));
        }

        // Підсвічує блокери, які прилягають до освітленого повітрям (вниз + по PLUS-сусідах).
        private void LightAdjacentBlockers(
            Point3 litAirPos,
            byte lightLevel,
            HashSet<Point3> targetArea,
            Dictionary<Point3, byte> best,
            HashSet<Point3> overlap)
        {
            Point3 below = litAirPos + Point3.Down;
            if (below.InBounds(Point3.Zero, _site.Size)
                && (targetArea == null || targetArea.Contains(below))
                && IsBlockingTile(below))
            {
                PutBestLight(below, lightLevel, best, overlap);
            }

            foreach (var n in plusNeighbours)
            {
                Point3 side = litAirPos + n;
                if (!side.InBounds(Point3.Zero, _site.Size))
                    continue;

                if (targetArea != null && !targetArea.Contains(side))
                    continue;

                if (IsBlockingTile(side))
                    PutBestLight(side, lightLevel, best, overlap);
            }
        }

        // Додає сфероподібну область впливу seed-позиції в локальну зону.
        private void AddInfluenceArea(Point3 seed, int radius, HashSet<Point3> affected)
        {
            int radiusSq = radius * radius;

            int minZ = Math.Max(-radius, -seed.Z);
            int maxZ = Math.Min(radius, _site.Size.Z - 1 - seed.Z);

            for (int dz = minZ; dz <= maxZ; dz++)
            {
                int zz = seed.Z + dz;
                int dzSq = dz * dz;
                int remXZSq = radiusSq - dzSq;
                if (remXZSq < 0)
                    continue;

                int dxLimit = (int)Math.Sqrt(remXZSq);
                int minX = Math.Max(-dxLimit, -seed.X);
                int maxX = Math.Min(dxLimit, _site.Size.X - 1 - seed.X);

                for (int dx = minX; dx <= maxX; dx++)
                {
                    int xx = seed.X + dx;
                    int dxSq = dx * dx;
                    int remYSq = remXZSq - dxSq;
                    if (remYSq < 0)
                        continue;

                    int dyLimit = (int)Math.Sqrt(remYSq);
                    int minY = Math.Max(-dyLimit, -seed.Y);
                    int maxY = Math.Min(dyLimit, _site.Size.Y - 1 - seed.Y);

                    for (int dy = minY; dy <= maxY; dy++)
                        affected.Add(new Point3(xx, seed.Y + dy, zz));
                }
            }
        }

        // Агрегує найкращий рівень світла в тайлі та фіксує накладання кількох джерел.
        private static void PutBestLight(Point3 pos, byte candidate, Dictionary<Point3, byte> best, HashSet<Point3> overlap)
        {
            if (!best.TryGetValue(pos, out byte existing))
            {
                best[pos] = candidate;
                return;
            }

            if (candidate > existing)
            {
                if (existing > 0)
                    overlap.Add(pos);
                best[pos] = candidate;
                return;
            }

            if (candidate > 0)
                overlap.Add(pos);
        }

        // Отримує потужність емітера з конструкції в тайлі (з обмеженням MaxEmitterPower).
        private byte GetEmitterPower(Point3 pos)
        {
            Tile tile = _site.Map[pos];
            if (!tile.Exists || !tile.HasConstruction || tile.Construction.Construction == null)
                return 0;

            byte power = tile.Construction.Construction.LightEmitter;
            if (power > MaxEmitterPower)
                power = MaxEmitterPower;

            return power;
        }

        // Поточний стан light-blocker для конкретної позиції.
        private bool IsBlockingTile(Point3 pos)
        {
            Tile tile = _site.Map[pos];
            return tile.Exists
                && tile.HasConstruction
                && tile.Construction.Construction != null
                && tile.Construction.Construction.IsLightBlocker;
        }

        // Синхронізація прапора blocker у PackedLight із фактичним тайлом на мапі.
        private void RefreshBlockerState(Point3 pos, ref PackedLight pl)
        {
            pl.IsLightBlocker = IsBlockingTile(pos);
        }
    }
}
