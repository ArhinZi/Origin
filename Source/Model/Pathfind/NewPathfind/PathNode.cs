using Origin.Source.Model.Pathfind.old;

using Priority_Queue;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Pathfind.NewPathfind
{
    internal class PathNode : FastPriorityQueueNode
    {
        public Point3 position;
        public Node node;
        public int costSoFar;
        public int heuristic;

        public int Expection => costSoFar + heuristic;

        public int CompareTo(PFNode other)
        {
            return this.Expection.CompareTo(other.Expection);
        }

        public override string ToString()
        {
            return $"{position.ToString()} {Expection}";
        }
    }
}