using MessagePack;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Save
{
    [MessagePackObject(true)]
    public struct SaveComponentRegistry
    {
        public int Id;
        public int ByteSize;
        public string Type;
    }
}