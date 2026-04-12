using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.NewWorld.Map
{
    internal class TileContainer
    {
        private readonly int _blocksX = 0;
        private readonly int _blocksY = 0;
        private readonly int _blocksZ = 0;

        // Layers: each Z level contains a 2D array of blocks [blocksX, blocksY]
        private readonly List<BlockBase[,]> _layers;

        // Block size in tiles (must match BlockBase implementation)
        private const int LOCAL_BLOCK_SIZE = BlockBase.BLOCK_SIZE;

        // Construct from world size in tiles. World size must be multiple of block size.
        public TileContainer(Point3 mapSize)
        {
            // ensure world dimensions are divisible by block size
            System.Diagnostics.Debug.Assert(mapSize.X % LOCAL_BLOCK_SIZE == 0, "World size X must be multiple of block size");
            System.Diagnostics.Debug.Assert(mapSize.Y % LOCAL_BLOCK_SIZE == 0, "World size Y must be multiple of block size");

            _blocksX = mapSize.X / LOCAL_BLOCK_SIZE;
            _blocksY = mapSize.Y / LOCAL_BLOCK_SIZE;
            _blocksZ = mapSize.Z;

            _layers = new List<BlockBase[,]>(_blocksZ);
            for (int z = 0; z < _blocksZ; z++)
            {
                _layers.Add(new BlockBase[_blocksX, _blocksY]);
            }
        }

        private bool InBoundsBlock(int bx, int by, int bz)
        {
            return bx >= 0 && by >= 0 && bz >= 0 && bx < _blocksX && by < _blocksY && bz < _blocksZ;
        }

        public Tile GetTile(Point3 sitePosition)
        {
            int bx = sitePosition.X / LOCAL_BLOCK_SIZE;
            int by = sitePosition.Y / LOCAL_BLOCK_SIZE;
            int bz = sitePosition.Z;
            if (!InBoundsBlock(bx, by, bz)) return default;

            var layer = _layers[bz];
            var block = layer[bx, by];
            if (block == null) return default;

            int lx = sitePosition.X - bx * LOCAL_BLOCK_SIZE;
            int ly = sitePosition.Y - by * LOCAL_BLOCK_SIZE;
            return block.GetTile(new Point3(lx, ly, sitePosition.Z));
        }

        public void SetTile(Point3 sitePosition, Tile tile)
        {
            int bx = sitePosition.X / LOCAL_BLOCK_SIZE;
            int by = sitePosition.Y / LOCAL_BLOCK_SIZE;
            int bz = sitePosition.Z;
            if (!InBoundsBlock(bx, by, bz)) return;

            var layer = _layers[bz];
            var block = layer[bx, by];

            int lx = sitePosition.X - bx * LOCAL_BLOCK_SIZE;
            int ly = sitePosition.Y - by * LOCAL_BLOCK_SIZE;

            if (block == null)
            {
                // create a similar (uniform) block first
                block = new BlockSimilar();
                layer[bx, by] = block;
            }

            var newBlock = block.SetTile(new Point3(lx, ly, sitePosition.Z), tile);
            if (!ReferenceEquals(newBlock, block))
            {
                layer[bx, by] = newBlock;
            }
        }
    }
}
