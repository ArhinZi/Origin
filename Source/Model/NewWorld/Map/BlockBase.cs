using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.NewWorld.Map
{
    internal abstract class BlockBase
    {
        public const int BLOCK_SIZE = 16;

        public abstract Tile GetTile(Point3 tilePosition);
        public abstract BlockBase SetTile(Point3 tilePosition, Tile tile);

        protected int GetFlatIndex(int lx, int ly) => ly * BLOCK_SIZE + lx;
    }
}
