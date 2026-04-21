using Arch.Core;
using Arch.Core.Extensions;
using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld.Systems.Vegetation
{
    internal class VegatationControlSystem : TickSystem
    {
        // Крок обслуговування росту: раз на 60 тіків.
        private const int GrowthTickInterval = 60;
        // Затримка витоптаності в тіках.
        private const int TrampleDelayTicks = 1000;

        private readonly List<Entity> _entitiesBuffer = [];
        // Буфери для відкладених structural-змін поза ітератором query.
        private readonly List<Entity> _removeGrowthDelayBuffer = [];
        private readonly List<Entity> _addRenderDirtyBuffer = [];
        private readonly List<Entity> _removeTrampleDelayBuffer = [];
        private readonly List<Entity> _removeRenderDirtyTagBuffer = [];
        // Відкладені structural-зміни для валідації.
        private readonly List<Entity> _destroyVegetationBuffer = [];
        private readonly List<Entity> _removeNeedsValidationTagBuffer = [];
        private readonly List<(Entity Entity, VegetationGrowthDelay Delay)> _addGrowthDelayBuffer = [];
        // Відкладені structural-зміни для витоптаності.
        private readonly List<Entity> _removeTrampledTagBuffer = [];

        public VegatationControlSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
            // Початкова генерація рослинності через ECS-ентіті на валідних тайлах.
            for (int z = 0; z < _site.Size.Z; z++)
            {
                for (int x = 0; x < _site.Size.X; x++)
                {
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        if (!VegUtilities.IsExposedTop(_site, pos))
                            continue;

                        // Після генерації рослинність одразу виросша (рівень 8).
                        VegUtilities.TrySpawnVegetationEntity(_site, pos, 8);
                    }
                }
            }

            // Початкова синхронізація сусідів для всіх створених ентіті.
            RefreshAllNeighbours();
            _site.InvalidateRender();
        }

        public override void Update(in ulong t)
        {
            // Валідація/рендер/витоптаність обробляються щотік, через ECS теги.
            ProcessValidationTags();
            ProcessTrampledTags();
            ProcessTrampleDelayTimers();
            ProcessRenderDirtyTags();

            // Ріст та глобальна перевірка умов росту — раз на 60 тіків.
            if (t % GrowthTickInterval == 0)
            {
                // Глобальну валідацію тепер централізовано робить VegetationEnvironmentDirtySystem
                // на основі дерті-сигналів середовища, тому тут запускаємо лише ріст.
                ProcessGrowthTick();
                ProcessRenderDirtyTags();
            }
        }

        // Перерахунок кількості сусідів для всіх рослин після генерації.
        private void RefreshAllNeighbours()
        {
            // Гарячий шлях: прямий ітератор по чанках без проміжного списку ентіті.
            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationTileLink, VegetationNeighboursComponent>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query)
            {
                var links = chunk.GetSpan<VegetationTileLink>();
                var neighboursSpan = chunk.GetSpan<VegetationNeighboursComponent>();

                foreach (var entityIndex in chunk)
                {
                    ref var link = ref links[entityIndex];
                    short neighbours = VegUtilities.GetNeighboursFor(_site, link.Pos);
                    neighboursSpan[entityIndex] = new VegetationNeighboursComponent { Value = neighbours };

                    Entity entity = chunk.Entity(entityIndex);
                    VegUtilities.SyncTileFromEntity(_site, entity);
                }
            }
        }

        // Позначає всі рослини тегом перевірки умов (сонце/конструкція/експозиція).
        private void MarkAllVegetationForValidation()
        {
            QueryDescription query = new QueryDescription().WithAll<VegetationTileLink>();
            _entitiesBuffer.Clear();
            _site.ArchWorld.GetEntities(in query, _entitiesBuffer);

            foreach (var entity in _entitiesBuffer)
            {
                if (!entity.IsAlive())
                    continue;

                entity.Add<VegetationNeedsValidationTag>();
            }
        }

        // Валідує ентіті рослинності по тегу і видаляє ті, що більше не відповідають умовам.
        private void ProcessValidationTags()
        {
            // Гарячий шлях: ітератор по чанках, structural-зміни виконуємо після проходу.
            _destroyVegetationBuffer.Clear();
            _removeNeedsValidationTagBuffer.Clear();
            _addGrowthDelayBuffer.Clear();

            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationNeedsValidationTag, VegetationTileLink, VegetationTypeTag>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query)
            {
                var links = chunk.GetSpan<VegetationTileLink>();

                foreach (var entityIndex in chunk)
                {
                    Entity entity = chunk.Entity(entityIndex);
                    if (!entity.IsAlive())
                        continue;

                    ref var link = ref links[entityIndex];

                    bool remove = false;
                    if (!_site.Map.TryGet(link.Pos, out Tile tile) || !tile.Exists || !tile.HasConstruction)
                    {
                        remove = true;
                    }
                    else if (!VegUtilities.IsExposedTop(_site, link.Pos))
                    {
                        remove = true;
                    }
                    else if (!VegUtilities.HasSunlightAt(_site, link.Pos))
                    {
                        // За вимогою: рослинність не існує при SunLighted == 0.
                        remove = true;
                    }
                    else if (!VegUtilities.TryGetVegetationFor(_site, link.Pos, tile.Construction, out _))
                    {
                        remove = true;
                    }

                    if (remove)
                    {
                        if (_site.Map.TryGet(link.Pos, out Tile oldTile) && oldTile.Exists && oldTile.HasVegetation)
                        {
                            oldTile.HasVegetation = false;
                            oldTile.VegetationEntity = Entity.Null;
                            _site.Map[link.Pos] = oldTile;
                            _site.InvalidateRender(link.Pos);
                        }

                        _destroyVegetationBuffer.Add(entity);
                        continue;
                    }

                    // Синхронізуємо сусідів, тайл і плануємо таймери росту.
                    short neighbours = VegUtilities.GetNeighboursFor(_site, link.Pos);
                    entity.Set(new VegetationNeighboursComponent { Value = neighbours });

                    if (entity.TryGet(out VegetationGrowthLevel level))
                    {
                        if (level.Value >= 8)
                        {
                            if (entity.Has<VegetationGrowthDelay>())
                                _removeGrowthDelayBuffer.Add(entity);
                        }
                        else if (!entity.Has<VegetationGrowthDelay>())
                        {
                            _addGrowthDelayBuffer.Add((entity, new VegetationGrowthDelay
                            {
                                TicksRemaining = VegUtilities.RollGrowthDelayTicks(_site, link.Pos, neighbours)
                            }));
                        }
                    }

                    VegUtilities.SyncTileFromEntity(_site, entity);
                    _removeNeedsValidationTagBuffer.Add(entity);
                }
            }

            foreach (var entity in _destroyVegetationBuffer)
            {
                if (entity.IsAlive())
                    _site.ArchWorld.Destroy(entity);
            }

            foreach (var entity in _removeNeedsValidationTagBuffer)
            {
                if (entity.IsAlive() && entity.Has<VegetationNeedsValidationTag>())
                    entity.Remove<VegetationNeedsValidationTag>();
            }

            foreach (var item in _addGrowthDelayBuffer)
            {
                if (item.Entity.IsAlive() && !item.Entity.Has<VegetationGrowthDelay>())
                    item.Entity.Add(item.Delay);
            }

            foreach (var entity in _removeGrowthDelayBuffer)
            {
                if (entity.IsAlive() && entity.Has<VegetationGrowthDelay>())
                    entity.Remove<VegetationGrowthDelay>();
            }
            _removeGrowthDelayBuffer.Clear();
        }

        // Раз на 60 тіків зменшує затримку росту і підвищує рівень росту при досягненні 0.
        private void ProcessGrowthTick()
        {
            // Гарячий шлях: ітератор по чанках + spans, structural-зміни відкладені.
            _removeGrowthDelayBuffer.Clear();
            _addRenderDirtyBuffer.Clear();

            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationGrowthDelay, VegetationGrowthLevel, VegetationNeighboursComponent, VegetationTileLink>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query)
            {
                var delays = chunk.GetSpan<VegetationGrowthDelay>();
                var levels = chunk.GetSpan<VegetationGrowthLevel>();
                var neighbours = chunk.GetSpan<VegetationNeighboursComponent>();
                var links = chunk.GetSpan<VegetationTileLink>();

                foreach (var entityIndex in chunk)
                {
                    ref var delay = ref delays[entityIndex];
                    ref var level = ref levels[entityIndex];
                    ref var neighbour = ref neighbours[entityIndex];
                    ref var link = ref links[entityIndex];

                    delay.TicksRemaining -= GrowthTickInterval;
                    if (delay.TicksRemaining > 0)
                        continue;

                    if (level.Value < 8)
                    {
                        level.Value++;
                        _addRenderDirtyBuffer.Add(chunk.Entity(entityIndex));
                    }

                    if (level.Value >= 8)
                    {
                        _removeGrowthDelayBuffer.Add(chunk.Entity(entityIndex));
                    }
                    else
                    {
                        delay.TicksRemaining = VegUtilities.RollGrowthDelayTicks(_site, link.Pos, neighbour.Value);
                    }

                    VegUtilities.SyncTileFromEntity(_site, chunk.Entity(entityIndex));
                }
            }

            // Structural-зміни застосовуємо окремо, після проходу ітератора.
            foreach (var entity in _removeGrowthDelayBuffer)
            {
                if (entity.IsAlive() && entity.Has<VegetationGrowthDelay>())
                    entity.Remove<VegetationGrowthDelay>();
            }

            foreach (var entity in _addRenderDirtyBuffer)
            {
                if (entity.IsAlive() && !entity.Has<VegetationRenderDirtyTag>())
                    entity.Add<VegetationRenderDirtyTag>();
            }
        }

        // Логіка витоптаності: при тегу Trampled старт/рестарт таймера і опційне зменшення рівня.
        private void ProcessTrampledTags()
        {
            // Гарячий шлях: ітератор по чанках, structural-зміни виконуємо після проходу.
            _removeTrampledTagBuffer.Clear();
            _addRenderDirtyBuffer.Clear();
            _addGrowthDelayBuffer.Clear();

            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationTrampledTag, VegetationGrowthLevel, VegetationTileLink>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query)
            {
                var levels = chunk.GetSpan<VegetationGrowthLevel>();
                var links = chunk.GetSpan<VegetationTileLink>();

                foreach (var entityIndex in chunk)
                {
                    Entity entity = chunk.Entity(entityIndex);
                    if (!entity.IsAlive())
                        continue;

                    ref var level = ref levels[entityIndex];
                    ref var link = ref links[entityIndex];

                    bool hadActiveDelay = entity.TryGet(out VegetationTrampleDelay trampleDelay) && trampleDelay.TicksRemaining > 0;
                    if (hadActiveDelay)
                        level.Value = (byte)Math.Max(0, level.Value - 1);

                    trampleDelay.TicksRemaining = TrampleDelayTicks;
                    if (entity.Has<VegetationTrampleDelay>())
                        entity.Set(trampleDelay);
                    else
                        entity.Add(trampleDelay);

                    if (level.Value < 8 && !entity.Has<VegetationGrowthDelay>())
                    {
                        short neighbours = entity.TryGet(out VegetationNeighboursComponent n) ? n.Value : (short)0;
                        _addGrowthDelayBuffer.Add((entity, new VegetationGrowthDelay
                        {
                            TicksRemaining = VegUtilities.RollGrowthDelayTicks(_site, link.Pos, neighbours)
                        }));
                    }

                    _removeTrampledTagBuffer.Add(entity);
                    _addRenderDirtyBuffer.Add(entity);
                    VegUtilities.SyncTileFromEntity(_site, entity);
                }
            }

            foreach (var item in _addGrowthDelayBuffer)
            {
                if (item.Entity.IsAlive() && !item.Entity.Has<VegetationGrowthDelay>())
                    item.Entity.Add(item.Delay);
            }

            foreach (var entity in _removeTrampledTagBuffer)
            {
                if (entity.IsAlive() && entity.Has<VegetationTrampledTag>())
                    entity.Remove<VegetationTrampledTag>();
            }

            foreach (var entity in _addRenderDirtyBuffer)
            {
                if (entity.IsAlive() && !entity.Has<VegetationRenderDirtyTag>())
                    entity.Add<VegetationRenderDirtyTag>();
            }
        }

        // Тіковий відлік таймера витоптаності; після завершення компонент видаляється.
        private void ProcessTrampleDelayTimers()
        {
            // Гарячий шлях: ітератор по чанках + відкладене видалення компонентів.
            _removeTrampleDelayBuffer.Clear();

            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationTrampleDelay>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query)
            {
                var trampleDelays = chunk.GetSpan<VegetationTrampleDelay>();

                foreach (var entityIndex in chunk)
                {
                    ref var trampleDelay = ref trampleDelays[entityIndex];
                    trampleDelay.TicksRemaining--;

                    if (trampleDelay.TicksRemaining <= 0)
                        _removeTrampleDelayBuffer.Add(chunk.Entity(entityIndex));
                }
            }

            foreach (var entity in _removeTrampleDelayBuffer)
            {
                if (entity.IsAlive() && entity.Has<VegetationTrampleDelay>())
                    entity.Remove<VegetationTrampleDelay>();
            }
        }

        // Рендер-дерті оновлення рослинності.
        private void ProcessRenderDirtyTags()
        {
            // Гарячий шлях: ітератор по чанках + відкладене зняття dirty-тега.
            _removeRenderDirtyTagBuffer.Clear();

            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationRenderDirtyTag, VegetationTileLink>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query)
            {
                var links = chunk.GetSpan<VegetationTileLink>();

                foreach (var entityIndex in chunk)
                {
                    ref var link = ref links[entityIndex];
                    Entity entity = chunk.Entity(entityIndex);

                    VegUtilities.SyncTileFromEntity(_site, entity);
                    _site.InvalidateRender(link.Pos);
                    _removeRenderDirtyTagBuffer.Add(entity);
                }
            }

            foreach (var entity in _removeRenderDirtyTagBuffer)
            {
                if (entity.IsAlive() && entity.Has<VegetationRenderDirtyTag>())
                    entity.Remove<VegetationRenderDirtyTag>();
            }
        }
    }
}
