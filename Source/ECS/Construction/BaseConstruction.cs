using Origin.Source.Resources;

namespace Origin.Source.ECS.Construction
{
    public struct BaseConstruction
    {
        public int ConstructionMetaID;
        public int MaterialMetaID;

        public Resources.Construction Construction => GlobalResources.GetByMetaID(GlobalResources.Constructions, ConstructionMetaID);

        public Material Material => GlobalResources.GetByMetaID(GlobalResources.Materials, MaterialMetaID);
    }
}