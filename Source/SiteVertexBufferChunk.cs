using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Newtonsoft.Json.Converters;

using Origin.Source.Utils;

using System;
using System.Collections;
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
        private static readonly object Lock = new object();

        /// <summary>
        /// Count of element in one vertex array
        /// There can be more then one vertex arrays in one chunk
        /// </summary>
        public static int MaxVertexCount = SiteRenderer.BASE_CHUNK_SIZE.X * SiteRenderer.BASE_CHUNK_SIZE.Y * 6;

        public static int CountOfLayers = Enum.GetNames(typeof(VertexBufferLayer)).Length;

        public Point3 SelfChunkPos { get; private set; }
        public SiteRenderer Renderer { get; private set; }

        /// <summary>
        /// Shows if chunk is uploaded to graphic device
        /// </summary>
        public bool IsSet { get; private set; }

        public bool IsFullyHidded { get; set; } = false;

        private GraphicsDevice _graphicDevice;

        /// <summary>
        /// Set of used textures for all chunks
        /// </summary>
        private static HashSet<Texture2D> _texture2Ds = new HashSet<Texture2D>();

        /// <summary>
        /// Static vertex Indexes:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     Vertex Index;
        /// </summary>
        private Dictionary<Texture2D, int[]> _staticVertexIndexes;

        /// <summary>
        /// Static vertices:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     List of Vertex massives if it need more vertices than one array;<br/>
        ///     Vertex array;
        /// </summary>
        private Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]> _staticVertices;

        /// <summary>
        /// Static vertex buffers:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     List of Vertex massives if it need more buffers than one array;<br/>
        ///     Vertex Buffer;
        /// </summary>
        private Dictionary<Texture2D, List<VertexBuffer>[]> _staticVertexBuffers;

        /// <summary>
        /// Dynamic vertex Indexes:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     Vertex Index;
        /// </summary>
        private Dictionary<Texture2D, int[]> _dynamicVertexIndexes;

        /// <summary>
        /// Dynamic vertices:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     List of Vertex massives if it need more vertices than one array;<br/>
        ///     Vertex array;
        /// </summary>
        private Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]> _dynamicVertices;

        public SiteVertexBufferChunk(SiteRenderer renderer, Point3 pos)
        {
            Renderer = renderer;
            SelfChunkPos = pos;
            _graphicDevice = OriginGame.Instance.GraphicsDevice;
            Init();
        }

        private void Init()
        {
            if (_staticVertices == null)
                _staticVertices = new Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]>();
            if (_staticVertexBuffers == null)
                _staticVertexBuffers = new Dictionary<Texture2D, List<VertexBuffer>[]>();

            if (_dynamicVertices == null)
                _dynamicVertices = new Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]>();

            _staticVertexIndexes = new Dictionary<Texture2D, int[]>();
            _dynamicVertexIndexes = new Dictionary<Texture2D, int[]>();
        }

        public void Clear(VertexBufferType type, int clLayer = -1)
        {
            if (type == VertexBufferType.Static)
            {
                DisposeStaticBuffer();
                foreach (var key in _staticVertexIndexes.Keys)
                {
                    for (int i = 0; i < _staticVertexIndexes[key].Length; i++)
                    {
                        _staticVertexIndexes[key][i] = 0;
                    }
                }
            }
            else if (type == VertexBufferType.Dynamic)
            {
                foreach (var key in _dynamicVertexIndexes.Keys)
                {
                    for (int i = 0; i < _dynamicVertexIndexes[key].Length; i++)
                    {
                        _dynamicVertexIndexes[key][i] = 0;
                    }
                }
            }
        }

        public void AddSprite(VertexBufferType type, int vblayer,
            Sprite sprite, Color col, Point3 cellPos, Point offsetPosition,
            float offsetZ = 0,
            Point drawSize = default)
        {
            if (cellPos.X / Renderer.ChunkSize.X != SelfChunkPos.X &&
               cellPos.Y / Renderer.ChunkSize.Y != SelfChunkPos.Y)
            {
                throw new Exception("The Cell is not from this Chunk");
            }

            ref Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]> vertexBatches = ref _staticVertices;
            ref var indarr = ref _staticVertexIndexes;
            if (type == VertexBufferType.Static)
            {
                vertexBatches = ref _staticVertices;
                indarr = ref _staticVertexIndexes;
            }
            else if (type == VertexBufferType.Dynamic)
            {
                vertexBatches = ref _dynamicVertices;
                indarr = ref _dynamicVertexIndexes;
            }
            else
            {
                throw new NotImplementedException();
            }

            lock (Lock)
            {
                if (!vertexBatches.ContainsKey(sprite.Texture))
                {
                    var tmpBatch = new List<VertexPositionColorTextureBlock[]>[CountOfLayers];
                    var tmpIndex = new int[CountOfLayers];
                    for (int i = 0; i < tmpBatch.Length; i++)
                    {
                        tmpBatch[i] = new List<VertexPositionColorTextureBlock[]>();
                        tmpIndex[i] = 0;
                    }
                    vertexBatches.Add(sprite.Texture, tmpBatch);
                    indarr.Add(sprite.Texture, tmpIndex);
                    _texture2Ds.Add(sprite.Texture);
                }
            }
            ref int rindex = ref indarr[sprite.Texture][vblayer];
            int VertexPack = rindex / MaxVertexCount;
            int index = rindex % MaxVertexCount;
            if (vertexBatches[sprite.Texture][vblayer].Count == 0 ||
                index == 0 && VertexPack >= vertexBatches[sprite.Texture][vblayer].Count)
            {
                vertexBatches[sprite.Texture][vblayer].Add(new VertexPositionColorTextureBlock[MaxVertexCount]);
            }

            VertexPositionColorTextureBlock[] vertices = vertexBatches[sprite.Texture][vblayer][VertexPack];

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

            if (sprite.Effect == MySpriteEffect.FlipHorizontally)
            {
                (textureTopLeft, textureTopRight) = (textureTopRight, textureTopLeft);
                (textureBottomLeft, textureBottomRight) = (textureBottomRight, textureBottomLeft);
            }
            if (sprite.Effect == MySpriteEffect.FlipVertically)
            {
                (textureTopLeft, textureBottomLeft) = (textureBottomLeft, textureTopLeft);
                (textureTopRight, textureBottomRight) = (textureBottomRight, textureTopRight);
            }
            if (sprite.Effect == MySpriteEffect.FlipBLTR)
            {
                (textureBottomLeft, textureTopRight) = (textureTopRight, textureBottomLeft);
            }
            if (sprite.Effect == MySpriteEffect.FlipTLBR)
            {
                (textureTopLeft, textureBottomRight) = (textureBottomRight, textureTopLeft);
            }

            // Add the vertices for the tile to the vertex buffer
            vertices[index++] = new VertexPositionColorTextureBlock(topLeft, col, textureTopLeft, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(topRight, col, textureTopRight, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(bottomLeft, col, textureBottomLeft, cellPos.ToVector3());

            vertices[index++] = new VertexPositionColorTextureBlock(topRight, col, textureTopRight, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(bottomRight, col, textureBottomRight, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(bottomLeft, col, textureBottomLeft, cellPos.ToVector3());

            rindex += 6;
        }

        /// <summary>
        /// Sends vertex buffers to videochip. Something like Update button.
        /// Caution! Dont run it in threads. It causes suspension;
        /// </summary>
        public void SetStaticBuffer()
        {
            DisposeStaticBuffer();
            _staticVertexBuffers = new Dictionary<Texture2D, List<VertexBuffer>[]>();
            foreach (var key in _staticVertices.Keys)
            {
                List<VertexBuffer>[] lvb = new List<VertexBuffer>[Enum.GetNames(typeof(VertexBufferLayer)).Length];
                for (int ilayer = 0; ilayer < _staticVertices[key].Length; ilayer++)
                {
                    ref List<VertexPositionColorTextureBlock[]> list = ref _staticVertices[key][ilayer];
                    lvb[ilayer] = new List<VertexBuffer>();
                    int index = _staticVertexIndexes[key][ilayer];
                    foreach (var item in list)
                    {
                        int count = index > item.Length ? MaxVertexCount : index;
                        VertexBuffer vb = new VertexBuffer(_graphicDevice,
                                     typeof(VertexPositionColorTextureBlock),
                                     count,
                                     BufferUsage.WriteOnly);
                        vb.SetData(item, 0, count);
                        lvb[ilayer].Add(vb);
                    }
                    list.Clear();
                }
                _staticVertexBuffers.Add(key, lvb);
            }
            IsSet = true;
        }

        public void Draw(Effect effect, List<int> typesToDraw = null)
        {
            //if (typesToDraw == null) typesToDraw = Enum.GetValues(typeof(VertexBufferLayer));
            typesToDraw ??= Enumerable.Range(0, CountOfLayers).ToList<int>();
            foreach (var layer in typesToDraw)
            {
                foreach (var key in _texture2Ds)
                {
                    effect.Parameters["Texture"].SetValue(key);
                    effect.CurrentTechnique.Passes[0].Apply();
                    if (_staticVertexBuffers.ContainsKey(key))
                    {
                        foreach (var item in _staticVertexBuffers[key][(int)layer])
                        {
                            //int count = (i == listVB.Count - 1) ? _staticVertexIndex : _maxVertexCount;
                            _graphicDevice.SetVertexBuffer(item);
                            _graphicDevice.DrawPrimitives(
                                   PrimitiveType.TriangleList, 0, item.VertexCount / 3);
                        }
                    }
                    if (_dynamicVertices.ContainsKey(key))
                    {
                        ref List<VertexPositionColorTextureBlock[]> listVP = ref _dynamicVertices[key][(int)layer];
                        for (int i = 0; i < listVP.Count; i++)
                        {
                            int index = _dynamicVertexIndexes[key][layer];
                            int count = i + 1 == listVP.Count ? index : MaxVertexCount;
                            if (count > 0) _graphicDevice.DrawUserPrimitives(PrimitiveType.TriangleList, listVP[i], 0, count / 3);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            DisposeStaticBuffer();
        }

        public void DisposeStaticBuffer(int dispLayer = -1)
        {
            if (_staticVertexBuffers != null)
                foreach (var key in _staticVertexBuffers.Keys)
                {
                    foreach (var layer in _staticVertexBuffers[key])
                    {
                        if (dispLayer >= 0)
                        {
                            if (layer[dispLayer] != null) layer[dispLayer].Dispose();
                            break;
                        }
                        foreach (var item in layer)
                        {
                            if (item != null) item.Dispose();
                        }
                    }
                }
        }
    }
}