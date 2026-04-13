using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Resources;
using Origin.Source.Utils;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    internal class UpdateVegsOnConstructionPlacedSystem : TickSystem
    {
        public UpdateVegsOnConstructionPlacedSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        public override void Update(in ulong t)
        {
        }

        public static void Apply(Site site, Point3 pos)
        {
            var belowPos = pos + Point3.Down;
            if (site.Map.TryGet(belowPos, out Tile belowTile) && belowTile.Exists && belowTile.HasVegetation)
            {
                if (belowTile.Vegetation.IsGrown)
                    VegUtilities.UpdateNeighboursOf(site, belowPos, -1);

                belowTile.HasVegetation = false;
                belowTile.Vegetation = default;
                site.Map[belowPos] = belowTile;
                site.InvalidateRender(belowPos);
            }

            if (site.Map.TryGet(pos, out Tile tile) && tile.Exists && tile.HasConstruction && !tile.HasVegetation &&
                VegUtilities.IsExposedTop(site, pos) && VegUtilities.TryGetVegetationFor(tile.Construction, out var vegetation))
            {
                tile.HasVegetation = true;
                tile.Vegetation = new TileVegetation
                {
                    VegetationMetaID = GlobalResources.Vegetations.IndexOf(vegetation.ID),
                    VegetationNeighbours = VegUtilities.GetNeighboursFor(site, pos),
                    IsGrown = false
                };
                site.Map[pos] = tile;
                site.InvalidateRender(pos);
            }
        }
    }
}
