﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Utils;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Origin.Source
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

        public SiteCell this[UInt16 x, UInt16 y, UInt16 z]
        {
            // get chunk -> get layer from chunk -> get cell
            get => _mapChunks[z / SparseSiteChunk.CHUNK_HEIGHT][z][x, y];
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

    public class Site : IDisposable
    {
        //public SiteCell[,,] Blocks { get; set; }
        public SparseSiteMap Blocks { get; set; }

        public Point3 Size { get; private set; }

        public Camera2D Camera { get; private set; }

        public MainWorld World { get; private set; }
        public SiteCell SelectedBlock { get; private set; }

        private int _currentLevel;

        public float SiteTime = 0.5f;
        private SiteRenderer Renderer { get; set; }

        public List<Point3> BlocksToReload { get; private set; }

        public Site(MainWorld world, Point3 size)
        {
            World = world;
            Size = size;
            Blocks = new SparseSiteMap(Size);
        }

        public void Init()
        {
            Camera = new Camera2D();
            Camera.Move(new Vector2(0,
                -(CurrentLevel * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET)
                    - Sprite.TILE_SIZE.Y * (Size.X / 2)
                 )));

            BlocksToReload = new List<Point3>();

            Renderer = new SiteRenderer(this, OriginGame.Instance.GraphicsDevice);
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
                SelectedBlock = Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z];
        }

        public SiteCell GetOrNull(Point3 pos)
        {
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 &&
                pos.X < Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
                return Blocks[(ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z];
            return null;
        }

        public void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Point3 sel = WorldUtils.MouseScreenToMap(Camera, m, CurrentLevel);
            SetSelected(new Point3(sel.X, sel.Y, CurrentLevel));
            OriginGame.Instance.debug.Add("Block: " + sel.ToString());
            OriginGame.Instance.debug.Add("Cam ZOOM: " + Camera.Zoom.ToString());
            OriginGame.Instance.debug.Add("Cam POS: " + Camera.Position.ToString());
            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;

            OriginGame.Instance.debug.Add("DayTime: " + (SiteTime).ToString("#.##"));
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