using Origin.Source.Model.NewWorld;
using Origin.Source.Utils;

namespace Origin.Source.Model.NewWorld.Map
{
    public class TileContainer
    {
        private readonly BlockBase[,,] _blocks;
        private readonly Point3 _size;
        private readonly int _blocksX;
        private readonly int _blocksY;
        private readonly int _blocksZ;

        private const int LocalBlockSize = BlockBase.BLOCK_SIZE;

        public TileContainer(Point3 mapSize)
        {
            _size = mapSize;
            _blocksX = (mapSize.X + LocalBlockSize - 1) / LocalBlockSize;
            _blocksY = (mapSize.Y + LocalBlockSize - 1) / LocalBlockSize;
            _blocksZ = mapSize.Z;
            _blocks = new BlockBase[_blocksX, _blocksY, _blocksZ];
        }

        public bool InBounds(Point3 position)
        {
            return position.InBounds(Point3.Zero, _size);
        }

        private void SplitPosition(Point3 position, out int bx, out int by, out int lx, out int ly)
        {
            bx = position.X / LocalBlockSize;
            by = position.Y / LocalBlockSize;
            lx = position.X % LocalBlockSize;
            ly = position.Y % LocalBlockSize;
        }

        public ref Tile GetRef(Point3 position)
        {
            SplitPosition(position, out int bx, out int by, out int lx, out int ly);
            var block = _blocks[bx, by, position.Z];
            if (block == null)
            {
                block = new BlockSimple();
                _blocks[bx, by, position.Z] = block;
            }
            else if (block is not BlockSimple simpleBlock)
            {
                simpleBlock = block.ToSimple();
                _blocks[bx, by, position.Z] = simpleBlock;
                block = simpleBlock;
            }

            return ref ((BlockSimple)block).GetRef(lx, ly);
        }

        public Tile this[int x, int y, int z]
        {
            get => this[new Point3(x, y, z)];
            set => this[new Point3(x, y, z)] = value;
        }

        public Tile this[Point3 p]
        {
            get
            {
                if (!InBounds(p))
                    return default;

                SplitPosition(p, out int bx, out int by, out int lx, out int ly);
                var block = _blocks[bx, by, p.Z];
                return block?.GetTile(lx, ly) ?? default;
            }
            set
            {
                if (!InBounds(p))
                    return;

                SplitPosition(p, out int bx, out int by, out int lx, out int ly);
                var block = _blocks[bx, by, p.Z];
                if (block == null)
                {
                    if (value.Equals(default(Tile)))
                        return;

                    block = new BlockSimilar(default);
                }

                _blocks[bx, by, p.Z] = block.SetTile(lx, ly, value);
            }
        }

        public bool TryGet(Point3 position, out Tile tile)
        {
            if (InBounds(position))
            {
                tile = this[position];
                return true;
            }

            tile = default;
            return false;
        }
    }
}
