using Origin.Source.Resources;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    public struct BaseConstruction
    {
        public string ConstructionID;
        public string MaterialID;

        public Construction Construction => GlobalResources.GetResourceBy(GlobalResources.Constructions, "ID", ConstructionID);
        public Material Material => GlobalResources.GetResourceBy(GlobalResources.Materials, "ID", MaterialID);
    }
}