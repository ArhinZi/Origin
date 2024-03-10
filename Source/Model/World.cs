using Microsoft.Xna.Framework;

using Origin.Source.ECS;
using Origin.Source.ECS.Light;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.ECS.Vegetation;
using Origin.Source.Model.Site;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Origin.Source.Model
{
    public class World : IDisposable
    {
        public SystemGroupsManager SystemManager;
        public WorldTimeManager TimeManager;

        //private SaveGameEntity sge = null;

        public string Name { get; private set; } = "Lost fields";
        public int Seed { get; private set; } = 1234;
        public Random Random { get; private set; }

        public Site.Site ActiveSite { get; private set; }
        public List<Site.Site> Sites { get; private set; } = new();

        public World()
        {
            //Instance = this;
            Random = new Random(Seed);

            // 64 128 192 256 320 384
            ActiveSite = new Site.Site(this, new Point3(256, 256, 128));
            Sites.Add(ActiveSite);

            TimeManager = new WorldTimeManager();
            TimeManager.SetGameSpeed(0.5f);

            SystemManager = new SystemGroupsManager(TimeManager);

            SystemManager.Groups.Add(new("Pathfinder", new Arch.System.ISystem<ulong>[] {
                new UpdateSitePathTickSystem(ActiveSite)
            }));
            SystemManager.Groups.Add(new("Vegetation", new Arch.System.ISystem<ulong>[] {
                new UpdateVegsOnConstructionRemovedTickSystem(ActiveSite),
                new UpdateVegsOnConstructionPlacedTickSystem(ActiveSite),
                new VegatationControlTickSystem(ActiveSite)
            }));
            SystemManager.Groups.Add(new("SunLight", new Arch.System.ISystem<ulong>[] {
                new UpdateLightTickSystem(ActiveSite)
            }));

            SystemManager.Init();

            ActiveSite.PostInit();

            /*SiteGeneratorParameters parameters = SiteBlocksMaker.GetDefaultParameters();
            SiteBlocksMaker.GenerateSite(ActiveSite, parameters, 553);
            ActiveSite.InitPathFinder();*/

            /*var sd = new Sprite[Enum.GetNames(typeof(IsometricDirection)).Length];
            sd[(int)IsometricDirection.NONE] = GlobalResources.GetSpriteByID("tempPawn");
            var entity = ECSworld.Create(new UserControlPawnComponent(),
                new DrawComponent() { Sprites = sd },
                new OnSitePosition() { position = new Utils.Point3(0, 0, 100) });*/
        }

        public void Init()
        {
            //Renderer = new SiteRenderer(ActiveSite, Global.GraphicsDevice);
        }

        public void Save()
        {
            /*if (sge == null)
            {
                sge = new SaveGameEntity(Name);
                sge.LastSaveTime = DateTime.Now;
                sge.Texture = ActiveSite.DrawControl.RenderTarget2D;
                sge.Save(this);
            }*/
        }

        public void Update(GameTime gameTime)
        {
            TimeManager.Update(gameTime);
            SystemManager.Update(gameTime);
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
            ActiveSite.Draw(gameTime);
            //Renderer.Draw(gameTime);
        }

        public void Dispose()
        {
            ActiveSite.Dispose();
        }
    }
}