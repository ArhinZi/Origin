using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Origin.Source.Pathfind
{
    public class SitePathfindingService : IUpdate
    {
        private Site _site;
        private Point3 _size;
        private ArchWorld _world;

        private PathfinderSystem _pathfinderSystem;

        public Point3 startPath;
        public Point3 endPath;
        public List<Point3> currPath2;

        public SitePathfindingService(Site site, Point3 size, ArchWorld world)
        {
            _site = site;
            _size = size;
            _world = world;

            InitPathFinder();
        }

        private SiteTileContainer Map => _site.Map;
        private Point3 Size => _size;

        private void InitPathFinder()
        {
            var query = new QueryDescription().WithAll<TilePathAble, IsTile>();

            _pathfinderSystem = new PathfinderSystem();
            _world.Query(in query, (ref TilePathAble pn, ref IsTile tile) =>
            {
                Point3 pos = tile.Position;
                SetPathNode(pos);
            });
        }

        private void SetPathNode(Point3 pos)
        {
            if (Map[pos.X, pos.Y, pos.Z] != Entity.Null && Map[pos.X, pos.Y, pos.Z].Has<TilePathAble>())
            {
                if (!_pathfinderSystem.HasNode(pos))
                    _pathfinderSystem.AddNode(pos, 1);

                int i = 0;
                for (int x = pos.X - 1; x <= pos.X + 1; x++)
                {
                    for (int y = pos.Y - 1; y <= pos.Y + 1; y++)
                    {
                        for (int z = pos.Z - 1; z <= pos.Z + 1; z++)
                        {
                            if (x >= 0 && y >= 0 && z >= 0 &&
                                x < Size.X && y < Size.Y && z < Size.Z &&
                                (x != pos.X || y != pos.Y || z != pos.Z))
                            {
                                float v = 1;
                                if (x != pos.X && y != pos.Y) v = 1.414f;
                                if (z != pos.Z) v = 3;

                                if (Map[x, y, z] != Entity.Null && Map[x, y, z].Has<TilePathAble>())
                                {
                                    Point3 otherPos = new Point3(x, y, z);

                                    if (!_pathfinderSystem.HasNode(otherPos))
                                        _pathfinderSystem.AddNode(otherPos, 1);
                                    _pathfinderSystem.AddNodeIntersections(pos,
                                        new RelationWith[] {new RelationWith() {
                                            position = otherPos ,
                                            traversalType = TraversalTypes.Walk,
                                            cost = v
                                        } });
                                    i++;
                                    //otherNode.Connect(node, Velocity.FromMetersPerSecond(1));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdatePathNode(Point3 pos)
        {
            if (_pathfinderSystem.HasNode(pos))
            {
                _pathfinderSystem.RemoveNode(pos);
            }
            SetPathNode(pos);
        }

        public PathInfo FindPath(Point3 start, Point3 end, bool debug = false)
        {
            long a, b;
            Stopwatch watch = Stopwatch.StartNew();
            //List<Point3> path;
            //for (int i = 0; i < 10; i++)

            var currPath2 = _pathfinderSystem.FindPath(start, end, debug);

            watch.Stop();
            b = watch.ElapsedMilliseconds;
            if (currPath2 != null)
                Debug.WriteLine(String.Format("Path Found with Len={0} in {1}ms looked {2} Nodes", currPath2.path.Count, b.ToString(), _pathfinderSystem.LastVisitedCount));
            return currPath2;
        }

        public void Update(GameTime gameTime)
        {
            if (_world.CountEntities(new QueryDescription().WithAll<WaitingForUpdateTileLogic>()) > 0)
            {
                // Update pathes
                var query = new QueryDescription().WithAll<WaitingForUpdateTileLogic, IsTile>();
                var commands = new CommandBuffer(_world);
                var visited = new HashSet<Point3>();
                _world.Query(in query, (Entity entity, ref IsTile rootComp) =>
                {
                    var p = rootComp.Position;

                    foreach (var n in WorldUtils.TOP_BOTTOM_NEIGHBOUR_PATTERN())
                    {
                        Point3 nPos = p + n;
                        Entity rootN;
                        if (Map.TryGet(nPos, out rootN) && rootN != Entity.Null)
                        {
                            // Remove path if Construction is on Tile
                            if (rootN.Has<BaseConstruction>())
                            {
                                commands.Remove<TilePathAble>(rootN);
                            }
                            else
                            {
                                // Check a construction under the Tile
                                Entity tmp;
                                if (Map.TryGet(nPos - new Point3(0, 0, 1), out tmp) && tmp != Entity.Null)
                                {
                                    if (tmp.Has<BaseConstruction>())
                                    {
                                        commands.Add<TilePathAble>(rootN);
                                    }
                                    else
                                    {
                                        commands.Remove<TilePathAble>(rootN);
                                    }
                                }
                            }
                        }
                        visited.Add(nPos);
                        commands.Remove<WaitingForUpdateTileLogic>(entity);
                    }
                });
                commands.Playback();
                foreach (var item in visited)
                {
                    UpdatePathNode(item);
                }
            }
        }
    }
}