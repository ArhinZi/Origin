using MessagePack;

using Origin.Source.Resources;

namespace Origin.Source.ECS.Construction
{
    [MessagePackObject]
    public struct ConstructionBase
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
                ConstructionMetaID = GlobalResources.Constructions.IndexOf(value);
            }
        }

        [Key("MaterialID")]
        public string MaterialID
        {
            get => Material.ID;
            set
            {
                MaterialMetaID = GlobalResources.Materials.IndexOf(value);
            }
        }

        [IgnoreMember]
        public Resources.Construction Construction => GlobalResources.Constructions[ConstructionMetaID];

        [IgnoreMember]
        public Material Material => GlobalResources.Materials[MaterialMetaID];
    }
}