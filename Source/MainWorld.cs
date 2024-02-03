using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Diagnostics;

namespace Origin.Source
{
    public class MainWorld : IDisposable
    {
        public static MainWorld Instance { get; private set; }

        public Site ActiveSite { get; private set; }
        //public SiteRenderer Renderer { get; private set; }

        public int Seed { get; private set; } = 1234;

        private SpriteBatch spriteBatch;

        public MainWorld()
        {
            Instance = this;
            spriteBatch = new SpriteBatch(OriginGame.Instance.GraphicsDevice);

            // 64 128 192 256 320 384
            ActiveSite = new Site(this, new Utils.Point3(256, 256, 128));

            /*SiteGeneratorParameters parameters = SiteBlocksMaker.GetDefaultParameters();
            SiteBlocksMaker.GenerateSite(ActiveSite, parameters, 553);
            ActiveSite.InitPathFinder();*/

            /*var sd = new Sprite[Enum.GetNames(typeof(IsometricDirection)).Length];
            sd[(int)IsometricDirection.NONE] = GlobalResources.GetSpriteByID("tempPawn");
            var entity = ECSworld.Create(new UserControlPawnComponent(),
                new DrawComponent() { Sprites = sd },
                new OnSitePosition() { position = new Utils.Point3(0, 0, 100) });*/
        }

        public void Update(GameTime gameTime)
        {
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