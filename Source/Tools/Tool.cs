using Microsoft.Xna.Framework;

using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Tools
{
    public abstract class Tool : MonoGame.Extended.IUpdate
    {
        public class SpritePositionColor : ICloneable
        {
            public Sprite sprite;
            public Point3 position;
            public Point offset;
            public Color color;

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        public string Name = "Unknown tool";
        protected SiteToolController Controller;
        protected Camera2D Camera => Controller.Site.Camera;

        public List<SpritePositionColor> sprites;
        public Point3 Position = Point3.Zero;

        public Tool(SiteToolController controller)
        {
            Controller = controller;
        }

        public abstract void Update(GameTime gameTime);

        public abstract void Reset();
    }
}