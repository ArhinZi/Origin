using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Controller.IO;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Fluid;
using Origin.Source.ECS.Render;
using Origin.Source.Model.Site;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Model.Site.Tools
{
    public class ToolPlaceWater : Tool
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
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "DirtWall"),
            offset = new Point(0, 0),
            color = Color.Blue
        };

        public ToolPlaceWater(SiteToolsComponent controller) :
            base(controller)
        {
            Name = "ToolPlaceWater";
            sprites = [];
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

            Wall = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SolidWall");

            if (!Active)
            {
                Position = MouseScreenToMap(Camera, m, Controller.Site.CurrentLevel - 2, Controller.Site, true, true);
                if (Position != Point3.Null)
                {
                    if (InputManager.JustPressed("mouse.left"))
                    {
                        if (Controller.Site.Map.TryGet(Position, out Entity ent))
                        {
                            if (ent.TryGet(out FluidParticle fluid))
                            {
                                fluid.Volume = 64;
                                if (ent.Has<IsFluidStatic>())
                                    ent.Remove<IsFluidStatic>();
                            }
                            else
                            {
                                ent.Add<FluidParticle>(new FluidParticle()
                                {
                                    Type = FluidType.WATER,
                                    Volume = 64
                                });
                            }
                            if (!ent.Has<UpdateTileRenderSelfRequest>())
                            {
                                ent.Add<UpdateTileRenderSelfRequest>();
                            }
                        }
                    }
                }
            }
            /*else if (Active)
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
                                    Point3 Pos = new(x, y, z);
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
                                    Point3 pos = new(x, y, z);

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
            }*/
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