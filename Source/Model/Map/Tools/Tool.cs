using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Origin.Source.ECS.Construction;
using Origin.Source.Model.Map;
using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Origin.Source.Utils;
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
            return WorldUtils.MouseScreenToMap(cam, mousePos, level, site, onFloor, clip);
        }

        public static Point3 MouseScreenToMapSurface(Camera2D cam, Point mousePos, int level, Site site,
            bool onFloor = false)
        {
            return WorldUtils.MouseScreenToMapSurface(cam, mousePos, level, site, onFloor);
        }
    }
}