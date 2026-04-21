using Microsoft.Xna.Framework;
using Origin.Source.Model.Generators;
using Origin.Source.Model.NewWorld.Systems.Fluid;
using Origin.Source.Model.NewWorld.Systems.Light;
using Origin.Source.Model.NewWorld.Systems.Pathfinding;
using Origin.Source.Model.NewWorld.Systems.Render;
using Origin.Source.Model.NewWorld.Systems.Vegetation;
using Origin.Source.Save;
using System;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld
{
    public class World : IDisposable
    {
        public WorldTickManager TimeManager;

        private SaveGameEntity sge = null;

        public SiteGenerationSettings GenerationSettings { get; private set; } = new SiteGenerationSettings();

        public static void Load(World world, string name, int seed, SaveGameEntity sge, List<Site> sites)
        {
            world.Name = name;
            world.sge = sge;
            world.Sites = sites;
            world.Random = new Random(seed);
            world.GenerationSettings = new SiteGenerationSettings();
        }

        public string Name { get; private set; } = "Lost fields";
        public int Seed { get; private set; } = 1234;
        public Random Random { get; private set; }

        public Site ActiveSite { get; private set; }
        public List<Site> Sites { get; private set; } = [];

        public World()
        {
        }

        public void NewInitialize(bool createRenderAndTools = true, Action<float, string> progress = null, Point3? siteSize = null, int? generationSeed = null, SiteGenerationSettings generationSettings = null)
        {
            Seed = generationSeed ?? Seed;
            Random = new Random(Seed);
            GenerationSettings = generationSettings ?? new SiteGenerationSettings();

            if (SaveGameEntity.Saves.ContainsKey(Name))
                sge = SaveGameEntity.Saves[Name];
            else
                sge = new SaveGameEntity(this.Name);

            Point3 size = siteSize ?? new Point3(64, 64, 128);
            ActiveSite = new Site(this, size, Sites.Count, createRenderAndTools, progress, Seed, GenerationSettings);
            Sites.Add(ActiveSite);
        }

        public void PostInitialize(bool load = false)
        {
            PreparePostInitialize(load);

            if (!load)
            {
                TimeManager.SystemsManager.Init();
            }
            else
            {
                TimeManager.SystemsManager.LoadInit();
            }
        }

        public void PreparePostInitialize(bool load = false)
        {
            if (ActiveSite == null)
                ActiveSite = Sites[0];

            TimeManager = new WorldTickManager(this);

            InitSystemManager(load, false);

            foreach (var site in Sites)
            {
                site.PostInit();
            }
        }

        public bool InitializeNextSystem(bool load = false)
        {
            return TimeManager.SystemsManager.InitNext(load);
        }

        public int InitializedSystemsCount => TimeManager?.SystemsManager.InitializedCount ?? 0;
        public int SystemsCount => TimeManager?.SystemsManager.Systems.Count ?? 0;
        public string PendingInitSystemName => TimeManager?.SystemsManager.PendingInitSystemName ?? string.Empty;

        private void InitSystemManager(bool load = false, bool initNow = true)
        {
            var SystemManager = TimeManager.SystemsManager;

            SystemManager.Systems.Add(new UpdateVegsOnConstructionRemovedSystem(ActiveSite));
            SystemManager.Systems.Add(new UpdateVegsOnConstructionPlacedSystem(ActiveSite));

            // Світлові системи мають оновити буфери перед валідацією рослинності по сонцю.
            SystemManager.Systems.Add(new SystemUpdateSunlight(ActiveSite));
            SystemManager.Systems.Add(new SystemUpdateArtificialLight(ActiveSite));

            // Спеціальна ECS-система, що ставить теги перевірки рослинності за змінами світла/рельєфу.
            SystemManager.Systems.Add(new VegetationEnvironmentDirtySystem(ActiveSite));
            // Основна ECS-система рослинності (валідація/ріст/витоптаність/рендер-дерті).
            SystemManager.Systems.Add(new VegatationControlSystem(ActiveSite));

            SystemManager.Systems.Add(new SystemUpdateFluids(ActiveSite));

            SystemManager.Systems.Add(new UpdateSitePathSystem(ActiveSite));

            SystemManager.Systems.Add(new SystemUpdateRenderTiles(ActiveSite));

            if (initNow)
            {
                if (!load)
                {
                    SystemManager.Init();
                }
                else
                {
                    SystemManager.LoadInit();
                }
            }
        }

        public void Save()
        {
            //if (sge == null)
            {
                sge.LastSaveTime = DateTime.Now;
                //sge.Texture = ActiveSite.DrawComponent.RenderTarget2D;
                sge.Save(this);
            }
        }

        public void Update(GameTime gameTime)
        {
            TimeManager.Update(gameTime);
            ActiveSite.Update(gameTime);

            /*if (gameTime.TotalGameTime.Ticks % 10 == 0)
            {
                var query = new QueryDescription().WithAll<UserControlPawnComponent, OnSitePosition>();
                ECSworld.Query(in query, (in Entity entity) =>
                {
                    var position = entity.Get<OnSitePosition>();
                    SiteCell sc = null;
                    IsometricDirection dir = IsometricDirection.NONE;
                    if (InputManager.JustPressedAndHoldDelayed("manual.tl"))
                    {
                        sc = position.Cell.GetNextCellByDirection(IsometricDirection.TL);
                        dir = IsometricDirection.TL;
                    }
                    if (InputManager.JustPressedAndHoldDelayed("manual.tr"))
                    {
                        sc = position.Cell.GetNextCellByDirection(IsometricDirection.TR);
                        dir = IsometricDirection.TR;
                    }
                    if (InputManager.JustPressedAndHoldDelayed("manual.bl"))
                    {
                        sc = position.Cell.GetNextCellByDirection(IsometricDirection.BL);
                        dir = IsometricDirection.BL;
                    }
                    if (InputManager.JustPressedAndHoldDelayed("manual.br"))
                    {
                        sc = position.Cell.GetNextCellByDirection(IsometricDirection.BR);
                        dir = IsometricDirection.BR;
                    }
                    if (sc != null)
                        entity.Set(new OnSitePosition() { Cell = sc, DirectionOfView = dir });
                });
            }*/

            //Renderer.Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            TimeManager.Draw(gameTime);
            ActiveSite.Draw(gameTime);
            //Renderer.Draw(gameTime);
        }

        public void Dispose()
        {
            if (Sites != null)
            {
                foreach (var site in Sites)
                {
                    site?.Dispose();
                }
                Sites.Clear();
            }

            ActiveSite = null;
            TimeManager = null;
        }
    }
}