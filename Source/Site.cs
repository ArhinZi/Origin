using Arch.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.GameComponentsServices;
using Origin.Source.Generators;
using Origin.Source.IO;
using Origin.Source.Utils;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        public GlobalWorld OriginWorld { get; private set; }

        public SiteStructureComp StructureComponent;
        public SiteRenderer Renderer { get; set; }

        public Point3 SelectedBlock { get; private set; }
        private int _currentLevel;
        public float SiteTime = 0.5f;

        public List<Point3> BlocksToReload { get; private set; }

        private IGameInfoMonitor debug { get => OriginGame.Instance.Services.GetService<IGameInfoMonitor>(); }

        public Site(GlobalWorld world, Point3 size)
        {
            OriginWorld = world;
            StructureComponent = new SiteStructureComp(this, size);
        }

        public void Init()
        {
            BlocksToReload = new List<Point3>();

            Renderer = new SiteRenderer(this, OriginGame.Instance.GraphicsDevice);
        }

        /*public Entity CellGetOrNull(Point3 pos)
        {
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 &&
                pos.X < Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
                return Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z];
            return Entity.Null;
        }

        public Entity CellGetOrCreate(Point3 pos)
        {
            Entity sc = Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z];
            if (sc == Entity.Null)
            {
                sc = blockGenerator.Generate(this, pos);
                Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z] = sc;
            }
            return sc;
        }*/

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                if (value < 0) _currentLevel = 0;
                else if (value > StructureComponent.Size.Z - 1) _currentLevel = StructureComponent.Size.Z - 1;
                else _currentLevel = value;
            }
        }

        /*
                public void SetSelected(Point3 pos)
                {
                    if (pos.X < 0 || pos.X >= Size.X || pos.Y < 0 || pos.Y >= Size.Y)
                        SelectedBlock = new Point3(-1, -1, -1);
                    else
                        SelectedBlock = pos;
                }*/

        public void Update(GameTime gameTime)
        {
            InputUpdate();
            Point m = Mouse.GetState().Position;
            StructureComponent.Update(gameTime);
            /*Point3 sel = WorldUtils.MouseScreenToMap(Camera, m, CurrentLevel);
            SetSelected(new Point3(sel.X, sel.Y, CurrentLevel));

            debug.Set("Block POS", sel.ToString(), 10);*/

            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;
            debug.Set("CurrentLevel", CurrentLevel.ToString(), 6);
            debug.Set("DayTime", (SiteTime).ToString("#.##"), 15);
            Renderer.Update(gameTime);
        }

        private void InputUpdate()
        {
            if (InputManager.JustPressedAndHoldDelayed("world.level.minus"))
                CurrentLevel++;
            if (InputManager.JustPressedAndHoldDelayed("world.level.plus"))
                CurrentLevel--;
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