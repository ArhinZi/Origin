﻿using MessagePack;

using Origin.Source.Resources;

namespace Origin.Source.ECS.Construction
{
    [MessagePackObject]
    public struct BaseConstruction
    {
        public int ConstructionMetaID;
        public int MaterialMetaID;

        [IgnoreMember]
        public Resources.Construction Construction => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionMetaID);

        [IgnoreMember]
        public Material Material => GlobalResources.GetByMetaID(GlobalResources.Materials, MaterialMetaID);
    }
}