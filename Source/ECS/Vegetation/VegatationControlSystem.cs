using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Render;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Model.Site.Light;
using Origin.Source.Utils;

using Schedulers;

using System;

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

            //_site.ArchWorld.Add(query, new BaseVegetation(), new GrownUpVegetation());
            var query = new QueryDescription().WithAll<ConstructionBase, IsTile>();
            var commands = new CommandBuffer(_site.ArchWorld.CountEntities(query));
            // TODO find way Why Parallels dont let Game exit completely
            //_site.ArchWorld.ParallelQuery(in query, (Entity ent, ref IsTile tile, ref ConstructionBase bcc) =>
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref ConstructionBase bcc) =>
            {
                if (!_site.Map.TryGet(tile.Position + Point3.Up, out Entity above) || !above.Has<ConstructionBase>())
                {
                    commands.Add<BaseVegetation>(ent);
                    commands.Add<GrownUpVegetation>(ent);
                }
            });
            commands.Playback(_site.ArchWorld);

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

            var query = new QueryDescription().WithAll<ConstructionBase, IsTile, BaseVegetation>()
                                            .WithNone<GrownUpVegetation>();
            var commands = new CommandBuffer();
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref ConstructionBase bc, ref BaseVegetation bvc) =>
            {
                float r = random.Next(0, 100);
                if (r < 0 + bvc.VegetationNeighbours * 10 && _site.LightControl.GetTile(tile.Position).SunLighted > 3)
                {
                    commands.Add<GrownUpVegetation>(ent);
                    foreach (var item in WorldUtils.FULL_NEIGHBOUR_PATTERN_3L())
                    {
                        var pos2 = tile.Position + item;
                        if ((_site.Map.TryGet(pos2, out Entity nent) && nent != Entity.Null && nent.Has<BaseVegetation>()))
                        {
                            ref BaseVegetation nvbc = ref nent.Get<BaseVegetation>();
                            nvbc.VegetationNeighbours++;
                        }
                    }
                    if (!ent.Has<SelfRequestUpdateTileRender>())
                        commands.Add<SelfRequestUpdateTileRender>(ent);
                }
            });
            commands.Playback(_site.ArchWorld);
        }
    }
}