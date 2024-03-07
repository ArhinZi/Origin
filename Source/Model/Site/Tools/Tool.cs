using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;
using Origin.Source.Resources;
using System;
using System.Collections.Generic;

namespace Origin.Source.Model.Site.Tools
{
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

        public Tool(SiteToolsComponent controller)
        {
            Controller = controller;
            RenderLayer = Global.DrawBufferLayer.FrontInteractives;
        }

        public abstract void Update(GameTime gameTime);

        public abstract void Reset();

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

            Point3 cellPos = new Point3()
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
                if (pos.GraterEqualOr(site.Size) ||
                //ignore null
                site.Map[pos.X, pos.Y, pos.Z] == Entity.Null ||
                //ignore air
                site.Map[pos.X, pos.Y, pos.Z] != Entity.Null &&
                !site.Map[pos.X, pos.Y, pos.Z].Has<BaseConstruction>() ||
                //ignore blocks on current level
                site.Map[pos.X, pos.Y, pos.Z] != Entity.Null &&
                site.Map[pos.X, pos.Y, pos.Z].Has<BaseConstruction>() &&
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