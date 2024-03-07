using MessagePack;

using Origin.Source.Resources;

namespace Origin.Source.ECS.Pathfinding
{
    [MessagePackObject]
    public struct IsWalkAbleTile
    {
        public int ConstructionBelowMetaID;

        [IgnoreMember]
        public Resources.Construction ConstructionBelow => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionBelowMetaID);
    }
}