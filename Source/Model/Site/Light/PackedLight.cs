using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Site.Light
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PackedLight
    {
        [FieldOffset(0)] // Start at the first byte
        public uint packedValue;

        // 4 bits for LightLevel - 16
        public byte LightLevel
        {
            get
            {
                uint LightLevelMask = 0b1111;
                return (byte)(packedValue & LightLevelMask);
            }
            set
            {
                uint LightLevelMask = 0b1111;
                value = value > 15 ? (byte)15 : value;
                packedValue = (uint)((packedValue & ~LightLevelMask) | (value & LightLevelMask));
            }
        }

        // 3 bits for SunLighted - 8
        public byte SunLighted
        {
            get
            {
                uint SunLightedMask = 0b111 << 4;
                return (byte)((packedValue & SunLightedMask) >> 4);
            }
            set
            {
                uint SunLightedMask = 0b111 << 4;
                value = value > 7 ? (byte)7 : value;
                packedValue = (uint)((packedValue & ~SunLightedMask) | ((value << 4) & SunLightedMask));
            }
        }

        // 1 bit for IsLightBlocker
        public bool IsLightBlocker
        {
            get
            {
                uint IsLightBlockerMask = 0b1 << 7;
                return (packedValue & IsLightBlockerMask) != 0;
            }
            set
            {
                uint IsLightBlockerMask = 0b1 << 7;
                packedValue = (uint)((packedValue & ~IsLightBlockerMask) | (value ? IsLightBlockerMask : 0x00));
            }
        }

        public override string ToString()
        {
            return $"{SunLighted}";
        }
    }
}