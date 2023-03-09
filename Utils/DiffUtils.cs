using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Utils
{
    class DiffUtils
    {
        public static T GetOrBound<T>(T value, T low, T high) where T: IComparable
        {
            if (value.CompareTo(low)<0) return low;
            if (value.CompareTo(high)>0) return high;
            return value;
        }

        public static bool InBounds<T>(T value, T low, T high) where T : IComparable
        {
            return (value.CompareTo(low) > 0) && (value.CompareTo(high) < 0);
        }
    }
}
