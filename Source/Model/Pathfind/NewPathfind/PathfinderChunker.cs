using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Pathfind.NewPathfind
{
    internal class PathfinderChunker
    {
        public readonly int CHINK_SIZE = 32;
        private Pathfinder _pathfinder;

        private List<Chunk> _chunks = [];
        private Dictionary<Point3, int> _pointToID = [];

        public PathfinderChunker(Pathfinder pathfinder)
        {
            _pathfinder = pathfinder;
        }

        public void Initialize()
        {
            _chunks.Clear();
            _pointToID.Clear();
        }
    }
}