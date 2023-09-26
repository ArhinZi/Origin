using Roy_T.AStar.Graphs;
using Roy_T.AStar.Primitives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    public struct TileHasPathNode
    {
        public Node node;

        public TileHasPathNode(Position position)
        {
            node = new Node(position);
        }
    }
}