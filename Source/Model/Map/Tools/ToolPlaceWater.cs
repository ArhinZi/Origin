using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Origin.Source.Controller.IO;
using Origin.Source.ECS.Fluid;
using Origin.Source.ECS.Render;
using Origin.Source.Resources;

using System;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Model.Map.Tools
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
        //private Point3 GroundPosition = Point3.Null;

        private SpritePositionColor template = new()
        {
            sprite = GlobalResources.Sprites["WallSoil"],
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

            Wall = GlobalResources.Sprites["SolidWall"];
            //GroundPosition = MouseScreenToMap(Camera, m, Controller.Site.CurrentLevel, Controller.Site, true, true);

            if (!Active)
            {
                Position = MouseScreenToMap(Camera, m, Controller.Site.CurrentLevel - 2, Controller.Site, true, true);
                if (Position != Point3.Null)
                {
                    if (InputManager.IsPressed("mouse.left"))
                    {
                        if (Controller.Site.Map.TryGet(Position, out Entity ent) && ent != Entity.Null)
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
                            if (!ent.Has<SelfRequestUpdateTileRender>())
                            {
                                ent.Add<SelfRequestUpdateTileRender>();
                            }
                        }
                        if (InputManager.JustReleased("mouse.left"))
                        {
                            Reset();
                        }
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
                //for (int i = Math.Min(GroundPosition.Z + 1, Controller.Site.CurrentLevel); i <= Position.Z; i++)
                int startZ = Position.Z -5;
                int endZ = Position.Z - 1;
                //if (GroundPosition != Point3.Null)
                //{
                //    startZ = GroundPosition.Z + 1;
                //    endZ = Position.Z;
                //}
                //else
                //{
                //    startZ = Position.Z;
                //    endZ = Position.Z;
                //}

                //if (startZ > endZ)
                //    (startZ, endZ) = (endZ, startZ);

                //startZ = Math.Max(0, startZ);
                endZ = Math.Min(Controller.Site.Size.Z - 1, endZ);

                for (int i = startZ; i <= endZ; i++)
                {
                    sprites.Add(new SpritePositionColor()
                    {
                        sprite = GlobalResources.Sprites["SelectionWall"],
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