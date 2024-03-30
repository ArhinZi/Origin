using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Light;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS.Vegetation
{
    internal class VegatationControlSystem : TickSystem
    {
        private Random random;

        public VegatationControlSystem(Site site) : base(site)
        {
            random = site.World.Random;
        }

        public override void Initialize()
        {
            base.Initialize();

            var query = new QueryDescription().WithAll<BaseConstruction, IsTile>();

            _site.ArchWorld.Add(query, new BaseVegetation(), new GrownUpVegetation());

            query = new QueryDescription().WithAll<IsTile, BaseVegetation>();
            var q = _site.ArchWorld.CountEntities(query);
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref BaseVegetation bvc) =>
            {
                Point3 pos = tile.Position;
                bvc.VegetationNeighbours = 0;

                foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                {
                    var npos = pos + item;
                    if ((_site.Map.TryGet(npos, out Entity e) && e != Entity.Null && e.Has<GrownUpVegetation>()) ||
                                    !npos.InBounds(new Utils.Point3(0, 0, 0), _site.Size, true, false))
                    {
                        bvc.VegetationNeighbours++;
                    }
                }
            });
        }

        public override void Update(in ulong t)
        {
            base.Update(in t);
            if (t % 60 != 0) return;

            var query = new QueryDescription().WithAll<BaseConstruction, IsTile, /*IsSunLightedComponent,*/ BaseVegetation>()
                                            .WithNone<GrownUpVegetation>();
            var commands = new CommandBuffer();
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref BaseConstruction bc, ref BaseVegetation bvc) =>
            {
                float r = random.Next(0, 100);
                if (r < 0 + bvc.VegetationNeighbours * 10)
                {
                    commands.Add<GrownUpVegetation>(ent);
                    foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                    {
                        var pos2 = tile.Position + item;
                        if ((_site.Map.TryGet(pos2, out Entity nent) && nent.Has<BaseVegetation>()))
                        {
                            ref BaseVegetation nvbc = ref nent.Get<BaseVegetation>();
                            nvbc.VegetationNeighbours++;
                        }
                    }
                    if (!ent.Has<UpdateTileRenderSelfRequest>())
                        commands.Add<UpdateTileRenderSelfRequest>(ent);
                }
            });
            commands.Playback(_site.ArchWorld);
        }
    }
}