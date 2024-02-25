using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS.Construction
{
    internal struct ConstructionPlacedEvent
    {
        public Point3 Position;
        public int ConstructionMetaID;
        public int MaterialMetaID;
    }
}