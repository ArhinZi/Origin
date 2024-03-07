﻿using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Controller.IO;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;

namespace Origin.Source.Model.Site.Tools
{
    public class ToolDig : Tool
    {
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

        public ToolDig(SiteToolsComponent controller) :
            base(controller)
        {
            Name = "ToolDig";
            sprites = new List<SpritePositionColor> { };
            RenderLayer = Global.DrawBufferLayer.FrontInteractives;
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

            //sprites.Clear();
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
                        DrawDirty = true;
                        start = startPos;
                        end = prevPos = Position;
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
                                Controller.Site.RemoveConstruction(new Point3(i, j, start.Z));
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

            if (Position != Point3.Null && (DrawDirty || !Active))
            {
                if (!DrawDirty)
                {
                    sprites.Clear();
                    DrawDirty = true;
                }
                sprites.Add(template);
                sprites[^1].position = Position;
                if (Active) sprites[^1].color = Color.Blue;
                else sprites[^1].color = Color.Red;
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
    }
}