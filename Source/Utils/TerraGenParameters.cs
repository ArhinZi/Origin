using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    public struct TerraGenParameters
    {
        public ushort DirtDepth;

        public static TerraGenParameters Default = new TerraGenParameters()
        {
            DirtDepth = 3
        };
    }
}