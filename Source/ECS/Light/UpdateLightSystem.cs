using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using MessagePack;

using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Model.Site.Light;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Origin.Source.ECS.Light
{
    internal class UpdateLightSystem : TickSystem
    {
        public UpdateLightSystem(Site site) : base(site)
        {
        }

        private List<HashSet<Point3>> recastPlan = [];
        //private HashSet<Point3> recastPlanFirst = [];

        //private HashSet<Point3> recastPlanSecond = [];

        //private bool recastPlanReverse = false;
        //private HashSet<Point3> recastPlanCurrent => recastPlanReverse ? recastPlanFirst : recastPlanSecond;
        //private HashSet<Point3> recastPlanNext => !recastPlanReverse ? recastPlanFirst : recastPlanSecond;

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
                    Entity ent = _site.Map[pos];
                    if (!ent.Has<BaseConstruction>())
                    {
                        PackedLight pl = new()
                        {
                            SunLighted = 7
                        };
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

        public override void Update(in ulong t)
        {
            var commands = new CommandBuffer();
            var visited = new HashSet<Point3>();

            // Update Sunlighted info on PlaceConstruction
            var query = new QueryDescription().WithAll<ConstructionPlacedEvent>();
            _site.ArchWorld.Query(in query, (ref ConstructionPlacedEvent cpe) =>
            {
                var pos = cpe.Position;
                Entity ent = _site.Map[pos];

                ref PackedLight pl = ref _site.LightControl.GetTile(pos);
                pl.SunLighted = 0;
                pl.IsLightBlocker = true;

                if (recastPlan[pos.Z] == null)
                    recastPlan[pos.Z] = [];

                foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(true))
                {
                    var pos2 = pos + n;
                    if (pos2.InBounds(Point3.Zero, _site.Size))
                    {
                        recastPlan[pos.Z].Add(pos2);
                    }
                }
                recastDirty = true;
            });

            //Update Sunlighted info on RemoveConstruction
            query = new QueryDescription().WithAll<ConstructionRemovedEvent>();
            _site.ArchWorld.Query(in query, (ref ConstructionRemovedEvent cpe) =>
            {
                var pos = cpe.Position;
                Entity ent = _site.Map[pos];

                _site.LightControl.SetTile(pos, new PackedLight());

                if (recastPlan[pos.Z + 1] == null)
                    recastPlan[pos.Z + 1] = [];

                //recastPlan[pos.Z + 1].Add(pos + Point3.Up);
                foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(true))
                {
                    var pos2 = pos + n + Point3.Up;
                    if (pos2.InBounds(Point3.Zero, _site.Size))
                    {
                        recastPlan[pos.Z + 1].Add(pos2);
                    }
                }
                recastDirty = true;
            });

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
            //recastPlan.Clear();
            for (int i = 0; i < _site.Size.Z; i++)
            {
                recastPlan[i] = null;
            }
        }

        //private void CastLightFrom(Point3 pos, PackedLight pl)
        //{
        //    if (pl.SunLighted == 0) return;

        //    ref PackedLight bpl = ref _site.LightControl.GetTile(pos + Point3.Down);
        //    bpl.SunLighted = Math.Max(pl.SunLighted, bpl.SunLighted);
        //    foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false))
        //    {
        //        var npos = pos + Point3.Down + n;
        //        if (bpl.SunLighted >= 3 && npos.InBounds(Point3.Zero, _site.Size))
        //        {
        //            ref PackedLight bnpl = ref _site.LightControl.GetTile(npos);
        //            bnpl.SunLighted += 1;
        //        }
        //    }
        //}

        private void RecursiveReCastSunlight(bool init = false)
        {
            for (int i = _site.Size.Z - 1; i > 0; i--)
            {
                var hs = recastPlan[i];
                if (hs != null)
                    foreach (var pos in hs)
                    {
                        var npos = pos + Point3.Down;
                        ref PackedLight npl = ref _site.LightControl.GetTile(npos);
                        if (init)
                        {
                            Entity ent = _site.Map[npos];
                            if (ent.Has<BaseConstruction>())
                            {
                                npl.IsLightBlocker = true;
                            }
                        }
                        if (!npl.IsLightBlocker)
                        {
                            // Collect available tiles below and clean them
                            if (recastPlan[i - 1] == null)
                                recastPlan[i - 1] = [];

                            recastPlan[i - 1].Add(npos);

                            // After clean the tile recalc its Sunlight using tiles Above
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
                                            sl += tnpl.SunLighted / 2;
                                        }
                                    }
                                    if (sl >= 7) break;
                                }

                                npl.SunLighted = (byte)sl;
                            }
                        }
                        else
                        {
                        }
                    }
            }
        }
    }
}