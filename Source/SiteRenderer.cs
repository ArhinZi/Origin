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
using System.Numerics;
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

        private CircleSliceArray<SiteVertexBufferChunk[,]> _renderChunkArray;

        private HashSet<Point3> _reloadChunkList = new HashSet<Point3>();

        // Shader with fix alpha blend
        // Using instead of BasicEffect, but fixes problem with bad alpha blending with z offset
        private AlphaTestEffect _alphaTestEffect;

        private BasicEffect _basicEffect;

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Z offset for every Left2Right diagonal block lines.
        /// Blocks in the far diagonal line are appearing behind the ones in the near line.
        /// </summary>
        public static readonly float Z_DIAGONAL_OFFSET = 0.01f;

        public static readonly Point BASE_CHUNK_SIZE = new Point(64, 64);
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

            _renderChunkArray = new CircleSliceArray<SiteVertexBufferChunk[,]>(ONE_MOMENT_DRAW_LEVELS);

            _reloadChunkList = new HashSet<Point3>();

            _graphicsDevice = graphicDevice;
            _spriteBatch = new SpriteBatch(MainGame.Instance.GraphicsDevice);

            CalcVisibility();

            _alphaTestEffect = new AlphaTestEffect(MainGame.Instance.GraphicsDevice);
            _alphaTestEffect.VertexColorEnabled = true;

            _basicEffect = new BasicEffect(MainGame.Instance.GraphicsDevice);
            //_basicEffect.AmbientLightColor = new Vector3(0, 0, 0);
            _basicEffect.EnableDefaultLighting();
        }

        private Vector2 MapToScreen(int mapX, int mapY, int mapZ)
        {
            var screenX = (mapX - mapY) * Sprite.TILE_SIZE.X / 2;
            var screenY = (mapY + mapX) * Sprite.TILE_SIZE.Y / 2 + -mapZ * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET);

            Vector2 res = new Vector2(screenX, screenY);
            //res += MainGame.cam.Pos;
            //res *= MainGame.cam.Zoom;
            //Matrix inverted = Matrix.Invert(MainGame.cam.get_transformation(MainGame.instance.GraphicsDevice));
            return res;
        }

        private void CalcChunkCellsVisibility(Point3 chunkCoord)
        {
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
        /// <param name="chunkCoord"></param>
        /// <param name="fillWalls"></param>
        /// <param name="fillEmbWall"></param>
        /// <param name="fillFloors"></param>
        /// <param name="fillEmbFloor"></param>
        /// <param name="drawHidden"></param>
        private void FillChunkVertices(
            Point3 chunkCoord,
            bool fillWalls,
            bool fillFloors,
            bool forceVisible = false)
        {
            // Loop through each tile block in the chunk
            for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
            {
                for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                {
                    int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                    int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;
                    SiteCell tile = Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];

                    if (fillWalls)
                    {
                        if (tile.WallID != TerrainMaterial.AIR_NULL_MAT_ID && tile.WallVisual.HasFlag(CellVisual.Visible | CellVisual.Discovered))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[tile.WallID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Wall"][tile.seed % tm.Sprites["Wall"].Count];
                            //sprite = tm.Sprites["Wall"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.Z][chunkCoord.X, chunkCoord.Y].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Back,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));

                            if (tile.EmbeddedWallID != null && tile.WallVisual.HasFlag(CellVisual.Visible))
                            {
                                tm = TerrainMaterial.TerraMats[tile.EmbeddedWallID];
                                sprite = tm.Sprites["Wall"][tile.seed % tm.Sprites["Wall"].Count];
                                c = tm.TerraColor;
                                _renderChunkArray[chunkCoord.Z][chunkCoord.X, chunkCoord.Y].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Back,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                            }
                        }
                        else if (tile.WallID != TerrainMaterial.AIR_NULL_MAT_ID &&
                            !tile.WallVisual.HasFlag(CellVisual.Discovered) && (tile.WallVisual.HasFlag(CellVisual.Visible) || forceVisible))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[TerrainMaterial.HIDDEN_MAT_ID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Wall"][0];
                            //sprite = tm.Sprites["Wall"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.Z][chunkCoord.X, chunkCoord.Y].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Back,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                        }
                    }

                    if (fillFloors)
                    {
                        if (tile.FloorID != TerrainMaterial.AIR_NULL_MAT_ID && tile.FloorVisual.HasFlag(CellVisual.Visible | CellVisual.Discovered))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[tile.FloorID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Floor"][tile.seed % tm.Sprites["Floor"].Count];
                            //sprite = tm.Sprites["Floor"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.Z][chunkCoord.X, chunkCoord.Y].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Front,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));

                            if (tile.EmbeddedFloorID != null && tile.FloorVisual.HasFlag(CellVisual.Visible))
                            {
                                tm = TerrainMaterial.TerraMats[tile.EmbeddedFloorID];
                                sprite = tm.Sprites["Floor"][tile.seed % tm.Sprites["Floor"].Count];
                                //sprite = tm.Sprites["Floor"][0];
                                c = tm.TerraColor;
                                _renderChunkArray[chunkCoord.Z][chunkCoord.X, chunkCoord.Y].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Front,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                            }
                        }
                        else if (tile.FloorID != TerrainMaterial.AIR_NULL_MAT_ID &&
                            !tile.FloorVisual.HasFlag(CellVisual.Discovered) && (tile.FloorVisual.HasFlag(CellVisual.Visible) || forceVisible))
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[TerrainMaterial.HIDDEN_MAT_ID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Floor"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.Z][chunkCoord.X, chunkCoord.Y].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Front,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                        }
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
        private void FillLevel(int level, bool fillWalls = true, bool fillFloors = true)
        {
            if (_renderChunkArray[level] == null)
                _renderChunkArray[level] = new SiteVertexBufferChunk[_chunksCount.X, _chunksCount.Y];

            bool forceVisible = false;
            if (level == _drawHighest)
                forceVisible = true;

            for (int x = 0; x < _chunksCount.X; x++)
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    if (_renderChunkArray[level][x, y] == null)
                        _renderChunkArray[level][x, y] = new SiteVertexBufferChunk(this, new Point3(x, y, level));
                    _renderChunkArray[level][x, y].Clear(VertexBufferType.Static);

                    FillChunkVertices(new Point3(x, y, level), fillWalls, fillFloors, forceVisible);
                }
            }
            for (int x = 0; x < _chunksCount.X; x++)
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    //if (_renderChunkArray[level][x, y] != null)
                    _renderChunkArray[level][x, y].SetStaticBuffer();
                }
            }
        }

        private void ReFillChunk(Point3 chunkPos, bool fillWalls = true, bool fillFloors = true)
        {
            bool forceVisible = false;
            if (chunkPos.Z == _drawHighest)
                forceVisible = true;

            if (_renderChunkArray[chunkPos.Z] != null && _renderChunkArray[chunkPos.Z][chunkPos.X, chunkPos.Y] == null)
                _renderChunkArray[chunkPos.Z][chunkPos.X, chunkPos.Y] = new SiteVertexBufferChunk(this, new Point3(chunkPos.X, chunkPos.Y, chunkPos.Z));
            _renderChunkArray[chunkPos.Z][chunkPos.X, chunkPos.Y].Clear(VertexBufferType.Static);
            FillChunkVertices(new Point3(chunkPos.X, chunkPos.Y, chunkPos.Z), fillWalls, fillFloors, forceVisible);

            _renderChunkArray[chunkPos.Z][chunkPos.X, chunkPos.Y].SetStaticBuffer();
        }

        public void Update(GameTime gameTime)
        {
            // First time slow drawing layer by layer
            if (_renderChunkArray.Count < ONE_MOMENT_DRAW_LEVELS)
            {
                if (gameTime.TotalGameTime.Ticks % 5 == 0)
                {
                    int chunkCoordZ = _drawLowest + _renderChunkArray.Count;
                    FillLevel(chunkCoordZ, fillFloors: true);
                }
            }
            // Check if CurrentLevel changed and redraw what need to redraw
            else if (_drawHighest != Site.CurrentLevel)
            {
                bool MoveUp = true;
                if (_drawHighest > Site.CurrentLevel) MoveUp = false;
                _drawHighest = Site.CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

                FillLevel(_drawHighest, true, false);

                if (MoveUp)
                {
                    FillLevel(_drawHighest - 1);
                    MainGame.Camera.Move(new Vector2(
                        0,
                        -(Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET)
                        ));
                }
                else
                {
                    FillLevel(_drawLowest);
                    MainGame.Camera.Move(new Vector2(
                        0,
                        (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET)
                        ));
                }
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
            foreach (var chunkPos in _reloadChunkList)
            {
                if (chunkPos.Z - 1 >= 0)
                {
                    CalcChunkCellsVisibility(chunkPos + new Point3(0, 0, -1));
                    ReFillChunk(chunkPos + new Point3(0, 0, -1));
                }
                CalcChunkCellsVisibility(chunkPos);
                ReFillChunk(chunkPos, true, false);
            }
            Site.BlocksToReload.Clear();
            _reloadChunkList.Clear();

            // Test drawing mouse selection on selectedBlock
            {
                SiteCell tile = Site.SelectedBlock;
                if (tile != null && _renderChunkArray[tile.Position.Z] != null)
                {
                    Sprite sprite;
                    sprite = Sprite.SpriteSet["SolidSelectionWall"];
                    _renderChunkArray[tile.Position.Z][tile.Position.X / ChunkSize.X, tile.Position.Y / ChunkSize.Y].AddSprite(
                        VertexBufferType.Dynamic,
                        VertexBufferLayer.Front,
                        sprite, new Color(30, 0, 0, 100), tile.Position, new Point(0, 0)
                        );
                    /*sprite = Sprite.SpriteSet["SolidSelectionFloor"];
                    _renderChunkArray[tile.Position.Z][tile.Position.X / BASE_CHUNK_SIZE.X, tile.Position.Y / BASE_CHUNK_SIZE.Y].AddSprite(
                        VertexBufferType.Dynamic, sprite, new Color(30, 0, 0, 100), tile.Position, new Point(0, -Sprite.FLOOR_YOFFSET)
                        );*/
                }
            }

            var query = new QueryDescription().WithAll<DrawComponent, SitePositionComponent>();
            Site.World.ECSworld.Query(in query, (in Entity entity) =>
            {
                var position = entity.Get<SitePositionComponent>();
                var draw = entity.Get<DrawComponent>();
                if (position.Position.Z <= _drawHighest && position.Position.Z > _drawLowest &&
                    _renderChunkArray[position.Position.Z] != null &&
                    position.Site == Site)
                {
                    Sprite sprite = draw.Sprite;
                    _renderChunkArray[position.Position.Z][position.Position.X / ChunkSize.X, position.Position.Y / ChunkSize.Y].AddSprite(
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
            _alphaTestEffect.World = MainGame.Camera.WorldMatrix;
            _alphaTestEffect.View = MainGame.Camera.Transformation;
            _alphaTestEffect.Projection = MainGame.Camera.Projection;
            _alphaTestEffect.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            _alphaTestEffect.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            for (int z = _drawLowest; z <= _drawHighest; z++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        if (_renderChunkArray[z] != null && _renderChunkArray[z][x, y] != null)
                        {
                            //_renderChunkArray[z][x, y].SetStaticBuffer();
                            _renderChunkArray[z][x, y].Draw(_alphaTestEffect);
                            _renderChunkArray[z][x, y].Clear(VertexBufferType.Dynamic);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}