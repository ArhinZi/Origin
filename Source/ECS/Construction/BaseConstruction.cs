using MessagePack;

using Origin.Source.Resources;

namespace Origin.Source.ECS.Construction
{
    [MessagePackObject]
    public struct BaseConstruction
    {
        [IgnoreMember]
        public int ConstructionMetaID;

        [IgnoreMember]
        public int MaterialMetaID;

        [Key("ConstructionID")]
        public string ConstructionID
        {
            get => Construction.ID;
            set
            {
                ConstructionMetaID = GlobalResources.GetResourceMetaID(GlobalResources.Constructions, value);
            }
        }

        [Key("MaterialID")]
        public string MaterialID
        {
            get => Material.ID;
            set
            {
                MaterialMetaID = GlobalResources.GetResourceMetaID(GlobalResources.Materials, value);
            }
        }

        [IgnoreMember]
        public Resources.Construction Construction => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionMetaID);

        [IgnoreMember]
        public Material Material => GlobalResources.GetByMetaID(GlobalResources.Materials, MaterialMetaID);
    }
}