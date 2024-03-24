using Origin.Source.Render;

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

        /// <summary>
        /// 0-12
        /// </summary>
        public byte Volume;

        /// <summary>
        /// 0-255
        /// </summary>
        public byte Pressure;

        public Direction Direction;

        public SpriteLocator SpriteLocator;

        public override string ToString()
        {
            if (!IsFluidBlocker)
                return $"{Enum.GetName(Type)}: {Volume}";
            else return "BLOCKER";
        }
    }
}