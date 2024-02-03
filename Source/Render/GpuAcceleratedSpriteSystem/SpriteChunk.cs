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
    /// <summary>
    /// Batched levels.
    /// Levels have batches by Texture
    /// Each Texture has List of layers in level
    /// Each layer has List of Data
    /// </summary>
    public class SpriteChunk
    {
        public static HashSet<Texture2D> texture2Ds { get; private set; } = new HashSet<Texture2D>();

        public class Layer : IDisposable
        {
            private GraphicsDevice graphicsDevice = OriginGame.Instance.GraphicsDevice;

            public SpriteMainData[] dataMain;
            public SpriteExtraData[] dataExtra;
            public uint dataIndex = 0;

            public int structSize;
            private byte _layer;
            public byte LayerID => _layer;

            public StructuredBuffer bufferDataMain;
            public StructuredBuffer bufferDataExtra;

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
            }

            public void ReallocMainBuffer()
            {
                if (bufferDataMain != null)
                    bufferDataMain.Dispose();
                bufferDataMain = new StructuredBuffer(graphicsDevice, typeof(SpriteMainData), structSize, BufferUsage.WriteOnly, ShaderAccess.Read);
            }

            public void ReallocExtraBuffer()
            {
                if (bufferDataExtra != null)
                    bufferDataExtra.Dispose();
                bufferDataExtra = new StructuredBuffer(graphicsDevice, typeof(SpriteExtraData), structSize, BufferUsage.WriteOnly, ShaderAccess.Read);
            }

            public void Dispose()
            {
                if (bufferDataMain != null)
                    bufferDataMain.Dispose();
                if (bufferDataExtra != null)
                    bufferDataExtra.Dispose();
            }

            public void Clear()
            {
                dataIndex = 0;
            }
        }

        public Dictionary<Texture2D, Dictionary<int, Layer>> layersBatches = new();
        private object _lock = new object();

        public Point position;

        public SpriteChunk(Point pos)
        {
            position = pos;
            Debug.Assert(!(position.X >= 8 || position.Y >= 8), "Position is invalid " + position.ToString());
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
                Debug.Assert(layer.structSize <= 1048576, "Too many elements");
            }
            SpriteLocator spriteLocator = new SpriteLocator()
            {
                i = layer.dataIndex,
                layer = layer.LayerID,
                x = (uint)position.X,
                y = (uint)position.Y
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
                batch.Clear();
            }
        }

        public void ClearLayer(int l)
        {
            foreach (var level in layersBatches)
            {
                var batch = level.Value;
                if (batch.TryGetValue(l, out Layer layer))
                {
                    layer.Clear();
                }
            }
        }
    }
}