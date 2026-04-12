using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Render;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Map;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Vegetation
{
    internal class UpdateVegsOnConstructionPlacedSystem : TickSystem
    {
        public UpdateVegsOnConstructionPlacedSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        public override void Update(in ulong t)
        {
            base.Update(in t);

            var commands = new CommandBuffer();
            var visited = new HashSet<Point3>();

            var query = new QueryDescription().WithAll<EventConstructionPlaced>();

            _site.ArchWorld.Query(in query, (ref EventConstructionPlaced cpe) =>
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
                        VegUtilities.UpdateNeighboursOf(_site, pos, -1);
                    }
                    if (!ent.Has<SelfRequestUpdateTileRender>())
                        commands.Add<SelfRequestUpdateTileRender>(ent);
                }
            });
            commands.Playback(_site.ArchWorld);

            _site.ArchWorld.Query(in query, (ref EventConstructionPlaced cre) =>
            {
                Point3 pos = cre.Position;
                if (_site.Map.TryGet(pos, out Entity ent) &&
                    ent.Has<ConstructionBase>() && !ent.Has<BaseVegetation>())
                {
                    short count = VegUtilities.GetNeighboursFor(_site, pos);
                    commands.Add(ent, new BaseVegetation() { VegetationNeighbours = count });
                }
            });
            commands.Playback(_site.ArchWorld);
        }
    }
}