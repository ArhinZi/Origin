using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using MonoGame.Extended;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.Model.Pathfind.NewPathfind;
using Origin.Source.Model.Site;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Origin.Source.Pathfind
{
    public class SitePathfindingService : IUpdate
    {
        private Site _site;
        private Point3 _size;
        private ArchWorld _world;

        private Pathfinder _pathfinderSystem;

        public Point3 startPath;
        public Point3 endPath;
        public List<Point3> currPath2;

        public PathfinderJob job;

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
            var query = new QueryDescription().WithAll<IsWalkAbleTile, IsTile>();

            _pathfinderSystem = new Pathfinder();
            _world.Query(in query, (ref IsTile tile) =>
            {
                Point3 pos = tile.Position;
                SetPathNode(pos);
            });

            job = new(_pathfinderSystem);
        }

        // TODO Fix Set incoming connections to changed Node
        public void SetPathNode(Point3 pos)
        {
            if (Map[pos.X, pos.Y, pos.Z] != Entity.Null && Map[pos.X, pos.Y, pos.Z].Has<IsWalkAbleTile>())
            {
                List<Node.Edge> edges = new List<Node.Edge>();

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
                                short v = 100;
                                if (x != pos.X && y != pos.Y) v = 141;
                                if (z != pos.Z) v = 300;

                                IsWalkAbleTile wat;
                                if (Map[x, y, z] != Entity.Null && Map[x, y, z].TryGet<IsWalkAbleTile>(out wat))
                                {
                                    Point3 otherPos = new(x, y, z);

                                    edges.Add(new Node.Edge()
                                    {
                                        Position = otherPos,
                                        Cost = v
                                    });

                                    i++;
                                    //otherNode.Connect(node, Velocity.FromMetersPerSecond(1));
                                }
                            }
                        }
                    }
                }
                var redges = new Node.Edge[1][];
                redges[(byte)TraversalType.Walk] = edges.ToArray();
                Node node = new Node()
                {
                    Difficulty = 1,
                    Edges = redges
                };
                _pathfinderSystem.AddNode(pos, node);
            }
        }

        public void RemovePathNode(Point3 pos)
        {
            _pathfinderSystem.RemoveNode(pos);
        }

        public void UpdatePathNode(Point3 pos)
        {
            RemovePathNode(pos);
            SetPathNode(pos);
        }

        public PathInfo FindPath(Point3 start, Point3 end, bool debug = false)
        {
            long a, b;
            Stopwatch watch = Stopwatch.StartNew();
            //List<Point3> path;
            //for (int i = 0; i < 10; i++)

            job.Initialize(start, end, new TraversalType[] { TraversalType.Walk });

            job.Execute();
            var currPath2 = job.ResultPath;

            watch.Stop();
            b = watch.ElapsedMilliseconds;
            if (currPath2 != null)
                Debug.WriteLine(String.Format("Path Found with Len={0} in {1}ms looked {2} Nodes", currPath2.path.Count, b.ToString(), job.VisitedCount));
            return currPath2;
        }

        public void Update(GameTime gameTime)
        {
        }
    }
}