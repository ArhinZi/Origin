using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.Utils;

using SharpDX.Direct2D1.Effects;

using System;
using System.Collections.Generic;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        public World ECSWorld { get; private set; }
        public SiteTileContainer Blocks { get; set; }
        public Point3 Size { get; private set; }
        public Camera2D Camera { get; private set; }

        public MainWorld World { get; private set; }
        public Entity SelectedBlock { get; private set; }

        private int _currentLevel;
        public int PreviousLevel { get; private set; }

        public float SiteTime = 0.5f;

        public List<Point3> BlocksToReload { get; private set; }

        public Site(MainWorld world, Point3 size)
        {
            ECSWorld = Arch.Core.World.Create();
            World = world;
            Size = size;
            Blocks = new SiteTileContainer(Size);

            CurrentLevel = (int)(Size.Z * 0.8f);

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
                if (_currentLevel != value)
                {
                    PreviousLevel = _currentLevel;
                    if (value < 0) _currentLevel = 0;
                    else if (value > Size.Z - 1) _currentLevel = Size.Z - 1;
                    else _currentLevel = value;
                }
            }
        }

        public void SetSelected(Point3 pos)
        {
            if (pos.X < 0 || pos.X >= Size.X || pos.Y < 0 || pos.Y >= Size.Y)
                SelectedBlock = Entity.Null;
            else
                SelectedBlock = Blocks[pos.X, pos.Y, pos.Z];
        }

        public Entity GetOrNull(Point3 pos)
        {
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 &&
                pos.X < Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
                return Blocks[pos.X, pos.Y, pos.Z];
            return Entity.Null;
        }

        public void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Point3 sel = WorldUtils.MouseScreenToMap(Camera, m, CurrentLevel);
            SetSelected(new Point3(sel.X, sel.Y, CurrentLevel));
            EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["SelectedBlock"] = sel.ToString(),
                ["Layer"] = CurrentLevel.ToString(),
                ["DayTime"] = (SiteTime).ToString("#.##")
            }));
            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;
        }

        public bool RemoveWall(Entity ent)
        {
            var onSite = ent.Get<OnSitePosition>();
            Point3 pos = onSite.position;
            if (Blocks[pos.X, pos.Y, pos.Z].Has<TileStructure>())
            {
                ref var structure = ref Blocks[pos.X, pos.Y, pos.Z].Get<TileStructure>();
                structure.WallMaterial = null;
                structure.WallEmbeddedMaterial = null;
                if (structure == TileStructure.Null)
                    Blocks[pos.X, pos.Y, pos.Z].Remove<TileStructure>();
                return true;
            }
            return false;
        }

        public bool RemoveFloor(Entity ent)
        {
            var onSite = ent.Get<OnSitePosition>();
            Point3 pos = onSite.position;
            if (Blocks[pos.X, pos.Y, pos.Z].Has<TileStructure>())
            {
                ref var structure = ref Blocks[pos.X, pos.Y, pos.Z].Get<TileStructure>();
                structure.FloorMaterial = null;
                structure.FloorEmbeddedMaterial = null;
                if (structure == TileStructure.Null)
                    Blocks[pos.X, pos.Y, pos.Z].Remove<TileStructure>();
                return true;
            }
            return false;
        }

        public void RemoveBlock(Entity ent)
        {
            RemoveBlock(ent);
            RemoveFloor(ent);
        }

        public void Dispose()
        {
        }
    }
}