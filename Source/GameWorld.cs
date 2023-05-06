using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;

using Microsoft.Xna.Framework;

using Origin.Source.ECS;
using Origin.Source.GameStates;
using Origin.Source.Generators;
using Origin.Source.IO;

using System;
using System.Collections.Generic;

namespace Origin.Source
{
    public class GameWorld : IDisposable
    {
        public static GameWorld Instance { get; private set; }

        public List<Site> Sites { get; private set; }
        public Site ActiveSite { get; private set; }

        public int Seed { get; private set; } = 1234;

        public Arch.Core.World ECSworld { get; private set; } = Arch.Core.World.Create();

        public ComponentType[] archetypePawn = new ComponentType[] {
                typeof(UserControlPawnComponent),
                typeof(SitePositionComponent),
                typeof(DrawComponent)};

        public GameWorld()
        {
            Instance = this;

            // 64 128 192 256 320 384
            ActiveSite = new Site(this, new Utils.Point3(128, 128, 128));
            /*SiteGeneratorParameters parameters = SiteBlocksMaker.GetDefaultParameters();
            SiteBlocksMaker.GenerateSite(ActiveSite, parameters, Seed);*/
            ActiveSite.CurrentLevel = 20;
            Sites = new List<Site>
            {
                ActiveSite
            };

            ActiveSite.Init();

            var sd = new Sprite[Enum.GetNames(typeof(IsometricDirection)).Length];
            sd[(int)IsometricDirection.NONE] = Sprite.SpriteSet["tempPawn"];
            /*var entity = ECSworld.Create(new UserControlPawnComponent(),
                new SitePositionComponent() { Cell = ActiveSite.Blocks[0, 0, 88] },
                new DrawComponent() { Sprites = sd });*/
        }

        public void Update(GameTime gameTime)
        {
            foreach (var item in Sites)
            {
                item.Update(gameTime);
            }

            if (gameTime.TotalGameTime.Ticks % 10 == 0)
            {
                var query = new QueryDescription().WithAll<UserControlPawnComponent, SitePositionComponent>();
                ECSworld.Query(in query, (in Entity entity) =>
                {
                    var position = entity.Get<SitePositionComponent>();
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
                        entity.Set(new SitePositionComponent() { Cell = sc, DirectionOfView = dir });
                });
            }
        }

        public void Draw(GameTime gameTime)
        {
            ActiveSite.Draw(gameTime);
        }

        public void Dispose()
        {
            foreach (var item in Sites)
            {
                item.Dispose();
            }
        }
    }
}