using Microsoft.Xna.Framework;
using Origin.Draw;
using Origin.World;
using SimplexNoise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Utils
{
    class WorldUtils
    {
        public static float[,] GenerateHeightMap(int width, int height, float scale)
        {

            float[,] heightMap = new float[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heightMap[i, j] = Noise.CalcPixel2D(i, j, scale)/128f;
                }
            }
                //Noise.Calc2D(width, height, scale);
            return heightMap;
        }

        public static float[,] GenerateFlatHeightMap(int width, int height, float scale)
        {

            float[,] heightMap = new float[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heightMap[i, j] = 1;
                }
            }
            //Noise.Calc2D(width, height, scale);
            return heightMap;
        }

        public static SiteBlock[,,] Generate3dWorldArray(float[,] heightMap, int worldWidth, int worldHeight, int worldDepth, int baseHeight, float scale)
        {
            SiteBlock[,,] worldArray = new SiteBlock[worldWidth, worldHeight, worldDepth];

            // Loop through each voxel in the world array
            for (int x = 0; x < worldWidth; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    // Calculate the height of the voxel based on the height map
                    float height = heightMap[x, y]*scale + baseHeight;
                    for (int z = 0; z < worldDepth; z++)
                    {
                        // Set the voxel value based on the height and the current z position
                        worldArray[x, y, z] = new SiteBlock((ushort)(z < height ? 1 : 100), (ushort)(z < height ? 1 : 100));
                    }
                }
            }

            return worldArray;
        }

        public static Point MouseScreenToMap(Point mousePos, int level)
        {
            Vector2 cursor = new Vector2(mousePos.X, mousePos.Y);


            cursor /= MainGame.cam.Zoom;
            cursor += MainGame.cam.Pos;

            cursor += new Vector2(0, TileSet.TILE_SIZE.Y * level + 4);

            var x = cursor.X + (2 * cursor.Y) - (TileSet.TILE_SIZE.X / 2);
            int mapX = (x < 0) ? -1 : (int)(x / TileSet.TILE_SIZE.X);

            var y = -cursor.X + (2 * cursor.Y) + (TileSet.TILE_SIZE.X / 2);
            int mapY = (y < 0) ? -1 : (int)(y / TileSet.TILE_SIZE.X);

            Vector2 res = new Vector2(mapX, mapY);
            return new Point(mapX, mapY);
        }
    }
}
