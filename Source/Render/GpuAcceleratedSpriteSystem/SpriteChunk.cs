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

            public int TextureMetaID { get; private set; }

            public StructuredBuffer bufferDataMain;
            public StructuredBuffer bufferDataExtra;

            public bool dirtyDataMain = false;
            public bool dirtyDataExtra = false;

            public Queue<uint> FreeSpace;
            public List<SpriteLocator> SpritesToRemove;
            public List<UpdateSpriteInstanceData> SpritesToUpdate;
            public List<UpdateSpriteInstanceData> SpritesToAdd;

            public Layer(byte layer, Texture2D tex)
            {
                _layer = layer;
                structSize = Global.GPU_LAYER_PACK_COUNT;
                dataMain = new SpriteMainData[structSize];
                dataExtra = new SpriteExtraData[structSize];

                TextureMetaID = GlobalResources.GetResourceMetaID(GlobalResources.Textures, tex);

                dataIndex = 0;

                ReallocMainBuffer();
                ReallocExtraBuffer();

                SpritesToRemove = new List<SpriteLocator>();
                SpritesToUpdate = new List<UpdateSpriteInstanceData>();
                SpritesToAdd = new List<UpdateSpriteInstanceData>();
                FreeSpace = new Queue<uint>();
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
                    layerList.Add(layer, new Layer(layer, texture));
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
                TextureMetaID = (uint)layer.TextureMetaID,
                Layer = layer.LayerID,
                Index = layer.dataIndex
            };
            layer.dataMain[layer.dataIndex] = dataMain;
            layer.dataExtra[layer.dataIndex] = dataExtra;
            layer.dataIndex++;
            layer.dirtyDataMain = true;
            layer.dirtyDataExtra = true;
            return spriteLocator;
        }

        public void InitSet()
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

        public void ClearLayer(byte l)
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

        public void ScheduleRemove(SpriteLocator locator)
        {
            layersBatches[GlobalResources.GetByMetaID(GlobalResources.Textures, (int)locator.TextureMetaID)]
                [(int)locator.Layer].SpritesToRemove.Add(locator);
        }

        public SpriteLocator ScheduleAdd(Layer layer, SpriteMainData dataMain, SpriteExtraData dataExtra)
        {
            uint ind = 0;
            if (layer.FreeSpace.TryDequeue(out uint res))
            {
                ind = res;
            }
            else
            {
                ind = layer.dataIndex;
                layer.dataIndex++;
                Debug.Assert(layer.dataIndex < layer.structSize);
            }
            layer.SpritesToAdd.Add(new UpdateSpriteInstanceData()
            {
                index = ind,
                mainData = dataMain,
                extraData = dataExtra
            });
            SpriteLocator spriteLocator = new SpriteLocator()
            {
                TextureMetaID = (uint)layer.TextureMetaID,
                Layer = layer.LayerID,
                Index = ind
            };
            return spriteLocator;
        }

        public void RemoveScheduled()
        {
            void Remove(Effect effect, GraphicsDevice device, Layer layer)
            {
                int count = layer.SpritesToRemove.Count;
                if (count == 0) return;

                uint[] remove = new uint[layer.SpritesToRemove.Count];
                for (int i = 0; i < remove.Length; i++)
                {
                    remove[i] = layer.SpritesToRemove[i].Index;
                    layer.FreeSpace.Enqueue(remove[i]);
                }
                layer.SpritesToRemove.Clear();
                var rbuff = new StructuredBuffer(device, typeof(uint), count, BufferUsage.WriteOnly, ShaderAccess.Read);

                SiteRenderer.InstanceMainEffect.Parameters["RWMainBuffer"].SetValue(layer.bufferDataMain);
                SiteRenderer.InstanceMainEffect.Parameters["RWExtraBuffer"].SetValue(layer.bufferDataExtra);
                rbuff.SetData(remove);
                SiteRenderer.InstanceMainEffect.Parameters["Remove"].SetValue(rbuff);
                SiteRenderer.InstanceMainEffect.Parameters["count"].SetValue(count);

                effect.CurrentTechnique.Passes["Remove"].ApplyCompute();
                int ihh = (count + 63) / 64;
                device.DispatchCompute((count + 63) / 64, 1, 1);
            }

            Effect effect = SiteRenderer.InstanceMainEffect;
            GraphicsDevice device = OriginGame.Instance.GraphicsDevice;
            effect.CurrentTechnique = effect.Techniques["BufferUpdating"];

            foreach (var level in layersBatches)
            {
                var batch = level.Value;
                foreach (var layer in batch.Values)
                {
                    Remove(effect, device, layer);
                }
            }
        }

        public void AddScheduled()
        {
            void Add(Effect effect, GraphicsDevice device, Layer layer)
            {
                int count = layer.SpritesToAdd.Count;
                if (count == 0) return;

                UpdateSpriteInstanceData[] add = layer.SpritesToAdd.ToArray();
                layer.SpritesToAdd.Clear();
                var buff = new StructuredBuffer(device, typeof(UpdateSpriteInstanceData), count, BufferUsage.WriteOnly, ShaderAccess.Read);

                SiteRenderer.InstanceMainEffect.Parameters["RWMainBuffer"].SetValue(layer.bufferDataMain);
                SiteRenderer.InstanceMainEffect.Parameters["RWExtraBuffer"].SetValue(layer.bufferDataExtra);
                buff.SetData(add);
                SiteRenderer.InstanceMainEffect.Parameters["UpdateData"].SetValue(buff);
                SiteRenderer.InstanceMainEffect.Parameters["count"].SetValue(count);

                effect.CurrentTechnique.Passes["Update"].ApplyCompute();
                int ihh = (count + 63) / 64;
                device.DispatchCompute((count + 63) / 64, 1, 1);
            }

            Effect effect = SiteRenderer.InstanceMainEffect;
            GraphicsDevice device = OriginGame.Instance.GraphicsDevice;
            effect.CurrentTechnique = effect.Techniques["BufferUpdating"];

            foreach (var level in layersBatches)
            {
                var batch = level.Value;
                foreach (var layer in batch.Values)
                {
                    Add(effect, device, layer);
                }
            }
        }
    }
}