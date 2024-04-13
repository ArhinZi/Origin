using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Fluid;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Vegetation
{
    internal class SystemUpdateFluidsOnConstructionRemoved : TickSystem
    {
        public SystemUpdateFluidsOnConstructionRemoved(Site site) : base(site)
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
                ent.Remove<IsFluidBlocker>();

                foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                {
                    var pos2 = pos + item;
                    if (_site.Map.TryGet(pos2, out Entity nent) && nent != Entity.Null && nent.Has<ConstructionBase>() && !nent.Has<IsFluidBlocker>())
                    {
                        nent.Add<IsFluidBlocker>();
                    }
                }
            });
            commands.Playback(_site.ArchWorld);
        }
    }
}