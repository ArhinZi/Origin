using Arch.Core;
using Arch.Core.Extensions;
using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Utils;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    // Централізована ECS-система, яка збирає зміни середовища (рельєф/світло)
    // і ставить тег перевірки умов росту на рослинність.
    internal class VegetationEnvironmentDirtySystem : TickSystem
    {
        // Черга позицій, де змінювався рельєф/конструкції.
        private readonly HashSet<Point3> _terrainDirtyPositions = [];
        // Буфер для обходу ентіті без зайвих алокацій.
        private readonly List<Entity> _entitiesBuffer = [];

        public VegetationEnvironmentDirtySystem(Site site) : base(site)
        {
            site.VegetationEnvironmentDirtySystem = this;
        }

        public override void Initialize()
        {
            _terrainDirtyPositions.Clear();
        }

        public override void Update(in ulong t)
        {
            // Якщо перераховувалося світло — рослинність потенційно втратила/отримала сонце.
            if (_site.LightControl.bufferDirty)
            {
                MarkAllVegetationForValidation();
                // Важливо: після повернення сонця на порожніх тайлах потрібно знову створити ентіті рослинності.
                TrySpawnVegetationOnSuitableTiles();
            }

            // Якщо були локальні terrain-зміни — ставимо валідацію навколо них.
            if (_terrainDirtyPositions.Count <= 0)
                return;

            foreach (var pos in _terrainDirtyPositions)
            {
                VegUtilities.MarkVegetationValidationAround(_site, pos);
                // Локальна спроба spawn на зміненому тайлі та сусідніх по вертикалі позиціях.
                TrySpawnVegetationNear(pos);
            }

            _terrainDirtyPositions.Clear();
        }

        // Пробує створити рослинність на тайлі та на сусідах по вертикалі після terrain-зміни.
        private void TrySpawnVegetationNear(Point3 pos)
        {
            VegUtilities.TrySpawnVegetationEntity(_site, pos, 0);
            VegUtilities.TrySpawnVegetationEntity(_site, pos + Point3.Down, 0);
            VegUtilities.TrySpawnVegetationEntity(_site, pos + Point3.Up, 0);
        }

        // Глобальний прохід після перерахунку світла: повертає рослинність там, де умови знову стали валідними.
        private void TrySpawnVegetationOnSuitableTiles()
        {
            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        VegUtilities.TrySpawnVegetationEntity(_site, pos, 0);
                    }
                }
            }
        }

        // Реєструє зміну рельєфу/конструкції у конкретній позиції.
        public void MarkTerrainDirty(Point3 pos)
        {
            _terrainDirtyPositions.Add(pos);
        }

        // Ставить тег валідації для всіх ентіті рослинності.
        private void MarkAllVegetationForValidation()
        {
            // Гарячий шлях: читаємо ентіті через ітератор query, structural add робимо після обходу.
            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationTileLink>();
            var query = _site.ArchWorld.Query(in queryDesc);

            _entitiesBuffer.Clear();
            foreach (ref var chunk in query)
            {
                foreach (var entityIndex in chunk)
                {
                    Entity entity = chunk.Entity(entityIndex);
                    if (entity.IsAlive())
                        _entitiesBuffer.Add(entity);
                }
            }

            foreach (var entity in _entitiesBuffer)
            {
                if (entity.IsAlive() && !entity.Has<VegetationNeedsValidationTag>())
                    entity.Add<VegetationNeedsValidationTag>();
            }
        }
    }
}
