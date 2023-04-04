using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Utils;

using System;
using System.Collections.Generic;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        private SiteCell[,,] _blocks;
        public Point3 Size { get; private set; }

        public SiteCell selectedBlock;
        private int _currentLevel;
        private SiteRenderer _renderer;

        public List<Point3> BlocksToReload { get; private set; }

        // 64 128 192 256
        public Site() : this(new Point3(128, 128, 100))
        {
        }

        public Site(Point3 size)
        {
            Size = size;
            GenerateBlockMap();
            MainGame.Camera.Move(new Vector2(0,
                -(CurrentLevel * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET)
                    - Sprite.TILE_SIZE.Y * (Size.X / 2)
                 )));

            BlocksToReload = new List<Point3>();
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
            if (pos.X < 0 || pos.X >= Size.X || pos.Y < 0 || pos.Y >= Size.Y)
                selectedBlock = null;
            else
                selectedBlock = Blocks[pos.X, pos.Y, pos.Z];
        }

        private void GenerateBlockMap()
        {
            float[,] heightMap = WorldUtils.GenerateHeightMap(Size.X, Size.Y, 0.005f);
            //float[,] heightMap = WorldUtils.GenerateFlatHeightMap(Size.X, Size.Y);

            Blocks = WorldUtils.Generate3dWorldArray(this, heightMap, Size, (int)(Size.Z * 0.7f), 5);
            CurrentLevel = (int)(Size.Z * 0.8f);
        }

        public void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Point3 sel = WorldUtils.MouseScreenToMap(m, CurrentLevel);
            SetSelected(new Point3(sel.X, sel.Y, CurrentLevel));
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