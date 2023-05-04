using Origin.Source.Utils;

using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D9;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Generators
{
    public class BlockGenerator
    {
        public Dictionary<string, float> Parameters { get; set; } = new Dictionary<string, float>();

        public SiteCell Generate(Site site, Point3 blockPosition)
        {
            FastNoiseLite fnl = new FastNoiseLite((int)Parameters["Seed"]);
            fnl.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFractalOctaves(8);
            fnl.SetFrequency(Parameters["Scale"]);
            fnl.SetFractalGain(0.2f);
            float h = fnl.GetNoise(blockPosition.X, blockPosition.Y) * 2 - 1;
            int height = (int)(Parameters["BaseHeight"] - h * 2);
            int localDirtDepth = (int)(Parameters["DirtDepth"] - (h * 0.5));
            localDirtDepth = localDirtDepth < 0 ? 0 : localDirtDepth;
            int z = blockPosition.Z;

            string wall, floor, embWall, embFloor;
            wall = floor = embWall = embFloor = null;
            int waterLevel = 0, lavaLevel = 0;

            if (z >= height)
            {
                if (z - height <= localDirtDepth)
                {
                    wall = floor = "Dirt";
                    if (z == height)
                    {
                        embFloor = "Grass";
                    }
                }
                else
                {
                    wall = floor = "Granite";
                }
            }
            /*else if (z < 30)
            {
                wall = floor = TerrainMaterial.AIR_NULL_MAT_ID;
                waterLevel = 7;
            }*/
            else
            {
                wall = floor = TerrainMaterial.AIR_NULL_MAT_ID;
            }

            return new SiteCell(site, blockPosition,
                wallMatID: wall, floorMatID: floor, embWallMatID: embWall, embFloorMatID: embFloor,
                waterLevel: waterLevel);
        }
    }
}