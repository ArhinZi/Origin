using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Origin.Source.Controller.IO;
using Origin.Source.Resources;

namespace Origin.Source.Model.Map.Tools
{
    public class ToolPlaceLighter : Tool
    {
        private readonly SpritePositionColor template = new()
        {
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SolidWall"),
            offset = new Point(0, 0),
            color = Color.Orange
        };

        public ToolPlaceLighter(SiteToolsComponent controller) : base(controller)
        {
            Name = "ToolPlaceLighter";
            sprites = [];
            RenderLayer = Global.DrawBufferLayer.FrontInteractives;
        }

        public override void Reset()
        {
            SetState(ToolState.Idle);
            DrawDirty = true;
            sprites.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Position = MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site, true);
            if (Position != Point3.Null)
            {
                Position = new Point3(Position.X, Position.Y, Position.Z + 1);
                if (!Position.InBounds(Point3.Zero, Controller.Site.Size))
                    Position = Point3.Null;
            }

            if (Position != Point3.Null && InputManager.JustPressed("mouse.left"))
            {
                var construction = GlobalResources.GetResourceBy(GlobalResources.Constructions, "ID", "Lighter");
                var material = GlobalResources.GetResourceBy(GlobalResources.Materials, "ID", "NONE");
                ExecuteCommand(new PlaceConstructionAreaCommand(Position, Position, construction, material));
            }

            if (Position != Point3.Null)
            {
                if (!DrawDirty)
                {
                    sprites.Clear();
                    DrawDirty = true;
                }

                var sprite = template.Clone() as SpritePositionColor;
                sprite.position = Position;
                sprite.color = Color.Orange * 0.7f;
                sprites.Add(sprite);
            }
            else
            {
                sprites.Clear();
                DrawDirty = true;
            }
        }
    }
}
