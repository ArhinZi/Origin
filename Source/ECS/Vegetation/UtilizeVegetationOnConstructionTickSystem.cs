using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Vegetation
{
    internal class UtilizeVegetationOnConstructionTickSystem : TickSystem
    {
        public UtilizeVegetationOnConstructionTickSystem(Site site) : base(site)
        {
        }

        public override void Init()
        {
        }

        protected override void DoTick()
        {
            var query = new QueryDescription().WithAll<ConstructionRemovedEvent>();
            //var commands = new CommandBuffer(_site.ArchWorld);
            var visited = new HashSet<Point3>();
            _site.ArchWorld.Query(in query, (ref ConstructionRemovedEvent cre) =>
            {
                var pos = cre.Position;
                Entity ent = _site.Map[pos];
                if (ent.Has<BaseVegetationComponent>())
                {
                    if (ent.Has<BaseVegetationComponent>())
                        ent.Remove<BaseVegetationComponent>();
                    if (ent.Has<GrowingVegetationComponent>())
                        ent.Remove<GrowingVegetationComponent>();
                    if (ent.Has<GrownUpVegetationComponent>())
                    {
                        ent.Remove<GrownUpVegetationComponent>();
                        foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                        {
                            var pos2 = pos + item;
                            if ((_site.Map.TryGet(pos2, out Entity nent) && nent.Has<BaseVegetationComponent>()))
                            {
                                ref BaseVegetationComponent nvbc = ref nent.Get<BaseVegetationComponent>();
                                nvbc.VegetationNeighbours++;
                            }
                        }
                    }
                    if (!ent.Has<ECS.UpdateTileRenderSelfRequest>())
                        ent.Add<ECS.UpdateTileRenderSelfRequest>();
                }
            });

            _site.ArchWorld.Query(in query, (ref ConstructionRemovedEvent cre) =>
            {
                Point3 pos = cre.Position;
                if (_site.Map.TryGet(pos + new Utils.Point3(0, 0, -1), out Entity ent) &&
                    ent.Has<BaseConstruction>() && !ent.Has<BaseVegetationComponent>())
                {
                    short count = 0;
                    foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                    {
                        var pos2 = pos + item + new Utils.Point3(0, 0, -1);
                        if ((_site.Map.TryGet(pos2, out Entity e) && e.Has<GrownUpVegetationComponent>()) ||
                                    !pos.InBounds(new Utils.Point3(0, 0, 0), _site.Size, true, false))
                        {
                            count++;
                        }
                    }
                    ent.Add(new BaseVegetationComponent() { VegetationNeighbours = count });
                }
            });

            query = new QueryDescription().WithAll<ConstructionPlacedEvent>();
            _site.ArchWorld.Query(in query, (ref ConstructionPlacedEvent cpe) =>
            {
                var pos = cpe.Position;
                Entity ent = _site.Map[pos + new Utils.Point3(0, 0, -1)];
                if (ent.Has<BaseVegetationComponent>())
                {
                    if (ent.Has<BaseVegetationComponent>())
                        ent.Remove<BaseVegetationComponent>();
                    if (ent.Has<GrowingVegetationComponent>())
                        ent.Remove<GrowingVegetationComponent>();
                    if (ent.Has<GrownUpVegetationComponent>())
                    {
                        ent.Remove<GrownUpVegetationComponent>();
                        foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                        {
                            var pos2 = pos + item;
                            if ((_site.Map.TryGet(item, out Entity nent) && nent.TryGet(out BaseVegetationComponent bvc)))
                            {
                                bvc.VegetationNeighbours--;
                            }
                        }
                    }
                    if (!ent.Has<ECS.UpdateTileRenderSelfRequest>())
                        ent.Add<ECS.UpdateTileRenderSelfRequest>();
                }

                /*pos = cpe.Position;
                if (_site.Map.TryGet(pos, out ent) && ent.Has<BaseConstruction>())
                {
                    ent.Add<BaseVegetationComponent>();
                }*/
            });

            _site.ArchWorld.Query(in query, (ref ConstructionPlacedEvent cre) =>
            {
                Point3 pos = cre.Position;
                if (_site.Map.TryGet(pos, out Entity ent) &&
                    ent.Has<BaseConstruction>() && !ent.Has<BaseVegetationComponent>())
                {
                    short count = 0;
                    foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                    {
                        var pos2 = pos + item;
                        if ((_site.Map.TryGet(pos2, out Entity e) && e.Has<GrownUpVegetationComponent>()) ||
                                    !pos.InBounds(new Utils.Point3(0, 0, 0), _site.Size, true, false))
                        {
                            count++;
                        }
                    }
                    ent.Add(new BaseVegetationComponent() { VegetationNeighbours = count });
                }
            });
        }
    }
}