using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source
{
    public class FluidType
    {
        public string Name { get; private set; }

        // 1 for water, 100 for lava
        public int Viscosity { get; private set; }
    }
}