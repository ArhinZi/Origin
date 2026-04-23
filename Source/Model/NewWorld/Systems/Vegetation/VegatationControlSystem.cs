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
        // Буфер для оновлення сусідів при перетині порогу видимості (рівень 0 ↔ 1).
        private readonly List<(Point3 Pos, short Delta)> _neighbourUpdateBuffer = [];

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
                // Глобальну валідацію тепер centraлізовано робить VegetationEnvironmentDirtySystem
                // на основі дерті-сигналів середовища, тому тут запускаємо лише ріст.
                ProcessGrowthTick();
                ProcessRenderDirtyTags();
            }
        }

        // Централізоване очищення зв'язку рослинності з тайлом перед знищенням ентіті.
        private void ClearTileVegetationLink(Point3 pos)
        {
            if (_site.Map.TryGet(pos, out Tile oldTile) && oldTile.Exists && oldTile.HasVegetation)
            {
                oldTile.HasVegetation = false;
                oldTile.VegetationEntity = Entity.Null;
                _site.Map[pos] = oldTile;
                _site.InvalidateRender(pos);
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

        // Валідує ентіті рослинності по тегу і видаляє ті, що більше не відповідають умовам.
        private void ProcessValidationTags()
        {
            // Гарячий шлях: ітератор по чанках, structural-зміни виконуємо після проходу.
            _destroyVegetationBuffer.Clear();
            _removeNeedsValidationTagBuffer.Clear();
            _addGrowthDelayBuffer.Clear();
            _removeGrowthDelayBuffer.Clear();

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
                    else if (!VegUtilities.TryGetVegetationFor(_site, link.Pos, tile.Construction, out _))
                    {
                        remove = true;
                    }
                    else if (entity.TryGet(out VegetationGrowthLevel growthLevel) && growthLevel.Value == 0 && !VegUtilities.HasSunlightAt(_site, link.Pos))
                    {
                        // На нульовому рівні без сонця рослинність прибираємо повністю.
                        remove = true;
                    }

                    if (remove)
                    {
                        // Єдиний шлях очищення тайла перед видаленням ентіті.
                        ClearTileVegetationLink(link.Pos);
                        _destroyVegetationBuffer.Add(entity);
                        continue;
                    }

                    // При втраті сонця не знищуємо ентіті: ріст/згасання обробляються в ProcessGrowthTick.
                    short neighbours = VegUtilities.GetNeighboursFor(_site, link.Pos);
                    entity.Set(new VegetationNeighboursComponent { Value = neighbours });

                    if (entity.TryGet(out VegetationGrowthLevel level))
                    {
                        if (level.Value >= 8 && VegUtilities.HasSunlightAt(_site, link.Pos))
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

        // Раз на 60 тіків зменшує/збільшує рівень росту залежно від сонця.
        private void ProcessGrowthTick()
        {
            _removeGrowthDelayBuffer.Clear();
            _addRenderDirtyBuffer.Clear();
            _addGrowthDelayBuffer.Clear();
            _destroyVegetationBuffer.Clear(); // Очищаємо буфер відкладеного видалення для цього тіку росту.
            _neighbourUpdateBuffer.Clear(); // скидаємо буфер оновлення сусідів

            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationGrowthLevel, VegetationNeighboursComponent, VegetationTileLink>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query)
            {
                var levels = chunk.GetSpan<VegetationGrowthLevel>();
                var neighbours = chunk.GetSpan<VegetationNeighboursComponent>();
                var links = chunk.GetSpan<VegetationTileLink>();

                foreach (var entityIndex in chunk)
                {
                    Entity entity = chunk.Entity(entityIndex);
                    ref var level = ref levels[entityIndex];
                    ref var neighbour = ref neighbours[entityIndex];
                    ref var link = ref links[entityIndex];

                    bool hasSunlight = VegUtilities.HasSunlightAt(_site, link.Pos);
                    bool hasDelay = entity.TryGet(out VegetationGrowthDelay delay);

                    if (!hasDelay)
                    {
                        // Якщо рослина вже згасла в 0 і сонця все ще нема — видаляємо ентіті одразу.
                        if (!hasSunlight && level.Value == 0)
                        {
                            // Єдиний шлях очищення тайла перед видаленням ентіті.
                            ClearTileVegetationLink(link.Pos);
                            _destroyVegetationBuffer.Add(entity);
                            continue;
                        }

                        // Додаємо таймер, якщо рослина може змінювати рівень (ріст або згасання).
                        if ((hasSunlight && level.Value < 8) || (!hasSunlight && level.Value > 0))
                        {
                            _addGrowthDelayBuffer.Add((entity, new VegetationGrowthDelay
                            {
                                TicksRemaining = VegUtilities.RollGrowthDelayTicks(_site, link.Pos, neighbour.Value)
                            }));
                        }
                        continue;
                    }

                    delay.TicksRemaining -= GrowthTickInterval;
                    if (delay.TicksRemaining > 0)
                    {
                        entity.Set(delay);
                        continue;
                    }

                    byte prevLevel = level.Value;
                    if (hasSunlight)
                    {
                        if (level.Value < 8)
                            level.Value++;
                    }
                    else
                    {
                        if (level.Value > 0)
                            level.Value--;
                    }

                    if (prevLevel != level.Value)
                    {
                        // Перетини порогу видимості 0↔1 впливають на сусідні коефіцієнти росту.
                        if (prevLevel == 0 && level.Value == 1)
                            _neighbourUpdateBuffer.Add((link.Pos, +1));
                        else if (prevLevel == 1 && level.Value == 0)
                            _neighbourUpdateBuffer.Add((link.Pos, -1));

                        _addRenderDirtyBuffer.Add(entity);
                    }

                    // Якщо після кроку згасання рівень став 0 і сонця нема — прибираємо рослинність повністю.
                    if (!hasSunlight && level.Value == 0)
                    {
                        // Єдиний шлях очищення тайла перед видаленням ентіті.
                        ClearTileVegetationLink(link.Pos);
                        _destroyVegetationBuffer.Add(entity);
                        continue;
                    }

                    bool reachedBoundary = hasSunlight ? level.Value >= 8 : level.Value == 0;
                    if (reachedBoundary)
                    {
                        _removeGrowthDelayBuffer.Add(entity);
                    }
                    else
                    {
                        delay.TicksRemaining = VegUtilities.RollGrowthDelayTicks(_site, link.Pos, neighbour.Value);
                        entity.Set(delay);
                    }

                    VegUtilities.SyncTileFromEntity(_site, entity);
                }
            }

            foreach (var entity in _destroyVegetationBuffer)
            {
                if (entity.IsAlive())
                    _site.ArchWorld.Destroy(entity);
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

            foreach (var entity in _addRenderDirtyBuffer)
            {
                if (entity.IsAlive() && !entity.Has<VegetationRenderDirtyTag>())
                    entity.Add<VegetationRenderDirtyTag>();
            }

            // Оновлюємо кількість сусідів для рослин навколо тих, що перетнули поріг.
            foreach (var (pos, delta) in _neighbourUpdateBuffer)
                VegUtilities.UpdateNeighboursOf(_site, pos, delta);
        }

        // Логіка витоптаності: при тегу Trampled старт/рестарт таймера і опційне зменшення рівня.
        private void ProcessTrampledTags()
        {
            // Гарячий шлях: ітератор по чанках, structural-зміни виконуємо після проходу.
            _removeTrampledTagBuffer.Clear();
            _addRenderDirtyBuffer.Clear();
            _addGrowthDelayBuffer.Clear();
            _neighbourUpdateBuffer.Clear(); // скидаємо буфер оновлення сусідів

            QueryDescription queryDesc = new QueryDescription().WithAll<VegetationTrampledTag, VegetationGrowthLevel, VegetationTileLink>();
            var query = _site.ArchWorld.Query(in queryDesc);

            foreach (ref var chunk in query
            )
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
                    {
                        byte prevLevel = level.Value;
                        level.Value = (byte)Math.Max(0, level.Value - 1);
                        // Перетин порогу 1→0: ця рослина перестала бути видимим сусідом
                        if (prevLevel == 1 && level.Value == 0)
                            _neighbourUpdateBuffer.Add((link.Pos, -1));
                    }

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

            // Оновлюємо кількість сусідів для рослин навколо тих, що перетнули поріг
            foreach (var (pos, delta) in _neighbourUpdateBuffer)
                VegUtilities.UpdateNeighboursOf(_site, pos, delta);
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
