using Origin.Source.Utils;

using SimplexNoise;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Reflection.Metadata.BlobBuilder;

namespace Origin.Source.Generators
{
    /*internal struct HMapCell
    {
        private float _groundHeight;

        public float GroundHeight
        {
            get
            {
                return _groundHeight;
            }
            set
            {
                if (value < -1) _groundHeight = -1;
                else if (value > 1) _groundHeight = 1;
                else _groundHeight = value;
            }
        }

        private float _waterHeight;

        public float WaterHeight
        {
            get
            {
                return _waterHeight;
            }
            set
            {
                if (value < -1) _waterHeight = -1;
                else if (value > 1) _waterHeight = 1;
                else _waterHeight = value;
            }
        }
    }*/

    internal class TerrainPass : AbstractPass
    {
        //private HMapCell[,] hMapCells;

        public override void Run(Site site, Point3 size, SiteGeneratorParameters parameters, int seed)
        {
            /*DiamondSquare ds = new DiamondSquare(size.X, 0.1, 1);
            double[,] hm = ds.getData();*/
            var scale = parameters.Get<float>("Float", "Scale").Value;
            float[,] heightMap = GenerateHeightMap(size.X, size.Y, scale);
            Generate3dWorldArray(site, heightMap, parameters);
        }

        public static void Generate3dWorldArray(
            Site site,
            float[,] heightMap,
            SiteGeneratorParameters parameters)
        {
            if (site.Blocks == null)
                site.Blocks = new SiteCell[site.Size.X, site.Size.Y, site.Size.Z];

            var dirtDepth = parameters.Get<int>("Int", "DirtDepth").Value;
            var baseHeight = (int)(site.Size.Z * 0.7f);
            var scale = 2;

            // Loop through each voxel in the world array
            for (int x = 0; x < site.Size.X; x++)
            {
                for (int y = 0; y < site.Size.Y; y++)
                {
                    // Calculate the height of the voxel based on the height map
                    int height = (int)(heightMap[x, y] * scale + baseHeight);
                    for (int z = 0; z < site.Size.Z; z++)
                    {
                        // Set the voxel value based on the height and the current z position
                        string wall, floor, embWall, embFloor;
                        wall = floor = embWall = embFloor = null;
                        int waterLevel = 0, lavaLevel = 0;
                        if (z <= height - dirtDepth)
                        {
                            wall = floor = "Granite";
                        }
                        else if (z > height - dirtDepth && z <= height)
                        {
                            wall = floor = "Dirt";
                            if (z == height)
                            {
                                embFloor = "Grass";
                            }
                        }
                        else if (z < 88)
                        {
                            wall = floor = TerrainMaterial.AIR_NULL_MAT_ID;
                            waterLevel = 7;
                        }
                        else
                        {
                            wall = floor = TerrainMaterial.AIR_NULL_MAT_ID;
                        }

                        site.Blocks[x, y, z] = new SiteCell(site, new Point3(x, y, z),
                            wallMatID: wall, floorMatID: floor, embWallMatID: embWall, embFloorMatID: embFloor,
                            waterLevel: waterLevel);
                    }
                }
            }
        }

        public float[,] GenerateHeightMap(int width, int height, float scale)
        {
            float[,] heightMap = new float[width, height];
            FastNoiseLite fnl = new FastNoiseLite(12345);
            fnl.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFractalOctaves(8);
            fnl.SetFrequency(scale);
            fnl.SetFractalGain(0.2f);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heightMap[i, j] = fnl.GetNoise(i, j) * 2 - 1;
                }
            }
            //heightMap = Noise.Calc2D(width, height, scale);
            return heightMap;
        }
    }
}