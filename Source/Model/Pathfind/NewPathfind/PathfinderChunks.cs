using Roy_T.AStar.Paths;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Pathfind.NewPathfind
{
    internal class PathfinderChunks
    {
        public readonly int CHUNK_SIZE = 32;

        private Pathfinder _pathfinder;

        private List<Chunk> _chunks = [];
        //private Dictionary<Point3, int> _pointToID = [];

        public PathfinderChunks(Pathfinder pathfinder)
        {
            _pathfinder = pathfinder;
        }

        public void Initialize()
        {
            _chunks.Clear();
            //_pointToID.Clear();
        }

        private void InitChunkify()
        {
            var dirs = new Dictionary<Point3, Point3[]>()
                {
                    { Point3.North, [ Point3.North, Point3.West, Point3.East ] },
                    { Point3.East,  [ Point3.North, Point3.East, Point3.South ] },
                    { Point3.South, [ Point3.West, Point3.South, Point3.East ] },
                    { Point3.West,  [ Point3.North, Point3.West, Point3.South ] },
                };

            // Nodes without chunk - all nodes for init
            HashSet<Point3> nodes = _pathfinder.Nodes.Keys.ToHashSet();

            // Candidates for current chunk
            List<Point3> toVisit = [];

            // do until we have nodes without chunk
            while (nodes.Count > 0)
            {
                // First/random node to start new chunk
                Point3 sPos = nodes.First();
                nodes.Remove(sPos);
                var sNode = _pathfinder.Nodes[sPos];

                Point3 lbound = sPos.XYdiv(CHUNK_SIZE);
                lbound.X *= CHUNK_SIZE;
                lbound.Y *= CHUNK_SIZE;
                Point3 hbound = lbound + new Utils.Point3(1, 1, 0);
                hbound.X *= CHUNK_SIZE;
                hbound.Y *= CHUNK_SIZE;
            }
        }
    }
}