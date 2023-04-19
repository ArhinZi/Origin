using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Origin.Source.ECS;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Origin.Source
{
    internal enum VisBufField : byte
    {
        FloorVisible = 1,
        WallVisible
    }

    internal enum RenderLayer : int
    {
        Wall,
        Floor,
        EmbeddedWall,
        EmbeddedFloor
    }

    public class SiteRenderer : IDisposable
    {
        public Site Site;

        public static bool ALL_DISCOVERED = false;

        public Point ChunkSize;

        private Point3 _chunksCount;
        private int _drawLowest;
        private int _drawHighest;

        private SiteVertexBufferChunk[,,] _renderChunkArray;

        private HashSet<Point3> _reloadChunkList = new HashSet<Point3>();

        private Effect _customEffect;
        private AlphaTestEffect _alphaTestEffect;

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Z offset for every Left2Right diagonal block lines.
        /// Blocks in the far diagonal line are appearing behind the ones in the near line.
        /// </summary>
        public static readonly float Z_DIAGONAL_OFFSET = 0.01f;

        public static readonly float Z_LEVEL_OFFSET = 0.01f;

        public static readonly Point BASE_CHUNK_SIZE = new Point(32, 32);
        public static readonly int ONE_MOMENT_DRAW_LEVELS = 16;

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            Site = site;
            //_visBuffer = new byte[_site.Size.X, _site.Size.Y, _site.Size.Z];

            ChunkSize = BASE_CHUNK_SIZE;
            //if (ChunkSize.X < Site.Size.X) ChunkSize.X = Site.Size.X;
            //if (ChunkSize.Y < Site.Size.Y) ChunkSize.Y = Site.Size.Y;
            if (Site.Size.X % ChunkSize.X != 0 || Site.Size.Y % ChunkSize.Y != 0) throw new Exception("Site size is invalid!");

            _drawHighest = Site.CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

            _chunksCount = new Point3(Site.Size.X / ChunkSize.X, Site.Size.Y / ChunkSize.Y, Site.Size.Z);

            _renderChunkArray = new SiteVertexBufferChunk[_chunksCount.X, _chunksCount.Y, _chunksCount.Z];

            _reloadChunkList = new HashSet<Point3>();

            _graphicsDevice = graphicDevice;
            _spriteBatch = new SpriteBatch(MainGame.Instance.GraphicsDevice);

            _customEffect = MainGame.Instance.Content.Load<Effect>("FX/MegaShader");
            _alphaTestEffect = new AlphaTestEffect(_graphicsDevice);

            CalcVisibility();
            Parallel.For(0, _chunksCount.Z, z =>
            //for (int z = 0; z < _chunksCount.Z; z++)
            {
                FillLevel(z);
            });
            /*for (int z = 0; z < _chunksCount.Z; z++)
            {
                SetLevel(z);
            }*/
        }

        private void CalcChunkCellsVisibility(Point3 chunkCoord)
        {
            if (chunkCoord.X >= 0 && chunkCoord.X < _chunksCount.X &&
                chunkCoord.Y >= 0 && chunkCoord.Y < _chunksCount.Y)
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
                {
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;
                        SiteCell tile = Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];
                        if (tile.FloorID != TerrainMaterial.AIR_NULL_MAT_ID)
                        {
                            // Check if tile have neighbors in TL & TR & BL & BR borders
                            if (
                                // Check BR
                                tileCoordX + 1 <= Site.Size.X &&
                                    (tileCoordX + 1 == Site.Size.X ||
                                    Site.Blocks[tileCoordX + 1, tileCoordY, chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    &&
                                // Check BL
                                tileCoordY + 1 <= Site.Size.Y &&
                                    (tileCoordY + 1 == Site.Size.Y ||
                                    Site.Blocks[tileCoordX, tileCoordY + 1, chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    &&
                                // Check TL
                                tileCoordX >= 0 &&
                                    (tileCoordX == 0 ||
                                    Site.Blocks[tileCoordX - 1, tileCoordY, chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    &&
                                // Check TR
                                tileCoordY >= 0 &&
                                    (tileCoordY == 0 ||
                                    Site.Blocks[tileCoordX, tileCoordY - 1, chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    )
                            {
                                // Then at least wall is invisible
                                tile.WallVisual = CellVisual.None;

                                if (tileCoordX + 1 == Site.Size.X || tileCoordY + 1 == Site.Size.Y)
                                {
                                    tile.WallVisual |= CellVisual.Visible;
                                    tile.FloorVisual |= CellVisual.Visible;
                                }

                                // Check if tile have neighbor above
                                if (chunkCoord.Z + 1 < Site.Size.Z &&
                                    Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z + 1].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                {
                                    // Then floor is invisible
                                    tile.FloorVisual |= CellVisual.None;
                                }
                                else
                                {
                                    // Else floor is visible
                                    tile.FloorVisual |= CellVisual.Visible | CellVisual.Discovered;
                                }
                            }
                            else
                            {
                                // Else both visible
                                tile.WallVisual = CellVisual.Visible | CellVisual.Discovered;
                                tile.FloorVisual = CellVisual.Visible | CellVisual.Discovered;
                            }
                        }
                    }
                }
        }

        private void CalcVisibility()
        {
            for (int l = 0; l < _chunksCount.Z; l++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        CalcChunkCellsVisibility(new Point3(x, y, l));
                    }
                }
            }
        }

        /// <summary>
        /// Fill chunk vertices in already created _renderChunkArray
        /// </summary>
        private void FillChunkVertices(
            Point3 chunkCoord)
        {
            if (chunkCoord.X >= 0 && chunkCoord.X < _chunksCount.X &&
                chunkCoord.Y >= 0 && chunkCoord.Y < _chunksCount.Y)
                // Loop through each tile block in the chunk
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
                {
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;
                        SiteCell tile = Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];

                        if (tile.WallID != TerrainMaterial.AIR_NULL_MAT_ID && tile.WallVisual.HasFlag(CellVisual.Visible | CellVisual.Discovered))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[tile.WallID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Wall"][tile.seed % tm.Sprites["Wall"].Count];
                            //sprite = tm.Sprites["Wall"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Back,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));

                            if (tile.EmbeddedWallID != null && tile.WallVisual.HasFlag(CellVisual.Visible))
                            {
                                tm = TerrainMaterial.TerraMats[tile.EmbeddedWallID];
                                sprite = tm.Sprites["Wall"][tile.seed % tm.Sprites["Wall"].Count];
                                c = tm.TerraColor;
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Back,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                            }
                        }
                        else if (tile.WallID != TerrainMaterial.AIR_NULL_MAT_ID &&
                            !tile.WallVisual.HasFlag(CellVisual.Discovered))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[TerrainMaterial.HIDDEN_MAT_ID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Wall"][0];
                            //sprite = tm.Sprites["Wall"][0];
                            c = tm.TerraColor;
                            if (tile.WallVisual.HasFlag(CellVisual.Visible))
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Back,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                            else
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.HiddenBack,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                        }

                        if (tile.FloorID != TerrainMaterial.AIR_NULL_MAT_ID && tile.FloorVisual.HasFlag(CellVisual.Visible | CellVisual.Discovered))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[tile.FloorID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Floor"][tile.seed % tm.Sprites["Floor"].Count];
                            //sprite = tm.Sprites["Floor"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Front,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));

                            if (tile.EmbeddedFloorID != null && tile.FloorVisual.HasFlag(CellVisual.Visible))
                            {
                                tm = TerrainMaterial.TerraMats[tile.EmbeddedFloorID];
                                sprite = tm.Sprites["Floor"][tile.seed % tm.Sprites["Floor"].Count];
                                //sprite = tm.Sprites["Floor"][0];
                                c = tm.TerraColor;
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Front,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                            }
                        }
                        else if (tile.FloorID != TerrainMaterial.AIR_NULL_MAT_ID &&
                            !tile.FloorVisual.HasFlag(CellVisual.Discovered))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[TerrainMaterial.HIDDEN_MAT_ID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Floor"][0];
                            c = tm.TerraColor;
                            if (tile.FloorVisual.HasFlag(CellVisual.Visible))
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Front,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                            else
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.HiddenFront,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                        }
                    }
                }
        }

        /// <summary>
        /// Fill whole level again
        /// </summary>
        /// <param name="level"></param>
        /// <param name="fillWalls"></param>
        /// <param name="fillFloors"></param>
        private void FillLevel(int level)
        {
            for (int x = 0; x < _chunksCount.X; x++)
            //Parallel.For(0, _chunksCount.X, x =>
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    if (_renderChunkArray[x, y, level] == null)
                        _renderChunkArray[x, y, level] = new SiteVertexBufferChunk(this, new Point3(x, y, level));
                    _renderChunkArray[x, y, level].Clear(VertexBufferType.Static);

                    FillChunkVertices(new Point3(x, y, level));
                }
            };
        }

        private void SetLevel(int level)
        {
            for (int x = 0; x < _chunksCount.X; x++)
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    _renderChunkArray[x, y, level].SetStaticBuffer();
                }
            }
        }

        private void FillChunk(Point3 chunkPos)
        {
            if (chunkPos.X >= 0 && chunkPos.X < _chunksCount.X &&
                chunkPos.Y >= 0 && chunkPos.Y < _chunksCount.Y)
            {
                if (_renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z] == null)
                    _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z] = new SiteVertexBufferChunk(this, new Point3(chunkPos.X, chunkPos.Y, chunkPos.Z));
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].Clear(VertexBufferType.Static);
                FillChunkVertices(chunkPos);
            }
        }

        private void SetChunk(Point3 chunkPos)
        {
            if (chunkPos.X >= 0 && chunkPos.X < _chunksCount.X &&
                chunkPos.Y >= 0 && chunkPos.Y < _chunksCount.Y)
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].SetStaticBuffer();
        }

        public void Update(GameTime gameTime)
        {
            // Check if CurrentLevel changed and redraw what need to redraw
            if (_drawHighest != Site.CurrentLevel)
            {
                _drawHighest = Site.CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);
            }

            // Collect all ChunksToReload and redraw them
            foreach (var item in Site.BlocksToReload)
            {
                if (item.Z >= _drawLowest && item.Z <= _drawHighest)
                {
                    int chunkX = (int)item.X / ChunkSize.X;
                    int chunkY = (int)item.Y / ChunkSize.Y;
                    _reloadChunkList.Add(new Point3(chunkX, chunkY, item.Z));
                }
            }
            Site.BlocksToReload.Clear();
            // Make chunk reload 5 times in sec
            if (_reloadChunkList.Count > 0 && gameTime.TotalGameTime.Ticks % 12 == 0)
            {
                Point3 toReload = _reloadChunkList.ToList()[0];
                {
                    Task t1 = Task.Run(() =>
                    {
                        if (toReload.Z - 1 >= 0)
                        {
                            CalcChunkCellsVisibility(toReload + new Point3(0, 0, -1));
                            CalcChunkCellsVisibility(toReload + new Point3(-1, 0, -1));
                            CalcChunkCellsVisibility(toReload + new Point3(0, -1, -1));
                            CalcChunkCellsVisibility(toReload + new Point3(1, 0, -1));
                            CalcChunkCellsVisibility(toReload + new Point3(0, 1, -1));
                            FillChunk(toReload + new Point3(0, 0, -1));
                            FillChunk(toReload + new Point3(-1, 0, -1));
                            FillChunk(toReload + new Point3(0, -1, -1));
                            FillChunk(toReload + new Point3(1, 0, -1));
                            FillChunk(toReload + new Point3(0, 1, -1));
                        }
                    });
                    Task t2 = Task.Run(() =>
                    {
                        CalcChunkCellsVisibility(toReload + new Point3(0, 0, 0));
                        CalcChunkCellsVisibility(toReload + new Point3(-1, 0, 0));
                        CalcChunkCellsVisibility(toReload + new Point3(0, -1, 0));
                        CalcChunkCellsVisibility(toReload + new Point3(1, 0, 0));
                        CalcChunkCellsVisibility(toReload + new Point3(0, 1, 0));
                        FillChunk(toReload + new Point3(0, 0, 0));
                        FillChunk(toReload + new Point3(-1, 0, 0));
                        FillChunk(toReload + new Point3(0, -1, 0));
                        FillChunk(toReload + new Point3(1, 0, 0));
                        FillChunk(toReload + new Point3(0, 1, 0));
                    });
                    Task.WaitAll(t1, t2);
                    SetChunk(toReload + new Point3(0, 0, -1));
                    SetChunk(toReload + new Point3(-1, 0, -1));
                    SetChunk(toReload + new Point3(0, -1, -1));
                    SetChunk(toReload + new Point3(1, 0, -1));
                    SetChunk(toReload + new Point3(0, 1, -1));
                    SetChunk(toReload + new Point3(0, 0, 0));
                    SetChunk(toReload + new Point3(-1, 0, 0));
                    SetChunk(toReload + new Point3(0, -1, 0));
                    SetChunk(toReload + new Point3(1, 0, 0));
                    SetChunk(toReload + new Point3(0, 1, 0));
                }
                _reloadChunkList.Remove(toReload);
            }

            // Test drawing mouse selection on selectedBlock
            {
                SiteCell tile = Site.SelectedBlock;
                if (tile != null)
                {
                    Sprite sprite = Sprite.SpriteSet["SolidSelectionWall"];
                    _renderChunkArray[tile.Position.X / ChunkSize.X, tile.Position.Y / ChunkSize.Y, tile.Position.Z].AddSprite(
                        VertexBufferType.Dynamic,
                        VertexBufferLayer.Back,
                        sprite, new Color(30, 0, 0, 100), tile.Position, new Point(0, 0)
                        );
                    /*sprite = Sprite.SpriteSet["SolidSelectionFloor"];
                    _renderChunkArray[tile.Position.Z][tile.Position.X / BASE_CHUNK_SIZE.X, tile.Position.Y / BASE_CHUNK_SIZE.Y].AddSprite(
                        VertexBufferType.Dynamic, sprite, new Color(30, 0, 0, 100), tile.Position, new Point(0, -Sprite.FLOOR_YOFFSET)
                        );*/
                }
            }

            // Draw Entities
            var query = new QueryDescription().WithAll<DrawComponent, SitePositionComponent>();
            Site.World.ECSworld.Query(in query, (in Entity entity) =>
            {
                var position = entity.Get<SitePositionComponent>();
                var draw = entity.Get<DrawComponent>();
                if (position.Position.Z <= _drawHighest && position.Position.Z > _drawLowest &&
                    position.Site == Site)
                {
                    Sprite sprite;
                    if (position.DirectionOfView != IsometricDirection.NONE)
                    {
                        sprite = draw.Sprites[(int)position.DirectionOfView] != null ?
                        draw.Sprites[(int)position.DirectionOfView] :
                        draw.Sprites[draw.Sprites[0] != null ? 0 : 1];
                    }
                    else
                    {
                        sprite = draw.Sprites[draw.Sprites[0] != null ? 0 : 1];
                    }
                    _renderChunkArray[position.Position.X / ChunkSize.X, position.Position.Y / ChunkSize.Y, position.Position.Z].AddSprite(
                        VertexBufferType.Dynamic,
                        VertexBufferLayer.Back,
                        sprite, Color.White, position.Position, new Point(0, 0),
                        //offsetZ: -SiteRenderer.Z_DIAGONAL_OFFSET / 2,
                        drawSize: Sprite.SPRITE_SIZE
                        );
                }
            });
        }

        public void Draw()
        {
            DrawVertices();
        }

        private void DrawVertices()
        {
            Matrix WVP = Matrix.Multiply(Matrix.Multiply(MainGame.Camera.WorldMatrix, MainGame.Camera.Transformation),
                MainGame.Camera.Projection);

            _customEffect.Parameters["WorldViewProjection"].SetValue(WVP);
            _customEffect.Parameters["DayTime"].SetValue(Site.SiteTime);
            _customEffect.Parameters["MinMaxLevel"].SetValue(new Vector2(_drawLowest, _drawHighest));

            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            for (int z = _drawLowest; z <= _drawHighest; z++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        if (!_renderChunkArray[x, y, z].IsSet)
                            _renderChunkArray[x, y, z].SetStaticBuffer();

                        if (z == _drawHighest)
                            _renderChunkArray[x, y, z].Draw(_customEffect,
                                new VertexBufferLayer[] { VertexBufferLayer.HiddenBack, VertexBufferLayer.Back }.ToArray());
                        else
                            _renderChunkArray[x, y, z].Draw(_customEffect,
                                new VertexBufferLayer[] { VertexBufferLayer.Back, VertexBufferLayer.Front }.ToArray());

                        _renderChunkArray[x, y, z].Clear(VertexBufferType.Dynamic);
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}