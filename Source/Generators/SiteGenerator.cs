using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Generators
{
    public class SiteGenerator
    {
        private BlockGenerator blockGenerator;
        private Site site;

        public SiteGenerator(BlockGenerator blockGenerator, Site site)
        {
            this.blockGenerator = blockGenerator;
            this.site = site;
        }

        public void InitLoad()
        {
            bool[,,] visited = new bool[site.Size.X, site.Size.Y, site.Size.Z];
            void Visit(int x, int y, int z)
            {
                if (x < 0 || y < 0 || z < 0 || x == site.Size.X || y == site.Size.Y || z == site.Size.Z)
                    return;
                if (visited[x, y, z])
                    return;

                SiteCell sc = blockGenerator.Generate(site, new Point3(x, y, z));
                site.Blocks[(ushort)x, (ushort)y, (ushort)z] = sc;
                visited[x, y, z] = true;

                if (sc.IsFullAir)
                {
                    Visit(x + 1, y, z);
                    Visit(x, y + 1, z);
                    Visit(x, y, z + 1);
                }
            }

            Visit(0, 0, 0);
        }
    }
}