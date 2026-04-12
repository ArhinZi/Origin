using Origin.Source.Resources;

namespace Origin.Source.Model.NewWorld
{
    public struct TileConstructionOver
    {
        public int ConstructionMetaID;
        public int MaterialMetaID;

        public readonly Construction Construction => GlobalResources.Constructions[ConstructionMetaID];
        public readonly Material Material => GlobalResources.Materials[MaterialMetaID];
    }
}
