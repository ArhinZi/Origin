using Arch.Core;
using Arch.Buffer;

using Arch.Core.Extensions;

using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Origin.Source.Model.Map;

namespace Origin.Source.ECS.Vegetation
{
    internal static class VegUtilities
    {
        public static void UpdateNeighboursOf(Site site, Point3 pos, short value)
        {
            foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
            {
                var pos2 = pos + item;
                if ((site.Map.TryGet(pos2, out Entity nent) && nent != Entity.Null && nent.Has<BaseVegetation>()))
                {
                    ref BaseVegetation nvbc = ref nent.Get<BaseVegetation>();
                    nvbc.VegetationNeighbours += value;
                }
            }
        }

        public static short GetNeighboursFor(Site site, Point3 pos)
        {
            short count = 0;
            foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
            {
                var pos2 = pos + item;
                if ((site.Map.TryGet(pos2, out Entity e) && e != Entity.Null && e.Has<GrownUpVegetation>()) ||
                            !pos2.InBounds(new Utils.Point3(0, 0, 0), site.Size, true, false))
                {
                    count++;
                }
            }
            return count;
        }
    }
}