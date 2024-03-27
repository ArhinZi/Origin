using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private bool Revers = false;

        private FluidType first_type;
        private byte first_volume;

        private FluidType second_type;
        private byte second_volume;

        public byte Direction;

        public FluidType Type
        {
            get => Revers ? first_type : second_type;
            set
            {
                if (Revers) first_type = value;
                else second_type = value;
            }
        }

        public byte Volume
        {
            get => Revers ? first_volume : second_volume;
            set
            {
                if (Revers) first_volume = value;
                else second_volume = value;
            }
        }

        public FluidType NextType
        {
            get => !Revers ? first_type : second_type;
            set
            {
                if (!Revers) first_type = value;
                else second_type = value;
            }
        }

        public byte NextVolume
        {
            get => !Revers ? first_volume : second_volume;
            set
            {
                if (!Revers) first_volume = value;
                else second_volume = value;
            }
        }

        public FluidParticle()
        {
        }

        public FluidParticle(FluidType Type, byte Volume)
        {
            this.Type = Type;
            this.Volume = Volume;
        }

        public void Sync()
        {
            Type = NextType;
            Volume = NextVolume;
        }

        public void SwitchBuffer()
        {
            Revers = !Revers;
        }
    }
}