using Origin.Source.Model.NewWorld.Systems;
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
            // Видаляємо рослинність на поточному тайлі, якщо вона існує.
            VegUtilities.RemoveVegetationEntityAt(site, pos);

            // Після зняття конструкції перевіряємо тайл нижче на можливість появи рослинності.
            var belowPos = pos + Point3.Down;
            VegUtilities.TrySpawnVegetationEntity(site, belowPos, 0);

            // Централізовано реєструємо terrain-дерті для подальшої валідації у спеціальній ECS-системі.
            site.VegetationEnvironmentDirtySystem?.MarkTerrainDirty(pos);
            site.VegetationEnvironmentDirtySystem?.MarkTerrainDirty(belowPos);
        }
    }
}
