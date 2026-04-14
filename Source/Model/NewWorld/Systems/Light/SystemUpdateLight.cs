using Arch.Core;
using Origin.Source.Model.Map.Light;
using Origin.Source.Model.NewWorld;
using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Utils;
using System.Collections.Generic;
using Tile = Origin.Source.Model.NewWorld.Tile;

namespace Origin.Source.Model.NewWorld.Systems.Light
{
    internal class SystemUpdateLight : TickSystem
    {
        public SystemUpdateLight(Site site) : base(site)
        {
            site.LightSystem = this;
        }

        private List<HashSet<Point3>> recastPlan = [];
        private bool recastDirty = false;

        public override void Initialize()
        {
            recastPlan.Capacity = _site.Size.Z;
            for (int i = 0; i < _site.Size.Z; i++)
            {
                recastPlan.Add(null);
            }

            recastDirty = true;
            recastPlan[_site.Size.Z - 1] = [];
            for (int x = 0; x < _site.Size.X; x++)
                for (int y = 0; y < _site.Size.Y; y++)
                {
                    var pos = new Point3(x, y, _site.Size.Z - 1);
                    recastPlan[_site.Size.Z - 1].Add(pos);

                    Tile tile = _site.Map[pos];
                    PackedLight pl = new() { SunLighted = 7 };
                    if (tile.Exists && tile.HasConstruction && tile.Construction.Construction != null && tile.Construction.Construction.IsLightBlocker)
                        pl.IsLightBlocker = true;

                    _site.LightControl.SetTile(pos, pl);
                }
            RecursiveReCastSunlight(true);
            ClearRecastPlan();
            _site.LightControl.bufferDirty = true;
        }

        public override void LoadInit()
        {
            base.LoadInit();
            Initialize();
        }

        public void OnConstructionPlaced(Point3 pos)
        {
            if (pos.InBounds(Point3.Zero, _site.Size))
            {
                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                Tile tile = _site.Map[pos];
                pl.IsLightBlocker = tile.Exists && tile.HasConstruction && tile.Construction.Construction != null && tile.Construction.Construction.IsLightBlocker;
            }

            foreach (var n in WorldUtils.FULL_NEIGHBOUR_PATTERN_1L(true))
            {
                var pos2 = pos + Point3.Down + n;
                if (!pos2.InBounds(Point3.Zero, _site.Size))
                    continue;

                if (recastPlan[pos2.Z] == null)
                    recastPlan[pos2.Z] = [];

                recastPlan[pos2.Z].Add(pos2);
            }

            recastDirty = true;
        }

        public void OnConstructionRemoved(Point3 pos)
        {
            foreach (var n in WorldUtils.FULL_NEIGHBOUR_PATTERN_1L(true))
            {
                var pos2 = pos + n;
                if (!pos2.InBounds(Point3.Zero, _site.Size))
                    continue;

                if (recastPlan[pos2.Z] == null)
                    recastPlan[pos2.Z] = [];

                recastPlan[pos2.Z].Add(pos2);
            }

            recastDirty = true;
        }

        public override void Update(in ulong t)
        {
            if (recastDirty)
            {
                RecursiveReCastSunlight();
                ClearRecastPlan();
                recastDirty = false;
                _site.LightControl.bufferDirty = true;
            }
        }

        private void ClearRecastPlan()
        {
            for (int i = 0; i < _site.Size.Z; i++)
                recastPlan[i] = null;
        }

        private void RecursiveReCastSunlight(bool init = false)
        {
            for (int i = _site.Size.Z - 1; i >= 0; i--)
            {
                var hs = recastPlan[i];
                if (hs == null)
                    continue;

                foreach (var pos in hs)
                {
                    if (!pos.InBounds(Point3.Zero, _site.Size))
                        continue;

                    ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                    Tile tile = _site.Map[pos];
                    pl.IsLightBlocker = tile.Exists && tile.HasConstruction && tile.Construction.Construction != null && tile.Construction.Construction.IsLightBlocker;

                    byte newSun;
                    if (pos.Z >= _site.Size.Z - 1)
                    {
                        newSun = 7;
                    }
                    else
                    {
                        ref PackedLight upl = ref _site.LightControl.GetTile(pos + Point3.Up);
                        if (!upl.IsLightBlocker)
                        {
                            newSun = upl.SunLighted;
                        }
                        else
                        {
                            newSun = 0;
                        }

                        if (newSun < 7)
                        {
                            float sl = newSun;
                            foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false))
                            {
                                var npos = pos + Point3.Up + n;
                                if (!npos.InBounds(Point3.Zero, _site.Size))
                                    continue;

                                if (_site.LightControl.TryGetTile(npos, out PackedLight npl) && !npl.IsLightBlocker && npl.SunLighted > 0)
                                {
                                    sl += (npl.SunLighted>1?1:0);
                                    if (sl >= 7)
                                    {
                                        sl = 7;
                                        break;
                                    }
                                }
                            }

                            newSun = (byte)sl;
                        }
                    }

                    pl.SunLighted = newSun;

                    Point3 below = pos + Point3.Down;
                    if (!below.InBounds(Point3.Zero, _site.Size))
                        continue;

                    if (pl.IsLightBlocker)
                    {
                        ref PackedLight bpl = ref _site.LightControl.GetTile(below);
                        bpl.SunLighted = 0;
                        continue;
                    }

                    if (recastPlan[below.Z] == null)
                        recastPlan[below.Z] = [];

                    recastPlan[below.Z].Add(below);
                }
            }
        }
    }
}
