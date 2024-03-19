using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Model.Site.Light;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Site.Fluid
{
    public partial class FluidComponent
    {
        public static readonly int CHUNK_SIZE = 32;

        private readonly Site site;

        public Point ChunksCount { get; private set; } = Point.Zero;

        public List<Chunk[,]> fluidData;

        public FluidComponent(Site site)
        {
            this.site = site;
            ChunksCount = new Point(site.Size.X / CHUNK_SIZE, site.Size.Y / CHUNK_SIZE);
            fluidData = new List<Chunk[,]>
            {
                Capacity = site.Size.Z
            };
            for (int z = 0; z < site.Size.Z; z++)
            {
                fluidData.Add(null);
            }
        }

        private void InitChunk(int z)
        {
            fluidData[z] = new Chunk[ChunksCount.X, ChunksCount.Y];
        }
    }
}