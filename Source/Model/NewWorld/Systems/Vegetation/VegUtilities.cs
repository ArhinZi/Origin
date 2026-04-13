using Origin.Source.Resources;
using Origin.Source.Utils;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    internal static class VegUtilities
    {
        public static bool TryGetVegetationFor(TileConstruction construction, out Origin.Source.Resources.Vegetation vegetation)
        {
            if (Origin.Source.Resources.Vegetation.VegetationByConstruction.TryGetValue(construction.Construction.ID, out vegetation))
                return true;
            if (!string.IsNullOrEmpty(construction.Construction.Category) &&
                Origin.Source.Resources.Vegetation.VegetationByConstrCategory.TryGetValue(construction.Construction.Category, out vegetation))
                return true;

            vegetation = null;
            return false;
        }

        public static void UpdateNeighboursOf(Site site, Point3 pos, short value)
        {
            foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
            {
                var pos2 = pos + item;
                if (site.Map.TryGet(pos2, out Tile tile) && tile.Exists && tile.HasVegetation)
                {
                    tile.Vegetation.VegetationNeighbours += value;
                    site.Map[pos2] = tile;
                    site.InvalidateRender(pos2);
                }
            }
        }

        public static short GetNeighboursFor(Site site, Point3 pos)
        {
            short count = 0;
            foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
            {
                var pos2 = pos + item;
                if (!pos2.InBounds(Point3.Zero, site.Size))
                {
                    count++;
                    continue;
                }

                var tile = site.Map[pos2];
                if (tile.Exists && tile.HasVegetation && tile.Vegetation.IsGrown)
                {
                    count++;
                }
            }
            return count;
        }

        public static bool IsExposedTop(Site site, Point3 pos)
        {
            if (!site.Map.TryGet(pos, out Tile tile) || !tile.Exists || !tile.HasConstruction)
                return false;

            var abovePos = pos + Point3.Up;
            return !site.Map.TryGet(abovePos, out Tile above) || !above.Exists || !above.HasConstruction;
        }
    }
}
