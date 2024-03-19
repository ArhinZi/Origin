using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Model.Site.Fluid
{
    //[StructLayout(LayoutKind.Explicit)]
    public struct FluidParticle
    {
        public FluidType Type;
        public bool IsFluidBlocker;
        public bool IsStatic;

        public byte Volume;
        public byte Pressure;
        public Direction Direction;

        public override string ToString()
        {
            if (!IsFluidBlocker)
                return $"{Enum.GetName(Type)}: {Volume}";
            else return "BLOCKER";
        }
    }
}