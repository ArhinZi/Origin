using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Origin.Source.Controller.IO;
using Origin.Source.Resources;

using System;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Model.Map.Tools
{
    public class ToolPlaceDirt : Tool
    {
        private Point3 prevPos;
        private Point3 startPos;
        private int currentLevel;
        private Point3 start;
        private Point3 end;
        private int CurrSiteLevel;

        private Sprite Wall;
        private Sprite Floor;

        private SpritePositionColor template = new()
        {
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "WallSoil"),
            offset = new Point(0, 0),
            color = Color.Blue
        };

        public ToolPlaceDirt(SiteToolsComponent controller) :
            base(controller)
        {
            Name = "ToolPlaceDirt";
            sprites = [];
            RenderLayer = DrawBufferLayer.FrontInteractives;
        }

        public override void Reset()
        {
            SetState(ToolState.Idle);
            DrawDirty = true;
            sprites.Clear();
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

            for (int z = start.Z; z <= end.Z; z++)
            {
                for (int x = start.X; x <= end.X; x++)
                {
                    for (int y = start.Y; y <= end.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        if (pos != Position)
                        {
                            sprites.Add(new SpritePositionColor()
                            {
                                sprite = Wall,
                                color = Color.White * 0.5f,
                                offset = new Point(0, 0),
                                position = pos
                            });
                            sprites.Add(new SpritePositionColor()
                            {
                                sprite = Floor,
                                color = Color.White * 0.5f,
                                offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
                                position = pos
                            });
                        }
                    }
                }
            }
        }

        private bool TryMoveAboveGround(ref Point3 pos)
        {
            if (pos == Point3.Null) return false;
            int z = pos.Z + 1;
            if (z >= Controller.Site.Size.Z)
                return false;

            pos = new Point3(pos.X, pos.Y, z);
            return true;
        }

        public override void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;

            Wall = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "WallSoil");
            Floor = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "FloorSoil");

            if (State == ToolState.Idle)
            {
                Position = MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site, true);
                if (!TryMoveAboveGround(ref Position))
                    Position = Point3.Null;

                if (Position != Point3.Null && InputManager.JustPressed("mouse.left"))
                {
                    SetState(ToolState.Selecting);
                    startPos = Position;
                    prevPos = Position;

                    currentLevel = startPos.Z;
                    CurrSiteLevel = Controller.Site.CurrentLevel;
                }
            }
            else if (State == ToolState.Selecting)
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
                        prevPos = Position;
                        RebuildSelectionPreview();
                    }

                    if (InputManager.JustPressed("mouse.left"))
                    {
                        DrawDirty = true;
                        sprites.Clear();
                        NormalizeSelection();

                        Construction construction = GlobalResources.GetResourceBy(GlobalResources.Constructions, "ID", "SoilWallFloor");
                        Material mat = GlobalResources.GetResourceBy(GlobalResources.Materials, "ID", "DIRT");
                        ExecuteCommand(new PlaceConstructionAreaCommand(start, end, construction, mat));

                        SetState(ToolState.Idle);
                        startPos = Position;
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
            else if (State == ToolState.Idle)
            {
                sprites.Clear();
                DrawDirty = true;
            }
        }
    }
}