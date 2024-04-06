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
    public struct Node
    {
        public struct Edge
        {
            public Point3 Position;
            public short Cost = 0;

            public Edge(byte cost)
            { Cost = cost; }
        }

        public byte Difficulty = 1;
        public Edge[][] Edges;

        public Node(byte diff, Edge[][] edges)
        {
            Difficulty = diff;
            Edges = edges;
        }

        public Edge[] GetByTraversalType(TraversalType type)
        {
            return Edges[(byte)type];
        }

        public void RemoveEdgeByPosition(Point3 pos)
        {
            var edges = Edges;
            for (int i = 0; i < Edges.Length; i++)
            {
                Edge[] edgesOfType = Edges[i];
                Debug.Assert(edgesOfType != null && edgesOfType.Length > 0);

                // Find the index of the Edge with the specified Position in the current subarray
                int indexToRemove = Array.FindIndex(edgesOfType, edge => edge.Position == pos);

                // If the index is found (not -1), remove the Edge from the subarray
                if (indexToRemove != -1)
                {
                    if (edgesOfType.Length > 1)
                    {
                        // Remove the Edge from the subarray
                        Edge[] newEdgesOfType = new Edge[edgesOfType.Length - 1];
                        Array.Copy(edgesOfType, 0, newEdgesOfType, 0, indexToRemove);
                        Array.Copy(edgesOfType, indexToRemove + 1, newEdgesOfType, indexToRemove, edgesOfType.Length - indexToRemove - 1);
                        Edges[i] = newEdgesOfType;
                    }
                    // if there are no more Edges of this TraversalType
                    else
                    {
                        Edge[][] newEdges = new Edge[Edges.Length - 1][];
                        Array.Copy(Edges, 0, newEdges, 0, i);
                        Array.Copy(Edges, i + 1, newEdges, i, Edges.Length - i - 1);
                        Edges = newEdges;

                        return;
                    }
                }
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}