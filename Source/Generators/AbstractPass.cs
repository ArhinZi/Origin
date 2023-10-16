using Origin.Source.Utils;

namespace Origin.Source.Generators
{
    public abstract class AbstractPass
    {
        public int order;

        public abstract void Run(Site site, Point3 size, SiteGeneratorParameters parameters, int seed);
    }
}