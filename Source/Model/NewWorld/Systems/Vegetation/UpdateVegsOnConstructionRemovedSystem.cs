using Arch.Core;
using Origin.Source.ECS;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Map;
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
            var query = new QueryDescription().WithAll<EventConstructionRemoved>();
            _site.ArchWorld.Query(in query, (ref EventConstructionRemoved cre) =>
            {
                var pos = cre.Position;
                if (_site.Map.TryGet(pos, out Tile tile) && tile.Exists && tile.HasVegetation)
                {
                    if (tile.Vegetation.IsGrown)
                        VegUtilities.UpdateNeighboursOf(_site, pos, -1);

                    tile.HasVegetation = false;
                    tile.Vegetation = default;
                    _site.Map[pos] = tile;
                    _site.InvalidateRender(pos);
                }

                var belowPos = pos + Point3.Down;
                if (_site.Map.TryGet(belowPos, out Tile belowTile) && belowTile.Exists && belowTile.HasConstruction && !belowTile.HasVegetation &&
                    VegUtilities.IsExposedTop(_site, belowPos) && VegUtilities.TryGetVegetationFor(belowTile.Construction, out var vegetation))
                {
                    belowTile.HasVegetation = true;
                    belowTile.Vegetation = new TileVegetation
                    {
                        VegetationMetaID = GlobalResources.Vegetations.IndexOf(vegetation.ID),
                        VegetationNeighbours = VegUtilities.GetNeighboursFor(_site, belowPos),
                        IsGrown = false
                    };
                    _site.Map[belowPos] = belowTile;
                    _site.InvalidateRender(belowPos);
                }
            });
        }
    }
}
