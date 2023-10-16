using Microsoft.Xna.Framework;

using System;

namespace Origin.Source
{
    public static class XMLoader
    {
        public static Color ParseColor(string s)
        {
            string[] components = s.Split(' ');
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
                throw new FormatException(String.Format("Color parse error: {0}", s));
            }
        }

        public static Rectangle ParseRectangle(string s)
        {
            var values = s.Split(' ');
            var x = int.Parse(values[0]);
            var y = int.Parse(values[1]);
            var w = int.Parse(values[2]);
            var h = int.Parse(values[3]);
            return new Rectangle(x, y, w, h);
        }
    }
}