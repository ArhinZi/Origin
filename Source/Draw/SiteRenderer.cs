using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Utils;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Origin.Source.Draw
{
    internal enum VisBufField : byte
    {
        FloorVisible = 1,
        WallVisible
    }

    internal enum RenderLayer : int
    {
        Block,
        Floor
    }

    public class SiteRenderer : IDisposable
    {
        private Site _site;
        //private byte[,,] _visBuffer;

        private Point3 _chunksCount;
        private Point _chunkSize;
        private int _drawLowest;
        private int _drawHighest;
        private VertexPositionColorTexture[] _wallVertices;

        private VertexPositionColorTexture[] _floorVertices;

        /// <summary>
        /// Layer 0 - Block
        /// Layer 1 - Floor
        /// </summary>
        private CircleSliceArray<DynamicVertexBuffer[,]>[] _renderLayers;

        //private CircleSliceArray<DynamicVertexBuffer[,]> _vertexBuffersBlock;
        //private CircleSliceArray<DynamicVertexBuffer[,]> _vertexBuffersGround;
        private BasicEffect effect;

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        public static readonly float Z_DIAGONAL_OFFSET = 0.01f;
        public static readonly Point BASE_CHUNK_SIZE = new Point(64, 64);
        public static readonly int ONE_MOMENT_DRAW_LEVELS = 16;

        public static readonly int LAYER_COUNT = 2;

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            _site = site;
            //_visBuffer = new byte[_site.Size.X, _site.Size.Y, _site.Size.Z];

            _chunkSize = BASE_CHUNK_SIZE;
            //if (_chunkSize.X < _site.Size.X) _chunkSize.X = _site.Size.X;
            //if (_chunkSize.Y < _site.Size.Y) _chunkSize.Y = _site.Size.Y;
            if (_site.Size.X % _chunkSize.X != 0 || _site.Size.Y % _chunkSize.Y != 0) throw new Exception("Site size is invalid!");

            _drawHighest = _site.CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

            _chunksCount = new Point3(_site.Size.X / _chunkSize.X, _site.Size.Y / _chunkSize.Y, _site.Size.Z);

            _renderLayers = new CircleSliceArray<DynamicVertexBuffer[,]>[LAYER_COUNT];
            _renderLayers[(int)RenderLayer.Block] = new CircleSliceArray<DynamicVertexBuffer[,]>(ONE_MOMENT_DRAW_LEVELS);
            _renderLayers[(int)RenderLayer.Floor] = new CircleSliceArray<DynamicVertexBuffer[,]>(ONE_MOMENT_DRAW_LEVELS);

            _graphicsDevice = graphicDevice;
            _spriteBatch = new SpriteBatch(MainGame.Instance.GraphicsDevice);

            //CalcVisBuffer();
            CalcVisibility();

            //ReCalcVertexBuffers();
            effect = new BasicEffect(MainGame.Instance.GraphicsDevice);
            effect.TextureEnabled = true;
            effect.VertexColorEnabled = true;
            effect.Texture = Texture.GetTextureByName(Texture.MAIN_TEXTURE_NAME);
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

        /*private void CalcVisBuffer()
        {
            for (int z = _site.Size.Z - 1; z >= 0; z--)
            {
                for (int y = _site.Size.Y - 1; y >= 0; y--)
                {
                    for (int x = _site.Size.X - 1; x >= 0; x--)
                    {
                        _visBuffer[x, y, z] = new byte();
                        if (_site.Blocks[x, y, z].FloorID != WorldUtils.AIR_NULL_MAT_ID)
                        {
                            if (DiffUtils.InBounds(x + 1, -1, _site.Size.X)
                                && _site.Blocks[x + 1, y, z].WallID != WorldUtils.AIR_NULL_MAT_ID &&
                                DiffUtils.InBounds(y + 1, -1, _site.Size.Y)
                                && _site.Blocks[x, y + 1, z].WallID != WorldUtils.AIR_NULL_MAT_ID &&
                                DiffUtils.InBounds(x - 1, -1, _site.Size.X)
                                && _site.Blocks[x - 1, y, z].WallID != WorldUtils.AIR_NULL_MAT_ID &&
                                DiffUtils.InBounds(y - 1, -1, _site.Size.Y)
                                && _site.Blocks[x, y - 1, z].WallID != WorldUtils.AIR_NULL_MAT_ID)
                            {
                                ByteField.SetBit(ref _visBuffer[x, y, z], (byte)VisBufField.WallVisible, false);

                                if (DiffUtils.InBounds(z + 1, 0, _site.Size.Z))
                                {
                                    if (_site.Blocks[x, y, z + 1].WallID == WorldUtils.AIR_NULL_MAT_ID)
                                    {
                                        ByteField.SetBit(ref _visBuffer[x, y, z], (byte)VisBufField.FloorVisible, true);
                                    }
                                    else
                                    {
                                        ByteField.SetBit(ref _visBuffer[x, y, z], (byte)VisBufField.FloorVisible, false);
                                    }
                                }
                            }
                            else
                            {
                                ByteField.SetBit(ref _visBuffer[x, y, z], (byte)VisBufField.WallVisible, true);
                                ByteField.SetBit(ref _visBuffer[x, y, z], (byte)VisBufField.FloorVisible, true);
                            }
                        }
                    }
                }
            }
        }*/

        private void CalcChunkCellsVisibility(Point3 chunkCoord)
        {
            for (int tileInChunkCoordX = 0; tileInChunkCoordX < _chunkSize.X; tileInChunkCoordX++)
            {
                for (int tileInChunkCoordY = 0; tileInChunkCoordY < _chunkSize.Y; tileInChunkCoordY++)
                {
                    int tileCoordX = chunkCoord.X * _chunkSize.X + tileInChunkCoordX;
                    int tileCoordY = chunkCoord.Y * _chunkSize.Y + tileInChunkCoordY;
                    SiteCell tile = _site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];
                    if (tile.FloorID != WorldUtils.AIR_NULL_MAT_ID)
                    {
                        if (
                            (DiffUtils.InBounds(tileCoordX + 1, -1, _site.Size.X) && _site.Blocks[tileCoordX + 1, tileCoordY, chunkCoord.Z].WallID != WorldUtils.AIR_NULL_MAT_ID)
                                &&
                            (DiffUtils.InBounds(tileCoordY + 1, -1, _site.Size.Y) && _site.Blocks[tileCoordX, tileCoordY + 1, chunkCoord.Z].WallID != WorldUtils.AIR_NULL_MAT_ID)
                                &&
                            (DiffUtils.InBounds(tileCoordX - 1, -1, _site.Size.X) && _site.Blocks[tileCoordX - 1, tileCoordY, chunkCoord.Z].WallID != WorldUtils.AIR_NULL_MAT_ID)
                                &&
                            (DiffUtils.InBounds(tileCoordY - 1, -1, _site.Size.Y) && _site.Blocks[tileCoordX, tileCoordY - 1, chunkCoord.Z].WallID != WorldUtils.AIR_NULL_MAT_ID))
                        {
                            tile.IsWallVisible = false;
                            if (DiffUtils.InBounds(chunkCoord.Z + 1, 0, _site.Size.Z))
                            {
                                if (_site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z + 1].WallID == WorldUtils.AIR_NULL_MAT_ID)
                                {
                                    tile.IsFloorVisible = true;
                                }
                                else
                                {
                                    tile.IsFloorVisible = false;
                                }
                            }
                        }
                        else
                        {
                            tile.IsWallVisible = true;
                            tile.IsFloorVisible = true;
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

        private void FillChunkAir(Point3 chunkCoord)
        {
        }

        private void VerticeAdder(Sprite tt, int x, int y, int z,
            ref int index, ref VertexPositionColorTexture[] vertices, Vector2 offset, Color c)
        {
            Rectangle textureRect = tt.RectPos;
            var VertexX = (x - y) * Sprite.TILE_SIZE.X / 2 + offset.X;
            var VertexY = (y + x) * Sprite.TILE_SIZE.Y / 2 - z * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET) + offset.Y;
            var VertexZ = 0;
            //(x+y)*Z_DIAGONAL_OFFSET;

            Vector3 topLeft =
                new Vector3(VertexX, VertexY, VertexZ);
            Vector3 topRight =
                new Vector3(VertexX + textureRect.Width, VertexY, VertexZ);
            Vector3 bottomLeft =
                new Vector3(VertexX, VertexY + textureRect.Height, VertexZ);
            Vector3 bottomRight =
                new Vector3(VertexX + textureRect.Width, VertexY + textureRect.Height, VertexZ);

            // Calculate the texture coordinates for the tile
            Vector2 textureTopLeft = new Vector2((float)textureRect.Left / tt.Texture.Width, (float)textureRect.Top / tt.Texture.Height);
            Vector2 textureTopRight = new Vector2((float)textureRect.Right / tt.Texture.Width, (float)textureRect.Top / tt.Texture.Height);
            Vector2 textureBottomLeft = new Vector2((float)textureRect.Left / tt.Texture.Width, (float)textureRect.Bottom / tt.Texture.Height);
            Vector2 textureBottomRight = new Vector2((float)textureRect.Right / tt.Texture.Width, (float)textureRect.Bottom / tt.Texture.Height);

            // Add the vertices for the tile to the vertex buffer
            vertices[index++] = new VertexPositionColorTexture(topLeft, c, textureTopLeft);
            vertices[index++] = new VertexPositionColorTexture(topRight, c, textureTopRight);
            vertices[index++] = new VertexPositionColorTexture(bottomLeft, c, textureBottomLeft);

            vertices[index++] = new VertexPositionColorTexture(topRight, c, textureTopRight);
            vertices[index++] = new VertexPositionColorTexture(bottomRight, c, textureBottomRight);
            vertices[index++] = new VertexPositionColorTexture(bottomLeft, c, textureBottomLeft);
        }

        private int FillWallVertices(
            CircleSliceArray<DynamicVertexBuffer[,]> vb,
            Point3 chunkCoord,
            bool drawHidden = false)
        {
            _wallVertices = new VertexPositionColorTexture[_chunkSize.X * _chunkSize.Y * 6];

            int index = 0;

            // Loop through each tile block in the chunk
            for (int tileInChunkCoordX = 0; tileInChunkCoordX < _chunkSize.X; tileInChunkCoordX++)
            {
                for (int tileInChunkCoordY = 0; tileInChunkCoordY < _chunkSize.Y; tileInChunkCoordY++)
                {
                    int tileCoordX = chunkCoord.X * _chunkSize.X + tileInChunkCoordX;
                    int tileCoordY = chunkCoord.Y * _chunkSize.Y + tileInChunkCoordY;
                    SiteCell tile = _site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];
                    if (tile.WallID != WorldUtils.AIR_NULL_MAT_ID && tile.IsWallVisible)
                    {
                        TerrainMaterial tm = TerrainMaterial.TerraMats[tile.WallID];
                        Sprite sprite;
                        Color c = Color.Wheat;
                        sprite = tm.Sprites["Wall"];
                        c = tm.TerraColor;
                        VerticeAdder(sprite, tileCoordX, tileCoordY, chunkCoord.Z, ref index, ref _wallVertices, Vector2.Zero, c);
                    }
                    else if (drawHidden && tile.WallID != WorldUtils.AIR_NULL_MAT_ID && !tile.IsWallVisible)
                    {
                        TerrainMaterial tm = TerrainMaterial.TerraMats[WorldUtils.HIDDEN_MAT_ID];
                        Sprite sprite;
                        Color c = Color.Wheat;
                        sprite = tm.Sprites["Wall"];
                        c = tm.TerraColor;
                        VerticeAdder(sprite, tileCoordX, tileCoordY, chunkCoord.Z, ref index, ref _wallVertices, Vector2.Zero, c);
                    }
                }
            }
            return index;
        }

        private int FillFloorVertices(CircleSliceArray<DynamicVertexBuffer[,]> vb, Point3 chunkCoord)
        {
            _floorVertices = new VertexPositionColorTexture[_chunkSize.X * _chunkSize.Y * 6];

            int index = 0;

            // Loop through each tile in the chunk
            for (int tileInChunkCoordX = 0; tileInChunkCoordX < _chunkSize.X; tileInChunkCoordX++)
            {
                for (int tileInChunkCoordY = 0; tileInChunkCoordY < _chunkSize.Y; tileInChunkCoordY++)
                {
                    int tileCoordX = chunkCoord.X * _chunkSize.X + tileInChunkCoordX;
                    int tileCoordY = chunkCoord.Y * _chunkSize.Y + tileInChunkCoordY;
                    SiteCell tile = _site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];
                    if (tile.FloorID != WorldUtils.AIR_NULL_MAT_ID && tile.IsFloorVisible)
                    {
                        TerrainMaterial tm = TerrainMaterial.TerraMats[tile.FloorID];
                        Sprite sprite;
                        Color c = Color.Wheat;
                        sprite = tm.Sprites["Floor"];
                        c = tm.TerraColor;
                        VerticeAdder(sprite, tileCoordX, tileCoordY, chunkCoord.Z, ref index, ref _floorVertices, new Vector2(0, -4), c);
                    }
                }
            }
            return index;
        }

        private void LevelDispose(int level)
        {
            if (_renderLayers[(int)RenderLayer.Block][level] != null)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        if (_renderLayers[(int)RenderLayer.Block][level][x, y] != null) _renderLayers[(int)RenderLayer.Block][level][x, y].Dispose();
                        if (_renderLayers[(int)RenderLayer.Floor][level][x, y] != null) _renderLayers[(int)RenderLayer.Floor][level][x, y].Dispose();
                    }
                }
            }
        }

        private void FillLevel(int level, bool fillWalls = true, bool fillFloors = true)
        {
            LevelDispose(level);

            bool hidden = false;
            if (level == _drawHighest) hidden = true;
            _renderLayers[(int)RenderLayer.Block][level] = new DynamicVertexBuffer[_chunksCount.X, _chunksCount.Y];
            _renderLayers[(int)RenderLayer.Floor][level] = new DynamicVertexBuffer[_chunksCount.X, _chunksCount.Y];
            for (int x = 0; x < _chunksCount.X; x++)
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    int wallIndex = 0;
                    int floorIndex = 0;
                    Task t1 = Task.Run(() =>
                    {
                        if (fillWalls)
                            wallIndex = FillWallVertices(_renderLayers[(int)RenderLayer.Block], new Point3(x, y, level), hidden);
                    });
                    Task t2 = Task.Run(() =>
                    {
                        if (fillFloors)
                            floorIndex = FillFloorVertices(_renderLayers[(int)RenderLayer.Floor], new Point3(x, y, level));
                    });
                    Task.WaitAll(t1, t2);

                    // Create the vertex buffer
                    _renderLayers[(int)RenderLayer.Block][level][x, y] =
                        new DynamicVertexBuffer(_graphicsDevice,
                             typeof(VertexPositionColorTexture),
                             wallIndex,
                             BufferUsage.WriteOnly);
                    // Set the data of the vertex buffer
                    if (wallIndex != 0) _renderLayers[(int)RenderLayer.Block][level][x, y].SetData(_wallVertices, 0, wallIndex);

                    // Create the vertex buffer
                    _renderLayers[(int)RenderLayer.Floor][level][x, y] =
                        new DynamicVertexBuffer(_graphicsDevice,
                            typeof(VertexPositionColorTexture),
                            floorIndex,
                            BufferUsage.WriteOnly);
                    // Set the data of the vertex buffer
                    if (floorIndex != 0) _renderLayers[(int)RenderLayer.Floor][level][x, y].SetData(_floorVertices, 0, floorIndex);
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Point sel = WorldUtils.MouseScreenToMap(m, _site.CurrentLevel);
            MainGame.Instance.debug.Add("Block: " + sel.ToString());
            if (_renderLayers[(int)RenderLayer.Block].Count < ONE_MOMENT_DRAW_LEVELS)
            {
                if (gameTime.TotalGameTime.Ticks % 5 == 0)
                {
                    int chunkCoordZ = _drawLowest + _renderLayers[(int)RenderLayer.Block].Count;
                    FillLevel(chunkCoordZ);
                }
            }
            else if (_drawHighest != _site.CurrentLevel)
            {
                bool MoveUp = true;
                if (_drawHighest > _site.CurrentLevel) MoveUp = false;
                _drawHighest = _site.CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

                FillLevel(_drawHighest, true, false);

                if (MoveUp)
                    FillLevel(_drawHighest - 1);
                else
                    FillLevel(_drawLowest);
            }
        }

        public void Draw()
        {
            DrawVertices();
        }

        private void DrawVertices()
        {
            effect.World = MainGame.cam.WorldMatrix;
            effect.View = MainGame.cam.Transformation;
            effect.Projection = MainGame.cam.Projection;
            effect.CurrentTechnique.Passes[0].Apply();

            for (int z = _drawLowest; z <= _drawHighest; z++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        if (z < _renderLayers[(int)RenderLayer.Block].Start + _renderLayers[(int)RenderLayer.Block].Count)
                        {
                            DynamicVertexBuffer vb = _renderLayers[(int)RenderLayer.Block][z][x, y];
                            if (vb != null && vb.VertexCount != 0)
                            {
                                _graphicsDevice.SetVertexBuffer(vb);
                                _graphicsDevice.DrawPrimitives(
                                       PrimitiveType.TriangleList, 0, vb.VertexCount / 3);
                            }
                        }

                        if (z < _renderLayers[(int)RenderLayer.Floor].Start + _renderLayers[(int)RenderLayer.Floor].Count)
                        {
                            if (z == _drawHighest) continue;
                            DynamicVertexBuffer vb = _renderLayers[(int)RenderLayer.Floor][z][x, y];
                            if (vb != null && vb.VertexCount != 0)
                            {
                                _graphicsDevice.SetVertexBuffer(vb);
                                _graphicsDevice.DrawPrimitives(
                                       PrimitiveType.TriangleList, 0, vb.VertexCount / 3);
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            for (int i = _drawLowest; i < _drawHighest; i++)
            {
                LevelDispose(i);
            }
        }

        /*private void DrawSprites()
        {
            _spriteBatch.Begin(
                transformMatrix: MainGame.cam.Transformation
                );

            for (int i = DiffUtils.GetOrBound<int>(_site.CurrentLevel - ONE_MOMENT_DRAW_LEVELS, 0, _site.Size.Z); i < _site.CurrentLevel; i++)
            {
                for (int y = 0; y < _site.Size.Y; y++)
                {
                    for (int x = 0; x < _site.Size.X; x++)
                    {
                        SiteBlock b = _site.Blocks[x, y, i];
                        Vector2 mts = MapToScreen(x, y, i);

                        if (ByteField.GetBit(_visBuffer[x, y, i], (byte)VisBufField.WallVisible))
                        //if(b.wallId != 0)
                        {
                            TileTexture wt = TileSet.SpriteSet[TileSet.IDs[b.wallId]];
                            //draw block
                            _spriteBatch.Draw(wt.Texture,
                                mts,
                                wt.RectPos,
                                Color.White
                                );
                        }
                        if (ByteField.GetBit(_visBuffer[x, y, i], (byte)VisBufField.FloorVisible))
                        //if(b.floorId != 0)
                        {
                            TileTexture ft = TileSet.SpriteSet[TileSet.IDs[b.wallId]];
                            //draw floor
                            _spriteBatch.Draw(ft.Texture,
                                mts + new Vector2(0, -4),
                                ft.RectPos,
                                Color.White
                                );
                        }
                    }
                }
            }

            _spriteBatch.End();
        }*/
    }
}