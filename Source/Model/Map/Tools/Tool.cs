using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Map;
using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;

namespace Origin.Source.Model.Map.Tools
{
    public enum ToolState
    {
        Idle,
        Selecting,
    }

    public interface IToolCommand
    {
        void Execute(Site site);
    }

    public abstract class Tool : MonoGame.Extended.IUpdate
    {
        public class SpritePositionColor : ICloneable
        {
            public Sprite sprite;
            public Point3 position;
            public Point offset;

            public float Zoffset;
            public Color color;

            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        public string Name = "Unknown tool";
        protected SiteToolsComponent Controller;
        protected Camera2D Camera => Controller.Site.Camera;

        public Global.DrawBufferLayer RenderLayer;

        public List<SpritePositionColor> sprites;
        public Point3 Position = Point3.Null;
        public Point3 PrevPosition = Point3.Null;
        public bool DrawDirty = false;
        public bool Active = false;
        public ToolState State { get; protected set; } = ToolState.Idle;

        public Tool(SiteToolsComponent controller)
        {
            Controller = controller;
            RenderLayer = Global.DrawBufferLayer.FrontInteractives;
        }

        protected void SetState(ToolState state)
        {
            State = state;
            Active = state != ToolState.Idle;
        }

        protected void ExecuteCommand(IToolCommand command)
        {
            Controller.Execute(command);
        }

        public abstract void Update(GameTime gameTime);

        public abstract void Reset();

        public virtual void Draw(GameTime gameTime)
        {
        }

        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level, Site site,
            bool onFloor = false,
            bool clip = false)
        {
            Vector3 worldPos = Global.GraphicsDevice.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), cam.Projection, cam.Transformation, cam.WorldMatrix);
            worldPos += new Vector3(0, level * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset) +
                (onFloor ? GlobalResources.Settings.FloorYoffset : 0)
                , 0);

            var cellPosX = worldPos.X / GlobalResources.Settings.TileSize.X - 0.5;
            var cellPosY = worldPos.Y / GlobalResources.Settings.TileSize.Y - 0.5;

            Point3 cellPos = new()
            {
                X = (int)Math.Round(cellPosX + cellPosY),
                Y = (int)Math.Round(cellPosY - cellPosX),
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
            for (int i = 0; i < Global.ONE_MOMENT_DRAW_LEVELS; i++)
            {
                Point3 pos = MouseScreenToMap(cam, mousePos, tlevel, site, onFloor);
                if (pos.LessOr(Point3.Zero))
                    return Point3.Null;

                Tile tile = site.Map[pos];
                if (pos.GraterEqualOr(site.Size) ||
                    !tile.Exists ||
                    !tile.HasConstruction ||
                    tile.HasConstruction && tlevel == site.CurrentLevel)
                {
                    tlevel--;
                    continue;
                }

                return pos;
            }

            return Point3.Null;
        }
    }
}