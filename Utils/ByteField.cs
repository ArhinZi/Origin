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
        public void SetBit(byte bitNumber, bool state)
        {
            if (bitNumber < 1 || bitNumber > 8)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 8");

            value = state ?
                (byte)(value | (1 << (bitNumber - 1))) :
                (byte)(value & ~(1 << (bitNumber - 1)));
        }

        public bool GetBit(byte bitNumber)
        {
            if (bitNumber < 1 || bitNumber > 8)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 16");

            return (value & (1 << (bitNumber - 1))) >= 1;
        }

        public override string ToString()
        {
            string s = "";
            for (byte i = 1; i <= 8; i++)
            {
                s += GetBit(i) ? "1" : "0";
            }
            return s;
        }
    }
}
