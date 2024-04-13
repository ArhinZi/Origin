using Arch.Core;
using Arch.System;

using Origin.Source.Model.Site;

namespace Origin.Source.ECS
{
    public class TickSystem : BaseSystem<World, ulong>
    {
        public int Interval { get; private set; } = 1;

        protected Site _site;

        public TickSystem(Site site) : base(site.ArchWorld)
        {
            _site = site;
        }

        public virtual void LoadInit()
        {
        }
    }
}