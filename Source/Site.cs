using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Draw;
using Origin.Source.Utils;

using System;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        private SiteCell[,,] _blocks;
        public Point3 Size { get; private set; }

        public SiteCell selectedBlock;
        private int _currentLevel;
        private SiteRenderer _renderer;

        // 64 128 192 256
        public Site() : this(new Point3(256, 256, 100))
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
            selectedBlock = Blocks[pos.X, pos.Y, pos.Z];
        }

        private void GenerateBlockMap()
        {
            float[,] heightMap = WorldUtils.GenerateHeightMap(Size.X, Size.Y, 0.005f);
            //float[,] heightMap = WorldUtils.GenerateFlatHeightMap(Size.X, Size.Y, 0.6f);

            Blocks = WorldUtils.Generate3dWorldArray(heightMap, Size.X, Size.Y, Size.Z, (int)(Size.Z * 0.7f), 10);
            CurrentLevel = (int)(Size.Z * 0.7f);
        }

        public void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Point sel = WorldUtils.MouseScreenToMap(m, CurrentLevel);
            MainGame.Instance.debug.Add("Block: " + sel.ToString());

            Renderer.Update(gameTime);
        }

        public void Draw()
        {
            Renderer.Draw();
        }

        public void Dispose()
        {
            Renderer.Dispose();
        }
    }
}