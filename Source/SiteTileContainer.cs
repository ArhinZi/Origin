using Arch.Core;

using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source
{
    public class SiteTileContainer
    {
        private List<Entity[,]> Blocks;
        private Point3 Size;

        public SiteTileContainer(Point3 size)
        {
            Blocks = new List<Entity[,]>();
            Size = size;
            Blocks.Capacity = Size.Z;
            for (int i = 0; i < size.Z; i++)
            {
                Blocks.Add(null);
            }
        }

        public Entity this[int x, int y, int z]
        {
            get
            {
                if (z >= 0 && Blocks[z] != null)
                    return Blocks[z][x, y];
                else return Entity.Null;
            }
            set
            {
                if (Blocks[z] == null)
                {
                    Blocks[z] = new Entity[Size.X, Size.Y];
                    for (int i = 0; i < Blocks[z].GetLength(0); i++)
                    {
                        for (int j = 0; j < Blocks[z].GetLength(1); j++)
                        {
                            Blocks[z][i, j] = Entity.Null;
                        }
                    }
                }
                Blocks[z][x, y] = value;
            }
        }
    }
}