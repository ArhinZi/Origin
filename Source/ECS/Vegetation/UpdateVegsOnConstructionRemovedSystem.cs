using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Render;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Vegetation
{
    internal class UpdateVegsOnConstructionRemovedSystem : TickSystem
    {
        public UpdateVegsOnConstructionRemovedSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        public override void Update(in ulong t)
        {
            var query = new QueryDescription().WithAll<EventConstructionRemoved>();
            var commands = new CommandBuffer();
            var visited = new HashSet<Point3>();

            _site.ArchWorld.Query(in query, (ref EventConstructionRemoved cre) =>
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
                        VegUtilities.UpdateNeighboursOf(_site, pos, -1);
                    }
                    if (!ent.Has<SelfRequestUpdateTileRender>())
                        commands.Add<SelfRequestUpdateTileRender>(ent);
                }
            });
            commands.Playback(_site.ArchWorld);

            _site.ArchWorld.Query(in query, (ref EventConstructionRemoved cre) =>
            {
                var pos = cre.Position;

                // Update Veg info for below tile
                if (_site.Map.TryGet(pos + new Utils.Point3(0, 0, -1), out Entity ment) &&
                    ment.Has<ConstructionBase>() && !ment.Has<BaseVegetation>())
                {
                    // Increase Veg power from nearest tiles
                    short count = VegUtilities.GetNeighboursFor(_site, pos);
                    commands.Add(ment, new BaseVegetation() { VegetationNeighbours = count });
                }
            });
            commands.Playback(_site.ArchWorld);
        }
    }
}