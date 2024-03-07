using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Vegetation
{
    internal class UpdateVegsOnConstructionPlacedTickSystem : TickSystem
    {
        public UpdateVegsOnConstructionPlacedTickSystem(Site site) : base(site)
        {
        }

        public override void Init()
        {
        }

        protected override void DoTick()
        {
            var query = new QueryDescription().WithAll<ConstructionRemovedEvent>();
            var commands = new CommandBuffer(_site.ArchWorld);
            var visited = new HashSet<Point3>();

            query = new QueryDescription().WithAll<ConstructionPlacedEvent>();
            _site.ArchWorld.Query(in query, (ref ConstructionPlacedEvent cpe) =>
            {
                // Update Vegs on tile below
                var pos = cpe.Position;
                Entity ent = _site.Map[pos + new Utils.Point3(0, 0, -1)];
                if (ent.Has<BaseVegetation>())
                {
                    if (ent.Has<BaseVegetation>())
                        commands.Remove<BaseVegetation>(ent);
                    if (ent.Has<GrowingVegetation>())
                        commands.Remove<GrowingVegetation>(ent);
                    if (ent.Has<GrownUpVegetation>())
                    {
                        commands.Remove<GrownUpVegetation>(ent);
                        foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                        {
                            var pos2 = pos + item;
                            if ((_site.Map.TryGet(item, out Entity nent) && nent.Has<BaseVegetation>()))
                            {
                                ref BaseVegetation nvbc = ref nent.Get<BaseVegetation>();
                                nvbc.VegetationNeighbours--;
                            }
                        }
                    }
                    if (!ent.Has<UpdateTileRenderSelfRequest>())
                        commands.Add<UpdateTileRenderSelfRequest>(ent);
                }
            });
            commands.Playback();

            _site.ArchWorld.Query(in query, (ref ConstructionPlacedEvent cre) =>
            {
                Point3 pos = cre.Position;
                if (_site.Map.TryGet(pos, out Entity ent) &&
                    ent.Has<BaseConstruction>() && !ent.Has<BaseVegetation>())
                {
                    short count = 0;
                    foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                    {
                        var pos2 = pos + item;
                        if ((_site.Map.TryGet(pos2, out Entity e) && e.Has<GrownUpVegetation>()) ||
                                    !pos.InBounds(new Utils.Point3(0, 0, 0), _site.Size, true, false))
                        {
                            count++;
                        }
                    }
                    commands.Add(ent, new BaseVegetation() { VegetationNeighbours = count });
                }
            });
            commands.Playback();
        }
    }
}