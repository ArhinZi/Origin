using Origin.Source.Model.Map.Light;
using Origin.Source.Model.NewWorld;
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
        private const int GeometricTailSteps = 3;
        private const int MaxEffectiveDistance = MaxEmitterPower + GeometricTailSteps - 1;

        private readonly List<HashSet<Point3>> recastPlan = [];
        private bool recastDirty = false;
        private readonly Point3[] neighbours = WorldUtils.FULL_NEIGHBOUR_PATTERN_3L(false);

        public SystemUpdateArtificialLight(Site site) : base(site)
        {
            site.ArtificialLightSystem = this;
        }

        public override void Initialize()
        {
            recastPlan.Clear();
            recastPlan.Capacity = _site.Size.Z;
            for (int i = 0; i < _site.Size.Z; i++)
                recastPlan.Add(null);

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

                if (recastPlan[target.Z] == null)
                    recastPlan[target.Z] = [];

                recastPlan[target.Z].Add(target);
                recastDirty = true;
            }
        }

        private void ClearRecastPlan()
        {
            for (int i = 0; i < _site.Size.Z; i++)
                recastPlan[i] = null;
        }

        private void RecalculateAllLight()
        {
            var sources = new List<(Point3 Pos, byte Power)>();
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

                        byte emitterPower = GetEmitterPower(pos);
                        if (emitterPower > 0)
                            sources.Add((pos, emitterPower));
                    }
                }
            }

            var best = new Dictionary<Point3, byte>();
            var overlap = new HashSet<Point3>();
            foreach (var source in sources)
                PropagateFromSource(source.Pos, source.Power, null, null, best, overlap);

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
            HashSet<Point3> area = BuildAffectedAreaFromDirtyPlan();
            if (area.Count == 0)
                return;

            HashSet<Point3> discoverArea = BuildDiscoveryAreaFromDirtyPlan();

            foreach (var pos in area)
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                RefreshBlockerState(pos, ref pl);
                pl.LightLevel = 0;
                pl.HasMultipleLightSources = false;
            }

            var sources = CollectSources(discoverArea);
            var best = new Dictionary<Point3, byte>();
            var overlap = new HashSet<Point3>();

            foreach (var source in sources)
                PropagateFromSource(source.Pos, source.Power, discoverArea, area, best, overlap);

            foreach (var pos in area)
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                pl.LightLevel = best.TryGetValue(pos, out var level) ? level : (byte)0;
                pl.HasMultipleLightSources = overlap.Contains(pos);
            }
        }

        private HashSet<Point3> BuildAffectedAreaFromDirtyPlan()
        {
            HashSet<Point3> affected = [];

            for (int z = 0; z < recastPlan.Count; z++)
            {
                var layer = recastPlan[z];
                if (layer == null)
                    continue;

                foreach (var seed in layer)
                    AddInfluenceArea(seed, MaxEffectiveDistance, affected);
            }

            return affected;
        }

        private HashSet<Point3> BuildDiscoveryAreaFromDirtyPlan()
        {
            HashSet<Point3> discover = [];

            int discoveryRadius = MaxEffectiveDistance * 2;
            for (int z = 0; z < recastPlan.Count; z++)
            {
                var layer = recastPlan[z];
                if (layer == null)
                    continue;

                foreach (var seed in layer)
                    AddInfluenceArea(seed, discoveryRadius, discover);
            }

            return discover;
        }

        private static byte ComputeLightAtDistance(byte emitterPower, double distance)
        {
            int dist = (int)Math.Ceiling(distance);
            if (dist <= emitterPower)
                return MaxLightLevel;

            int extraDistance = dist - emitterPower;
            if (extraDistance >= GeometricTailSteps)
                return 0;

            return (byte)(MaxLightLevel >> extraDistance);
        }

        private void PropagateFromSource(
            Point3 sourcePos,
            byte emitterPower,
            HashSet<Point3> discoveryArea,
            HashSet<Point3> targetArea,
            Dictionary<Point3, byte> best,
            HashSet<Point3> overlap)
        {
            int maxDistance = emitterPower + GeometricTailSteps - 1;
            int maxDistanceSq = maxDistance * maxDistance;

            var visited = new HashSet<Point3> { sourcePos };
            Queue<Point3> queue = new();
            queue.Enqueue(sourcePos);

            while (queue.Count > 0)
            {
                Point3 nodePos = queue.Dequeue();

                int dxs = nodePos.X - sourcePos.X;
                int dys = nodePos.Y - sourcePos.Y;
                int dzs = nodePos.Z - sourcePos.Z;
                int distSq = dxs * dxs + dys * dys + dzs * dzs;
                if (distSq > maxDistanceSq)
                    continue;

                byte candidate = ComputeLightAtDistance(emitterPower, Math.Sqrt(distSq));
                if (candidate == 0)
                    continue;

                if (targetArea == null || targetArea.Contains(nodePos))
                    PutBestLight(nodePos, candidate, best, overlap);

                if (nodePos != sourcePos && IsBlockingTile(nodePos))
                    continue;

                foreach (var n in neighbours)
                {
                    Point3 npos = nodePos + n;
                    if (!npos.InBounds(Point3.Zero, _site.Size))
                        continue;

                    // Вгору не заходимо у лайт-блокер (не освітлюємо blocker зверху).
                    if (n.Z > 0 && IsBlockingTile(npos))
                        continue;

                    if (discoveryArea != null && !discoveryArea.Contains(npos))
                        continue;

                    if (!visited.Add(npos))
                        continue;

                    queue.Enqueue(npos);
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

        private HashSet<Point3> ExpandArea(HashSet<Point3> area, int radius)
        {
            HashSet<Point3> expanded = [];
            foreach (var pos in area)
                AddInfluenceArea(pos, radius, expanded);

            return expanded;
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
                    {
                        affected.Add(new Point3(xx, seed.Y + dy, zz));
                    }
                }
            }
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
