using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Origin.Source.Model.NewWorld;

namespace Origin.Source.Model.NewWorld.Map
{
    internal abstract class BlockBase
    {
        public const int BLOCK_SIZE = 16;
        public const int TILE_COUNT = BLOCK_SIZE * BLOCK_SIZE;

        protected static int GetIndex(int x, int y) => y * BLOCK_SIZE + x;

        public abstract Tile GetTile(int x, int y);
        public abstract BlockBase SetTile(int x, int y, Tile tile);
        public abstract BlockSimple ToSimple();
    }
}
