using Origin.Source.Model.NewWorld;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld.Map
{
    internal sealed class BlockPartial : BlockBase
    {
        private const int MaxPaletteSize = 32;

        private readonly List<Tile> _palette;
        private readonly Dictionary<Tile, byte> _paletteLookup;
        private readonly byte[] _indices;

        public BlockPartial(Tile uniformTile)
        {
            _palette = [uniformTile];
            _paletteLookup = new Dictionary<Tile, byte>
            {
                [uniformTile] = 0
            };
            _indices = new byte[TILE_COUNT];
        }

        public override Tile GetTile(int x, int y)
        {
            return _palette[_indices[GetIndex(x, y)]];
        }

        public override BlockBase SetTile(int x, int y, Tile tile)
        {
            int index = GetIndex(x, y);
            byte currentIndex = _indices[index];
            if (_palette[currentIndex].Equals(tile))
                return this;

            if (!_paletteLookup.TryGetValue(tile, out byte paletteIndex))
            {
                if (_palette.Count >= MaxPaletteSize)
                {
                    var simple = ToSimple();
                    simple.SetTile(x, y, tile);
                    return simple;
                }

                paletteIndex = (byte)_palette.Count;
                _palette.Add(tile);
                _paletteLookup[tile] = paletteIndex;
            }

            _indices[index] = paletteIndex;
            return this;
        }

        public override BlockSimple ToSimple()
        {
            var tiles = new Tile[TILE_COUNT];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = _palette[_indices[i]];
            return new BlockSimple(tiles);
        }
    }
}
