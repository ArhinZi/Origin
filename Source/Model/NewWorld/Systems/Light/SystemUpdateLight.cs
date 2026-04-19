using Arch.Core;
using Origin.Source.Model.Map.Light;
using Origin.Source.Model.NewWorld;
using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;
using Tile = Origin.Source.Model.NewWorld.Tile;

namespace Origin.Source.Model.NewWorld.Systems.Light
{
    internal class SystemUpdateLight : TickSystem
    {
        // Максимальний рівень сонячного світла (3 біти у PackedLight).
        private const byte MaxSunLight = 7;

        public SystemUpdateLight(Site site) : base(site)
        {
            site.LightSystem = this;
        }

        private readonly List<HashSet<Point3>> recastPlan = [];
        private bool recastDirty = false;

        public override void Initialize()
        {
            recastPlan.Clear();
            recastPlan.Capacity = _site.Size.Z;
            for (int i = 0; i < _site.Size.Z; i++)
                recastPlan.Add(null);

            RecalculateAllSunlight();
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

            RecursiveReCastSunlight();
            ClearRecastPlan();
            recastDirty = false;
            _site.LightControl.bufferDirty = true;
        }

        private void MarkForRecast(Point3 pos)
        {
            // Позначаємо лише змінений тайл як стартову точку часткового перерахунку.
            if (!pos.InBounds(Point3.Zero, _site.Size))
                return;

            if (recastPlan[pos.Z] == null)
                recastPlan[pos.Z] = [];

            recastPlan[pos.Z].Add(pos);
            recastDirty = true;
        }

        private void ClearRecastPlan()
        {
            for (int i = 0; i < _site.Size.Z; i++)
                recastPlan[i] = null;
        }

        private void RecursiveReCastSunlight(bool init = false)
        {
            if (init)
            {
                RecalculateAllSunlight();
                return;
            }

            HashSet<Point3> affected = BuildAffectedAreaFromDirtyPlan();
            if (affected.Count == 0)
                return;

            RecalculateSunlightForArea(affected);
        }

        private HashSet<Point3> BuildAffectedAreaFromDirtyPlan()
        {
            // Збираємо сумарну область впливу від усіх "брудних" seed-позицій.
            HashSet<Point3> affected = [];

            for (int z = 0; z < recastPlan.Count; z++)
            {
                var layer = recastPlan[z];
                if (layer == null)
                    continue;

                foreach (var seed in layer)
                    AddInfluenceArea(seed, affected);
            }

            return affected;
        }

        private void AddInfluenceArea(Point3 seed, HashSet<Point3> affected)
        {
            // Світло може поширитись максимум на MaxSunLight по XY та на всі рівні вниз.
            for (int z = seed.Z; z >= 0; z--)
            {
                for (int dx = -MaxSunLight; dx <= MaxSunLight; dx++)
                {
                    int maxDy = MaxSunLight - Math.Abs(dx);
                    for (int dy = -maxDy; dy <= maxDy; dy++)
                    {
                        Point3 pos = new(seed.X + dx, seed.Y + dy, z);
                        if (pos.InBounds(Point3.Zero, _site.Size))
                            affected.Add(pos);
                    }
                }
            }
        }

        private void RecalculateAllSunlight()
        {
            // Повний прохід: скидаємо світло і перезапускаємо розповсюдження з верхнього шару.
            Queue<Point3> queue = new();

            for (int z = _site.Size.Z - 1; z >= 0; z--)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                        RefreshBlockerState(pos, ref pl);
                        pl.SunLighted = 0;

                        if (z == _site.Size.Z - 1 && !pl.IsLightBlocker)
                        {
                            pl.SunLighted = MaxSunLight;
                            queue.Enqueue(pos);
                        }
                    }
                }
            }

            PropagateSunlight(queue, null);
        }

        private void RecalculateSunlightForArea(HashSet<Point3> area)
        {
            // Частковий прохід: перераховуємо тільки локальну область впливу.
            Queue<Point3> queue = new();

            foreach (var pos in area)
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                RefreshBlockerState(pos, ref pl);
                pl.SunLighted = 0;
            }

            foreach (var pos in area)
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                byte best = 0;

                if (pos.Z == _site.Size.Z - 1 && !pl.IsLightBlocker)
                    best = MaxSunLight;

                Point3 above = pos + Point3.Up;
                if (above.InBounds(Point3.Zero, _site.Size) && !area.Contains(above))
                {
                    // Якщо верхній тайл поза локальною зоною, беремо його як зовнішнє джерело.
                    ref PackedLight apl = ref _site.LightControl.GetTile(above);
                    if (!apl.IsLightBlocker)
                        best = Math.Max(best, apl.SunLighted);
                }

                foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false))
                {
                    Point3 npos = pos + n;
                    if (!npos.InBounds(Point3.Zero, _site.Size) || area.Contains(npos))
                        continue;

                    ref PackedLight npl = ref _site.LightControl.GetTile(npos);
                    if (npl.IsLightBlocker)
                        continue;

                    byte candidate = pl.IsLightBlocker
                        ? npl.SunLighted
                        : (byte)Math.Max(0, npl.SunLighted - 1);

                    if (candidate > best)
                        best = candidate;
                }

                if (best > 0)
                {
                    pl.SunLighted = best;
                    if (!pl.IsLightBlocker)
                        queue.Enqueue(pos);
                }
            }

            PropagateSunlight(queue, area);
        }

        private void PropagateSunlight(Queue<Point3> queue, HashSet<Point3> area)
        {
            // BFS-розповсюдження всередині всієї мапи або переданої локальної області.
            while (queue.Count > 0)
            {
                Point3 pos = queue.Dequeue();
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);

                if (pl.IsLightBlocker || pl.SunLighted == 0)
                    continue;

                Point3 below = pos + Point3.Down;
                if (below.InBounds(Point3.Zero, _site.Size) && (area == null || area.Contains(below)))
                {
                    // Вниз світло йде без втрати.
                    ref PackedLight bpl = ref _site.LightControl.GetTile(below);
                    byte candidate = pl.SunLighted;
                    if (candidate > bpl.SunLighted)
                    {
                        bpl.SunLighted = candidate;
                        if (!bpl.IsLightBlocker)
                            queue.Enqueue(below);
                    }
                }

                foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false))
                {
                    Point3 npos = pos + n;
                    if (!npos.InBounds(Point3.Zero, _site.Size) || (area != null && !area.Contains(npos)))
                        continue;

                    ref PackedLight npl = ref _site.LightControl.GetTile(npos);
                    // Вбік: у блокер без втрати, у неблокер з втратою -1.
                    byte candidate = npl.IsLightBlocker
                        ? pl.SunLighted
                        : (byte)Math.Max(0, pl.SunLighted - 1);

                    if (candidate > npl.SunLighted)
                    {
                        npl.SunLighted = candidate;
                        if (!npl.IsLightBlocker && candidate > 0)
                            queue.Enqueue(npos);
                    }
                }
            }
        }

        private void RefreshBlockerState(Point3 pos, ref PackedLight pl)
        {
            // Стан блокера завжди синхронізується з поточним станом тайла на мапі.
            Tile tile = _site.Map[pos];
            pl.IsLightBlocker = tile.Exists
                && tile.HasConstruction
                && tile.Construction.Construction != null
                && tile.Construction.Construction.IsLightBlocker;
        }
    }
}


