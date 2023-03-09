using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.World
{
    public struct SiteBlock
    {
        public ushort wallId;
        public ushort floorId;
        public bool isSelected;
        public bool isVisible;
        public bool isDiscovered;

        public SiteBlock(ushort wallid = 0, ushort floorid = 0)
        {
            this.wallId = wallid;
            this.floorId = floorid;
            this.isSelected = false;
            this.isVisible = false;
            this.isDiscovered = false;
        }
    }
}
