using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using Origin.Source.ECS;
using Origin.Source.IO;
using Origin.Source.Pathfind;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System.Collections.Generic;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Tools
{
    public class ToolPathfind : Tool
    {
        public bool Active = false;

        public Point3 start = Point3.Null;
        public Point3 end = Point3.Null;

        public PathInfo LastPath = null;

        private SpritePositionColor template = new SpritePositionColor()
        {
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SolidSelectionWall"),
            offset = new Point(0, 0)
        };

        private Color baseColor = new Color(255, 0, 0);
        private Color pathColor = new Color(0, 0, 255);
        private Color debugColor = Color.Yellow;

        public ToolPathfind(SiteToolController controller) :
            base(controller)
        {
            Name = "ToolPathfind";
            sprites = new List<SpritePositionColor> { };
            RenderLayer = DrawBufferLayer.FrontInteractives;
        }

        public override void Reset()
        {
            Active = false;
            start = Point3.Null;
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
                var SelectedBlock = Controller.Site.Map[pos.X, pos.Y, pos.Z];
                if (SelectedBlock != Arch.Core.Entity.Null && SelectedBlock.Has<TilePathAble>())
                    Position = pos;
                else
                    Position = Point3.Null;
            }
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
                LastPath = MainWorld.Instance.ActiveSite.Pathfinder.FindPath(start, end, true);
                if (LastPath != null)
                {
                    LastPath.path.Sort();
                    //LastPath.path.Reverse();
                    LastPath.visited.Sort();
                }
            }

            if (Position != Point3.Null)
            {
                sprites.Add(template.Clone() as SpritePositionColor);
                sprites[^1].position = Position;
                sprites[^1].color = baseColor;
            }
            if (LastPath != null)
            {
                foreach (var Pos in LastPath.visited)
                {
                    SpritePositionColor spc = template.Clone() as SpritePositionColor;
                    sprites.Add(spc);
                    sprites[^1].position = Pos;
                    sprites[^1].color = debugColor;
                }
                foreach (var Pos in LastPath.path)
                {
                    SpritePositionColor spc = template.Clone() as SpritePositionColor;
                    sprites.Add(spc);
                    sprites[^1].position = Pos;
                    sprites[^1].color = pathColor;
                }
            }

            PrevPosition = Position;
        }
    }
}