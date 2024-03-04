using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.Components;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Light;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS.Vegetation
{
    internal class VegatationControlTickSystem : TickSystem
    {
        private Random random = new Random(1234);

        public VegatationControlTickSystem(Site site) : base(site)
        {
        }

        public override void Init()
        {
            base.Init();

            var query = new QueryDescription().WithAll<BaseConstruction, IsTile, IsSunLightedComponent>();
            _site.ArchWorld.Add(query, new BaseVegetationComponent(), new GrownUpVegetationComponent());

            query = new QueryDescription().WithAll<IsTile, BaseVegetationComponent>();
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref BaseVegetationComponent bvc) =>
            {
                Point3 pos = tile.Position;

                foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                {
                    pos = pos + item;
                    if ((_site.Map.TryGet(pos, out Entity e) && e != Entity.Null && e.Has<GrownUpVegetationComponent>()) ||
                                    !pos.InBounds(new Utils.Point3(0, 0, 0), _site.Size, true, false))
                    {
                        bvc.VegetationNeighbours++;
                    }
                }
            });
        }

        public override void Tick(long ticks)
        {
            if (ticks % 30 != 0) return;
            base.Tick(ticks);
        }

        protected override void DoTick()
        {
            base.DoTick();

            var query = new QueryDescription().WithAll<BaseConstruction, IsTile, /*IsSunLightedComponent,*/ BaseVegetationComponent>()
                                            .WithNone<GrownUpVegetationComponent>();
            var commands = new CommandBuffer(_site.ArchWorld);
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref BaseConstruction bc, ref BaseVegetationComponent bvc) =>
            {
                float r = random.Next(0, 100);
                if (r <= 5 + bvc.VegetationNeighbours * 10)
                {
                    commands.Add<GrownUpVegetationComponent>(ent);
                    foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                    {
                        var pos2 = tile.Position + item;
                        if ((_site.Map.TryGet(pos2, out Entity nent) && nent.Has<BaseVegetationComponent>()))
                        {
                            ref BaseVegetationComponent nvbc = ref nent.Get<BaseVegetationComponent>();
                            nvbc.VegetationNeighbours++;
                        }
                    }
                    if (!ent.Has<ECS.UpdateTileRenderSelfRequest>())
                        commands.Add<ECS.UpdateTileRenderSelfRequest>(ent);
                }
            });
            commands.Playback();
        }
    }
}