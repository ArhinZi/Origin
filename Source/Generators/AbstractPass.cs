using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Generators
{
    public abstract class AbstractPass
    {
        public int order;

        public abstract void Run(Site site, Point3 size, SiteGeneratorParameters parameters, int seed);
    }
}