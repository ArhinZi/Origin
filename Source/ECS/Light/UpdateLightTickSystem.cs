using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Model.Site.Light;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Origin.Source.ECS.Light
{
    internal class UpdateLightTickSystem : TickSystem
    {
        public UpdateLightTickSystem(Site site) : base(site)
        {
        }

        private List<HashSet<Point3>> recastPlan = new List<HashSet<Point3>>();
        private bool recastDirty = false;

        public override void Init()
        {
            recastPlan.Capacity = _site.Size.Z;
            for (int i = 0; i < _site.Size.Z; i++)
            {
                recastPlan.Add(new());
            }

            Point3 pos;
            for (int x = 0; x < _site.Size.X; x++)
                for (int y = 0; y < _site.Size.Y; y++)
                {
                    pos = new Point3(x, y, _site.Size.Z - 1);
                    Entity ent = _site.Map[pos];
                    if (!ent.Has<BaseConstruction>())
                    {
                        PackedLight pl = new PackedLight()
                        {
                            SunLighted = 7
                        };
                        _site.LightControl.SetTile(pos, pl);
                        CastLightFrom(pos, pl);
                    }
                }
            int[,] done = new int[_site.Size.X, _site.Size.Y];
            for (int z = _site.Size.Z - 2; z >= 0; z--)
                for (int x = 0; x < _site.Size.X; x++)
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        if (done[x, y] > 0) continue;

                        pos = new Point3(x, y, z);
                        Entity ent = _site.Map[pos];
                        if (ent != Entity.Null)
                        {
                            if (!ent.Has<BaseConstruction>())
                            {
                                if (_site.LightControl.TryGetTile(pos, out PackedLight pl))
                                {
                                    CastLightFrom(pos, pl);
                                }
                            }
                            else
                            {
                                /*byte sl = 0;
                                foreach (var n in WorldUtils.FULL_NEIGHBOUR_PATTERN_1L(false))
                                {
                                    var npos = pos + n;
                                    if (_site.LightControl.TryGetTile(npos, out PackedLight pl))
                                    {
                                        sl = pl.SunLighted;
                                    }
                                    if (sl >= 7)
                                        break;
                                }*/
                                _site.LightControl.SetTile(pos, new PackedLight()
                                {
                                    IsLightBlocker = true,
                                });
                                done[x, y] = z;
                            }
                        }
                    }
            _site.LightControl.bufferDirty = true;
        }

        protected override void DoTick()
        {
            var commands = new CommandBuffer(_site.ArchWorld);
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

                recastPlan[pos.Z].Add(pos);
                foreach (var n in WorldUtils.FULL_NEIGHBOUR_PATTERN_1L(false))
                {
                    recastPlan[pos.Z].Add(pos + n);
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
                //pl.SunLighted = 0;
                //pl.IsLightBlocker = false;
                //pl.IsDirectSunLight = false;

                recastPlan[pos.Z + 1].Add(pos + Point3.Up);
                foreach (var n in WorldUtils.FULL_NEIGHBOUR_PATTERN_1L(false))
                {
                    recastPlan[pos.Z + 1].Add(pos + n + Point3.Up);
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
            for (int i = 0; i < _site.Size.Z; i++)
            {
                recastPlan[i].Clear();
            }
        }

        private void CastLightFrom(Point3 pos, PackedLight pl)
        {
            if (pl.SunLighted == 0) return;

            ref PackedLight bpl = ref _site.LightControl.GetTile(pos + Point3.Down);
            bpl.SunLighted = Math.Max(pl.SunLighted, bpl.SunLighted); foreach (var n in WorldUtils.FULL_NEIGHBOUR_PATTERN_1L(false))
            {
                var npos = pos + Point3.Down + n;
                if (bpl.SunLighted >= 3 && npos.InBounds(Point3.Zero, _site.Size))
                {
                    ref PackedLight bnpl = ref _site.LightControl.GetTile(npos);
                    bnpl.SunLighted += 1;
                }
            }
        }

        private void RecursiveReCastSunlight()
        {
            for (int i = 127; i > 0; i--)
            {
                var hs = recastPlan[i];
                foreach (var pos in hs)
                {
                    var npos = pos + Point3.Down;
                    ref PackedLight npl = ref _site.LightControl.GetTile(npos);
                    if (!npl.IsLightBlocker)
                    {
                        // Collect available tiles below and clean them
                        recastPlan[i - 1].Add(npos);
                        //npl.SunLighted = 0;

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