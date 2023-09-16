using Arch.Core;

using Microsoft.Xna.Framework;

using MonoGame.Extended;

using Origin.Source.GameComponentsServices;
using Origin.Source.Generators;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Point3 = Origin.Source.Utils.Point3;

namespace Origin.Source
{
    public class SiteStructureComp
    {
        public Point3 Size { get; private set; }

        private Site _Site;
        private World _ECSWBlock;
        public SparseSiteMap<Dictionary<CellStructure, Entity>> _Blocks;

        private BlockGenerator blockGenerator;
        private SiteGenerator siteGenerator;

        public SiteStructureComp(Site site, Point3 size)
        {
            Size = size;
            _Blocks = new SparseSiteMap<Dictionary<CellStructure, Entity>>(Size);
            _Blocks.AddChunk(0);
            _Site = site;

            _ECSWBlock = World.Create();
            Entity e = _ECSWBlock.Create();

            // TODO: make event for updating current size of world
            

            blockGenerator = new BlockGenerator();
            blockGenerator.Parameters.Add("Seed", 12345);
            blockGenerator.Parameters.Add("Scale", 0.005f);
            blockGenerator.Parameters.Add("BaseHeight", 25);
            blockGenerator.Parameters.Add("DirtDepth", 4f);

            siteGenerator = new SiteGenerator(blockGenerator, ref _Blocks, _ECSWBlock, site, Size);

            siteGenerator.InitLoad();
            Size = new Point3(Size.X, Size.Y, _Blocks.ChunksCount * 8);
        }

        public void Update(GameTime gameTime)
        {
            Size = new Point3(Size.X, Size.Y, _Blocks.ChunksCount * 8);
        }

        private void OnWorldChanged()
        {
            // TODO: make event for updating current size of world
            Size = new Point3(Size.X, Size.Y, _Blocks.ChunksCount * 8);
        }
    }
}