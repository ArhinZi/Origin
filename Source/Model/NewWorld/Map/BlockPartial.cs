using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.NewWorld.Map
{
    internal sealed class BlockPartial : BlockBase
    {
        const int MAX_PARTIAL_TILES = 32; // Maximum number of different tiles allowed in a partial block before converting to a BlockSimple.
        // Palette of unique tiles used inside this block.
        private readonly List<Tile> _palette = new(MAX_PARTIAL_TILES);

        // 16x16 = 256 cells, one byte per cell referencing palette index.
        private readonly byte[] _indices = new byte[256];

        // Map tile -> palette index for O(1) lookups when inserting.
        private readonly Dictionary<Tile, byte> _tileToIndex = new(MAX_PARTIAL_TILES);

        // Construct from a uniform block (BlockSimilar) — fill palette with uniform tile and point all indices to 0.
        public BlockPartial(BlockSimilar same)
        {
            _palette.Add(same.UniformTile);
            _tileToIndex[same.UniformTile] = 0;
            Array.Fill(_indices, (byte)0);
        }

        public override Tile GetTile(Point3 tilePosition)
        {
            int flat = GetFlatIndex(tilePosition.X, tilePosition.Y);
            byte idx = _indices[flat];
            if (idx >= _palette.Count) return default;
            return _palette[idx];
        }

        public override BlockBase SetTile(Point3 tilePosition, Tile tile)
        {
            int flat = GetFlatIndex(tilePosition.X, tilePosition.Y);
            byte currentIdx = _indices[flat];

            if (currentIdx < _palette.Count && EqualityComparer<Tile>.Default.Equals(_palette[currentIdx], tile))
                return this;

            if (!_tileToIndex.TryGetValue(tile, out byte newIdx))
            {
                newIdx = (byte)_palette.Count;
                _palette.Add(tile);
                _tileToIndex[tile] = newIdx;

                if (_palette.Count > MAX_PARTIAL_TILES)
                {
                    // Convert to BlockSimple: provide copies of palette and indices so BlockSimple can expand to full storage.
                    return new BlockSimple(_palette, (byte[])_indices.Clone());
                }
            }

            _indices[flat] = newIdx;
            return this;
        }
    }
}
