using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    internal interface ISiteLayer
    {
        public SiteCell this[UInt16 x, UInt16 y] { get; set; }
    }

    internal class SparseLightLayer : ISiteLayer
    {
        public Dictionary<UInt32, SiteCell> Blocks = new Dictionary<UInt32, SiteCell>();

        public SiteCell this[UInt16 x, UInt16 y]
        {
            get
            {
                if (Blocks.ContainsKey(((UInt32)x << 16) | y))
                {
                    return Blocks[((UInt32)x << 16) | y];
                }
                return null;
            }
            set => Blocks[((UInt32)x << 16) | y] = value;
        }
    }

    internal class SparseHeavyLayer : ISiteLayer
    {
        public SiteCell[,] Blocks;

        public SparseHeavyLayer(UInt16 sizeX, UInt16 sizeY)
        {
            Blocks = new SiteCell[sizeX, sizeY];
        }

        public SiteCell this[UInt16 x, UInt16 y]
        {
            get => Blocks[x, y];

            set => Blocks[x, y] = value;
        }
    }

    internal class SparseSiteChunk
    {
        public static readonly int CHUNK_HEIGHT = 8;
        public ISiteLayer[] Layers = new ISiteLayer[CHUNK_HEIGHT];

        public ISiteLayer this[UInt16 subz]
        {
            get => Layers[subz % CHUNK_HEIGHT];
        }

        public void SetLayer(int sublevel, UInt16 heavyLayerX = 0, UInt16 heavyLayerY = 0, bool makeHeavyLayer = false)
        {
            if (!makeHeavyLayer)
                Layers[sublevel] = new SparseLightLayer();
            else
                Layers[sublevel] = new SparseHeavyLayer(heavyLayerX, heavyLayerY);
        }
    }

    public class SparseSiteMap
    {
        public int ChunksCount { get; private set; } = 0;
        public Point3 Size { get; private set; }
        private List<SparseSiteChunk> _mapChunks = new List<SparseSiteChunk>();

        public SparseSiteMap(Point3 Size)
        {
            this.Size = Size;
        }

        public void AddChunk(int number)
        {
            if (ChunksCount == number)
            {
                _mapChunks.Add(new SparseSiteChunk());
                ChunksCount++;
                return;
            }
            throw new Exception("Wrong chunk number");
        }

        public SiteCell this[Point3 p]
        {
            get
            {
                if (p != new Point3(-1, -1, -1))
                    return this[(ushort)p.X, (ushort)p.Y, (ushort)p.Z];
                return null;
            }
        }

        public SiteCell this[UInt16 x, UInt16 y, UInt16 z]
        {
            // get chunk -> get layer from chunk -> get cell
            get
            {
                if (_mapChunks[z / SparseSiteChunk.CHUNK_HEIGHT][z] != null)
                {
                    return _mapChunks[z / SparseSiteChunk.CHUNK_HEIGHT][z][x, y];
                }
                else
                    return null;
            }
            set
            {
                if (z / SparseSiteChunk.CHUNK_HEIGHT == ChunksCount) AddChunk(ChunksCount);
                // TODO: Might be comment in release
                else if (z / SparseSiteChunk.CHUNK_HEIGHT > ChunksCount) throw new Exception("Trying to set too far chunk");

                if (_mapChunks[z / SparseSiteChunk.CHUNK_HEIGHT][z] == null)
                    _mapChunks[z / SparseSiteChunk.CHUNK_HEIGHT].SetLayer(z % SparseSiteChunk.CHUNK_HEIGHT);
                _mapChunks[z / SparseSiteChunk.CHUNK_HEIGHT][z][x, y] = value;
            }
        }
    }
}