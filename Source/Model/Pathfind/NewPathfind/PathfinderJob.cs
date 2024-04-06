using MonoGame.Extended.Collections;

using Origin.Source.Model.Pathfind.old;

using Priority_Queue;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Pathfind.NewPathfind
{
    public class PathfinderJob
    {
        public FastPriorityQueue<PathNode> interesting;
        private Pathfinder Pathfinder;
        private Dictionary<PathNode, PathNode> path = [];
        private Dictionary<Point3, PathNode> visited = [];

        public Point3 Start { get; private set; }
        public Point3 Goal { get; private set; }
        public TraversalType[] AllowedTypes { get; private set; }

        public int VisitedCount = 0;

        public PathInfo ResultPath;

        public PathfinderJob(Pathfinder pathfinder, int size = 128)
        {
            Pathfinder = pathfinder;
            interesting = new FastPriorityQueue<PathNode>(size);
        }

        public void Initialize(Point3 pstart, Point3 pgoal, TraversalType[] allowedTypes)
        {
            Debug.Assert(Pathfinder.Nodes.ContainsKey(pstart));
            Debug.Assert(Pathfinder.Nodes.ContainsKey(pgoal));
            Debug.Assert(allowedTypes.Length > 0);

            Start = pstart;
            Goal = pgoal;
            AllowedTypes = allowedTypes;
            VisitedCount = 0;
            ResultPath = new();

            interesting.Clear();
            visited.Clear();
            path.Clear();
        }

        public void Execute()
        {
            PathNode reached = null;

            //add start
            var head = new PathNode()
            {
                position = Start,
                node = Pathfinder.Nodes[Start],
                costSoFar = 0,
                heuristic = Heuristic(Start, Goal)
            };
            interesting.Enqueue(head, Heuristic(Start, Goal));

            //do
            while (interesting.Count > 0)
            {
                var current = interesting.Dequeue();
                if (GoalReached(Goal, current.position))
                {
                    reached = current;
                    break;
                }

                var nCurrent = current.node;

                //current.entity.GetRelationships<RelWalkTo>();
                for (int ti = 0; ti < AllowedTypes.Length; ti++)
                {
                    for (int i = 0; i < nCurrent.Edges[ti].Length; i++)
                    {
                        var nEdge = nCurrent.Edges[ti][i];
                        var nNode = Pathfinder.Nodes[nEdge.Position];

                        var nextCost = current.costSoFar + GetCost(nCurrent, nNode) * nEdge.Cost;

                        bool bvis = visited.TryGetValue(nEdge.Position, out PathNode pNode);
                        if (!bvis || pNode.costSoFar > nextCost)
                        {
                            var node = new PathNode()
                            {
                                position = nEdge.Position,
                                node = nNode,
                                costSoFar = nextCost,
                                heuristic = Heuristic(nEdge.Position, Goal)
                            };
                            //if (bvis)
                            //    interesting.UpdatePriority(node, node.Expection);
                            //else
                            {
                                EnsureInterestingCapacity();
                                interesting.Enqueue(node, node.Expection);
                            }

                            visited[nEdge.Position] = node;
                            path[node] = current;
                        }
                        else
                        { }
                    }
                }
                VisitedCount++;
            }

            ResultPath.path = [];
            bool reconstructed = false;
            if (reached != null)
            {
                var current = reached;
                ResultPath.path.Insert(0, current.position);
                while (!reconstructed)
                {
                    if (current.position == Start)
                        break;
                    current = path[current];
                    ResultPath.path.Insert(0, current.position);
                }
            }
            ResultPath.visited = visited.Keys.ToList();
        }

        private void EnsureInterestingCapacity()
        {
            if (interesting.Count + 1 == interesting.MaxSize)
            {
                interesting.Resize(interesting.MaxSize * 2);
            }
        }

        private bool GoalReached(Point3 goal, Point3 current) => current == goal;

        private int GetCost(Node a, Node b)
        {
            return (a.Difficulty + b.Difficulty) / 2;
        }

        private int Heuristic(Point3 a, Point3 b)
        {
            return Euclidean(a, b);

            int Manhatten(Point3 a, Point3 b)
            {
                int dx = Math.Abs(a.X - b.X);
                int dy = Math.Abs(a.Y - b.Y);
                int dz = Math.Abs(a.Z - b.Z);
                return dx + dy + dz;
            }
            int Euclidean(Point3 a, Point3 b)
            {
                int dx = a.X - b.X;
                int dy = a.Y - b.Y;
                int dz = a.Z - b.Z;
                return (int)(Math.Sqrt(dx * dx + dy * dy) * 141 + Math.Abs(dz) * 300);
            }
            int Diagonal(Point3 a, Point3 b)
            {
                int dx = Math.Abs(a.X - b.X);
                int dy = Math.Abs(a.Y - b.Y);
                int dz = Math.Abs(a.Z - b.Z);
                return Math.Max(dx, Math.Max(dy, dz));
            }
        }
    }
}