using Arch.Core;

using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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