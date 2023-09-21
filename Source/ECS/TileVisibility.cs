using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    public struct TileVisibility
    {
        public bool WallVisible;
        public bool WallDiscovered;

        public bool FloorVisible;
        public bool FloorDiscovered;
    }
}