using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.Site.Fluid
{
    public partial class FluidComponent
    {
        public class Chunk
        {
            private readonly Site site;

            public FluidParticle[] packedFluids;

            public Chunk(Site site)
            {
                this.site = site;
            }

            private int Unfold(int X, int Y)
            {
                return (X * CHUNK_SIZE + Y);
            }

            private void Fold(int index, out int X, out int Y)
            {
                Y = index / CHUNK_SIZE;
                X = index % CHUNK_SIZE;
            }
        }
    }
}