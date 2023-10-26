using Arch.Core.Extensions;
using Arch.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.ECS;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Origin.Source.IO;
using System.Security.Policy;
using Roy_T.AStar.Primitives;
using Origin.Source.Resources;

namespace Origin.Source.Tools
{
    public class ToolPlaceDirt : Tool
    {
        public bool Active = false;
        private Point3 prevPos;
        private Point3 startPos;
        private int currentLevel;
        private Point3 start;
        private Point3 end;
        private int CurrSiteLevel;

        private SpritePositionColor template = new SpritePositionColor()
        {
            sprite = GlobalResources.GetSpriteByID("DirtWall"),
            offset = new Point(0, 0),
            color = Color.Blue
        };

        public ToolPlaceDirt(SiteToolController controller) :
            base(controller)
        {
            Name = "ToolPlaceDirt";
            sprites = new List<SpritePositionColor> { };
            RenderLayer = VertexBufferLayer.FrontInteractives;
        }

        public override void Reset()
        {
            Active = false;
            sprites.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            sprites.Clear();

            if (!Active)
            {
                Position = MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site, true);
                if (Position != Point3.Null)
                {
                    if (InputManager.JustPressed("mouse.left"))
                    {
                        Active = true;
                        startPos = Position;

                        currentLevel = startPos.Z;
                        CurrSiteLevel = Controller.Site.CurrentLevel;
                    }
                }
            }
            else if (Active)
            {
                if (CurrSiteLevel != Controller.Site.CurrentLevel)
                {
                    int mod = Controller.Site.CurrentLevel - CurrSiteLevel;
                    CurrSiteLevel = Controller.Site.CurrentLevel;
                    currentLevel += mod;
                }
                Position = MouseScreenToMap(Camera, m, currentLevel, Controller.Site, onFloor: true, clip: true);
                if (Position != Point3.Null)
                {
                    if (prevPos != Position)
                    {
                        start = startPos;
                        end = Position;
                        if (end.X < start.X) (start.X, end.X) = (end.X, start.X);
                        if (end.Y < start.Y) (start.Y, end.Y) = (end.Y, start.Y);
                        for (int z = start.Z; z <= end.Z; z++)
                        {
                            for (int x = start.X; x <= end.X; x++)
                            {
                                for (int y = start.Y; y <= end.Y; y++)
                                {
                                    Point3 Pos = new Point3(x, y, z);
                                    if (Pos != Position)
                                    {
                                        sprites.Add(new SpritePositionColor()
                                        {
                                            sprite = GlobalResources.GetSpriteByID("DirtWall"),
                                            color = Color.White * 0.5f,
                                            offset = new Point(0, 0),
                                            position = Pos
                                        });
                                        sprites.Add(new SpritePositionColor()
                                        {
                                            sprite = GlobalResources.GetSpriteByID("DirtFloor"),
                                            color = Color.White * 0.5f,
                                            offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
                                            position = Pos
                                        });
                                    }
                                }
                            }
                        }
                    }
                    if (InputManager.JustPressed("mouse.left"))
                    {
                        sprites.Clear();
                        for (int z = start.Z; z <= end.Z; z++)
                        {
                            for (int x = start.X; x <= end.X; x++)
                            {
                                for (int y = start.Y; y <= end.Y; y++)
                                {
                                    Point3 pos = new Point3(x, y, z);
                                    var ent = Controller.Site.Blocks[x, y, z];
                                    Controller.Site.ECSWorld.Destroy(ent);
                                    ent = Controller.Site.ECSWorld.Create(new OnSitePosition(new Point3(pos.X, pos.Y, pos.Z)), new TileVisibility());
                                    Controller.Site.ECSWorld.Add(ent,
                                            new TileStructure()
                                            {
                                                WallMaterial = GlobalResources.GetTerrainMaterialByID("DIRT"),
                                                FloorMaterial = GlobalResources.GetTerrainMaterialByID("DIRT"),
                                            });
                                    Controller.Site.ECSWorld.Add<WaitingForUpdateTileLogic, WaitingForUpdateTileRender>(ent);
                                    Controller.Site.Blocks[x, y, z] = ent;
                                }
                            }
                        }

                        Active = false;
                        startPos = Position;
                    }
                    if (InputManager.JustPressed("mouse.right"))
                    {
                        Reset();
                    }
                }
            }
            if (Position != Point3.Null)
            {
                sprites.Add(new SpritePositionColor()
                {
                    sprite = GlobalResources.GetSpriteByID("DirtWall"),
                    color = Color.White * 0.5f,
                    offset = new Point(0, 0),
                    position = Position
                });
                sprites.Add(new SpritePositionColor()
                {
                    sprite = GlobalResources.GetSpriteByID("DirtFloor"),
                    color = Color.White * 0.5f,
                    offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
                    position = Position
                });
                for (int i = Math.Min(Position.Z + 1, Controller.Site.CurrentLevel); i <= Controller.Site.CurrentLevel; i++)
                {
                    sprites.Add(new SpritePositionColor()
                    {
                        sprite = GlobalResources.GetSpriteByID("SelectionWall"),
                        color = new Color(25, 25, 25, 200),
                        position = new Point3(Position.X, Position.Y, i)
                    });
                }
            }
        }

        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level, Site site,
            bool onFloor = false,
            bool clip = false)
        {
            Vector3 worldPos = OriginGame.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), cam.Projection, cam.Transformation, cam.WorldMatrix);
            worldPos += new Vector3(0, level * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset) +
                (onFloor ? GlobalResources.Settings.FloorYoffset : 0)
                , 0);

            var cellPosX = (worldPos.X / GlobalResources.Settings.TileSize.X) - 0.5;
            var cellPosY = (worldPos.Y / GlobalResources.Settings.TileSize.Y) - 0.5;

            Point3 cellPos = new Point3()
            {
                X = (int)Math.Round((cellPosX + cellPosY)),
                Y = (int)Math.Round((cellPosY - cellPosX)),
                Z = level
            };
            if (clip && (cellPos.LessOr(Point3.Zero) || cellPos.GraterEqualOr(site.Size)))
                return Point3.Null;
            return cellPos;
        }

        public static Point3 MouseScreenToMapSurface(Camera2D cam, Point mousePos, int level, Site site,
            bool onFloor = false)
        {
            int tlevel = level;
            for (int i = 0; i < SiteRenderer.ONE_MOMENT_DRAW_LEVELS; i++)
            {
                Point3 pos = MouseScreenToMap(cam, mousePos, tlevel, site, onFloor);
                if (pos.LessOr(Point3.Zero))
                    return Point3.Null;
                if (pos.GraterEqualOr(site.Size) || pos.Z - 1 >= 0 &&
                     site.Blocks[pos.X, pos.Y, pos.Z - 1] != Entity.Null &&
                     !site.Blocks[pos.X, pos.Y, pos.Z - 1].Has<TileStructure>())
                {
                    tlevel--;
                    continue;
                }
                else return pos;
            }

            return Point3.Null;
        }
    }
}