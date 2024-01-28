using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.ECS;
using Origin.Source.Resources;
using Origin.Source.Utils;

using static System.Net.Mime.MediaTypeNames;
using static Origin.Source.Render.GpuAcceleratedSpriteSystem.SpriteChunk;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    public class StaticHiddenLayeredDrawer : IBaseLayeredDrawer
    {
        public static int BIT_COUNT = 32 * 4;
        private GraphicsDevice device = OriginGame.Instance.GraphicsDevice;
        private Float4[][] _lData;
        private Float4[][] _sData;
        private StructuredBuffer[] bufferLayers;
        private StructuredBuffer[] bufferSides;

        private int RBIT_COUNT;
        private int LCHUNK_SIZE;
        private int SCHUNK_SIZE;

        private Site _site;

        private Effect _effect;
        private Texture2D _texture;

        public VertexBuffer geometryBuffer;

        public StaticHiddenLayeredDrawer(Site site)
        {
            _site = site;
            _effect = OriginGame.Instance.Content.Load<Effect>("FX/InstancedTileDraw");
            _texture = GlobalResources.HIDDEN_WALL_SPRITE.Texture;
            RBIT_COUNT = Math.Min(_site.Size.X, BIT_COUNT);
            LCHUNK_SIZE = (_site.Size.X / RBIT_COUNT) * _site.Size.Y;
            SCHUNK_SIZE = (_site.Size.X / RBIT_COUNT) + (_site.Size.Y / RBIT_COUNT);

            _lData = new Float4[_site.Size.Z][];
            for (int z = 0; z < _lData.Length; z++)
            {
                _lData[z] = new Float4[LCHUNK_SIZE];
                for (int xy = 0; xy < _lData[z].Length; xy++)
                    _lData[z][xy] = new Float4();
            }

            _sData = new Float4[_site.Size.Z][];
            for (int z = 0; z < _sData.Length; z++)
            {
                _sData[z] = new Float4[SCHUNK_SIZE];
                for (int xy = 0; xy < _sData[z].Length; xy++)
                    _sData[z][xy] = new Float4();
            }

            bufferLayers = new StructuredBuffer[_site.Size.Z];
            for (int i = 0; i < bufferLayers.Length; i++)
            {
                bufferLayers[i] = new StructuredBuffer(device, typeof(Float4), LCHUNK_SIZE, BufferUsage.None, ShaderAccess.Read);
            }
            bufferSides = new StructuredBuffer[_site.Size.Z];
            for (int i = 0; i < bufferSides.Length; i++)
            {
                bufferSides[i] = new StructuredBuffer(device, typeof(Float4), SCHUNK_SIZE, BufferUsage.None, ShaderAccess.Read);
            }

            GenerateInstanceGeometry();
        }

        public void MakeHidden(Point3 pos)
        {
            SetHiddence(pos, true);
        }

        public void ClearHidden(Point3 pos)
        {
            SetHiddence(pos, false);
        }

        public void SetHiddence(Point3 pos, bool value)
        {
            int nxbit = pos.X % RBIT_COUNT;
            int xy = (_site.Size.X / RBIT_COUNT) * pos.Y + pos.X / RBIT_COUNT;
            _lData[pos.Z][xy].SetBit(nxbit, value);

            if (_site.Size.Y - 1 == pos.Y)
            {
                xy = pos.X / RBIT_COUNT;
                nxbit = pos.X % RBIT_COUNT;
                _sData[pos.Z][xy].SetBit(nxbit, value);
            }
            else if (_site.Size.X - 1 == pos.X)
            {
                xy = (_site.Size.X / RBIT_COUNT) + ((_site.Size.Y / RBIT_COUNT) - 1 - pos.Y / RBIT_COUNT);
                nxbit = (_site.Size.Y - 1 - pos.Y) % RBIT_COUNT;
                _sData[pos.Z][xy].SetBit(nxbit, value);
            }
        }

        public void InitTerrainHiddence()
        {
            for (int z = 0; z < _site.Size.Z; z++)
                for (int x = 0; x < _site.Size.X; x++)
                    for (int y = 0; y < _site.Size.Y; y++)
                    {
                        Point3 tilePos = new Point3(x, y, z);
                        Entity tile = _site.Map[tilePos];

                        if (tile == Entity.Null)
                        {
                            MakeHidden(tilePos);
                        }
                    }
            Set();
        }

        public void Set()
        {
            for (int z = 0; z < _site.Size.Z; z++)
            {
                bufferLayers[z].SetData(_lData[z]);
                bufferSides[z].SetData(_sData[z]);
            }
        }

        private void GenerateInstanceGeometry()
        {
            int size = _site.Size.X * _site.Size.Y;
            GeometryData[] _vertices = new GeometryData[6 * size];

            #region filling vertices

            for (int i = 0; i < size; i++)
            {
                _vertices[i * 6 + 0].World = new Color((byte)0, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 1].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 2].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 3].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 4].World = new Color((byte)255, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 5].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
            }

            #endregion filling vertices

            geometryBuffer = new VertexBuffer(device, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
            geometryBuffer.SetData(_vertices);
        }

        public void Draw(int layer)
        {
            Matrix WVP = Matrix.Multiply(Matrix.Multiply(_site.Camera.WorldMatrix, _site.Camera.Transformation),
                                _site.Camera.Projection);

            _effect.Parameters["SpriteTexture"].SetValue(_texture);
            _effect.Parameters["texSize"].SetValue(new Vector2(_texture.Width, _texture.Height));
            _effect.Parameters["worldSize"].SetValue(new Vector2(_site.Size.X, _site.Size.Y));
            _effect.Parameters["CurrentLevel"].SetValue(layer);
            _effect.Parameters["RBIT_COUNT"].SetValue(RBIT_COUNT);
            _effect.CurrentTechnique = _effect.Techniques["HiddenInstancing"];

            _effect.Parameters["WorldViewProjection"].SetValue(WVP);

            _effect.Parameters["HiddenLBuffer"].SetValue(bufferLayers[layer]);

            device.SetVertexBuffer(geometryBuffer);

            _effect.CurrentTechnique.Passes[0].Apply();

            device.DepthStencilState = DepthStencilState.Default;
            device.BlendState = BlendState.AlphaBlend;

            device.DrawPrimitives(PrimitiveType.TriangleList, 0, _site.Size.X * _site.Size.Y * 2);
            //device.DrawPrimitives(PrimitiveType.TriangleList, 0, 32 * 2 * 2);
        }
    }
}