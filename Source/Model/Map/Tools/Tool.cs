using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Map;
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

        /// <summary>
        /// Converts a mouse position in screen space to a map cell coordinate on an isometric grid.
        /// </summary>
        /// <param name="cam">Camera used to unproject screen coordinates into world space.</param>
        /// <param name="mousePos">Mouse position in screen pixels.</param>
        /// <param name="level">Target Z level to project onto.</param>
        /// <param name="site">Current site used for optional bounds clipping.</param>
        /// <param name="onFloor">
        /// Adds floor vertical offset when <see langword="true"/>, so selection aligns with floor surface.
        /// </param>
        /// <param name="clip">
        /// When <see langword="true"/>, returns <see cref="Point3.Null"/> if the resulting cell is outside site bounds.
        /// </param>
        /// <returns>
        /// A map cell position at the requested level, or <see cref="Point3.Null"/> when clipping rejects the result.
        /// </returns>
        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level, Site site,
            bool onFloor = false,
            bool clip = false)
        {
            // Convert screen-space mouse coordinates to world-space position.
            Vector3 worldPos = Global.GraphicsDevice.Viewport.Unproject(
                new Vector3(mousePos.X, mousePos.Y, 1),
                cam.Projection,
                cam.Transformation,
                cam.WorldMatrix);

            // Shift world Y to the requested map level (and optionally to floor surface height).
            worldPos += new Vector3(
                0,
                level * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset) +
                (onFloor ? GlobalResources.Settings.FloorYoffset : 0),
                0);

            // Convert world coordinates into intermediate isometric cell-space values.
            var cellPosX = worldPos.X / GlobalResources.Settings.TileSize.X - 0.5;
            var cellPosY = worldPos.Y / GlobalResources.Settings.TileSize.Y - 0.5;

            // Resolve intermediate values to integer map cell coordinates.
            Point3 cellPos = new()
            {
                X = (int)Math.Round(cellPosX + cellPosY),
                Y = (int)Math.Round(cellPosY - cellPosX),
                Z = level
            };

            // Optionally reject out-of-bounds cells.
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
                if (pos.GraterEqualOr(site.Size) ||
                //ignore null
                site.Map[pos.X, pos.Y, pos.Z] == Entity.Null ||
                //ignore air
                site.Map[pos.X, pos.Y, pos.Z] != Entity.Null &&
                !site.Map[pos.X, pos.Y, pos.Z].Has<ConstructionBase>() ||
                //ignore blocks on current level
                site.Map[pos.X, pos.Y, pos.Z] != Entity.Null &&
                site.Map[pos.X, pos.Y, pos.Z].Has<ConstructionBase>() &&
                tlevel == site.CurrentLevel
                )
                {
                    tlevel--;
                    continue;
                }
                else return pos;
            }

            return Point3.Null;
        }
    }
}