using Microsoft.Xna.Framework;
using Origin.Source.ECS.BaseSystems;
using Origin.Source.ECS.Fluid;
using Origin.Source.ECS.Light;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.ECS.Render;
using Origin.Source.Model.Map;
using Origin.Source.Model.NewWorld.Systems.Vegetation;
using Origin.Source.Save;
using System;
using System.Collections.Generic;

namespace Origin.Source.Model
{
    public class World : IDisposable
    {
        public WorldTickManager TimeManager;

        private SaveGameEntity sge = null;

        public static void Load(World world, string name, int seed, SaveGameEntity sge, List<Map.Site> sites)
        {
            world.Name = name;
            world.sge = sge;
            world.Sites = sites;
            world.Random = new Random(seed);
        }

        public string Name { get; private set; } = "Lost fields";
        public int Seed { get; private set; } = 1234;
        public Random Random { get; private set; }

        public Map.Site ActiveSite { get; private set; }
        public List<Map.Site> Sites { get; private set; } = [];

        public World()
        {
        }

        public void NewInitialize()
        {
            Random = new Random(1234);

            if (SaveGameEntity.Saves.ContainsKey(Name))
                sge = SaveGameEntity.Saves[Name];
            else
                sge = new SaveGameEntity(this.Name);

            // 64 128 192 256 320 384
            ActiveSite = new Site(this, new Point3(256, 256, 128), Sites.Count);
            Sites.Add(ActiveSite);
        }

        public void PostInitialize(bool load = false)
        {
            if (ActiveSite == null)
                ActiveSite = Sites[0];

            TimeManager = new WorldTickManager(this);

            InitSystemManager(load);

            foreach (var site in Sites)
            {
                site.PostInit();
            }
        }

        private void InitSystemManager(bool load = false)
        {
            var SystemManager = TimeManager.SystemsManager;

            SystemManager.Systems.Add(new UpdateVegsOnConstructionRemovedSystem(ActiveSite));
            SystemManager.Systems.Add(new UpdateVegsOnConstructionPlacedSystem(ActiveSite));
            SystemManager.Systems.Add(new VegatationControlSystem(ActiveSite));

            SystemManager.Systems.Add(new SystemUpdateLight(ActiveSite));

            SystemManager.Systems.Add(new SystemUpdateFluidsOnConstructionPlaced(ActiveSite));
            SystemManager.Systems.Add(new SystemUpdateFluidsOnConstructionRemoved(ActiveSite));
            SystemManager.Systems.Add(new SystemUpdateFluids(ActiveSite));

            SystemManager.Systems.Add(new UpdateSitePathSystem(ActiveSite));

            SystemManager.Systems.Add(new SystemClearEvents(ActiveSite));

            SystemManager.Systems.Add(new SystemUpdateRenderTiles(ActiveSite));

            if (!load)
            {
                SystemManager.Init();
            }
            else
            {
                SystemManager.LoadInit();
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
            ActiveSite.Dispose();
        }
    }
}