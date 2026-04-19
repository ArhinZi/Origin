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
        private const byte MaxLightLevel = 7;
        private const int MaxEmitterPower = 7;
        private const int TailSteps = MaxLightLevel - 1;
        private const int MaxEffectiveDistance = MaxEmitterPower + TailSteps;
        private const int BlockSize = BlockBase.BLOCK_SIZE;

        private readonly HashSet<Point3> dirtyPositions = [];
        private readonly Dictionary<Point3, byte> emitterPowers = [];
        private readonly Dictionary<Point3, HashSet<Point3>> emittersByBlock = [];

        private readonly Point3[] starNeighbours = WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(false);
        private readonly Point3[] plusNeighbours = WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false);
        private bool recastDirty = false;

        private readonly record struct LightWaveNode(Point3 Pos, byte Level, byte FlatStepsLeft);

        public SystemUpdateArtificialLight(Site site) : base(site)
        {
            site.ArtificialLightSystem = this;
        }

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

        private void MarkForRecast(Point3 pos)
        {
            foreach (var n in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(true))
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

        private void UpdateEmitterAt(Point3 pos)
        {
            UnregisterEmitter(pos);

            byte power = GetEmitterPower(pos);
            if (power > 0)
                RegisterEmitter(pos, power);
        }

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

            var best = new Dictionary<Point3, byte>();
            var overlap = new HashSet<Point3>();

            foreach (var emitter in emitterPowers)
                PropagateFromSource(emitter.Key, emitter.Value, null, null, best, overlap);

            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                        pl.LightLevel = best.TryGetValue(pos, out var level) ? level : (byte)0;
                        pl.HasMultipleLightSources = overlap.Contains(pos);
                    }
                }
            }
        }

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

            var best = new Dictionary<Point3, byte>();
            var overlap = new HashSet<Point3>();

            foreach (var emitterPos in recastEmitters)
            {
                if (emitterPowers.TryGetValue(emitterPos, out var power))
                    PropagateFromSource(emitterPos, power, null, area, best, overlap);
            }

            foreach (var pos in area)
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                pl.LightLevel = best.TryGetValue(pos, out var level) ? level : (byte)0;
                pl.HasMultipleLightSources = overlap.Contains(pos);
            }
        }

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

        private static int GetEmitterRange(byte emitterPower)
        {
            byte attenuation = GetAttenuationStep(emitterPower);
            int tail = (int)Math.Ceiling((MaxLightLevel - 1) / (double)attenuation);
            return emitterPower + tail;
        }

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

            byte flatSteps = emitterPower > MaxEmitterPower ? (byte)MaxEmitterPower : emitterPower;
            byte attenuationStep = GetAttenuationStep(emitterPower);

            Queue<LightWaveNode> queue = new();
            Dictionary<Point3, ushort> visited = new();

            queue.Enqueue(new LightWaveNode(sourcePos, MaxLightLevel, flatSteps));
            visited[sourcePos] = PackState(MaxLightLevel, flatSteps);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                Point3 pos = node.Pos;

                if (targetArea == null || targetArea.Contains(pos))
                    PutBestLight(pos, node.Level, best, overlap);

                if (node.Level == 0)
                    continue;

                if (!IsBlockingTile(pos))
                    LightAdjacentBlockers(pos, node.Level, targetArea, best, overlap);
                else
                    continue;

                byte nextLevel = node.FlatStepsLeft > 0
                    ? node.Level
                    : (byte)Math.Max(0, node.Level - attenuationStep);

                if (nextLevel == 0)
                    continue;

                byte nextFlatSteps = node.FlatStepsLeft > 0
                    ? (byte)(node.FlatStepsLeft - 1)
                    : (byte)0;

                foreach (var step in starNeighbours)
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

                    ushort packed = PackState(nextLevel, nextFlatSteps);
                    if (visited.TryGetValue(nextPos, out ushort existing) && existing >= packed)
                        continue;

                    visited[nextPos] = packed;
                    queue.Enqueue(new LightWaveNode(nextPos, nextLevel, nextFlatSteps));
                }
            }
        }

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

        private static byte GetAttenuationStep(byte emitterPower)
        {
            if (emitterPower == 0)
                return MaxLightLevel;

            return (byte)Math.Max(1, (int)Math.Ceiling(MaxEmitterPower / (double)emitterPower));
        }

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

        private static ushort PackState(byte level, byte flatStepsLeft)
        {
            return (ushort)((level << 8) | flatStepsLeft);
        }

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

        private List<(Point3 Pos, byte Power)> CollectSources(HashSet<Point3> area)
        {
            List<(Point3 Pos, byte Power)> sources = [];
            foreach (var pos in area)
            {
                byte emitterPower = GetEmitterPower(pos);
                if (emitterPower > 0)
                    sources.Add((pos, emitterPower));
            }

            return sources;
        }

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

        private bool IsBlockingTile(Point3 pos)
        {
            Tile tile = _site.Map[pos];
            return tile.Exists
                && tile.HasConstruction
                && tile.Construction.Construction != null
                && tile.Construction.Construction.IsLightBlocker;
        }

        private void RefreshBlockerState(Point3 pos, ref PackedLight pl)
        {
            pl.IsLightBlocker = IsBlockingTile(pos);
        }
    }
}
