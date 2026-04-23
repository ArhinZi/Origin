using Origin.Source.Render;
using Origin.Source.Utils;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    // Тег/компонент типу рослинності (meta id ресурсу Vegetation).
    internal struct VegetationTypeTag
    {
        public int VegetationMetaID;
    }

    // Поточний рівень росту рослинності 0..8.
    internal struct VegetationGrowthLevel
    {
        public byte Value;
    }

    // Затримка росту в тіках до наступного підвищення рівня.
    internal struct VegetationGrowthDelay
    {
        public int TicksRemaining;
    }

    // Кількість сусідів рослинності.
    internal struct VegetationNeighboursComponent
    {
        public short Value;
    }

    // Позиція тайла, до якого прив'язана ентіті рослинності.
    internal struct VegetationTileLink
    {
        public Point3 Pos;
    }

    // Останній locator спрайта рослинності у статичному drawer.
    internal struct VegetationSpriteRenderLocator
    {
        public SpriteLocator Value;
    }

    // Тег: ентіті потрібно перевірити на валідність умов росту.
    internal struct VegetationNeedsValidationTag
    {
    }

    // Тег: ентіті потрібно оновити у рендері.
    internal struct VegetationRenderDirtyTag
    {
    }

    // Тег: рослинність щойно потоптали.
    internal struct VegetationTrampledTag
    {
    }

    // Таймер витоптаності (у тіках). Поки активний, повторне витоптування зменшує рівень.
    internal struct VegetationTrampleDelay
    {
        public int TicksRemaining;
    }
}
