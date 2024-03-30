using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.ECS.Pathfinding
{
    public class UpdateSitePathSystem : TickSystem
    {
        public UpdateSitePathSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        public override void Update(in ulong t)
        {
            var query = new QueryDescription().WithAll<ConstructionRemovedEvent>();
            var commands = new CommandBuffer();
            var visited = new HashSet<Point3>();
            _site.ArchWorld.Query(in query, (ref ConstructionRemovedEvent cre) =>
            {
                var pos = cre.Position;

                foreach (var n in WorldUtils.TOP_BOTTOM_NEIGHBOUR_PATTERN())
                {
                    var Npos = pos + n;
                    Entity Nent;
                    if (_site.Map.TryGet(Npos, out Nent))
                    {
                        // Check a construction below the Tile
                        Entity below;
                        if (_site.Map.TryGet(Npos - new Point3(0, 0, 1), out below) && below != Entity.Null)
                        {
                            BaseConstruction belowbc;
                            if (below.TryGet(out belowbc))
                            {
                                if (!Nent.Has<IsWalkAbleTile>())
                                    commands.Add(Nent, new IsWalkAbleTile() { ConstructionBelowMetaID = belowbc.ConstructionMetaID });
                            }
                            else
                            {
                                if (Nent.Has<IsWalkAbleTile>())
                                    commands.Remove<IsWalkAbleTile>(Nent);
                            }
                        }
                        visited.Add(Npos);
                    }
                }
            });
            commands.Playback(_site.ArchWorld);

            commands = new CommandBuffer();
            query = new QueryDescription().WithAll<ConstructionPlacedEvent>();
            _site.ArchWorld.Query(in query, (ref ConstructionPlacedEvent cpe) =>
            {
                var pos = cpe.Position;

                foreach (var n in WorldUtils.TOP_BOTTOM_NEIGHBOUR_PATTERN())
                {
                    var Npos = pos + n;
                    Entity Nent;
                    if (_site.Map.TryGet(Npos, out Nent))
                    {
                        // Remove path if Construction is on Tile
                        if (Nent.Has<BaseConstruction>())
                        {
                            if (Nent.Has<IsWalkAbleTile>())
                                commands.Remove<IsWalkAbleTile>(Nent);
                        }
                        else
                        {
                            // Check a construction below the Tile
                            Entity below;
                            if (_site.Map.TryGet(Npos - new Point3(0, 0, 1), out below) && below != Entity.Null)
                            {
                                BaseConstruction belowbc;
                                if (below.TryGet(out belowbc))
                                {
                                    if (!Nent.Has<IsWalkAbleTile>())
                                        commands.Add(Nent, new IsWalkAbleTile() { ConstructionBelowMetaID = belowbc.ConstructionMetaID });
                                }
                                else
                                {
                                    if (Nent.Has<IsWalkAbleTile>())
                                        commands.Remove<IsWalkAbleTile>(Nent);
                                }
                            }
                        }
                        visited.Add(Npos);
                    }
                }
            });

            commands.Playback(_site.ArchWorld);
            foreach (var item in visited)
            {
                _site.Pathfinder.UpdatePathNode(item);
            }
        }
    }
}