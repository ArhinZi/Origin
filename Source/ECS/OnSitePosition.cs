using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.Utils;

namespace Origin.Source.ECS
{
    public struct OnSitePosition
    {
        public Site site;
        public Point3 position;

        public IsometricDirection DirectionOfView;

        public OnSitePosition(Site site, Point3 pos, IsometricDirection dir = IsometricDirection.NONE)
        {
            this.site = site;
            this.position = pos;
            DirectionOfView = dir;
        }
    }
}