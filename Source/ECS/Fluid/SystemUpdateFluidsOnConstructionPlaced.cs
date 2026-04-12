using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Map;

namespace Origin.Source.ECS.Fluid
{
    internal class SystemUpdateFluidsOnConstructionPlaced : TickSystem
    {
        public SystemUpdateFluidsOnConstructionPlaced(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        public override void Update(in ulong t)
        {
            base.Update(in t);

            var commands = new CommandBuffer();

            var query = new QueryDescription().WithAll<EventConstructionPlaced>();
            _site.ArchWorld.Query(in query, (ref EventConstructionPlaced cpe) =>
            {
                // Update Vegs on tile below
                var pos = cpe.Position;
                Entity ent = _site.Map[pos];
                if (ent.Has<FluidParticle>())
                {
                    ent.Remove<FluidParticle>();
                }
                if (!ent.Has<IsFluidBlocker>())
                    ent.Add<IsFluidBlocker>();
            });
            commands.Playback(_site.ArchWorld);
        }
    }
}