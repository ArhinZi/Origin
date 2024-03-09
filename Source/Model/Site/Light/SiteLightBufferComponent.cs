using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

using Origin.Source.Resources;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Reflection.Metadata.BlobBuilder;

namespace Origin.Source.Model.Site.Light
{
    public class SiteLightBufferComponent
    {
        private Site site;

        public List<PackedLight[]> lightData { get; private set; }

        public List<StructuredBuffer> buffers;
        public bool bufferDirty = false;

        public SiteLightBufferComponent(Site site)
        {
            this.site = site;
            lightData = new List<PackedLight[]>();
            lightData.Capacity = site.Size.Z;
            buffers = new List<StructuredBuffer>();
            for (int i = 0; i < site.Size.Z; i++)
            {
                lightData.Add(null);
                buffers.Add(new StructuredBuffer(Global.GraphicsDevice, typeof(PackedLight), site.Size.X * site.Size.Y, BufferUsage.None, ShaderAccess.Read, StructuredBufferType.Basic));
            }
        }

        private int unfold(int X, int Y)
        {
            return (X * site.Size.X + Y);
        }

        private void fold(int index, out int X, out int Y)
        {
            Y = index / site.Size.Y;
            X = index % site.Size.Y;
        }

        public void SetTile(Point3 pos, PackedLight pl)
        {
            if (lightData[pos.Z] == null)
            {
                lightData[pos.Z] = new PackedLight[site.Size.X * site.Size.Y];
            }
            lightData[pos.Z][unfold(pos.X, pos.Y)] = pl;
        }

        public bool TryGetTile(Point3 pos, out PackedLight pl)
        {
            pl = default;
            if (lightData[pos.Z] == null) return false;

            pl = lightData[pos.Z][unfold(pos.X, pos.Y)];
            return true;
        }

        public ref PackedLight GetTile(Point3 pos)
        {
            if (lightData[pos.Z] == null)
            {
                lightData[pos.Z] = new PackedLight[site.Size.X * site.Size.Y];
            }
            return ref lightData[pos.Z][unfold(pos.X, pos.Y)];
        }

        public void SetBuffers()
        {
            if (bufferDirty)
            {
                for (int i = 0; i < site.Size.Z; i++)
                {
                    if (lightData[i] != null)
                    {
                        buffers[i].SetData(lightData[i]);

                        //buffers[i].GetData(lightData[i]);
                    }
                }
            }
            bufferDirty = false;
        }
    }
}