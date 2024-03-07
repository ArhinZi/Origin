using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;

namespace Origin.Source.ECS.Light
{
    internal class UpdateLightTickSystem : TickSystem
    {
        public UpdateLightTickSystem(Site site) : base(site)
        {
        }

        public override void Init()
        {
            for (int x = 0; x < _site.Size.X; x++)
            {
                for (int y = 0; y < _site.Size.Y; y++)
                {
                    for (int z = _site.Size.Z - 1; z >= 0; z--)
                    {
                        Entity ent = _site.Map[x, y, z];
                        ent.Add<IsSunLightedComponent>();
                        if (ent.Has<BaseConstruction>())
                            break;
                    }
                }
            }
        }

        protected override void DoTick()
        {
            base.DoTick();

            var query = new QueryDescription().WithAny<ConstructionPlacedEvent, ConstructionRemovedEvent>();
            //var commands = new CommandBuffer(_site.ArchWorld);
            _site.ArchWorld.Query(in query, (Entity ent) =>
            {
                Point3 pos;
                if (ent.TryGet(out ConstructionPlacedEvent cpe)) pos = cpe.Position;
                else if (ent.TryGet(out ConstructionRemovedEvent cre)) pos = cre.Position;
                else return;

                if (_site.Map[pos + new Utils.Point3(0, 0, 1)].Has<IsSunLightedComponent>())
                    if (!ent.Has<IsSunLightedComponent>())
                        ent.Add<IsSunLightedComponent>();
                if (ent.Has<BaseConstruction>())
                    return;

                for (int z = pos.Z - 1; z >= 0; z--)
                {
                    Entity e = _site.Map[pos.X, pos.Y, z];
                    if (e.Has<IsSunLightedComponent>())
                        if (!e.Has<IsSunLightedComponent>())
                            e.Add<IsSunLightedComponent>();
                    if (e.Has<BaseConstruction>())
                        return;
                }
            });
        }
    }
}