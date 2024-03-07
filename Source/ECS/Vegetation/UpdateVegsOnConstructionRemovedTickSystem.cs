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
    internal class UpdateVegsOnConstructionRemovedTickSystem : TickSystem
    {
        public UpdateVegsOnConstructionRemovedTickSystem(Site site) : base(site)
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

            _site.ArchWorld.Query(in query, (ref ConstructionRemovedEvent cre) =>
            {
                var pos = cre.Position;

                // Update Related to current tile
                Entity ent = _site.Map[pos];
                if (ent.Has<BaseVegetation>())
                {
                    if (ent.Has<BaseVegetation>())
                        commands.Remove<BaseVegetation>(ent);
                    if (ent.Has<GrowingVegetation>())
                        commands.Remove<GrowingVegetation>(ent);
                    if (ent.Has<GrownUpVegetation>())
                    {
                        commands.Remove<GrownUpVegetation>(ent);

                        // Update nearest tiles
                        foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                        {
                            var pos2 = pos + item;
                            if ((_site.Map.TryGet(pos2, out Entity nent) && nent.Has<BaseVegetation>()))
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

            _site.ArchWorld.Query(in query, (ref ConstructionRemovedEvent cre) =>
            {
                var pos = cre.Position;

                // Update Veg info for below tile
                if (_site.Map.TryGet(pos + new Utils.Point3(0, 0, -1), out Entity ment) &&
                    ment.Has<BaseConstruction>() && !ment.Has<BaseVegetation>())
                {
                    // Increase Veg power from nearest tiles
                    short count = 0;
                    foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                    {
                        var pos2 = pos + item + new Utils.Point3(0, 0, -1);
                        if ((_site.Map.TryGet(pos2, out Entity mnent) && mnent.Has<GrownUpVegetation>()) ||
                                    !pos.InBounds(new Utils.Point3(0, 0, 0), _site.Size, true, false))
                        {
                            count++;
                        }
                    }
                    commands.Add(ment, new BaseVegetation() { VegetationNeighbours = count });
                }
            });
            commands.Playback();
        }
    }
}