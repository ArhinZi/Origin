using Microsoft.Xna.Framework;

using Origin.Source.ECS;
using Origin.Source.ECS.Light;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.ECS.Vegetation;
using Origin.Source.Model.Site;

using System;
using System.Collections.Generic;

namespace Origin.Source.Model
{
    public class World : IDisposable
    {
        private TickManager _tickManager;

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

            _tickManager = new TickManager();

            _tickManager.Systems.Add(new UpdateSitePathTickSystem(ActiveSite));
            _tickManager.Systems.Add(new UtilizeVegetationOnConstructionTickSystem(ActiveSite));
            _tickManager.Systems.Add(new UpdateLightTickSystem(ActiveSite));
            _tickManager.Systems.Add(new VegatationControlTickSystem(ActiveSite));

            _tickManager.Init();

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
            _tickManager.Update(gameTime);
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