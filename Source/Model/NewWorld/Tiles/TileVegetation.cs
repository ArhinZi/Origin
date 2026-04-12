using Origin.Source.Resources;

namespace Origin.Source.Model.NewWorld
{
    public struct TileVegetation
    {
        public int VegetationMetaID;
        public short VegetationNeighbours;
        public bool IsGrown;

        public string VegetationID
        {
            get => Vegetation.ID;
            set => VegetationMetaID = GlobalResources.Vegetations.IndexOf(value);
        }

        public readonly Vegetation Vegetation => GlobalResources.Vegetations[VegetationMetaID];
    }
}
