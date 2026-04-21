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
            bool anyChange = false;

            // Якщо перераховувалося світло — рослинність потенційно втратила/отримала сонце.
            if (_site.LightControl.bufferDirty)
            {
                MarkAllVegetationForValidation();
                anyChange = true;
            }

            // Якщо були локальні terrain-зміни — ставимо валідацію навколо них.
            if (_terrainDirtyPositions.Count > 0)
            {
                foreach (var pos in _terrainDirtyPositions)
                    VegUtilities.MarkVegetationValidationAround(_site, pos);

                _terrainDirtyPositions.Clear();
                anyChange = true;
            }

            if (anyChange)
            {
                // Рендер-інвалідація робиться цільово далі в системі рослинності через RenderDirtyTag.
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
