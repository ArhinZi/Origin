using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Origin.Source.Controller.IO;
using Origin.Source.Resources;

using System;

namespace Origin.Source.Model.Map.Tools
{
    public class ToolDig : Tool
    {
        private Point3 prevPos;
        private Point3 startPos;
        private Point3 start;
        private Point3 end;

        private SpritePositionColor template = new()
        {
            sprite = GlobalResources.Sprites["SelectionFloor"],
            offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
            color = Color.Red
        };

        public ToolDig(SiteToolsComponent controller) :
            base(controller)
        {
            Name = "ToolDig";
            sprites = [];
            RenderLayer = Global.DrawBufferLayer.FrontInteractives;
        }

        public override void Reset()
        {
            SetState(ToolState.Idle);
            DrawDirty = true;
            sprites.Clear();
            prevPos = Point3.Null;
            startPos = Point3.Null;
        }

        private void NormalizeSelection()
        {
            start = startPos;
            end = prevPos;
            if (end.X < start.X) (start.X, end.X) = (end.X, start.X);
            if (end.Y < start.Y) (start.Y, end.Y) = (end.Y, start.Y);
        }

        private void RebuildSelectionPreview()
        {
            DrawDirty = true;
            sprites.Clear();
            NormalizeSelection();

            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    Point3 pos = new(x, y, start.Z);
                    if (pos != Position)
                    {
                        SpritePositionColor spc = template.Clone() as SpritePositionColor;
                        sprites.Add(spc);
                        sprites[^1].position = pos;
                    }
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;

            if (State == ToolState.Idle)
            {
                Position = MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site, true);
                if (Position != Point3.Null && InputManager.JustPressed("mouse.left"))
                {
                    startPos = Position;
                    prevPos = Position;
                    SetState(ToolState.Selecting);
                }
            }
            else if (State == ToolState.Selecting)
            {
                Position = MouseScreenToMap(Camera, m, startPos.Z, Controller.Site, onFloor: true, clip: true);
                if (Position != Point3.Null)
                {
                    if (prevPos != Position)
                    {
                        prevPos = Position;
                        RebuildSelectionPreview();
                    }

                    if (InputManager.JustPressed("mouse.left"))
                    {
                        NormalizeSelection();
                        ExecuteCommand(new RemoveConstructionAreaCommand(start, end));
                        SetState(ToolState.Idle);
                        startPos = Position;
                        DrawDirty = true;
                        sprites.Clear();
                    }

                    if (InputManager.JustPressed("mouse.right"))
                    {
                        Reset();
                    }
                }
            }

            if (Position != Point3.Null && (DrawDirty || State == ToolState.Idle))
            {
                if (!DrawDirty)
                {
                    sprites.Clear();
                    DrawDirty = true;
                }
                sprites.Add(template);
                sprites[^1].position = Position;
                sprites[^1].color = State == ToolState.Selecting ? Color.Blue : Color.Red;

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
            else if (State == ToolState.Idle)
            {
                sprites.Clear();
                DrawDirty = true;
            }
        }
    }
}