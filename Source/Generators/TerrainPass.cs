using Origin.Source.Utils;

using SimplexNoise;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Reflection.Metadata.BlobBuilder;

namespace Origin.Source.Generators
{
    internal class TerrainPass : AbstractPass
    {
        public override void Run(Site site, Point3 size, SiteGeneratorParameters parameters, int seed)
        {
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
            var scale = 5;

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
                        else
                        {
                            wall = floor = TerrainMaterial.AIR_NULL_MAT_ID;
                        }

                        site.Blocks[x, y, z] = new SiteCell(site, new Point3(x, y, z),
                            wallMatID: wall, floorMatID: floor, embWallMatID: embWall, embFloorMatID: embFloor);
                    }
                }
            }
        }

        public float[,] GenerateHeightMap(int width, int height, float scale)
        {
            float[,] heightMap = new float[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heightMap[i, j] = Noise.CalcPixel2D(i, j, scale) / 128f;
                }
            }
            //heightMap = Noise.Calc2D(width, height, scale);
            return heightMap;
        }
    }
}