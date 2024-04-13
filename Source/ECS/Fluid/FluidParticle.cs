using System.Diagnostics;

using static Origin.Source.Resources.Global;

namespace Origin.Source.ECS.Fluid
{
    /// <summary>
    /// Double buffered struct
    /// </summary>
    public struct FluidParticle
    {
        /// <summary>
        /// 64
        /// </summary>
        public static byte MaxVolume = 64;

        public FluidType Type;

        private byte volume;

        public byte Volume
        {
            get => volume;
            set
            {
                Debug.Assert(value <= MaxVolume);
                volume = value;
            }
        }

        public byte Direction;

        public FluidParticle()
        {
        }

        public FluidParticle(FluidType Type, byte Volume)
        {
            this.Type = Type;
            this.Volume = Volume;
        }
    }
}