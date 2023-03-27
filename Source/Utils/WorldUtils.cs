using Microsoft.Xna.Framework;

using SimplexNoise;

namespace Origin.Source.Utils
{
    internal class WorldUtils
    {
        public static readonly string AIR_NULL_MAT_ID = "Air";
        public static readonly string HIDDEN_MAT_ID = "Hidden";

        public static float[,] GenerateHeightMap(int width, int height, float scale)
        {
            float[,] heightMap = new float[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heightMap[i, j] = Noise.CalcPixel2D(i, j, scale) / 128f;
                }
            }
            //Noise.Calc2D(width, height, scale);
            return heightMap;
        }

        public static float[,] GenerateFlatHeightMap(int width, int height)
        {
            float[,] heightMap = new float[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heightMap[i, j] = 1;
                }
            }
            return heightMap;
        }

        public static SiteCell[,,] Generate3dWorldArray(float[,] heightMap, int worldWidth, int worldHeight, int worldDepth, int baseHeight, float scale)
        {
            SiteCell[,,] worldArray = new SiteCell[worldWidth, worldHeight, worldDepth];

            // Loop through each voxel in the world array
            for (int x = 0; x < worldWidth; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    // Calculate the height of the voxel based on the height map
                    float height = heightMap[x, y] * scale + baseHeight;
                    for (int z = 0; z < worldDepth; z++)
                    {
                        // Set the voxel value based on the height and the current z position
                        worldArray[x, y, z] = new SiteCell(wmatid: (z < height ? "Granite" : AIR_NULL_MAT_ID), (z < height ? "Granite" : AIR_NULL_MAT_ID));
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

            cursor += new Vector2(0, Sprite.TILE_SIZE.Y * level + 4);

            var x = cursor.X + 2 * cursor.Y - Sprite.TILE_SIZE.X / 2;
            int mapX = x < 0 ? -1 : (int)(x / Sprite.TILE_SIZE.X);

            var y = -cursor.X + 2 * cursor.Y + Sprite.TILE_SIZE.X / 2;
            int mapY = y < 0 ? -1 : (int)(y / Sprite.TILE_SIZE.X);

            //Vector2 res = new Vector2(mapX, mapY);
            return new Point(mapX, mapY);
        }
    }
}