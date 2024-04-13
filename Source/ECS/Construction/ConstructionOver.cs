using MessagePack;

using Origin.Source.Resources;

namespace Origin.Source.ECS.Construction
{
    [MessagePackObject]
    public struct ConstructionOver
    {
        public int ConstructionMetaID;
        public int MaterialMetaID;

        [IgnoreMember]
        public Resources.Construction Construction => GlobalResources.Constructions[ConstructionMetaID];

        [IgnoreMember]
        public Material Material => GlobalResources.Materials[MaterialMetaID];
    }
}