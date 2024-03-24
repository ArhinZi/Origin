using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Model.Site.Fluid
{
    public abstract class AbstractFluid
    {
        public FluidType Type;
        public byte MaxVolume = 100;
        public float Fluidity;
    }
}