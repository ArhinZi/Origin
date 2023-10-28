using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Extensions.Dangerous;
using Arch.Core.Utils;
using Arch.Relationships;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    public static class Extensions
    {
        public static bool HasRelationWithComponent<T, R>(this Relationship<R> rels)
        {
            foreach (var rel in rels)
            {
                return rel.Key.Has<T>();
            }
            return false;
        }

        public static Entity GetFirstRelationWithComponent<COMP, REL>(this Relationship<REL> rels)
        {
            foreach (var rel in rels)
            {
                if (rel.Key.Has<COMP>())
                    return rel.Key;
            }
            return Entity.Null;
        }
    }
}