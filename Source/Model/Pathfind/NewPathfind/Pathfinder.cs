using Origin.Source.Pathfind;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Pathfind.NewPathfind
{
    public class Pathfinder
    {
        public Dictionary<Point3, Node> Nodes = [];

        public Pathfinder()
        {
        }

        public void AddNode(Point3 pos, Node node)
        {
            Nodes.Add(pos, node);
        }

        public bool HasNode(Point3 position)
        {
            return Nodes.ContainsKey(position);
        }

        public void RemoveNode(Point3 position)
        {
            if (Nodes.TryGetValue(position, out var node))
                foreach (var group in node.Edges)
                {
                    foreach (var edge in group)
                    {
                        var nNode = Nodes[edge.Position];
                        nNode.RemoveEdgeByPosition(position);
                    }
                }
        }
    }
}