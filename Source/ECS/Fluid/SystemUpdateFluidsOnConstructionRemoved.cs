using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Map;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Fluid
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
        }
    }
}