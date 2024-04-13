using MessagePack;

using Origin.Source.Resources;

namespace Origin.Source.ECS.Vegetation.Components
{
    [MessagePackObject]
    internal struct BaseVegetation
    {
        [IgnoreMember]
        public int VegetationMetaID;

        [Key("VegetationID")]
        public string VegetationID
        {
            get => Vegetation.ID;
            set
            {
                VegetationMetaID = GlobalResources.Vegetations.IndexOf(value);
            }
        }

        [IgnoreMember]
        public Resources.Vegetation Vegetation => GlobalResources.Vegetations[VegetationMetaID];

        [Key("VegetationNeighbours")]
        public short VegetationNeighbours;
    }
}