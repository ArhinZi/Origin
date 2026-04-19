using System.Runtime.InteropServices;

namespace Origin.Source.Model.Map.Light
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PackedLight
    {
        [FieldOffset(0)] // Start at the first byte
        public uint packedValue;

        // 3 bits for LightLevel - 8 states
        public byte LightLevel
        {
            get
            {
                uint lightLevelMask = 0b111;
                return (byte)(packedValue & lightLevelMask);
            }
            set
            {
                uint lightLevelMask = 0b111;
                value = value > 7 ? (byte)7 : value;
                packedValue = (uint)((packedValue & ~lightLevelMask) | (value & lightLevelMask));
            }
        }

        // 3 bits for SunLighted - 8 states
        public byte SunLighted
        {
            get
            {
                uint sunLightedMask = 0b111 << 3;
                return (byte)((packedValue & sunLightedMask) >> 3);
            }
            set
            {
                uint sunLightedMask = 0b111 << 3;
                value = value > 7 ? (byte)7 : value;
                packedValue = (uint)((packedValue & ~sunLightedMask) | ((value << 3) & sunLightedMask));
            }
        }

        // 1 bit for IsLightBlocker
        public bool IsLightBlocker
        {
            get
            {
                uint isLightBlockerMask = 0b1 << 6;
                return (packedValue & isLightBlockerMask) != 0;
            }
            set
            {
                uint isLightBlockerMask = 0b1 << 6;
                packedValue = (uint)((packedValue & ~isLightBlockerMask) | (value ? isLightBlockerMask : 0x00));
            }
        }

        // 1 reserved bit
        public bool HasMultipleLightSources
        {
            get
            {
                uint reservedMask = 0b1 << 7;
                return (packedValue & reservedMask) != 0;
            }
            set
            {
                uint reservedMask = 0b1 << 7;
                packedValue = (uint)((packedValue & ~reservedMask) | (value ? reservedMask : 0x00));
            }
        }

        public override string ToString()
        {
            if (!IsLightBlocker)
                return $"S{SunLighted}, L{LightLevel}";
            else return "BLOCKER";
        }
    }
}