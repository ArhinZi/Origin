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
    public class TickSystem : ISystem<ulong>
    {
        public int Interval { get; private set; } = 1;

        protected Site _site;

        public TickSystem(Site site)
        {
            _site = site;
        }

        public virtual void Initialize()
        { }

        public virtual void LoadInit()
        {
        }

        public virtual void BeforeUpdate(in ulong t)
        {
        }

        public virtual void Update(in ulong t)
        {
        }

        public virtual void AfterUpdate(in ulong t)
        {
        }

        public virtual void Dispose()
        {
        }
    }
}