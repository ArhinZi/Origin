using Arch.Core;
using Arch.Core.Extensions;
using Arch.Relationships;

using Myra.Graphics2D.UI;

using Origin.Source.Utils;

using Roy_T.AStar.Graphs;

using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Pathfind
{
    public enum TraversalTypes : ushort
    {
        Walk,
        Climb,
        Fly
    }

    public struct RelationWith
    {
        public Point3 position;
        public TraversalTypes traversalType;
        public float cost;
    }

    public class Node
    {
        public Point3 position;
        public float difficulty;
        public int chunk;
    }

    public class Edge
    {
        public float cost;
    }

    public class PFNode : IComparable<PFNode>
    {
        public Node node;
        public float costSoFar;
        public float heuristic;

        public float Expection => costSoFar + heuristic;

        public int CompareTo(PFNode other)
        {
            return this.Expection.CompareTo(other.Expection);
        }
    }

    public class PathInfo
    {
        public List<Point3> path;
        public List<Point3> visited;
    }

    public class PathfinderSystem
    {
        private Dictionary<Point3, Node> nodes;
        private Dictionary<TraversalTypes, Dictionary<Node, (List<Node>, List<Edge>)>> connections;

        public int LastVisitedCount = 0;

        private Dictionary<TraversalTypes, Type> traversalTypePairs = new Dictionary<TraversalTypes, Type>() {
            {TraversalTypes.Walk, typeof(RelWalkTo) },
            {TraversalTypes.Climb, typeof(RelClimbTo) }
        };

        public PathfinderSystem()
        {
            nodes = new Dictionary<Point3, Node>();
            connections = new Dictionary<TraversalTypes, Dictionary<Node, (List<Node>, List<Edge>)>>();
            foreach (var type in Enum.GetNames(typeof(TraversalTypes)))
            {
                TraversalTypes t = (TraversalTypes)Enum.Parse(typeof(TraversalTypes), type);
                connections.Add(t, new Dictionary<Node, (List<Node>, List<Edge>)>());
            }
        }

        public void AddNode(Point3 position, int difficulty)
        {
            var node = new Node()
            {
                position = position,
                difficulty = difficulty
            };
            nodes.Add(position, node);
        }

        public bool HasNode(Point3 position)
        {
            return nodes.ContainsKey(position);
        }

        public void AddNodeIntersections(Point3 position, RelationWith[] relations)
        {
            var sourceNode = nodes[position];
            foreach (RelationWith rel in relations)
            {
                var targetNode = nodes[rel.position];

                if (!connections[rel.traversalType].ContainsKey(sourceNode))
                    connections[rel.traversalType].Add(sourceNode, (new List<Node>(), new List<Edge>()));
                if (!connections[rel.traversalType][sourceNode].Item1.Contains(targetNode))
                {
                    connections[rel.traversalType][sourceNode].Item1.Add(targetNode);
                    connections[rel.traversalType][sourceNode].Item2.Add(new Edge() { cost = rel.cost });
                }

                if (!connections[rel.traversalType].ContainsKey(targetNode))
                    connections[rel.traversalType].Add(targetNode, (new List<Node>(), new List<Edge>()));
                if (!connections[rel.traversalType][targetNode].Item1.Contains(sourceNode))
                {
                    connections[rel.traversalType][targetNode].Item1.Add(sourceNode);
                    connections[rel.traversalType][targetNode].Item2.Add(new Edge() { cost = rel.cost });
                }
            }
        }

        public void RemoveNode(Point3 position)
        {
            var n = nodes[position];
            foreach (var nnode in connections[TraversalTypes.Walk][n].Item1)
            {
                var its = connections[TraversalTypes.Walk][nnode];
                int index = connections[TraversalTypes.Walk][nnode].Item1.IndexOf(n);
                connections[TraversalTypes.Walk][nnode].Item1.RemoveAt(index);
                connections[TraversalTypes.Walk][nnode].Item2.RemoveAt(index);
            }
            connections[TraversalTypes.Walk].Remove(n);
            nodes.Remove(position);
        }

        public PathInfo FindPath(Point3 pstart, Point3 pgoal, bool debug = false)
        {
            MinHeap<PFNode> interesting = new MinHeap<PFNode>();
            Dictionary<Point3, PFNode> visited = new Dictionary<Point3, PFNode>();
            Dictionary<PFNode, PFNode> path = new Dictionary<PFNode, PFNode>();
            Node goal;

            PathInfo pathInfo = new PathInfo();
            if (debug)
                pathInfo.visited = new();

            if (!nodes.TryGetValue(pgoal, out goal) || !nodes.ContainsKey(pstart))
                return null;

            int visitedCount = 0;
            PFNode reached = null;

            //add first
            var head = new PFNode()
            {
                node = nodes[pstart],
                costSoFar = 0,
                heuristic = Heuristic(pstart, pgoal)
            };
            interesting.Insert(head);

            //do
            while (interesting.Count > 0)
            {
                var pfCurrent = interesting.Extract();
                if (GoalReached(goal, pfCurrent))
                {
                    reached = pfCurrent;
                    break;
                }

                if (debug)
                    pathInfo.visited.Add(pfCurrent.node.position);

                var nCurrent = pfCurrent.node;

                //current.entity.GetRelationships<RelWalkTo>();
                var relations = connections[TraversalTypes.Walk][nCurrent];

                for (int i = 0; i < relations.Item1.Count; i++)
                {
                    var nNext = relations.Item1[i];
                    var nextCost = pfCurrent.costSoFar + GetCost(nCurrent, nNext) * relations.Item2[i].cost;

                    PFNode tmp;
                    if (!visited.TryGetValue(nNext.position, out tmp) || nextCost < tmp.costSoFar)
                    {
                        var pFNode = new PFNode()
                        {
                            node = nNext,
                            costSoFar = nextCost,
                            heuristic = Heuristic(nNext.position, pgoal) * 2f
                        };

                        if (!visited.ContainsKey(nNext.position))
                            interesting.Insert(pFNode);

                        visited[nNext.position] = pFNode;
                        path[pFNode] = pfCurrent;
                    }
                }
                visitedCount++;
            }

            pathInfo.path = new List<Point3>();
            bool reconstructed = false;
            if (reached != null)
            {
                var current = reached;
                Node nCurrent;
                nCurrent = current.node;
                pathInfo.path.Insert(0, nCurrent.position);
                while (!reconstructed)
                {
                    if (nCurrent.position == pstart)
                        break;
                    current = path[current];
                    nCurrent = current.node;
                    pathInfo.path.Insert(0, nCurrent.position);
                }
            }

            LastVisitedCount = visitedCount;
            return pathInfo;
        }

        private bool GoalReached(Node goal, PFNode current) => current.node == goal;

        private float GetCost(Node a, Node b)
        {
            return (a.difficulty + b.difficulty) / 2;
        }

        private float Heuristic(Point3 a, Point3 b)
        {
            return Euclidean(a, b);

            float Manhatten(Point3 a, Point3 b)
            {
                int dx = Math.Abs(a.X - b.X);
                int dy = Math.Abs(a.Y - b.Y);
                int dz = Math.Abs(a.Z - b.Z);
                return dx + dy + dz;
            }
            float Euclidean(Point3 a, Point3 b)
            {
                int dx = a.X - b.X;
                int dy = a.Y - b.Y;
                int dz = a.Z - b.Z;
                return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            float Diagonal(Point3 a, Point3 b)
            {
                int dx = Math.Abs(a.X - b.X);
                int dy = Math.Abs(a.Y - b.Y);
                int dz = Math.Abs(a.Z - b.Z);
                return Math.Max(dx, Math.Max(dy, dz));
            }
        }
    }
}