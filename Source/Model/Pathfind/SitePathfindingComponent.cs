using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using MonoGame.Extended;

using Origin.Source.Model.NewWorld;
using Origin.Source.Model.NewWorld.Map;
using Origin.Source.Model.Pathfind.NewPathfind;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Origin.Source.Model.Pathfind
{
    public class SitePathfindingComponent : IUpdate
    {
        private Site _site;
        private Point3 _size;

        private Pathfinder _pathfinderSystem;

        public Point3 startPath;
        public Point3 endPath;
        public List<Point3> currPath2;

        private PathfinderJob job;

        public SitePathfindingComponent(Site site, Point3 size)
        {
            _site = site;
            _size = size;
            InitPathFinder();
        }

        private TileContainer Map => _site.Map;
        private Point3 Size => _size;

        private void InitPathFinder()
        {
            _pathfinderSystem = new Pathfinder();
            for (int z = 0; z < Size.Z; z++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    for (int y = 0; y < Size.Y; y++)
                    {
                        Point3 pos = new(x, y, z);
                        if (Map[pos].Exists && Map[pos].IsWalkable)
                        {
                            SetPathNode(pos);
                        }
                    }
                }
            }

            job = new(_pathfinderSystem);
        }

        public void SetPathNode(Point3 pos)
        {
            if (Map[pos].Exists && Map[pos].IsWalkable)
            {
                List<Node.Edge> edges = new List<Node.Edge>();

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

                                Tile other = Map[x, y, z];
                                if (other.Exists && other.IsWalkable)
                                {
                                    Point3 otherPos = new(x, y, z);
                                    edges.Add(new Node.Edge()
                                    {
                                        Position = otherPos,
                                        Cost = v
                                    });
                                }
                            }
                        }
                    }
                }

                Node node = new Node()
                {
                    Difficulty = 1,
                    Edges = edges.ToArray()
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
            //RemovePathNode(pos);
            //SetPathNode(pos);
        }

        public PathInfo FindPath(Point3 start, Point3 end, bool debug = false)
        {
            long b;
            Stopwatch watch = Stopwatch.StartNew();

            job.Initialize(start, end, new TraversalType[] { TraversalType.Walk });
            job.Execute();
            var currPath2 = job.ResultPath;

            watch.Stop();
            b = watch.ElapsedMilliseconds;
            if (currPath2 != null)
                Debug.WriteLine($"Path Found with Len={currPath2.path.Count} in {b}ms looked {job.VisitedCount} Nodes");
            return currPath2;
        }

        public void Update(GameTime gameTime)
        {
        }
    }
}