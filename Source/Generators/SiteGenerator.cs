using Arch.Core;
using Arch.Core.Extensions;

using Origin.Source.ECS.Components;
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
        private Point3 size;
        private World ECSW;
        private Site site;
        private SparseSiteMap<Dictionary<CellStructure, Entity>> blocks;

        public SiteGenerator(BlockGenerator blockGenerator,
            ref SparseSiteMap<Dictionary<CellStructure, Entity>> blocks,
            World eCSW, Site site, Point3 size)
        {
            this.blockGenerator = blockGenerator;
            this.blocks = blocks;
            ECSW = eCSW;
            this.site = site;
            this.size = size;
        }

        public void InitLoad()
        {
            List<bool[,]> visited = new List<bool[,]>();
            //bool[,,] visited = new bool[size.X, size.Y, size.Z];
            void Visit(int x, int y, int z)
            {
                if (visited.Count == z) visited.Add(new bool[size.X, size.Y]);
                if (x < 0 || y < 0 || z < 0 || x == size.X || y == size.Y || z == 1024)
                    return;
                if (visited[z][x, y])
                    return;

                Entity ewall, efloor;
                (ewall, efloor) = blockGenerator.Generate(new Point3(x, y, z), ECSW);
                if (blocks[(ushort)x, (ushort)y, (ushort)z] == null)
                    blocks[(ushort)x, (ushort)y, (ushort)z] = new Dictionary<CellStructure, Entity>();
                if (ewall != Entity.Null)
                {
                    ewall.Add(new Position() { site = site, position = new Point3(x, y, z) });
                    blocks[(ushort)x, (ushort)y, (ushort)z][CellStructure.Wall] = ewall;
                }

                if (efloor != Entity.Null)
                {
                    efloor.Add(new Position() { site = site, position = new Point3(x, y, z) });
                    blocks[(ushort)x, (ushort)y, (ushort)z][CellStructure.Floor] = efloor;
                }

                visited[z][x, y] = true;

                HasMaterial wmat;
                ewall.TryGet<HasMaterial>(out wmat);
                HasMaterial fmat;
                efloor.TryGet<HasMaterial>(out fmat);
                bool IsFullAir = wmat.Material.ID == "AIR" && fmat.Material.ID == "AIR";

                if (IsFullAir)
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