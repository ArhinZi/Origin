using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.NewWorld.Map
{
    internal sealed class BlockSimple : BlockBase
    {
        private Tile[] _tiles = new Tile[BLOCK_SIZE * BLOCK_SIZE];

        // Construct from partial data (palette + indices)
        public BlockSimple(List<Tile> palette, byte[] indices)
        {
            // Expand indices to full tile array
            for (int i = 0; i < BLOCK_SIZE * BLOCK_SIZE; i++)
            {
                var idx = indices[i];
                _tiles[i] = idx < palette.Count ? palette[idx] : default;
            }
        }

        public override Tile GetTile(Point3 tilePosition)
        {
            int flat = GetFlatIndex(tilePosition.X, tilePosition.Y);
            return _tiles[flat];
        }

        public override BlockBase SetTile(Point3 tilePosition, Tile tile)
        {
            int flat = GetFlatIndex(tilePosition.X, tilePosition.Y);
            _tiles[flat] = tile;
            return this;
        }
    }
}
