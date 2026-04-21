using Origin.Source.Model.NewWorld.Systems;
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
            // При встановленні конструкції прибираємо рослинність під нею (якщо була).
            var belowPos = pos + Point3.Down;
            VegUtilities.RemoveVegetationEntityAt(site, belowPos);

            // Пробуємо заселити поточний тайл новою рослинністю, якщо умови підходять.
            VegUtilities.TrySpawnVegetationEntity(site, pos, 0);

            // Централізовано реєструємо terrain-дерті для подальшої валідації у спеціальній ECS-системі.
            site.VegetationEnvironmentDirtySystem?.MarkTerrainDirty(pos);
            site.VegetationEnvironmentDirtySystem?.MarkTerrainDirty(belowPos);
        }
    }
}
