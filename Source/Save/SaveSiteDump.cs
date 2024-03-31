using MessagePack;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Save
{
    [MessagePackObject(true)]
    public struct SaveSiteDump
    {
        public int ID;
        public int CurrentLevel;
        public Point3 Size;
    }
}