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
            sprite = GlobalResources.GetSpriteByID("SelectionFloor"),
            offset = new Point(0, -Sprite.FLOOR_YOFFSET),
            color = Color.Red
        };

        public ToolDig(SiteToolController controller) :
            base(controller)
        {
            Name = "ToolDig";
            sprites = new List<SpritePositionColor> { };
        }

        public override void Reset()
        {
            Active = false;
            sprites.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Position = MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site);
            if (!Active && Position != new Point3(-1, -1, -1))
            {
                if (sprites.Count == 0) sprites.Add(template);
                else sprites[0] = template.Clone() as SpritePositionColor;
                sprites[0].position = Position;
                if (InputManager.JustPressed("mouse.left"))
                {
                    Active = true;
                    startPos = Position;
                }
            }
            else if (Active)
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
                            SpritePositionColor spc = template.Clone() as SpritePositionColor;
                            sprites.Add(spc);
                            sprites[^1].position = Pos;
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
                            Controller.Site.RemoveBlock(Controller.Site.Blocks[i, j, start.Z]);
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

        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level)
        {
            Vector3 worldPos = OriginGame.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), cam.Projection, cam.Transformation, cam.WorldMatrix);
            worldPos += new Vector3(0, level * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET) + Sprite.FLOOR_YOFFSET, 0);
            // Also works
            //int tileX = (int)Math.Round((worldPos.X / Sprite.TILE_SIZE.X + worldPos.Y / Sprite.TILE_SIZE.Y - 1));
            //int tileY = (int)Math.Round((worldPos.Y / Sprite.TILE_SIZE.Y - worldPos.X / Sprite.TILE_SIZE.X));

            var cellPosX = (worldPos.X / Sprite.TILE_SIZE.X) - 0.5;
            var cellPosY = (worldPos.Y / Sprite.TILE_SIZE.Y) - 0.5;

            Point3 cellPos = new Point3()
            {
                X = (int)Math.Round((cellPosX + cellPosY)),
                Y = (int)Math.Round((cellPosY - cellPosX)),
                Z = level
            };
            return cellPos;
        }

        public static Point3 MouseScreenToMapSurface(Camera2D cam, Point mousePos, int level, Site site)
        {
            for (int i = 0; i < SiteRenderer.ONE_MOMENT_DRAW_LEVELS; i++)
            {
                Point3 pos = MouseScreenToMap(cam, mousePos, level);
                if (pos.LessOr(Point3.Zero))
                    return new Point3(-1, -1, -1);
                if (pos.GraterEqualOr(site.Size) ||
                    site.Blocks[pos.X, pos.Y, pos.Z] != Entity.Null &&
                    !site.Blocks[pos.X, pos.Y, pos.Z].Has<TileStructure>())
                {
                    level--;
                    continue;
                }
                else return pos;
            }
            return new Point3(-1, -1, -1);
        }
    }
}