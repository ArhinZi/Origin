using Origin.Source.Resources;

namespace Origin.Source.Components
{
    public struct OnSitePosition
    {
        public Point3 position;

        public IsometricDirection DirectionOfView;

        public OnSitePosition(Point3 pos, IsometricDirection dir = IsometricDirection.NONE)
        {
            position = pos;
            DirectionOfView = dir;
        }
    }
}