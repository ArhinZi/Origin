using System.Runtime.InteropServices;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    /*[StructLayout(LayoutKind.Sequential)]
    public struct SpriteLocatorPacked
    {
        private uint data; // 32 bits in total

        // BitField 3 bits - 8
        public uint x
        {
            get { return data & 0x7; } // Mask the lower 3 bits
            set
            {
                Debug.Assert(value < 8);
                data = ((data & 0xFFFFFFF8) | (value & 0x7));
            }
        }

        // BitField 3 bits - 8
        public uint y
        {
            get { return (data >> 3) & 0x7; } // Shift and mask the next 3 bits
            set
            {
                Debug.Assert(value < 8);
                data = ((data & 0xFFFFF1F) | ((value & 0x7) << 3));
            }
        }

        // BitField 20 bits - 1048576 (1024^2)
        public uint i
        {
            get { return (data >> 6) & 0xFFFFF; } // Shift and mask the lower 20 bits
            set
            {
                Debug.Assert(value < 1048576);
                data = ((data & 0xFFFFFC3F) | ((value & 0xFFFFF) << 6));
            }
        }

        // BitField 6 bits - 64
        public uint layer
        {
            get { return (data >> 26) & 0x3F; } // Shift and mask the next 6 bits
            set
            {
                Debug.Assert(value < 64);
                data = ((data & 0xFC000000) | ((value & 0x3F) << 26));
            }
        }
    }*/

    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteLocator
    {
        public uint TextureMetaID;
        public uint Layer;
        public uint Index;
        public uint pud1;
    }
}