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
                    if (!tile.HasConstruction)
                    {
                        PackedLight pl = new() { SunLighted = 7 };
                        _site.LightControl.SetTile(pos, pl);
                    }
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
            ref PackedLight pl = ref _site.LightControl.GetTile(pos);
            pl.SunLighted = 0;
            pl.IsLightBlocker = true;

            if (recastPlan[pos.Z] == null)
                recastPlan[pos.Z] = [];
            if (pos.Z > 0 && recastPlan[pos.Z - 1] == null)
                recastPlan[pos.Z - 1] = [];

            foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(true))
            {
                var pos2 = pos + n;
                if (pos2.InBounds(Point3.Zero, _site.Size))
                    recastPlan[pos2.Z].Add(pos2);

                pos2 = pos + n + Point3.Down;
                if (pos2.InBounds(Point3.Zero, _site.Size))
                    recastPlan[pos2.Z].Add(pos2);
            }
            recastDirty = true;
        }

        public void OnConstructionRemoved(Point3 pos)
        {
            _site.LightControl.SetTile(pos, new PackedLight());

            if (pos.Z + 1 < _site.Size.Z && recastPlan[pos.Z + 1] == null)
                recastPlan[pos.Z + 1] = [];

            foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(true))
            {
                var pos2 = pos + n + Point3.Up;
                if (pos2.InBounds(Point3.Zero, _site.Size))
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
            for (int i = _site.Size.Z - 1; i > 0; i--)
            {
                var hs = recastPlan[i];
                if (hs == null)
                    continue;

                foreach (var pos in hs)
                {
                    var npos = pos + Point3.Down;
                    ref PackedLight npl = ref _site.LightControl.GetTile(npos);
                    Tile tile = _site.Map[npos];
                    if (tile.Exists && tile.HasConstruction && !tile.IsRamp)
                    {
                        npl.IsLightBlocker = true;
                    }
                    if (!npl.IsLightBlocker)
                    {
                        if (recastPlan[i - 1] == null)
                            recastPlan[i - 1] = [];

                        recastPlan[i - 1].Add(npos);
                        ref PackedLight unpl = ref _site.LightControl.GetTile(npos + Point3.Up);
                        npl.SunLighted = unpl.SunLighted;
                        if (npl.SunLighted < 7)
                        {
                            float sl = npl.SunLighted;
                            foreach (var tn in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false))
                            {
                                var tnpos = npos + tn + Point3.Up;
                                if (tnpos.InBounds(Point3.Zero, _site.Size) && _site.LightControl.TryGetTile(tnpos, out PackedLight tnpl))
                                {
                                    var nnpos = tnpos + Point3.Down;
                                    if (nnpos.InBounds(Point3.Zero, _site.Size) &&
                                        _site.LightControl.TryGetTile(nnpos, out PackedLight nnpl) &&
                                        !nnpl.IsLightBlocker)
                                    {
                                        sl += tnpl.SunLighted / 2f;
                                    }
                                }
                                if (sl >= 7) break;
                            }

                            npl.SunLighted = (byte)sl;
                        }
                    }
                }
            }
        }
    }
}
