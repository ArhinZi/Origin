using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;

using SimplexNoise;

using System;

namespace Origin.Source.Utils
{
    internal class WorldUtils
    {
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

        public static SiteCell[,,] Generate3dWorldArray(
            Site site,
            float[,] heightMap,
            Point3 worldSize,
            int baseHeight,
            float scale,
            TerraGenParameters tgp = default)
        {
            if (tgp.Equals(default(TerraGenParameters))) tgp = TerraGenParameters.Default;
            SiteCell[,,] worldArray = new SiteCell[worldSize.X, worldSize.Y, worldSize.Z];

            // Loop through each voxel in the world array
            for (int x = 0; x < worldSize.X; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    // Calculate the height of the voxel based on the height map
                    int height = (int)(heightMap[x, y] * scale + baseHeight);
                    for (int z = 0; z < worldSize.Z; z++)
                    {
                        // Set the voxel value based on the height and the current z position
                        string wall, floor, embWall, embFloor;
                        wall = floor = embWall = embFloor = null;
                        if (z <= height - tgp.DirtDepth)
                        {
                            wall = floor = "Granite";
                        }
                        else if (z > height - tgp.DirtDepth && z <= height)
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

                        worldArray[x, y, z] = new SiteCell(site, new Point3(x, y, z),
                            wallMatID: wall, floorMatID: floor, embWallMatID: embWall, embFloorMatID: embFloor);
                    }
                }
            }

            return worldArray;
        }

        public static Point3 MouseScreenToMap(Point mousePos, int level)
        {
            Camera2D c = MainGame.Camera;

            Vector3 worldPos = MainGame.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), c.Projection, c.Transformation, c.WorldMatrix);
            worldPos += new Vector3(0, level * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET), 0);
            // Also works
            //int tileX = (int)Math.Round((worldPos.X / Sprite.TILE_SIZE.X + worldPos.Y / Sprite.TILE_SIZE.Y - 1));
            //int tileY = (int)Math.Round((worldPos.Y / Sprite.TILE_SIZE.Y - worldPos.X / Sprite.TILE_SIZE.X));

            var cellPosX = (worldPos.X / Sprite.TILE_SIZE.X) - 0.5;
            var cellPosY = (worldPos.Y / Sprite.TILE_SIZE.Y) - 0.5;

            Vector3 cellPos = new Vector3()
            {
                X = (float)Math.Round((cellPosX + cellPosY)),
                Y = (float)Math.Round((cellPosY - cellPosX))
            };
            return new Point3(cellPos);
        }

        public static Point GetSpritePositionByCellPosition(Point3 cellPos)
        {
            var VertexX = (cellPos.X - cellPos.Y) * Sprite.TILE_SIZE.X / 2;
            var VertexY = ((cellPos.X + cellPos.Y) * Sprite.TILE_SIZE.Y / 2)
                    - cellPos.Z * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET);
            return new Point(VertexX, VertexY);
        }

        public static float GetSpriteZOffsetByCellPos(Point3 cellPos)
        {
            var VertexZ = (cellPos.X + cellPos.Y) * SiteRenderer.Z_DIAGONAL_OFFSET;
            return (float)VertexZ;
        }
    }
}