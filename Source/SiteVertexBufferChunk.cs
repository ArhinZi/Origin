using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

    public enum VertexBufferLayer
    {
        Back = 0,
        Middle = 1,
        Front = 2
    }

    public class SiteVertexBufferChunk : IDisposable
    {
        // Count of element in one vertex array
        // There can be more then one vertex arrays in one chunk
        private static int _maxVertexCount = 64 * 64 * 6;

        public Point3 ChunkPos { get; private set; }
        public SiteRenderer Renderer { get; private set; }

        private GraphicsDevice _device;
        private HashSet<Texture2D> _texture2Ds;

        private int[] _staticVertexIndex;
        private Dictionary<Texture2D, List<VertexPositionColorTexture[]>[]> _staticVertices;
        private Dictionary<Texture2D, List<VertexBuffer>[]> _staticVertexBuffer;

        private int[] _dynamicVertexIndex;
        private Dictionary<Texture2D, List<VertexPositionColorTexture[]>[]> _dynamicVertices;
        //private Dictionary<Texture2D, List<VertexBuffer>> _dynamicVertexBuffer;

        public SiteVertexBufferChunk(SiteRenderer renderer, Point3 pos)
        {
            //DynamicVertexBuffer = new Dictionary<Texture2D, List<VertexBuffer>>();
            Renderer = renderer;
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
                _staticVertices = new Dictionary<Texture2D, List<VertexPositionColorTexture[]>[]>();
                _staticVertexIndex = new int[Enum.GetNames(typeof(VertexBufferLayer)).Length];
            }
            else if (type == VertexBufferType.Dynamic)
            {
                //DisposeDynamicBuffer();
                if (_dynamicVertices != null)
                    for (int i = 0; i < Enum.GetNames(typeof(VertexBufferLayer)).Length; i++)
                    {
                        foreach (var key in _dynamicVertices.Keys)
                        {
                            _dynamicVertices[key][i].Clear();
                        }
                        _dynamicVertexIndex[i] = 0;
                    }
                else
                {
                    _dynamicVertices = new Dictionary<Texture2D, List<VertexPositionColorTexture[]>[]>();
                    _dynamicVertexIndex = new int[Enum.GetNames(typeof(VertexBufferLayer)).Length];
                }
            }
        }

        public void AddSprite(VertexBufferType type, VertexBufferLayer vblayer,
            Sprite sprite, Color col, Point3 cellPos, Point offsetPosition,
            float offsetZ = 0,
            Point drawSize = default)
        {
            if (cellPos.X / Renderer.ChunkSize.X != ChunkPos.X &&
               cellPos.Y / Renderer.ChunkSize.Y != ChunkPos.Y)
            {
                throw new Exception("The Cell is not from this Chunk");
            }

            ref Dictionary<Texture2D, List<VertexPositionColorTexture[]>[]> vertexBatches = ref _staticVertices;
            ref int[] indarr = ref _staticVertexIndex;
            if (type == VertexBufferType.Static)
            {
                vertexBatches = ref _staticVertices;
                indarr = ref _staticVertexIndex;
            }
            else if (type == VertexBufferType.Dynamic)
            {
                vertexBatches = ref _dynamicVertices;
                indarr = ref _dynamicVertexIndex;
            }
            else
            {
                throw new NotImplementedException();
            }

            if (!vertexBatches.ContainsKey(sprite.Texture))
            {
                List<VertexPositionColorTexture[]>[] tmp = new List<VertexPositionColorTexture[]>[Enum.GetNames(typeof(VertexBufferLayer)).Length];
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = new List<VertexPositionColorTexture[]>();
                }
                vertexBatches.Add(sprite.Texture, tmp);
                _texture2Ds.Add(sprite.Texture);
            }
            List<VertexPositionColorTexture[]> verticesList = vertexBatches[sprite.Texture][(int)vblayer];
            if (verticesList.Count == 0 ||
                indarr[(int)vblayer] > _maxVertexCount - 6)
            {
                verticesList.Add(new VertexPositionColorTexture[_maxVertexCount]);
                indarr[(int)vblayer] = 0;
            }

            ref int index = ref indarr[(int)vblayer];
            VertexPositionColorTexture[] vertices = verticesList[^1];

            Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos) + offsetPosition;
            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos) + offsetZ;

            Rectangle textureRect = sprite.RectPos;

            if (drawSize == default) drawSize = new Point(textureRect.Width, textureRect.Height);

            // Calc the sprite corners positions
            Vector3 topLeft =
                new Vector3(spritePos.X, spritePos.Y, vertexZ);
            Vector3 topRight =
                new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
            Vector3 bottomLeft =
                new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
            Vector3 bottomRight =
                new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

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

        /// <summary>
        /// Sends vertex buffers to videochip. Something like Update button.
        /// Caution! Dont run it in threads. It causes suspension;
        /// </summary>
        public void SetStaticBuffer()
        {
            DisposeStaticBuffer();
            _staticVertexBuffer = new Dictionary<Texture2D, List<VertexBuffer>[]>();
            foreach (var key in _staticVertices.Keys)
            {
                List<VertexBuffer>[] lvb = new List<VertexBuffer>[Enum.GetNames(typeof(VertexBufferLayer)).Length];
                for (int ilayer = 0; ilayer < _staticVertices[key].Length; ilayer++)
                {
                    List<VertexPositionColorTexture[]> list = _staticVertices[key][ilayer];
                    lvb[ilayer] = new List<VertexBuffer>();
                    for (int ilist = 0; ilist < list.Count; ilist++)
                    {
                        int count = (ilist == list.Count - 1) ? _staticVertexIndex[ilayer] : _maxVertexCount;
                        VertexPositionColorTexture[] tarr = list[ilist];
                        VertexBuffer vb = new VertexBuffer(_device,
                                     typeof(VertexPositionColorTexture),
                                     count,
                                     BufferUsage.WriteOnly);
                        vb.SetData(tarr, 0, count);
                        lvb[ilayer].Add(vb);
                    }
                }
                _staticVertexBuffer.Add(key, lvb);
            }
            /*_staticVertexBuffer = new Dictionary<Texture2D, List<VertexBuffer>[]>();
            foreach (var key in _staticVertices.Keys)
            {
                List<VertexBuffer>[] vbarray = new List<VertexBuffer>[Enum.GetNames(typeof(VertexBufferLayer)).Length];
                for (int i = 0; i < vbarray.Length; i++)
                {
                    vbarray[i] = new List<VertexBuffer>();
                    foreach (var list in _staticVertices[key])
                    {
                        for (int lk = 0; lk < list.Count; lk++)
                        {
                            int count = (lk == list.Count - 1) ? _staticVertexIndex : _maxVertexCount;
                            VertexBuffer vb = new VertexBuffer(_device,
                                     typeof(VertexPositionColorTexture),
                                     count,
                                     BufferUsage.WriteOnly);
                            vb.SetData(list[lk], 0, count);
                            vbarray[lk].Add(vb);
                        }
                    }
                }
                _staticVertexBuffer.Add(key, vbarray);
            }*/
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
            foreach (var layer in Enum.GetValues(typeof(VertexBufferLayer)))
            {
                foreach (var key in _texture2Ds)
                {
                    effect.Texture = key;
                    effect.CurrentTechnique.Passes[0].Apply();
                    if (_staticVertexBuffer.ContainsKey(key))
                    {
                        List<VertexBuffer> listVB = _staticVertexBuffer[key][(int)layer];
                        for (int i = 0; i < listVB.Count; i++)
                        {
                            //int count = (i == listVB.Count - 1) ? _staticVertexIndex : _maxVertexCount;
                            _device.SetVertexBuffer(listVB[i]);
                            _device.DrawPrimitives(
                                   PrimitiveType.TriangleList, 0, listVB[i].VertexCount / 3);
                        }
                    }
                    if (_dynamicVertices.ContainsKey(key))
                    {
                        List<VertexPositionColorTexture[]> listVP = _dynamicVertices[key][(int)layer];
                        for (int i = 0; i < listVP.Count; i++)
                        {
                            int count = (i == listVP.Count - 1) ? _dynamicVertexIndex[(int)layer] : _maxVertexCount;
                            _device.DrawUserPrimitives(PrimitiveType.TriangleList, listVP[i], 0, count / 3);
                        }
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
            if (_staticVertexBuffer != null)
                foreach (var key in _staticVertexBuffer.Keys)
                {
                    foreach (var array in _staticVertexBuffer[key])
                    {
                        foreach (var item in array)
                        {
                            if (item != null) item.Dispose();
                        }
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