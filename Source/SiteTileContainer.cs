﻿using Arch.Core;

using Origin.Source.Utils;

using System.Collections.Generic;

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

        public Entity this[Point3 p]
        {
            get { return this[p.X, p.Y, p.Z]; }
            set
            {
                this[p.X, p.Y, p.Z] = value;
            }
        }

        public bool TryGet(Point3 position, out Entity entity)
        {
            if (position.InBounds(Point3.Zero, Size))
            {
                entity = this[position];
                return true;
            }
            else
            {
                entity = Entity.Null;
                return false;
            }
        }
    }
}