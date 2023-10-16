using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using Origin.Source.ECS;

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

            Point3 cellPos = new Point3()
            {
                X = (int)Math.Round((cellPosX + cellPosY)),
                Y = (int)Math.Round((cellPosY - cellPosX)),
                Z = level
            };
            return cellPos;
        }

        public static Point3 MouseScreenToMapSurface(Camera2D cam, Point mousePos, int level, Site site)
        {
            for (int i = 0; i < SiteRenderer.ONE_MOMENT_DRAW_LEVELS; i++)
            {
                Point3 pos = MouseScreenToMap(cam, mousePos, level);
                if (pos.LessOr(Point3.Zero))
                    return pos;
                if (pos.GraterEqualOr(site.Size) || pos.Z - 1 >= 0 &&
                    site.Blocks[pos.X, pos.Y, pos.Z - 1] != Entity.Null &&
                    !site.Blocks[pos.X, pos.Y, pos.Z - 1].Has<TileStructure>())
                {
                    level--;
                    continue;
                }
                else return pos;
            }
            return new Point3(-1, -1, -1);
        }

        public static Point3 ProjectToSurface(Point3 position, Site site)
        {
            Point3 pos = position;
            if (position.LessOr(Point3.Zero) || position.GraterEqualOr(site.Size))
            {
                return pos;
            }
            while (pos.Z - 1 >= 0 && !site.Blocks[pos.X, pos.Y, pos.Z - 1].Has<TileStructure>())
            {
                pos = pos - new Point3(1, 1, 1);
                if (pos.X < 0 || pos.X >= site.Size.X || pos.Y < 0 || pos.Y >= site.Size.Y)
                    return pos;
            }
            return pos;
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