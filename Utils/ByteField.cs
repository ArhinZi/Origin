using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Utils
{
    class ByteField
    {
        public static void SetBit(ref byte value, byte bitNumber, bool state)
        {
            if (bitNumber < 1 || bitNumber > 8)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 8");

            value = state ?
                (byte)(value | (1 << (bitNumber - 1))) :
                (byte)(value & ~(1 << (bitNumber - 1)));
        }

        public static bool GetBit(byte value, byte bitNumber)
        {
            if (bitNumber < 1 || bitNumber > 8)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 16");

            return (value & (1 << (bitNumber - 1))) >= 1;
        }
    }
}
