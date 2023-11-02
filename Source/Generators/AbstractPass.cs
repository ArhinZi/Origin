using Arch.Core;

namespace Origin.Source.Generators
{
    public abstract class AbstractPass
    {
        public abstract Entity Pass(Entity ent, Point3 pos);
    }
}