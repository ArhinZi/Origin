using Origin.Source.Resources;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS.Pathfinding
{
    public struct IsWalkAbleTile
    {
        public int ConstructionBelowMetaID;

        public Resources.Construction ConstructionBelow => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionBelowMetaID);
    }
}