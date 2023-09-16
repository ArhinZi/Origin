using Microsoft.Xna.Framework;

namespace Origin.Source.Events
{
    public class UpdateMousePosition
    {
        public Point mousePosition;

        public UpdateMousePosition(Point mousePosition)
        {
            this.mousePosition = mousePosition;
        }
    }
}