using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Resources;
using Origin.Source.Utils;
using System;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    internal class VegatationControlSystem : TickSystem
    {
        private readonly Random random;

        public VegatationControlSystem(Site site) : base(site)
        {
            random = site.World.Random;
        }

        public override void Initialize()
        {
            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        if (!VegUtilities.IsExposedTop(_site, pos))
                            continue;

                        Tile tile = _site.Map[pos];
                        if (!VegUtilities.TryGetVegetationFor(tile.Construction, out var vegetation))
                            continue;

                        tile.HasVegetation = true;
                        tile.Vegetation = new TileVegetation
                        {
                            VegetationMetaID = GlobalResources.Vegetations.IndexOf(vegetation.ID),
                            IsGrown = true,
                            VegetationNeighbours = 0
                        };
                        _site.Map[pos] = tile;
                    }
                }
            }

            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        Tile tile = _site.Map[pos];
                        if (!tile.Exists || !tile.HasVegetation)
                            continue;

                        tile.Vegetation.VegetationNeighbours = VegUtilities.GetNeighboursFor(_site, pos);
                        _site.Map[pos] = tile;
                    }
                }
            }

            _site.InvalidateRender();
        }

        public override void Update(in ulong t)
        {
            // Disabled for now: full scan of all tiles each tick is too expensive.
            // TODO: replace with event-driven or active-set based growth updates.
        }
    }
}
