using Origin.Source.Resources;
using Origin.Source.Utils;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    internal static class VegUtilities
    {
        // Перевіряє відповідну рослинність для конструкції з урахуванням умов тайла (зокрема сонячного світла).
        public static bool TryGetVegetationFor(Site site, Point3 pos, TileConstruction construction, out Origin.Source.Resources.Vegetation vegetation)
        {
            if (Origin.Source.Resources.Vegetation.VegetationByConstruction.TryGetValue(construction.Construction.ID, out vegetation)
                && IsSunlightConditionSatisfied(site, pos, vegetation))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(construction.Construction.Category)
                && Origin.Source.Resources.Vegetation.VegetationByConstrCategory.TryGetValue(construction.Construction.Category, out vegetation)
                && IsSunlightConditionSatisfied(site, pos, vegetation))
            {
                return true;
            }

            vegetation = null;
            return false;
        }

        // Зворотна сумісність для старих місць виклику без site/pos.
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

        // Якщо рослинність вимагає сонце, перевіряємо кеш сонячного світла.
        // Якщо кеш ще не встиг оновитись (після змін конструкцій/на старті),
        // дозволяємо ріст на тайлах, які структурно відкриті зверху.
        private static bool IsSunlightConditionSatisfied(Site site, Point3 pos, Origin.Source.Resources.Vegetation vegetation)
        {
            if (!vegetation.SunLightRequired)
                return true;

            ref var light = ref site.LightControl.GetTile(pos);
            if (light.SunLighted > 0)
                return true;

            return IsExposedTop(site, pos);
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
