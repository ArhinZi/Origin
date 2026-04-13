using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Resources;
using Origin.Source.Utils;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    internal class UpdateVegsOnConstructionRemovedSystem : TickSystem
    {
        public UpdateVegsOnConstructionRemovedSystem(Site site) : base(site)
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
            if (site.Map.TryGet(pos, out Tile tile) && tile.Exists && tile.HasVegetation)
            {
                if (tile.Vegetation.IsGrown)
                    VegUtilities.UpdateNeighboursOf(site, pos, -1);

                tile.HasVegetation = false;
                tile.Vegetation = default;
                site.Map[pos] = tile;
                site.InvalidateRender(pos);
            }

            var belowPos = pos + Point3.Down;
            if (site.Map.TryGet(belowPos, out Tile belowTile) && belowTile.Exists && belowTile.HasConstruction && !belowTile.HasVegetation &&
                VegUtilities.IsExposedTop(site, belowPos) && VegUtilities.TryGetVegetationFor(belowTile.Construction, out var vegetation))
            {
                belowTile.HasVegetation = true;
                belowTile.Vegetation = new TileVegetation
                {
                    VegetationMetaID = GlobalResources.Vegetations.IndexOf(vegetation.ID),
                    VegetationNeighbours = VegUtilities.GetNeighboursFor(site, belowPos),
                    IsGrown = false
                };
                site.Map[belowPos] = belowTile;
                site.InvalidateRender(belowPos);
            }
        }
    }
}
