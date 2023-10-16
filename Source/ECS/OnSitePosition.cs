using Origin.Source.Utils;

namespace Origin.Source.ECS
{
    public struct OnSitePosition
    {
        public Point3 position;

        public IsometricDirection DirectionOfView;

        public OnSitePosition(Point3 pos, IsometricDirection dir = IsometricDirection.NONE)
        {
            this.position = pos;
            DirectionOfView = dir;
        }
    }
}