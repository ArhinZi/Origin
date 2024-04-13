using Arch.Core;

using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;

namespace Origin.Source.ECS.BaseSystems
{
    public class SystemClearEvents : TickSystem
    {
        public SystemClearEvents(Site site) : base(site)
        {
        }

        public override void AfterUpdate(in ulong t)
        {
            var query = new QueryDescription().WithAny<EventConstructionRemoved, EventConstructionPlaced>();
            _site.ArchWorld.Destroy(query);
        }
    }
}