namespace Origin.Source.ECS.Construction
{
    internal struct ConstructionRemovedEvent
    {
        public Point3 Position;
        public int ConstructionMetaID;
        public int MaterialMetaID;
    }
}