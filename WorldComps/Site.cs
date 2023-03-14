using Origin.Draw;
using Origin.Utils;

namespace Origin.WorldComps
{
    public class Site
    {
        private SiteBlock[,,] _blocks;
        public Point3 Size { get; private set; }

        private SiteBlock _selectedBlock;
        private int _currentLevel;
        private SiteRenderer _renderer;

        public Site() : this(new Point3(128, 128, 128))
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
                if (value < 1) _currentLevel = 1;
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

        public SiteBlock[,,] Blocks
        {
            get
            {
                if (_blocks == null)
                {
                    _blocks = new SiteBlock[Size.X, Size.Y, Size.Z];
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
            _selectedBlock.isSelected = false;
            _selectedBlock = Blocks[pos.X, pos.Y, pos.Z];
            _selectedBlock.isSelected = true;
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