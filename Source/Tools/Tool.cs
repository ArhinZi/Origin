﻿using Microsoft.Xna.Framework;

using Origin.Source.Resources;

using System;
using System.Collections.Generic;

namespace Origin.Source.Tools
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
                return this.MemberwiseClone();
            }
        }

        public string Name = "Unknown tool";
        protected SiteToolController Controller;
        protected Camera2D Camera => Controller.Site.Camera;

        public Global.DrawBufferLayer RenderLayer;

        public List<SpritePositionColor> sprites;
        public Point3 Position = Point3.Null;
        public Point3 PrevPosition = Point3.Null;

        public Tool(SiteToolController controller)
        {
            Controller = controller;
            RenderLayer = Global.DrawBufferLayer.FrontInteractives;
        }

        public abstract void Update(GameTime gameTime);

        public abstract void Reset();
    }
}