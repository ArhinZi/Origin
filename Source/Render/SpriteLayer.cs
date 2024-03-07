using Microsoft.Xna.Framework.Graphics;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Origin.Source.Render
{
    public class SpriteLayer : IDisposable
    {
        private GraphicsDevice graphicsDevice = Global.GraphicsDevice;

        public SpriteMainData[] dataMain;
        public SpriteExtraData[] dataExtra;
        public uint dataIndex { get; private set; } = 0;

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

        public SpriteLayer(byte layer, Texture2D tex, int reserv = Global.GPU_LAYER_PACK_COUNT)
        {
            _layer = layer;
            structSize = reserv;
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

        /// <summary>
        /// Increment internal counter of sprites
        /// </summary>
        /// <param name="dynamic">true if need to update structure buffers only</param>
        /// <returns></returns>
        public uint Increment(bool dynamic = false)
        {
            dataIndex++;
            if (dataIndex >= structSize)
            {
                if (dynamic)
                {
                    bufferDataMain.GetData(dataMain);
                    bufferDataExtra.GetData(dataExtra);
                }

                while (dataIndex >= structSize)
                {
                    structSize = structSize * 2;
                }
                Debug.Assert(structSize <= 1048576, "Too many elements");
                Array.Resize(ref dataMain, structSize);
                Array.Resize(ref dataExtra, structSize);

                ReallocMainBuffer();
                ReallocExtraBuffer();

                if (dynamic)
                {
                    bufferDataMain.SetData(dataMain);
                    bufferDataExtra.SetData(dataExtra);
                }
            }
            return dataIndex;
        }

        public void ReallocMainBuffer()
        {
            if (bufferDataMain != null)
                bufferDataMain.Dispose();
            bufferDataMain = new StructuredBuffer(graphicsDevice, typeof(SpriteMainData), structSize, BufferUsage.None, ShaderAccess.Read);
        }

        public void ReallocExtraBuffer()
        {
            if (bufferDataExtra != null)
                bufferDataExtra.Dispose();
            bufferDataExtra = new StructuredBuffer(graphicsDevice, typeof(SpriteExtraData), structSize, BufferUsage.None, ShaderAccess.Read);
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
}