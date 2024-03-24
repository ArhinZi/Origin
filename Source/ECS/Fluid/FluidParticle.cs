using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Resources.Global;

namespace Origin.Source.ECS.Fluid
{
    public struct FluidParticle
    {
        public FluidType Type;

        /// <summary>
        /// 0-64
        /// </summary>
        public byte Volume;
    }
}