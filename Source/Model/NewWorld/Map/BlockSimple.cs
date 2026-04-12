using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Origin.Source.Model.NewWorld;

namespace Origin.Source.Model.NewWorld.Map
{
    internal sealed class BlockSimple : BlockBase
    {
        private readonly Tile[] _tiles;

        public BlockSimple()
        {
            _tiles = new Tile[TILE_COUNT];
        }

        public BlockSimple(Tile[] tiles)
        {
            _tiles = tiles;
        }

        public ref Tile GetRef(int x, int y)
        {
            return ref _tiles[GetIndex(x, y)];
        }

        public override Tile GetTile(int x, int y)
        {
            return _tiles[GetIndex(x, y)];
        }

        public override BlockBase SetTile(int x, int y, Tile tile)
        {
            _tiles[GetIndex(x, y)] = tile;
            return this;
        }

        public override BlockSimple ToSimple()
        {
            return this;
        }
    }
}
