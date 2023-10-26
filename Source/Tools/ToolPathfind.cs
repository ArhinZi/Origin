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
using Origin.Source.Resources;

namespace Origin.Source.Tools
{
    public class ToolPathfind : Tool
    {
        public bool Active = false;

        public Point3 start = Point3.Null;
        public Point3 end = Point3.Null;

        public List<Point3> LastPath = null;

        private SpritePositionColor template = new SpritePositionColor()
        {
            sprite = GlobalResources.GetSpriteByID("SolidSelectionWall"),
            offset = new Point(0, 0),
            color = Color.Blue
        };

        public ToolPathfind(SiteToolController controller) :
            base(controller)
        {
            Name = "ToolPathfind";
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
            Point m = new Point(InputManager.MouseX, InputManager.MouseY);
            Point3 pos = Position = WorldUtils.MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site);

            sprites.Clear();

            if (pos.X < 0 || pos.X >= Controller.Site.Size.X || pos.Y < 0 || pos.Y >= Controller.Site.Size.Y)
            {
                Position = Point3.Zero;
            }
            else
            {
                var SelectedBlock = Controller.Site.Blocks[pos.X, pos.Y, pos.Z];
                if (SelectedBlock != Arch.Core.Entity.Null && SelectedBlock.Has<TilePathAble>())
                    Position = pos;
                else
                    Position = Point3.Null;
            }

            /*if (InputManager.JustPressed("mouse.right") && Position != Point3.Null)
            {
                start = Point3.Null;
                LastPath = null;
            }
            if (InputManager.JustPressed("mouse.left") && Position != Point3.Null)
            {
                if (start == Point3.Null)
                {
                    start = pos;
                }
                else
                {
                    end = pos;
                    LastPath = MainWorld.Instance.ActiveSite.FindPath(start, end);
                    LastPath.Sort();
                }
            }*/
            if (InputManager.JustPressed("mouse.right") && Position != Point3.Null)
            {
                start = Point3.Null;
                LastPath = null;
            }
            if (InputManager.JustPressed("mouse.left") && Position != Point3.Null)
            {
                start = pos;
            }
            if (start != Point3.Null && pos != Point3.Null)
            {
                end = pos;
                LastPath = MainWorld.Instance.ActiveSite.FindPath(start, end);
                if (LastPath != null)
                    LastPath.Sort();
            }

            if (Position != Point3.Null)
            {
                sprites.Add(template.Clone() as SpritePositionColor);
                sprites[^1].position = Position;
                sprites[^1].color = new Color(255, 0, 0, 255);
            }
            if (LastPath != null)
            {
                if (LastPath != null)
                    foreach (var Pos in LastPath)
                    {
                        SpritePositionColor spc = template.Clone() as SpritePositionColor;
                        sprites.Add(spc);
                        sprites[^1].position = Pos;
                    }
            }

            PrevPosition = Position;
        }
    }
}