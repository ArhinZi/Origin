using Arch.Core;

using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;

namespace Origin.Source.ECS.BaseSystems
{
    public class ClearEventsSystem : TickSystem
    {
        public ClearEventsSystem(Site site) : base(site)
        {
        }

        public override void AfterUpdate(in ulong t)
        {
            var query = new QueryDescription().WithAny<ConstructionRemovedEvent, ConstructionPlacedEvent>();
            _site.ArchWorld.Destroy(query);
        }
    }
}