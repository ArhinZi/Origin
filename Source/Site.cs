using Origin.Source.Draw;
using Origin.Source.Utils;

namespace Origin.Source
{
    public class Site
    {
        private SiteCell[,,] _blocks;
        public Point3 Size { get; private set; }

        private SiteCell _selectedBlock;
        private int _currentLevel;
        private SiteRenderer _renderer;

        public Site() : this(new Point3(128, 128, 100))
        {
        }

        public Site(Point3 size)
        {
            Size = size;
            GenerateBlockMap();
        }

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                if (value < 0) _currentLevel = 0;
                else if (value > Size.Z - 1) _currentLevel = Size.Z - 1;
                else _currentLevel = value;
            }
        }

        public SiteRenderer Renderer
        {
            get
            {
                if (_renderer == null)
                {
                    _renderer = new SiteRenderer(this, MainGame.Instance.GraphicsDevice);
                }
                return _renderer;
            }
        }

        public SiteCell[,,] Blocks
        {
            get
            {
                if (_blocks == null)
                {
                    _blocks = new SiteCell[Size.X, Size.Y, Size.Z];
                }
                return _blocks;
            }
            private set
            {
                _blocks = value;
            }
        }

        public void SetSelected(Point3 pos)
        {
            _selectedBlock = Blocks[pos.X, pos.Y, pos.Z];
        }

        private void GenerateBlockMap()
        {
            float[,] heightMap = WorldUtils.GenerateHeightMap(Size.X, Size.Y, 0.005f);
            //float[,] heightMap = WorldUtils.GenerateFlatHeightMap(Size.X, Size.Y, 0.6f);

            Blocks = WorldUtils.Generate3dWorldArray(heightMap, Size.X, Size.Y, Size.Z, (int)(Size.Z * 0.7f), 10);
            CurrentLevel = (int)(Size.Z * 0.7f);
        }

        public void Update()
        {
            Renderer.Update();
        }

        public void Draw()
        {
            Renderer.Draw();
        }
    }
}