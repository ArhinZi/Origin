using Arch.Core;

using info.lundin.math;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Newtonsoft.Json.Linq;

using Origin.Source.ECS;
using Origin.Source.Resources;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Render.GpuAcceleratedSpriteSystem.SpriteChunk;
using static System.Reflection.Metadata.BlobBuilder;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct SpriteLocator
    {
        [FieldOffset(0)]
        private int data; // 32 bits in total

        // BitField 3 bits - 8
        public int x
        {
            get { return data & 0x7; } // Mask the lower 3 bits
            set => data = (int)((data & 0xFFFFFFF8) | (value & 0x7));
        }

        // BitField 3 bits - 8
        public int y
        {
            get { return (data >> 3) & 0x7; } // Shift and mask the next 3 bits
            set => data = (int)((data & 0xFFFFFFF1F) | ((value & 0x7) << 3));
        }

        // BitField 20 bits - 1048576 (1024^2)
        public int i
        {
            get { return (data >> 6) & 0xFFFFF; } // Shift and mask the lower 20 bits
            set => data = (int)((data & 0xFFFFFC3F) | ((value & 0xFFFFF) << 6));
        }

        // BitField 6 bits - 64
        public int layer
        {
            get { return (data >> 26) & 0x3F; } // Shift and mask the next 6 bits
            set => data = (int)((data & 0xFC000000) | ((value & 0x3F) << 26));
        }
    }

    /// <summary>
    /// Batched levels.
    /// Levels have batches by Texture
    /// Each Texture has List of layers in level
    /// Each layer has List of Data
    /// </summary>
    public class SpriteChunk
    {
        public static HashSet<Texture2D> texture2Ds { get; private set; } = new HashSet<Texture2D>();
        public static GraphicsDevice GraphicsDevice;
        public static Effect Effect;

        public class Layer : IDisposable
        {
            public SpriteMainData[] dataMain;
            public SpriteExtraData[] dataExtra;
            public int dataIndex = 0;

            public int structSize;
            private byte _layer;
            public byte LayerID => _layer;

            public StructuredBuffer bufferDataMain;
            public StructuredBuffer bufferDataExtra;

            public VertexBuffer geometryBuffer;

            public bool dirtyDataMain = false;
            public bool dirtyDataExtra = false;

            public Layer(byte layer)
            {
                _layer = layer;
                structSize = Global.GPU_LAYER_PACK_COUNT;
                dataMain = new SpriteMainData[structSize];
                dataExtra = new SpriteExtraData[structSize];

                dataIndex = 0;

                ReallocMainBuffer();
                ReallocExtraBuffer();
                ReGenerateCommonGeometry();
            }

            public void ReallocMainBuffer()
            {
                if (bufferDataMain != null)
                    bufferDataMain.Dispose();
                bufferDataMain = new StructuredBuffer(GraphicsDevice, typeof(SpriteMainData), structSize, BufferUsage.WriteOnly, ShaderAccess.Read);
            }

            public void ReallocExtraBuffer()
            {
                if (bufferDataExtra != null)
                    bufferDataExtra.Dispose();
                bufferDataExtra = new StructuredBuffer(GraphicsDevice, typeof(SpriteExtraData), structSize, BufferUsage.WriteOnly, ShaderAccess.Read);
            }

            public void Dispose()
            {
                if (bufferDataMain != null)
                    bufferDataMain.Dispose();
                if (bufferDataExtra != null)
                    bufferDataExtra.Dispose();
                if (geometryBuffer != null)
                    geometryBuffer.Dispose();
            }

            public void ReGenerateCommonGeometry()
            {
                int size = structSize * 6;
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

                geometryBuffer = new VertexBuffer(GraphicsDevice, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
                geometryBuffer.SetData(_vertices);
            }
        }

        public Dictionary<Texture2D, Dictionary<int, Layer>> layersBatches = new();
        public Point position;
        private object _lock = new object();

        private Site site;

        public SpriteChunk(Point pos, Site site)
        {
            position = pos;
            Debug.Assert(!(position.X >= 8 || position.Y >= 8), "Position is invalid " + position.ToString());

            this.site = site;
        }

        public Layer GetLayer(Texture2D texture, byte layer)
        {
            Debug.Assert(layer < 64, "Layer is invalid " + layer);
            lock (_lock)
            {
                if (!layersBatches.TryGetValue(texture, out Dictionary<int, Layer> layerList))
                {
                    layerList = new Dictionary<int, Layer>();
                    layersBatches.Add(texture, layerList);
                    texture2Ds.Add(texture);
                }

                if (layerList.TryGetValue(layer, out Layer val))
                {
                    return val;
                }
                else
                {
                    layerList.Add(layer, new Layer(layer));
                    return layerList.Last().Value;
                }
            }
        }

        /// <summary>
        /// After the Append you should call SetLevel to send data to GPU
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="dataMain"></param>
        /// <param name="dataExtra"></param>
        /// <returns></returns>
        public SpriteLocator AppendDataDirectly(Layer layer, SpriteMainData dataMain, SpriteExtraData dataExtra)
        {
            if (layer.structSize == layer.dataIndex)
            {
                Array.Resize(ref layer.dataMain, layer.structSize * 2);
                Array.Resize(ref layer.dataExtra, layer.structSize * 2);
                layer.structSize = layer.structSize * 2;
                layer.ReGenerateCommonGeometry();
                Debug.Assert(layer.structSize <= 1048576, "Too many elements");
            }
            SpriteLocator spriteLocator = new SpriteLocator()
            {
                i = layer.dataIndex,
                layer = layer.LayerID,
                x = position.X,
                y = position.Y
            };
            layer.dataMain[layer.dataIndex] = dataMain;
            layer.dataExtra[layer.dataIndex] = dataExtra;
            layer.dataIndex++;
            layer.dirtyDataMain = true;
            layer.dirtyDataExtra = true;
            return spriteLocator;
        }

        public void Set()
        {
            // Set Data Main
            void SetDataMain(Layer layer)
            {
                if (layer.dirtyDataMain)
                {
                    layer.dirtyDataMain = false;
                    if (layer.bufferDataMain.ElementCount < layer.structSize)
                        layer.ReallocMainBuffer();
                    layer.bufferDataMain.SetData(layer.dataMain);
                }
            }

            // Set Data Extra
            void SetDataExtra(Layer layer)
            {
                if (layer.dirtyDataExtra)
                {
                    layer.dirtyDataExtra = false;
                    if (layer.bufferDataExtra.ElementCount < layer.structSize)
                        layer.ReallocExtraBuffer();
                    layer.bufferDataExtra.SetData(layer.dataExtra);
                }
            }

            foreach (var level in layersBatches)
            {
                var batch = level.Value;
                foreach (var layer in batch.Values)
                {
                    SetDataMain(layer);
                    SetDataExtra(layer);
                }
            }
        }

        public void Clear()
        {
            foreach (var level in layersBatches)
            {
                var batch = level.Value;
                for (int i = 0; i < batch.Count; i++)
                {
                    batch.Clear();
                }
            }
        }

        public void Draw()
        {
            Matrix WVP = Matrix.Multiply(Matrix.Multiply(site.Camera.WorldMatrix, site.Camera.Transformation),
                                        site.Camera.Projection);
            foreach (var tex in layersBatches.Keys)
            {
                Effect.Parameters["SpriteTexture"].SetValue(tex);
                Effect.Parameters["texSize"].SetValue(new Vector2(tex.Width, tex.Height));
                Effect.CurrentTechnique.Passes[0].Apply();
                foreach (var pair in layersBatches[tex].OrderBy(x => x.Key))
                    if (pair.Value.dataIndex != 0)
                    {
                        Effect.CurrentTechnique = Effect.Techniques["Instancing"];
                        Effect.Parameters["WorldViewProjection"].SetValue(WVP);
                        Effect.Parameters["MainBuffer"].SetValue(pair.Value.bufferDataMain);
                        Effect.Parameters["ExtraBuffer"].SetValue(pair.Value.bufferDataExtra);

                        Effect.CurrentTechnique.Passes[0].Apply();
                        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                        GraphicsDevice.BlendState = BlendState.AlphaBlend;

                        GraphicsDevice.SetVertexBuffer(pair.Value.geometryBuffer);

                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, pair.Value.dataIndex * 2);
                    }
            }
        }
    }
}