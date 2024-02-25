using Origin.Source.Resources;

namespace Origin.Source.Components
{
    public struct OverConstruction
    {
        public int ConstructionMetaID;
        public int MaterialMetaID;

        public Construction Construction => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionMetaID);

        public Material Material => GlobalResources.GetByMetaID(GlobalResources.Materials, MaterialMetaID);
    }
}