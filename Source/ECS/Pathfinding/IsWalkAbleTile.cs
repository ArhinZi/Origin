using Origin.Source.Resources;

namespace Origin.Source.ECS.Pathfinding
{
    public struct IsWalkAbleTile
    {
        public int ConstructionBelowMetaID;

        public Resources.Construction ConstructionBelow => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionBelowMetaID);
    }
}