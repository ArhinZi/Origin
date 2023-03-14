using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Origin.Utils;
using Origin.View;
using Origin.WorldComps;
using System;

namespace Origin.Draw
{
    internal enum VisBufField : byte
    {
        FloorVisible = 1,
        WallVisible
    }

    public class SiteRenderer
    {
        private Site _site;
        private byte[,,] _visBuffer;

        private Point3 _chunksCount;
        private Point _chunkSize;
        private DynamicVertexBuffer[,,] _vertexBuffersLayer;
        private DynamicVertexBuffer[,,] _vertexBuffersGround;
        private BasicEffect effect;

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        public static float Z_DIAGONAL_OFFSET = 0.01f;
        public static Point BASE_CHUNK_SIZE = new Point(64, 64);

        private Matrix worldMatrix;

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            _site = site;
            _visBuffer = new byte[_site.Size.X, _site.Size.Y, _site.Size.Z];

            _chunkSize = BASE_CHUNK_SIZE;
            //if (_chunkSize.X < _site.Size.X) _chunkSize.X = _site.Size.X;
            //if (_chunkSize.Y < _site.Size.Y) _chunkSize.Y = _site.Size.Y;
            if (_site.Size.X % _chunkSize.X != 0 || _site.Size.Y % _chunkSize.Y != 0) throw new Exception("Site size is invalid!");

            _chunksCount = new Point3(_site.Size.X / _chunkSize.X, _site.Size.Y / _chunkSize.Y, _site.Size.Z);
            _vertexBuffersLayer = new DynamicVertexBuffer[_chunksCount.X, _chunksCount.Y, _chunksCount.Z];
            _vertexBuffersGround = new DynamicVertexBuffer[_chunksCount.X, _chunksCount.Y, _chunksCount.Z];

            _graphicsDevice = graphicDevice;
            _spriteBatch = new SpriteBatch(MainGame.Instance.GraphicsDevice);

            CalcVisBuffer();

            worldMatrix = Matrix.CreateWorld(new Vector3(0, 0, 0), new Vector3(0, 0, -1), Vector3.Up);

            ReCalcVertexBuffers();
            effect = new BasicEffect(MainGame.Instance.GraphicsDevice);
            effect.TextureEnabled = true;
            effect.VertexColorEnabled = true;
            effect.Texture = TileSet.texture;
        }

        private Vector2 MapToScreen(int mapX, int mapY, int mapZ)
        {
            var screenX = ((mapX - mapY) * TileSet.TILE_SIZE.X / 2);
            var screenY = ((mapY + mapX) * TileSet.TILE_SIZE.Y / 2) + -mapZ * (TileSet.TILE_SIZE.Y + TileSet.FLOOR_YOFFSET);

            Vector2 res = new Vector2(screenX, screenY);
            //res += MainGame.cam.Pos;
            //res *= MainGame.cam.Zoom;
            //Matrix inverted = Matrix.Invert(MainGame.cam.get_transformation(MainGame.instance.GraphicsDevice));
            return res;
        }

        private void CalcVisBuffer()
        {
            _visBuffer = new byte[_site.Size.X, _site.Size.Y, _site.Size.Z];
            for (int z = _site.Size.Z - 1; z >= 0; z--)
            {
                for (int y = _site.Size.Y - 1; y >= 0; y--)
                {
                    for (int x = _site.Size.X - 1; x >= 0; x--)
                    {
                        _visBuffer[x, y, z] = new byte();
                        if (_site.Blocks[x, y, z].floorId != 0 && _site.Blocks[x, y, z].wallId != 0)
                        {
                            if ((DiffUtils.InBounds<int>(x + 1, 0, _site.Size.X)
                                && _site.Blocks[x + 1, y, z].wallId != 100) &&
                                (DiffUtils.InBounds<int>(y + 1, 0, _site.Size.Y)
                                && _site.Blocks[x, y + 1, z].wallId != 100))
                            {
                                ByteField.SetBit(ref _visBuffer[x, y, z], (byte)VisBufField.WallVisible, false);

                                if (DiffUtils.InBounds<int>(z + 1, 0, _site.Size.Z))
                                {
                                    if (_site.Blocks[x, y, z + 1].wallId == 100)
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
        }

        private void VerticeChanger(TileTexture tt, int x, int y, int z,
            ref DynamicVertexBuffer[,,] buffer, Vector2 offset, Color c)
        {
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6];

            Rectangle textureRect = tt.RectPos;
            var VertexX = ((x - y) * TileSet.TILE_SIZE.X / 2) + offset.X;
            var VertexY = ((y + x) * TileSet.TILE_SIZE.Y / 2) - z * (TileSet.TILE_SIZE.Y + TileSet.FLOOR_YOFFSET) - offset.Y;
            var VertexZ = (x + y) * Z_DIAGONAL_OFFSET;

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
            vertices[0] = new VertexPositionColorTexture(topLeft, c, textureTopLeft);
            vertices[1] = new VertexPositionColorTexture(topRight, c, textureTopRight);
            vertices[2] = new VertexPositionColorTexture(bottomLeft, c, textureBottomLeft);

            vertices[3] = new VertexPositionColorTexture(topRight, c, textureTopRight);
            vertices[4] = new VertexPositionColorTexture(bottomRight, c, textureBottomRight);
            vertices[5] = new VertexPositionColorTexture(bottomLeft, c, textureBottomLeft);

            int startIndex = (x * _site.Size.Y * 6) + _site.Size.X * 6;

            buffer[0, 0, z].SetData(vertices, 0, 6);
        }

        private void VerticeAdder(TileTexture tt, int x, int y, int z,
            ref int index, ref VertexPositionColorTexture[] vertices, Vector2 offset, Color c)
        {
            Rectangle textureRect = tt.RectPos;
            var VertexX = ((x - y) * TileSet.TILE_SIZE.X / 2) + offset.X;
            var VertexY = ((y + x) * TileSet.TILE_SIZE.Y / 2) - z * (TileSet.TILE_SIZE.Y + TileSet.FLOOR_YOFFSET) + offset.Y;
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

        private void ReFillVertexBuffer(Point3 chunkCoord)
        {
            // Create the vertex buffer
            _vertexBuffersLayer[chunkCoord.X, chunkCoord.Y, chunkCoord.Z] =
                new DynamicVertexBuffer(_graphicsDevice,
                    typeof(VertexPositionColorTexture),
                    _chunkSize.X * _chunkSize.Y * 6,
                    BufferUsage.WriteOnly);
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[_chunkSize.X * _chunkSize.Y * 6];

            int index = 0;

            // Loop through each tile block in the chunk
            for (int tileInChunkCoordX = 0; tileInChunkCoordX < _chunkSize.X; tileInChunkCoordX++)
            {
                for (int tileInChunkCoordY = 0; tileInChunkCoordY < _chunkSize.Y; tileInChunkCoordY++)
                {
                    int tileCoordX = chunkCoord.X * _chunkSize.X + tileInChunkCoordX;
                    int tileCoordY = chunkCoord.Y * _chunkSize.Y + tileInChunkCoordY;
                    SiteBlock tile = _site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];
                    if (tile.wallId != 100 && ByteField.GetBit(_visBuffer[tileCoordX, tileCoordY, chunkCoord.Z], (byte)VisBufField.WallVisible))
                    {
                        TileTexture tt;
                        Color c = new Color(255, 255, 255, 255);
                        tt = TileSet.WallSet[tile.wallId];
                        VerticeAdder(tt, tileCoordX, tileCoordY, chunkCoord.Z, ref index, ref vertices, Vector2.Zero, c);
                        //VerticeChanger(tt, x, y, position.Z, ref _vertexBuffersLayer, Vector2.Zero, c);
                    }
                    else
                    {
                        index += 6;
                    }
                }
            }
            // Set the data of the vertex buffer
            _vertexBuffersLayer[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].SetData(vertices);

            //---------------------------------------------------------------------------------------------------
            // Create the vertex buffer
            _vertexBuffersGround[chunkCoord.X, chunkCoord.Y, chunkCoord.Z] =
                new DynamicVertexBuffer(_graphicsDevice,
                    typeof(VertexPositionColorTexture),
                    _chunkSize.X * _chunkSize.Y * 6,
                    BufferUsage.WriteOnly);
            vertices = new VertexPositionColorTexture[_chunkSize.X * _chunkSize.Y * 6];

            index = 0;

            // Loop through each tile in the chunk
            for (int tileInChunkCoordX = 0; tileInChunkCoordX < _chunkSize.X; tileInChunkCoordX++)
            {
                for (int tileInChunkCoordY = 0; tileInChunkCoordY < _chunkSize.Y; tileInChunkCoordY++)
                {
                    int tileCoordX = chunkCoord.X * _chunkSize.X + tileInChunkCoordX;
                    int tileCoordY = chunkCoord.Y * _chunkSize.Y + tileInChunkCoordY;
                    SiteBlock tile = _site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];
                    if (tile.wallId != 100 && ByteField.GetBit(_visBuffer[tileCoordX, tileCoordY, chunkCoord.Z], (byte)VisBufField.FloorVisible))
                    {
                        TileTexture tt;
                        Color c = new Color(255, 255, 255, 255);
                        tt = TileSet.FloorSet[tile.floorId];
                        VerticeAdder(tt, tileCoordX, tileCoordY, chunkCoord.Z, ref index, ref vertices, new Vector2(0, -4), c);
                    }
                    else
                    {
                        index += 6;
                    }
                }
            }

            // Set the data of the vertex buffer
            _vertexBuffersGround[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].SetData(vertices);
        }

        private void ReCalcVertexBuffers()
        {
            for (int z = 0; z < _chunksCount.Z; z++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        ReFillVertexBuffer(new Point3(x, y, z));
                    }
                }
            }
        }

        public void Update()
        {
            Point m = Mouse.GetState().Position;
            Point sel = WorldUtils.MouseScreenToMap(m, _site.CurrentLevel);
            MainGame.Instance.debug.Add("Block: " + sel.ToString());
            //_site.SetSelected(new Point3(sel.X, sel.Y, _site.CurrentLevel));
        }

        public void Draw()
        {
            DrawVertices();
        }

        private void DrawVertices()
        {
            effect.World = worldMatrix;
            effect.View = MainGame.cam.TransformMatrix;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height, 0, -1, 100); ;
            effect.CurrentTechnique.Passes[0].Apply();

            for (int z = DiffUtils.GetOrBound<int>(_site.CurrentLevel - 32, 0, _site.Size.Z);
                z <= _site.CurrentLevel; z++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        _graphicsDevice.SetVertexBuffer(_vertexBuffersLayer[x, y, z]);
                        _graphicsDevice.DrawPrimitives(
                                PrimitiveType.TriangleList, 0, _vertexBuffersLayer[x, y, z].VertexCount / 3);

                        if (z == _site.CurrentLevel) continue;
                        _graphicsDevice.SetVertexBuffer(_vertexBuffersGround[x, y, z]);
                        _graphicsDevice.DrawPrimitives(
                                PrimitiveType.TriangleList, 0, _vertexBuffersLayer[x, y, z].VertexCount / 3);
                    }
                }
            }
        }

        private void DrawSprites()
        {
            _spriteBatch.Begin(
                transformMatrix: MainGame.cam.GetTransformation()
                );

            for (int i = DiffUtils.GetOrBound<int>(_site.CurrentLevel - 32, 0, _site.Size.Z); i < _site.CurrentLevel; i++)
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
                            TileTexture wt = TileSet.WallSet[b.wallId - 1];
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
                            TileTexture ft = TileSet.FloorSet[b.floorId - 1];
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
        }
    }
}