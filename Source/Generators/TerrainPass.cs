using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;

using Origin.Source.ECS;
using Origin.Source.Utils;

using System.Collections.Generic;

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
                site.Blocks = new Arch.Core.Entity[site.Size.X, site.Size.Y, site.Size.Z];

            var dirtDepth = parameters.Get<int>("Int", "DirtDepth").Value;
            var baseHeight = (int)(site.Size.Z * 0.7f);
            var scale = 5;

            var archetype = new ComponentType[0];
            //site.ECSWorld.Reserve(archetype, site.Size.X * site.Size.Y * site.Size.Z);
            // Loop through each voxel in the world array
            for (int x = 0; x < site.Size.X; x++)
            {
                for (int y = 0; y < site.Size.Y; y++)
                {
                    // Calculate the height of the voxel based on the height map
                    int height = (int)(heightMap[x, y] * scale + baseHeight);
                    for (int z = 0; z < site.Size.Z; z++)
                    {
                        Entity ent = site.ECSWorld.Create(new OnSitePosition(new Point3(x, y, z)),
                            new TileVisibility());
                        // Set the voxel value based on the height and the current z position

                        if (z <= height - dirtDepth)
                        {
                            site.ECSWorld.Add(ent,
                                new TileStructure()
                                {
                                    WallMaterial = GlobalResources.GetTerrainMaterialByID("GRANITE"),
                                    FloorMaterial = GlobalResources.GetTerrainMaterialByID("GRANITE"),
                                });
                        }
                        else if (z > height - dirtDepth && z <= height)
                        {
                            site.ECSWorld.Add(ent,
                                new TileStructure()
                                {
                                    WallMaterial = GlobalResources.GetTerrainMaterialByID("DIRT"),
                                    FloorMaterial = GlobalResources.GetTerrainMaterialByID("DIRT"),
                                });

                            if (z == height)
                            {
                                ref var structure = ref ent.Get<TileStructure>();
                                structure.FloorEmbeddedMaterial = GlobalResources.GetTerrainMaterialByID("GRASS");
                            }
                        }
                        else
                        {
                            //site.ECSWorld.Add(ent, new IsAirTile());
                        }
                        site.Blocks[x, y, z] = ent;
                    }
                }
            }
        }

        /*public float[,] GenerateHeightMap(int width, int height, float scale)
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
        }*/

        public float[,] GenerateHeightMap(int width, int height, float scale)
        {
            float[,] heightMap = new float[width, height];
            FastNoiseLite fnl = new FastNoiseLite(12345);
            fnl.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFractalOctaves(8);
            fnl.SetFrequency(scale);
            fnl.SetFractalGain(0.3f);
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