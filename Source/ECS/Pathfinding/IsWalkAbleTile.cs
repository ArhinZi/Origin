using MessagePack;

using Origin.Source.Resources;

namespace Origin.Source.ECS.Pathfinding
{
    [MessagePackObject]
    public struct IsWalkAbleTile
    {
        [IgnoreMember]
        public int ConstructionBelowMetaID;

        [Key("ConstructionBelowID")]
        public string ConstructionBelowID
        {
            get => ConstructionBelow.ID;
            set
            {
                ConstructionBelowMetaID = GlobalResources.Constructions.IndexOf(value);
            }
        }

        [IgnoreMember]
        public Resources.Construction ConstructionBelow => GlobalResources.Constructions[ConstructionBelowMetaID];
    }
}