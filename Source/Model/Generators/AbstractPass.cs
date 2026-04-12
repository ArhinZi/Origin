using Arch.Core;
using Origin.Source.Model.NewWorld;
using Origin.Source.Utils;

namespace Origin.Source.Model.Generators
{
    public abstract class AbstractPass
    {
        public abstract Tile Pass(Tile tile, Point3 pos);
    }
}