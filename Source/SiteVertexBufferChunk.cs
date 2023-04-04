using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json.Converters;

using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source
{
    public enum VertexBufferType
    {
        Static,
        Dynamic
    }

    public class SiteVertexBufferChunk : IDisposable
    {
        private static int _maxVertexCount = 64 * 64 * 6;

        public Point3 ChunkPos { get; private set; }

        private int _staticVertexIndex = 0;
        private GraphicsDevice _device;
        private HashSet<Texture2D> _texture2Ds;
        public Dictionary<Texture2D, List<VertexPositionColorTexture[]>> StaticVertices { get; private set; }
        public Dictionary<Texture2D, List<VertexBuffer>> StaticVertexBuffer { get; set; }

        private int _dynamicVertexIndex = 0;
        public Dictionary<Texture2D, List<VertexPositionColorTexture[]>> DynamicVertices { get; private set; }
        public Dictionary<Texture2D, List<VertexBuffer>> DynamicVertexBuffer { get; set; }

        public SiteVertexBufferChunk(Point3 pos)
        {
            //DynamicVertexBuffer = new Dictionary<Texture2D, List<VertexBuffer>>();

            ChunkPos = pos;
            _device = MainGame.Instance.GraphicsDevice;
            Reset();
        }

        public void Reset()
        {
            Clear(VertexBufferType.Static);
            Clear(VertexBufferType.Dynamic);
            _texture2Ds = new HashSet<Texture2D>();
        }

        public void Clear(VertexBufferType type)
        {
            if (type == VertexBufferType.Static)
            {
                DisposeStaticBuffer();
                StaticVertices = new Dictionary<Texture2D, List<VertexPositionColorTexture[]>>();
            }
            else if (type == VertexBufferType.Dynamic)
            {
                //DisposeDynamicBuffer();
                DynamicVertices = new Dictionary<Texture2D, List<VertexPositionColorTexture[]>>();
            }
        }

        public void AddSprite(VertexBufferType type, Sprite sprite, Color col, Point3 cellPos, Point offset)
        {
            if (cellPos.X / SiteRenderer.BASE_CHUNK_SIZE.X != ChunkPos.X &&
               cellPos.Y / SiteRenderer.BASE_CHUNK_SIZE.Y != ChunkPos.Y)
            {
                throw new Exception("The Cell is not from this Chunk");
            }

            Dictionary<Texture2D, List<VertexPositionColorTexture[]>> vertexBatches = null;
            ref int index = ref _staticVertexIndex;
            if (type == VertexBufferType.Static)
            {
                vertexBatches = StaticVertices;
                index = ref _staticVertexIndex;
            }
            else if (type == VertexBufferType.Dynamic)
            {
                vertexBatches = DynamicVertices;
                index = ref _dynamicVertexIndex;
            }
            else
            {
                throw new NotImplementedException();
            }

            if (!vertexBatches.ContainsKey(sprite.Texture))
            {
                vertexBatches.Add(sprite.Texture, new List<VertexPositionColorTexture[]>());
                _texture2Ds.Add(sprite.Texture);
            }
            List<VertexPositionColorTexture[]> verticesList = vertexBatches[sprite.Texture];
            if (verticesList.Count == 0 ||
                index >= _maxVertexCount)
            {
                verticesList.Add(new VertexPositionColorTexture[_maxVertexCount]);
                index = 0;
            }
            VertexPositionColorTexture[] vertices = verticesList[^1];

            Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos) + offset;
            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

            Rectangle textureRect = sprite.RectPos;

            // Calc the sprite corners positions
            Vector3 topLeft =
                new Vector3(spritePos.X, spritePos.Y, vertexZ);
            Vector3 topRight =
                new Vector3(spritePos.X + textureRect.Width, spritePos.Y, vertexZ);
            Vector3 bottomLeft =
                new Vector3(spritePos.X, spritePos.Y + textureRect.Height, vertexZ);
            Vector3 bottomRight =
                new Vector3(spritePos.X + textureRect.Width, spritePos.Y + textureRect.Height, vertexZ);

            // Calc the texture coordinates
            Vector2 textureTopLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
            Vector2 textureTopRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
            Vector2 textureBottomLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);
            Vector2 textureBottomRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);

            if (sprite.Effect == SpriteEffects.FlipHorizontally)
            {
                (textureTopLeft, textureTopRight) = (textureTopRight, textureTopLeft);
                (textureBottomLeft, textureBottomRight) = (textureBottomRight, textureBottomLeft);
            }
            if (sprite.Effect == SpriteEffects.FlipVertically)
            {
                (textureTopLeft, textureBottomLeft) = (textureBottomLeft, textureTopLeft);
                (textureTopRight, textureBottomRight) = (textureBottomRight, textureTopRight);
            }

            // Add the vertices for the tile to the vertex buffer
            vertices[index++] = new VertexPositionColorTexture(topLeft, col, textureTopLeft);
            vertices[index++] = new VertexPositionColorTexture(topRight, col, textureTopRight);
            vertices[index++] = new VertexPositionColorTexture(bottomLeft, col, textureBottomLeft);

            vertices[index++] = new VertexPositionColorTexture(topRight, col, textureTopRight);
            vertices[index++] = new VertexPositionColorTexture(bottomRight, col, textureBottomRight);
            vertices[index++] = new VertexPositionColorTexture(bottomLeft, col, textureBottomLeft);
        }

        public void SetStaticBuffer()
        {
            DisposeStaticBuffer();
            StaticVertexBuffer = new Dictionary<Texture2D, List<VertexBuffer>>();
            foreach (var item in StaticVertices.Keys)
            {
                List<VertexBuffer> listVB = new List<VertexBuffer>();
                StaticVertexBuffer.Add(item, listVB);
                for (int i = 0; i < StaticVertices[item].Count; i++)
                {
                    int count = (i == StaticVertices[item].Count - 1) ? _staticVertexIndex : _maxVertexCount;
                    VertexBuffer vb = new VertexBuffer(_device,
                             typeof(VertexPositionColorTexture),
                             count,
                             BufferUsage.WriteOnly);
                    vb.SetData(StaticVertices[item][i], 0, count);
                    listVB.Add(vb);
                }
            }
        }

        /*public void SetDynamicBuffer()
        {
            foreach (var key in DynamicVertexBuffer.Keys)
            {
                foreach (var item in DynamicVertexBuffer[key])
                {
                    item.Dispose();
                }
            }
            DynamicVertexBuffer = new Dictionary<Texture2D, List<VertexBuffer>>();
            foreach (var item in DynamicVertices.Keys)
            {
                List<VertexBuffer> listVB = new List<VertexBuffer>();
                DynamicVertexBuffer.Add(item, listVB);
                for (int i = 0; i < DynamicVertices[item].Count; i++)
                {
                    int count = (i == DynamicVertices[item].Count - 1) ? _dynamicVertexIndex : _maxVertexCount;
                    VertexBuffer vb = new VertexBuffer(_device,
                             typeof(VertexPositionColorTexture),
                             count,
                             BufferUsage.WriteOnly);
                    vb.SetData(DynamicVertices[item][i], 0, count);
                    listVB.Add(vb);
                }
            }
        }*/

        public void Draw(AlphaTestEffect effect)
        {
            foreach (var key in _texture2Ds)
            {
                effect.Texture = key;
                effect.CurrentTechnique.Passes[0].Apply();
                if (StaticVertexBuffer.ContainsKey(key))
                {
                    List<VertexBuffer> listVB = StaticVertexBuffer[key];
                    for (int i = 0; i < listVB.Count; i++)
                    {
                        //int count = (i == listVB.Count - 1) ? _staticVertexIndex : _maxVertexCount;
                        _device.SetVertexBuffer(listVB[i]);
                        _device.DrawPrimitives(
                               PrimitiveType.TriangleList, 0, listVB[i].VertexCount / 3);
                    }
                }
                if (DynamicVertices.ContainsKey(key))
                {
                    List<VertexPositionColorTexture[]> listVP = DynamicVertices[key];
                    for (int i = 0; i < listVP.Count; i++)
                    {
                        int count = (i == listVP.Count - 1) ? _dynamicVertexIndex : _maxVertexCount;
                        _device.DrawUserPrimitives(PrimitiveType.TriangleList, listVP[i], 0, count / 3);
                    }
                }
            }
        }

        public void Dispose()
        {
            DisposeStaticBuffer();
            //DisposeDynamicBuffer();
        }

        public void DisposeStaticBuffer()
        {
            if (StaticVertexBuffer != null)
                foreach (var key in StaticVertexBuffer.Keys)
                {
                    foreach (var item in StaticVertexBuffer[key])
                    {
                        if (item != null) item.Dispose();
                    }
                }
        }

        /*public void DisposeDynamicBuffer()
        {
            foreach (var key in DynamicVertexBuffer.Keys)
            {
                foreach (var item in DynamicVertexBuffer[key])
                {
                    if (item != null) item.Dispose();
                }
            }
        }*/
    }
}