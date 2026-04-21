namespace Origin.Source.Model.NewWorld
{
    public struct Tile
    {
        // Компактне сховище булевих станів тайла (16 біт).
        private TileFlags16 _flags;

        public bool Exists { readonly get => _flags.Exists; set => _flags.Exists = value; }

        public bool HasConstruction { readonly get => _flags.HasConstruction; set => _flags.HasConstruction = value; }
        public TileConstruction Construction;

        public bool HasConstructionOver { readonly get => _flags.HasConstructionOver; set => _flags.HasConstructionOver = value; }
        public TileConstructionOver ConstructionOver;

        public bool IsRamp { readonly get => _flags.IsRamp; set => _flags.IsRamp = value; }

        public bool HasConstructionShape { readonly get => _flags.HasConstructionShape; set => _flags.HasConstructionShape = value; }
        public TileConstructionShape ConstructionShape;

        public bool HasConstructionRotation { readonly get => _flags.HasConstructionRotation; set => _flags.HasConstructionRotation = value; }
        public TileConstructionRotation ConstructionRotation;

        public bool HasFluid { readonly get => _flags.HasFluid; set => _flags.HasFluid = value; }
        public TileFluid Fluid;

        public bool IsFluidStatic { readonly get => _flags.IsFluidStatic; set => _flags.IsFluidStatic = value; }

        public bool IsFluidBlocker { readonly get => _flags.IsFluidBlocker; set => _flags.IsFluidBlocker = value; }

        public bool HasVegetation { readonly get => _flags.HasVegetation; set => _flags.HasVegetation = value; }
        public TileVegetation Vegetation;

        public bool IsWalkable { readonly get => _flags.IsWalkable; set => _flags.IsWalkable = value; }

        public int WalkableConstructionBelowMetaID;

        public readonly bool IsAir => Exists && !HasConstruction;
    }
}
