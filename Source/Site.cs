using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Events;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        public SiteCell[,,] Blocks { get; set; }
        public Point3 Size { get; private set; }
        public Camera2D Camera { get; private set; }

        public MainWorld World { get; private set; }
        public SiteCell SelectedBlock { get; private set; }

        private int _currentLevel;

        public float SiteTime = 0.5f;

        public List<Point3> BlocksToReload { get; private set; }

        public Site(MainWorld world, Point3 size)
        {
            World = world;
            Size = size;
            Blocks = new SiteCell[Size.X, Size.Y, Size.Z];
        }

        public void Init()
        {
            Camera = new Camera2D();
            Camera.Move(new Vector2(0,
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

        public void SetSelected(Point3 pos)
        {
            if (pos.X < 0 || pos.X >= Size.X || pos.Y < 0 || pos.Y >= Size.Y)
                SelectedBlock = null;
            else
                SelectedBlock = Blocks[pos.X, pos.Y, pos.Z];
        }

        public SiteCell GetOrNull(Point3 pos)
        {
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 &&
                pos.X < Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
                return Blocks[pos.X, pos.Y, pos.Z];
            return null;
        }

        public void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Point3 sel = WorldUtils.MouseScreenToMap(Camera, m, CurrentLevel);
            SetSelected(new Point3(sel.X, sel.Y, CurrentLevel));
            EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["SelectedBlock"] = sel.ToString(),
                ["DayTime"] = (SiteTime).ToString("#.##")
            }));
            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;
        }

        public void Dispose()
        {
        }
    }
}