using Microsoft.Xna.Framework;

namespace Origin.Source.Events
{
    public class ScreenBoundsChanged
    {
        public Rectangle screenBounds;

        public ScreenBoundsChanged(Rectangle bounds)
        {
            screenBounds = bounds;
        }
    }
}