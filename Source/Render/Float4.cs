using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Origin.Source.Render
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Float4
    {
        //128 bits
        //public static int BITS_COUNT = 32;
        //[FieldOffset(0)] private byte[] data = new byte[16];

        private int A = 0;
        private int B = 0;
        private int C = 0;
        private int D = 0;

        public Float4()
        {
        }

        public void SetBit(int position, bool bitValue = true)
        {
            int i = position / 32;
            if (position < 0 || position >= sizeof(uint) * 8 * 4)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Invalid bit position");
            }

            if (bitValue)
            {
                Set(i, Get(i) | 1 << position);
            }
            else
            {
                Set(i, Get(i) & ~(1 << position));
            }
        }

        public bool GetBit(int position)
        {
            int i = position / 32;
            if (position < 0 || position >= sizeof(int) * 8 * 4)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Invalid bit position");
            }

            return (Get(i) & 1 << position) != 0;
        }

        private int Get(int i)
        {
            if (i == 0) return A;
            else if (i == 1) return B;
            else if (i == 2) return C;
            else if (i == 3) return D;
            return 0;
        }

        private void Set(int i, int value)
        {
            if (i == 0) A = value;
            else if (i == 1) B = value;
            else if (i == 2) C = value;
            else if (i == 3) D = value;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            foreach (int value in new List<int>() { A, B, C, D })
            {
                result.Append(Convert.ToString(value, 2)).Append("|");
            }

            // Remove the trailing " | "
            result.Length -= 1;

            return result.ToString();
        }
    }
}