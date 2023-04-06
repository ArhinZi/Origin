using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    internal struct SitePositionComponent
    {
        public SiteCell Cell { get; set; }

        public Site Site
        {
            get => Cell.ParentSite;
        }

        public Point3 Position
        {
            get => Cell.Position;
        }
    }
}