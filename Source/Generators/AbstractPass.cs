using Arch.Core;

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
        public abstract Entity Pass(Entity ent, Point3 pos);
    }
}