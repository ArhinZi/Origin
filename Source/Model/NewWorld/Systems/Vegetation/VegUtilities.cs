using Arch.Core;
using Arch.Core.Extensions;
using Origin.Source.Resources;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    internal static class VegUtilities
    {
        // Повертає випадкову рослинність, що підходить під конструкцію та умови тайла.
        public static bool TryGetVegetationFor(Site site, Point3 pos, TileConstruction construction, out Origin.Source.Resources.Vegetation vegetation)
        {
            var candidates = GetSuitableVegetations(site, pos, construction);
            if (candidates.Count == 0)
            {
                vegetation = null;
                return false;
            }

            int index = site.World.Random.Next(candidates.Count);
            vegetation = candidates[index];
            return true;
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

        // Створює ECS-ентіті рослинності і прив'язує її до тайла.
        public static bool TrySpawnVegetationEntity(Site site, Point3 pos, byte initialGrowthLevel = 0)
        {
            if (!site.Map.TryGet(pos, out Tile tile) || !tile.Exists || !tile.HasConstruction || tile.HasVegetation)
                return false;

            if (!IsExposedTop(site, pos))
                return false;

            if (!TryGetVegetationFor(site, pos, tile.Construction, out var vegetation))
                return false;

            short neighbours = GetNeighboursFor(site, pos);
            int vegetationMeta = GlobalResources.Vegetations.IndexOf(vegetation.ID);
            if (vegetationMeta < 0)
                return false;

            byte clampedGrowth = initialGrowthLevel;
            if (clampedGrowth > 8)
                clampedGrowth = 8;

            Entity entity = site.ArchWorld.Create();
            entity.Add(new VegetationTypeTag { VegetationMetaID = vegetationMeta });
            entity.Add(new VegetationGrowthLevel { Value = clampedGrowth });
            entity.Add(new VegetationNeighboursComponent { Value = neighbours });
            entity.Add(new VegetationTileLink { Pos = pos });
            entity.Add<VegetationNeedsValidationTag>();
            entity.Add<VegetationRenderDirtyTag>();

            if (clampedGrowth < 8)
            {
                entity.Add(new VegetationGrowthDelay
                {
                    TicksRemaining = RollGrowthDelayTicks(site, pos, neighbours)
                });
            }

            tile.HasVegetation = true;
            tile.VegetationEntity = entity;
            site.Map[pos] = tile;

            UpdateNeighboursOf(site, pos, +1);
            site.InvalidateRender(pos);
            return true;
        }

        // Видаляє ECS-ентіті рослинності з тайла.
        public static bool RemoveVegetationEntityAt(Site site, Point3 pos)
        {
            if (!site.Map.TryGet(pos, out Tile tile) || !tile.Exists || !tile.HasVegetation)
                return false;

            Entity entity = tile.VegetationEntity;
            if (entity != Entity.Null && entity.IsAlive())
                site.ArchWorld.Destroy(entity);

            UpdateNeighboursOf(site, pos, -1);

            tile.HasVegetation = false;
            tile.VegetationEntity = Entity.Null;
            site.Map[pos] = tile;
            site.InvalidateRender(pos);
            return true;
        }

        // Оновлює тайл-посилання на ентіті рослинності.
        public static void SyncTileFromEntity(Site site, in Entity entity)
        {
            if (!entity.IsAlive() || !entity.TryGet(out VegetationTileLink link))
                return;

            if (!site.Map.TryGet(link.Pos, out Tile tile) || !tile.Exists || !tile.HasVegetation)
                return;

            tile.VegetationEntity = entity;
            site.Map[link.Pos] = tile;
        }

        // Повертає ECS-стан рослинності для тайла (тип, рівень, сусіди).
        public static bool TryGetVegetationState(Site site, in Tile tile, out Origin.Source.Resources.Vegetation vegetation, out byte growthLevel, out short neighbours)
        {
            vegetation = null;
            growthLevel = 0;
            neighbours = 0;

            if (!tile.HasVegetation)
                return false;

            Entity entity = tile.VegetationEntity;
            if (entity == Entity.Null || !entity.IsAlive())
                return false;

            if (!entity.TryGet(out VegetationTypeTag type)
                || !entity.TryGet(out VegetationGrowthLevel level)
                || !entity.TryGet(out VegetationNeighboursComponent neighboursComponent))
            {
                return false;
            }

            if (type.VegetationMetaID < 0 || type.VegetationMetaID >= GlobalResources.Vegetations.Count)
                return false;

            vegetation = GlobalResources.Vegetations[type.VegetationMetaID];
            growthLevel = level.Value;
            neighbours = neighboursComponent.Value;
            return true;
        }

        // Ставить тег перевірки умов росту на рослинність у радіусі (використовується після зміни конструкцій/рельєфу/світла).
        public static void MarkVegetationValidationAround(Site site, Point3 center, int radius = 8)
        {
            int radiusSq = radius * radius;
            int minZ = Math.Max(center.Z - radius, 0);
            int maxZ = Math.Min(center.Z + radius, site.Size.Z - 1);

            for (int z = minZ; z <= maxZ; z++)
            {
                int dz = z - center.Z;
                for (int x = Math.Max(center.X - radius, 0); x <= Math.Min(center.X + radius, site.Size.X - 1); x++)
                {
                    int dx = x - center.X;
                    for (int y = Math.Max(center.Y - radius, 0); y <= Math.Min(center.Y + radius, site.Size.Y - 1); y++)
                    {
                        int dy = y - center.Y;
                        if (dx * dx + dy * dy + dz * dz > radiusSq)
                            continue;

                        Point3 pos = new(x, y, z);
                        if (!site.Map.TryGet(pos, out Tile tile) || !tile.Exists || !tile.HasVegetation)
                            continue;

                        Entity entity = tile.VegetationEntity;
                        // Не додаємо тег повторно, інакше Arch спробує перемістити ентіті в той самий архетип.
                        if (entity != Entity.Null && entity.IsAlive() && !entity.Has<VegetationNeedsValidationTag>())
                            entity.Add<VegetationNeedsValidationTag>();
                    }
                }
            }
        }

        // Розраховує випадкову затримку росту (100..1000), зменшує її за рахунок сусідів і сонця.
        public static int RollGrowthDelayTicks(Site site, Point3 pos, short vegetationNeighbours)
        {
            int baseDelay = site.World.Random.Next(100, 1001);
            ref var light = ref site.LightControl.GetTile(pos);

            float neighboursFactor = 1f + Math.Clamp(vegetationNeighbours, (short)0, (short)20) * 0.06f;
            float sunlightFactor = Math.Clamp(light.SunLighted / 7f, 0f, 1f);
            float totalFactor = neighboursFactor * (0.5f + sunlightFactor);

            int delay = (int)Math.Ceiling(baseDelay / Math.Max(0.1f, totalFactor));
            return Math.Clamp(delay, 20, 1000);
        }

        public static void UpdateNeighboursOf(Site site, Point3 pos, short value)
        {
            foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
            {
                var pos2 = pos + item;
                if (site.Map.TryGet(pos2, out Tile tile) && tile.Exists && tile.HasVegetation)
                {
                    site.InvalidateRender(pos2);

                    Entity entity = tile.VegetationEntity;
                    if (entity != Entity.Null && entity.IsAlive() && entity.TryGet(out VegetationNeighboursComponent neighbours))
                    {
                        neighbours.Value += value;
                        entity.Set(neighbours);
                    }
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
                if (tile.Exists && tile.HasVegetation)
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

        // Ентіті рослинності не повинна існувати без сонячного світла.
        public static bool HasSunlightAt(Site site, Point3 pos)
        {
            ref var light = ref site.LightControl.GetTile(pos);
            return light.SunLighted > 0;
        }

        // Перевірка умови сонця для рослинності.
        // Якщо кеш світла ще не встиг оновитися після копання/зміни рельєфу,
        // допускаємо spawn на тайлі, який структурно відкритий зверху.
        private static bool IsSunlightConditionSatisfied(Site site, Point3 pos, Origin.Source.Resources.Vegetation vegetation)
        {
            if (!vegetation.SunLightRequired)
                return true;

            if (HasSunlightAt(site, pos))
                return true;

            return IsExposedTop(site, pos);
        }

        // Повертає всі рослини, які підходять під конструкцію і поточні умови тайла.
        private static List<Origin.Source.Resources.Vegetation> GetSuitableVegetations(Site site, Point3 pos, TileConstruction construction)
        {
            List<Origin.Source.Resources.Vegetation> result = [];
            string constructionId = construction.Construction.ID;
            string constructionCategory = construction.Construction.Category;

            foreach (var vegetation in GlobalResources.Vegetations)
            {
                if (vegetation == null || vegetation.Drawing == null)
                    continue;

                // Перевірка SunLightRequired винесена в окремий метод (з fallback для свіжих terrain-змін).
                if (!IsSunlightConditionSatisfied(site, pos, vegetation))
                    continue;

                bool anyMatch = false;
                foreach (var drawing in vegetation.Drawing)
                {
                    if (drawing == null)
                        continue;

                    bool byConstruction = drawing.OnConstructions != null && drawing.OnConstructions.Contains(constructionId);
                    bool byCategory = !string.IsNullOrEmpty(constructionCategory)
                        && drawing.OnConstructionsCategories != null
                        && drawing.OnConstructionsCategories.Contains(constructionCategory);

                    if (byConstruction || byCategory)
                    {
                        anyMatch = true;
                        break;
                    }
                }

                if (anyMatch)
                    result.Add(vegetation);
            }

            return result;
        }
    }
}
