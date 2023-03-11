using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Utils
{
    class ByteField
    {
        public byte value;

        public ByteField()
        {
            value = 0;
        }
        public static void SetBit(ByteField value, byte bitNumber, bool state)
        {
            if (bitNumber < 1 || bitNumber > 8)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 8");

            value.value = state ?
                (byte)(value.value | (1 << (bitNumber - 1))) :
                (byte)(value.value & ~(1 << (bitNumber - 1)));
        }

        public static bool GetBit(ByteField value, byte bitNumber)
        {
            if (bitNumber < 1 || bitNumber > 8)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 16");

            return (value.value & (1 << (bitNumber - 1))) >= 1;
        }

        public override string ToString()
        {
            string s = "";
            for (byte i = 1; i <= 8; i++)
            {
                s += i.ToString() + ":"; 
                s += GetBit(this, i) ? "1" : "0";
            }
            return s;
        }
    }
}
