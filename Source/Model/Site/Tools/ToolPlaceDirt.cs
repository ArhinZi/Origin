using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Origin.Source.Controller.IO;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;
using Origin.Source.Resources;
using System;
using System.Collections.Generic;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Model.Site.Tools
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

        private Sprite Wall;
        private Sprite Floor;

        private SpritePositionColor template = new SpritePositionColor()
        {
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "DirtWall"),
            offset = new Point(0, 0),
            color = Color.Blue
        };

        public ToolPlaceDirt(SiteToolsComponent controller) :
            base(controller)
        {
            Name = "ToolPlaceDirt";
            sprites = new List<SpritePositionColor> { };
            RenderLayer = DrawBufferLayer.FrontInteractives;
        }

        public override void Reset()
        {
            Active = false;
            DrawDirty = true;
            sprites.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;

            Wall = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "DirtWall");
            Floor = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "DirtFloor");

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
                        DrawDirty = true;
                        sprites.Clear();
                        start = startPos;
                        end = prevPos = Position;
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
                                            sprite = Wall,
                                            color = Color.White * 0.5f,
                                            offset = new Point(0, 0),
                                            position = Pos
                                        });
                                        sprites.Add(new SpritePositionColor()
                                        {
                                            sprite = Floor,
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
                        DrawDirty = true;
                        sprites.Clear();
                        Construction construction = GlobalResources.GetResourceBy(GlobalResources.Constructions, "ID", "SoilWallFloor");
                        Material mat = GlobalResources.GetResourceBy(GlobalResources.Materials, "ID", "Dirt");
                        for (int z = start.Z; z <= end.Z; z++)
                        {
                            for (int x = start.X; x <= end.X; x++)
                            {
                                for (int y = start.Y; y <= end.Y; y++)
                                {
                                    Point3 pos = new Point3(x, y, z);

                                    Controller.Site.PlaceConstruction(pos, construction, mat);
                                }

                                Active = false;
                                startPos = Position;
                            }
                        }
                    }
                    if (InputManager.JustPressed("mouse.right"))
                    {
                        Reset();
                    }
                }
            }
            if (Position != Point3.Null && (DrawDirty || !Active))
            {
                if (!DrawDirty)
                {
                    sprites.Clear();
                    DrawDirty = true;
                }
                sprites.Add(new SpritePositionColor()
                {
                    sprite = Wall,
                    color = Color.White * 0.5f,
                    offset = new Point(0, 0),
                    position = Position
                });
                sprites.Add(new SpritePositionColor()
                {
                    sprite = Floor,
                    color = Color.White * 0.5f,
                    offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
                    position = Position
                });
                for (int i = Math.Min(Position.Z + 1, Controller.Site.CurrentLevel); i <= Controller.Site.CurrentLevel; i++)
                {
                    sprites.Add(new SpritePositionColor()
                    {
                        sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SelectionWall"),
                        color = new Color(25, 25, 25, 200),
                        position = new Point3(Position.X, Position.Y, i)
                    });
                }
            }
            else if (!Active)
            {
                sprites.Clear();
                DrawDirty = true;
            }
        }

        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level, Site site,
                bool onFloor = false,
                bool clip = false)
        {
            Vector3 worldPos = GraphicsDevice.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), cam.Projection, cam.Transformation, cam.WorldMatrix);
            worldPos += new Vector3(0, level * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset) +
                (onFloor ? GlobalResources.Settings.FloorYoffset : 0)
                , 0);

            var cellPosX = worldPos.X / GlobalResources.Settings.TileSize.X - 0.5;
            var cellPosY = worldPos.Y / GlobalResources.Settings.TileSize.Y - 0.5;

            Point3 cellPos = new Point3()
            {
                X = (int)Math.Round(cellPosX + cellPosY),
                Y = (int)Math.Round(cellPosY - cellPosX),
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
            for (int i = 0; i < ONE_MOMENT_DRAW_LEVELS; i++)
            {
                Point3 pos = MouseScreenToMap(cam, mousePos, tlevel, site, onFloor);

                Entity tmp;
                if (pos.InBounds(Point3.Zero, site.Size) &&
                    site.Map.TryGet(pos, out tmp) && tmp != Entity.Null && !tmp.Has<BaseConstruction>() &&
                    site.Map.TryGet(pos - new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && tmp.Has<BaseConstruction>())
                    return pos;
                else if (site.Map.TryGet(pos, out tmp) && tmp == Entity.Null)
                    return Point3.Null;
                else
                {
                    tlevel--;
                    continue;
                }
            }

            return Point3.Null;
        }
    }
}