using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Fluid;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Vegetation
{
    internal class UpdateFluidsOnConstructionRemovedSystem : TickSystem
    {
        public UpdateFluidsOnConstructionRemovedSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        public override void Update(in ulong t)
        {
            var query = new QueryDescription().WithAll<ConstructionRemovedEvent>();
            var commands = new CommandBuffer(_site.ArchWorld);
            var visited = new HashSet<Point3>();

            _site.ArchWorld.Query(in query, (ref ConstructionRemovedEvent cre) =>
            {
                var pos = cre.Position;

                // Update Related to current tile
                Entity ent = _site.Map[pos];
                ent.Remove<IsFluidBlocker>();
            });
            commands.Playback();
        }
    }
}