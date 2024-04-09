using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Construction;
using Origin.Source.Model.Pathfind.old;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System.Collections.Generic;
using System.Diagnostics;

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
                    bool changed = false;
                    var Npos = pos + n;
                    Entity Nent;
                    if (_site.Map.TryGet(Npos, out Nent))
                    {
                        Debug.Assert(Nent != Entity.Null);
                        // Check a construction below the Tile
                        if (_site.Map.TryGet(Npos - new Point3(0, 0, 1), out Entity below) && below != Entity.Null)
                        {
                            if (below.TryGet(out BaseConstruction belowbc))
                            {
                                if (!Nent.Has<IsWalkAbleTile>())
                                {
                                    commands.Add(Nent, new IsWalkAbleTile() { ConstructionBelowMetaID = belowbc.ConstructionMetaID });
                                    changed = true;
                                }
                            }
                            else
                            {
                                if (Nent.Has<IsWalkAbleTile>())
                                {
                                    changed = true;
                                    commands.Remove<IsWalkAbleTile>(Nent);
                                }
                            }
                        }
                        if (changed)
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
                    if (_site.Map.TryGet(Npos, out Entity Nent))
                    {
                        // Remove path if Construction is on Tile
                        if (Nent.Has<BaseConstruction>())
                        {
                            if (Nent.Has<IsWalkAbleTile>())
                            {
                                commands.Remove<IsWalkAbleTile>(Nent);
                            }
                        }
                        else
                        {
                            // Check a construction below the Tile
                            if (_site.Map.TryGet(Npos - new Point3(0, 0, 1), out Entity below) && below != Entity.Null)
                            {
                                if (below.TryGet(out BaseConstruction belowbc))
                                {
                                    if (!Nent.Has<IsWalkAbleTile>())
                                    {
                                        commands.Add(Nent, new IsWalkAbleTile() { ConstructionBelowMetaID = belowbc.ConstructionMetaID });
                                    }
                                }
                                else
                                {
                                    if (Nent.Has<IsWalkAbleTile>())
                                    {
                                        commands.Remove<IsWalkAbleTile>(Nent);
                                    }
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
                _site.Pathfinder.RemovePathNode(item);
                _site.Pathfinder.SetPathNode(item);
            }
        }
    }
}