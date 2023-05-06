using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.GameComponentsServices;
using Origin.Source.Generators;
using Origin.Source.Utils;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        public SparseSiteMap Blocks { get; set; }

        public Point3 Size { get; private set; }

        public Camera2D Camera { get; private set; }

        public GameWorld World { get; private set; }
        public Point3 SelectedBlock { get; private set; }

        private int _currentLevel;

        public float SiteTime = 0.5f;
        private SiteRenderer Renderer { get; set; }
        private BlockGenerator blockGenerator;
        private SiteGenerator siteGenerator;

        public List<Point3> BlocksToReload { get; private set; }

        public Site(GameWorld world, Point3 size)
        {
            World = world;
            Size = size;
            Blocks = new SparseSiteMap(Size);

            blockGenerator = new BlockGenerator();
            blockGenerator.Parameters.Add("Seed", 12345);
            blockGenerator.Parameters.Add("Scale", 0.005f);
            blockGenerator.Parameters.Add("BaseHeight", 25);
            blockGenerator.Parameters.Add("DirtDepth", 4f);

            siteGenerator = new SiteGenerator(blockGenerator, this);
        }

        public void Init()
        {
            Camera = new Camera2D();
            Camera.Move(new Vector2(0,
                -(CurrentLevel * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET)
                    - Sprite.TILE_SIZE.Y * (Size.X / 2)
                 )));

            BlocksToReload = new List<Point3>();
            // TODO: make event for updating current size of world
            Size = new Point3(Size.X, Size.Y, Blocks.ChunksCount * 8);

            siteGenerator.InitLoad();
            Renderer = new SiteRenderer(this, OriginGame.Instance.GraphicsDevice);
        }

        public SiteCell CellGetOrNull(Point3 pos)
        {
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 &&
                pos.X < Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
                return Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z];
            return null;
        }

        public SiteCell CellGetOrCreate(Point3 pos)
        {
            SiteCell sc = Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z];
            if (sc == null)
            {
                sc = blockGenerator.Generate(this, pos);
                Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z] = sc;
            }
            return sc;
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

        public void SetSelected(Point3 pos)
        {
            if (pos.X < 0 || pos.X >= Size.X || pos.Y < 0 || pos.Y >= Size.Y)
                SelectedBlock = new Point3(-1, -1, -1);
            else
                SelectedBlock = pos;
        }

        public void Update(GameTime gameTime)
        {
            // TODO: make event for updating current size of world
            Size = new Point3(Size.X, Size.Y, Blocks.ChunksCount * 8);

            Point m = Mouse.GetState().Position;
            Point3 sel = WorldUtils.MouseScreenToMap(Camera, m, CurrentLevel);
            SetSelected(new Point3(sel.X, sel.Y, CurrentLevel));

            IGameInfoMonitor debug = OriginGame.Instance.Services.GetService<IGameInfoMonitor>();
            debug.Set("Block POS", sel.ToString(), 10);
            debug.Set("Cam ZOOM", Camera.Zoom.ToString(), 11);
            debug.Set("Cam POS", Camera.Position.ToString(), 12);
            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;

            debug.Set("DayTime", (SiteTime).ToString("#.##"), 15);
            Renderer.Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            Renderer.Draw(gameTime);
        }

        public void Dispose()
        {
            Renderer.Dispose();
        }
    }
}