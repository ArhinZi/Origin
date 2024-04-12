using Arch.Core;
using Arch.System;

using Microsoft.Xna.Framework;

using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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