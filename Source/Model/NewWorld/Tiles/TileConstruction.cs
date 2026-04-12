using Origin.Source.Resources;

namespace Origin.Source.Model.NewWorld
{
    public struct TileConstruction
    {
        public int ConstructionMetaID;
        public int MaterialMetaID;

        public string ConstructionID
        {
            get => Construction.ID;
            set => ConstructionMetaID = GlobalResources.Constructions.IndexOf(value);
        }

        public string MaterialID
        {
            get => Material.ID;
            set => MaterialMetaID = GlobalResources.Materials.IndexOf(value);
        }

        public readonly Construction Construction => GlobalResources.Constructions[ConstructionMetaID];
        public readonly Material Material => GlobalResources.Materials[MaterialMetaID];
    }
}
