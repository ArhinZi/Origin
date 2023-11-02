using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.ECS;
using Origin.Source.IO;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;

namespace Origin.Source.Tools
{
    public class ToolDig : Tool
    {
        public bool Active = false;
        private Point3 prevPos;
        private Point3 startPos;
        private Point3 start;
        private Point3 end;

        private SpritePositionColor template = new SpritePositionColor()
        {
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SelectionFloor"),
            offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
            color = Color.Red
        };

        public ToolDig(SiteToolController controller) :
            base(controller)
        {
            Name = "ToolDig";
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
                    }
                }
            }
            else if (Active)
            {
                Position = MouseScreenToMap(Camera, m, startPos.Z, Controller.Site, onFloor: true, clip: true);
                if (Position != Point3.Null)
                {
                    if (prevPos != Position)
                    {
                        start = startPos;
                        end = Position;
                        if (end.X < start.X) (start.X, end.X) = (end.X, start.X);
                        if (end.Y < start.Y) (start.Y, end.Y) = (end.Y, start.Y);
                        sprites.Clear();
                        for (int i = start.X; i <= end.X; i++)
                        {
                            for (int j = start.Y; j <= end.Y; j++)
                            {
                                Point3 Pos = new Point3(i, j, start.Z);
                                if (Pos != Position)
                                {
                                    SpritePositionColor spc = template.Clone() as SpritePositionColor;
                                    sprites.Add(spc);
                                    sprites[^1].position = Pos;
                                }
                            }
                        }
                    }
                    if (InputManager.JustPressed("mouse.left"))
                    {
                        sprites.Clear();
                        for (int i = start.X; i <= end.X; i++)
                        {
                            for (int j = start.Y; j <= end.Y; j++)
                            {
                                Controller.Site.RemoveBlock(new Point3(i, j, start.Z));
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
                sprites.Add(template);
                sprites[^1].position = Position;
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
                if (pos.GraterEqualOr(site.Size) ||
                //ignore null
                site.Map[pos.X, pos.Y, pos.Z] == Entity.Null ||
                //ignore air
                site.Map[pos.X, pos.Y, pos.Z] != Entity.Null &&
                !site.Map[pos.X, pos.Y, pos.Z].Has<BaseConstruction>() ||
                //ignore blocks on current level
                site.Map[pos.X, pos.Y, pos.Z] != Entity.Null &&
                site.Map[pos.X, pos.Y, pos.Z].Has<BaseConstruction>() &&
                tlevel == site.CurrentLevel
                )
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