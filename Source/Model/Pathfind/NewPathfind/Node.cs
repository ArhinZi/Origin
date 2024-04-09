using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Pathfind.NewPathfind
{
    internal struct Node
    {
        public struct Edge
        {
            public Point3 Position;
            public short Cost = 0;

            public Edge(byte cost)
            { Cost = cost; }
        }

        public byte Difficulty = 1;
        public int Chunk;
        public Edge[] Edges;

        public Node(byte diff, Edge[] edges)
        {
            Difficulty = diff;
            Edges = edges;
        }

        public void RemoveEdgeByPosition(Point3 pos)
        {
            // Find the index of the Edge with the specified Position in the current subarray
            int indexToRemove = Array.FindIndex(Edges, edge => edge.Position == pos);

            // If the index is found (not -1), remove the Edge from the subarray
            if (indexToRemove != -1)
            {
                if (Edges.Length > 1)
                {
                    // Remove the Edge from the subarray
                    Edge[] newEdges = new Edge[Edges.Length - 1];
                    Array.Copy(Edges, 0, newEdges, 0, indexToRemove);
                    Array.Copy(Edges, indexToRemove + 1, newEdges, indexToRemove, Edges.Length - indexToRemove - 1);
                    Edges = newEdges;
                }
                // if there are no more Edges of this TraversalType
                else
                {
                    Edges = [];
                }
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}