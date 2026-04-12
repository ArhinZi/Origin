namespace Origin.Source.Model.NewWorld
{
    public struct Tile
    {
        public bool Exists;

        public bool HasConstruction;
        public TileConstruction Construction;

        public bool HasConstructionOver;
        public TileConstructionOver ConstructionOver;

        public bool IsRamp;
        public bool HasConstructionShape;
        public TileConstructionShape ConstructionShape;
        public bool HasConstructionRotation;
        public TileConstructionRotation ConstructionRotation;

        public bool HasFluid;
        public TileFluid Fluid;
        public bool IsFluidStatic;
        public bool IsFluidBlocker;

        public bool HasVegetation;
        public TileVegetation Vegetation;

        public bool IsWalkable;
        public int WalkableConstructionBelowMetaID;

        public readonly bool IsAir => Exists && !HasConstruction;
    }
}
