using Arch.Core;
using Origin.Source.ECS;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Map;
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
            var query = new QueryDescription().WithAll<EventConstructionPlaced>();
            _site.ArchWorld.Query(in query, (ref EventConstructionPlaced cpe) =>
            {
                var pos = cpe.Position;
                var belowPos = pos + Point3.Down;
                if (_site.Map.TryGet(belowPos, out Tile belowTile) && belowTile.Exists && belowTile.HasVegetation)
                {
                    if (belowTile.Vegetation.IsGrown)
                        VegUtilities.UpdateNeighboursOf(_site, belowPos, -1);

                    belowTile.HasVegetation = false;
                    belowTile.Vegetation = default;
                    _site.Map[belowPos] = belowTile;
                    _site.InvalidateRender(belowPos);
                }

                if (_site.Map.TryGet(pos, out Tile tile) && tile.Exists && tile.HasConstruction && !tile.HasVegetation &&
                    VegUtilities.IsExposedTop(_site, pos) && VegUtilities.TryGetVegetationFor(tile.Construction, out var vegetation))
                {
                    tile.HasVegetation = true;
                    tile.Vegetation = new TileVegetation
                    {
                        VegetationMetaID = GlobalResources.Vegetations.IndexOf(vegetation.ID),
                        VegetationNeighbours = VegUtilities.GetNeighboursFor(_site, pos),
                        IsGrown = false
                    };
                    _site.Map[pos] = tile;
                    _site.InvalidateRender(pos);
                }
            });
        }
    }
}
