using Origin.Source.Model.NewWorld;

namespace Origin.Source.Model.NewWorld.Map
{
    internal sealed class BlockSimilar : BlockBase
    {
        public Tile UniformTile { get; }

        public BlockSimilar(Tile uniformTile)
        {
            UniformTile = uniformTile;
        }

        public override Tile GetTile(int x, int y)
        {
            return UniformTile;
        }

        public override BlockBase SetTile(int x, int y, Tile tile)
        {
            if (tile.Equals(UniformTile))
                return this;

            return new BlockPartial(UniformTile).SetTile(x, y, tile);
        }

        public override BlockSimple ToSimple()
        {
            var tiles = new Tile[TILE_COUNT];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = UniformTile;
            return new BlockSimple(tiles);
        }
    }
}
