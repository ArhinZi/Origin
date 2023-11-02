using System;

using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Origin.Source.Utils
{
    public static class Parser
    {
        public static Color ColorFromString(string str)
        {
            string[] components = str.Split(' ');
            try
            {
                byte r = byte.Parse(components[0]);
                byte g = byte.Parse(components[1]);
                byte b = byte.Parse(components[2]);
                byte a = byte.Parse(components[3]);
                return new Color(r, g, b, a);
            }
            catch (Exception)
            {
                return Color.Pink;
                throw new FormatException(String.Format("Color parse error: {0}", str));
            }
        }

        public static Rectangle RectangleFromString(string str)
        {
            string[] component = str.Split(' ');
            try
            {
                var x = int.Parse(component[0]);
                var y = int.Parse(component[1]);
                var w = int.Parse(component[2]);
                var h = int.Parse(component[3]);
                return new Rectangle(x, y, w, h);
            }
            catch (Exception)
            {
                return Rectangle.Empty;
                throw new FormatException(String.Format("Rectangle parse error: {0}", str));
            }
        }
    }
}