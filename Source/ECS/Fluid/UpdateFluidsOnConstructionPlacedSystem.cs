using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Fluid;
using Origin.Source.Model.Site;

namespace Origin.Source.ECS.Vegetation
{
    internal class UpdateFluidsOnConstructionPlacedSystem : TickSystem
    {
        public UpdateFluidsOnConstructionPlacedSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        public override void Update(in ulong t)
        {
            base.Update(in t);

            var commands = new CommandBuffer();

            var query = new QueryDescription().WithAll<ConstructionPlacedEvent>();
            _site.ArchWorld.Query(in query, (ref ConstructionPlacedEvent cpe) =>
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