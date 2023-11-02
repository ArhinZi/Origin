using Origin.Source.Resources;

namespace Origin.Source.ECS
{
    public struct BaseConstruction
    {
        public int ConstructionMetaID;
        public int MaterialMetaID;

        public Construction Construction => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionMetaID);

        public Material Material => GlobalResources.GetByMetaID(GlobalResources.Materials, MaterialMetaID);
    }
}