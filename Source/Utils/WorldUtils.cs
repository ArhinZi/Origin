using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;

using Origin.Source.Generators;

using SimplexNoise;

using System;

namespace Origin.Source.Utils
{
    internal class WorldUtils
    {
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

        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level)
        {
            Vector3 worldPos = OriginGame.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), cam.Projection, cam.Transformation, cam.WorldMatrix);
            worldPos += new Vector3(0, level * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET), 0);
            // Also works
            //int tileX = (int)Math.Round((worldPos.X / Sprite.TILE_SIZE.X + worldPos.Y / Sprite.TILE_SIZE.Y - 1));
            //int tileY = (int)Math.Round((worldPos.Y / Sprite.TILE_SIZE.Y - worldPos.X / Sprite.TILE_SIZE.X));

            var cellPosX = (worldPos.X / Sprite.TILE_SIZE.X) - 0.5;
            var cellPosY = (worldPos.Y / Sprite.TILE_SIZE.Y) - 0.5;

            Vector3 cellPos = new Vector3()
            {
                X = (float)Math.Round((cellPosX + cellPosY)),
                Y = (float)Math.Round((cellPosY - cellPosX)),
                Z = level
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

        public static Point3 GetChunkByCell(Point3 cellPos, Point3 chunkSize)
        {
            Point3 chunkPos = new Point3(
                cellPos.X / chunkSize.X,
                cellPos.Y / chunkSize.Y,
                cellPos.Z);
            return chunkPos;
        }
    }
}