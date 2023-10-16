using Roy_T.AStar.Primitives;

using System.Collections.Generic;

namespace Roy_T.AStar.Graphs
{
    public interface INode
    {
        Position Position { get; }
        IList<IEdge> Incoming { get; }
        IList<IEdge> Outgoing { get; }
    }
}
