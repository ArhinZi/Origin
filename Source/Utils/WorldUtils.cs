using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using Origin.Source.ECS;
using Origin.Source.Resources;

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
            worldPos += new Vector3(0, level * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset), 0);
            // Also works
            //int tileX = (int)Math.Round((worldPos.X / GlobalResources.Settings.TileSize.X + worldPos.Y / GlobalResources.Settings.TileSize.Y - 1));
            //int tileY = (int)Math.Round((worldPos.Y / GlobalResources.Settings.TileSize.Y - worldPos.X / GlobalResources.Settings.TileSize.X));

            var cellPosX = (worldPos.X / GlobalResources.Settings.TileSize.X) - 0.5;
            var cellPosY = (worldPos.Y / GlobalResources.Settings.TileSize.Y) - 0.5;

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
                    site.Map[pos.X, pos.Y, pos.Z - 1] != Entity.Null &&
                    !site.Map[pos.X, pos.Y, pos.Z - 1].Has<BaseConstruction>())
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
            while (pos.Z - 1 >= 0 && !site.Map[pos.X, pos.Y, pos.Z - 1].Has<BaseConstruction>())
            {
                pos = pos - new Point3(1, 1, 1);
                if (pos.X < 0 || pos.X >= site.Size.X || pos.Y < 0 || pos.Y >= site.Size.Y)
                    return pos;
            }
            return pos;
        }

        public static Point GetSpritePositionByCellPosition(Point3 cellPos)
        {
            var VertexX = (cellPos.X - cellPos.Y) * GlobalResources.Settings.TileSize.X / 2;
            var VertexY = ((cellPos.X + cellPos.Y) * GlobalResources.Settings.TileSize.Y / 2)
                    - cellPos.Z * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset);
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

        public static Point3 RotatePosition(Point3 pos, Point3 size, WorldRotation rotation)
        {
            Point3 res = new Point3(0, 0, pos.Z);
            if (rotation == WorldRotation.TR)
                res = pos;
            else if (rotation == WorldRotation.TL)
            {
                res.X = size.Y - pos.X - 1;
                res.Y = pos.X;
            }
            else if (rotation == WorldRotation.BL)
            {
                res.X = size.X - pos.X - 1;
                res.Y = size.Y - pos.Y - 1;
            }
            else if (rotation == WorldRotation.BR)
            {
                res.X = pos.Y;
                res.Y = size.X - pos.X - 1;
            }
            return res;
        }

        #region Neighbour Patterns

        // Inclusive - includes (0,0,0)
        // Exclusive - excludes (0,0,0)

        public static Point3[] PLUS_NEIGHBOUR_PATTERN_1L(bool inclusive = true)
        {
            if (inclusive)
                return new Point3[]
                {
                    new Point3(0,0,0),
                    new Point3(1,0,0),
                    new Point3(0,1,0),
                    new Point3(-1,0,0),
                    new Point3(0,-1,0)
                };
            else
                return new Point3[]
                {
                    new Point3(1,0,0),
                    new Point3(0,1,0),
                    new Point3(-1,0,0),
                    new Point3(0,-1,0)
                };
        }

        public static Point3[] FULL_NEIGHBOUR_PATTERN_1L(bool inclusive = true)
        {
            if (inclusive)
                return new Point3[]
                {
                    new Point3(0,0,0),
                    new Point3(1,0,0),
                    new Point3(0,1,0),
                    new Point3(-1,0,0),
                    new Point3(0,-1,0),
                    new Point3(-1,-1,0),
                    new Point3(-1,1,0),
                    new Point3(1,-1,0),
                    new Point3(1,1,0),
                };
            else
                return new Point3[]
                {
                    new Point3(1,0,0),
                    new Point3(0,1,0),
                    new Point3(-1,0,0),
                    new Point3(0,-1,0),
                    new Point3(-1,-1,0),
                    new Point3(-1,1,0),
                    new Point3(1,-1,0),
                    new Point3(1,1,0),
                };
        }

        public static Point3[] STAR_NEIGHBOUR_PATTERN_3L(bool inclusive = true)
        {
            if (inclusive)
                return new Point3[]
                {
                    new Point3(0,0,0),
                    new Point3(1,0,0),
                    new Point3(0,1,0),
                    new Point3(0,0,1),
                    new Point3(-1,0,0),
                    new Point3(0,-1,0),
                    new Point3(0,0,-1),
                };
            else
                return new Point3[]
                {
                    new Point3(1,0,0),
                    new Point3(0,1,0),
                    new Point3(0,0,1),
                    new Point3(-1,0,0),
                    new Point3(0,-1,0),
                    new Point3(0,0,-1),
                };
        }

        public static Point3[] FULL_NEIGHBOUR_PATTERN_3L(bool inclusive = true)
        {
            if (inclusive)
                return new Point3[]
                {
                    new Point3(0,0,0),
                    new Point3(1,0,     0),
                    new Point3(0,1,     0),
                    new Point3(-1,0,    0),
                    new Point3(0,-1,    0),
                    new Point3(-1,-1,   0),
                    new Point3(-1,1,    0),
                    new Point3(1,-1,    0),
                    new Point3(1,1,     0),
                    new Point3(1,0,     -1),
                    new Point3(0,1,     -1),
                    new Point3(-1,0,    -1),
                    new Point3(0,-1,    -1),
                    new Point3(-1,-1,   -1),
                    new Point3(-1,1,    -1),
                    new Point3(1,-1,    -1),
                    new Point3(1,1,     -1),
                    new Point3(1,0,     1),
                    new Point3(0,1,     1),
                    new Point3(-1,0,    1),
                    new Point3(0,-1,    1),
                    new Point3(-1,-1,   1),
                    new Point3(-1,1,    1),
                    new Point3(1,-1,    1),
                    new Point3(1,1,     1),
                };
            else
                return new Point3[]
                {
                    new Point3(1,0,     0),
                    new Point3(0,1,     0),
                    new Point3(-1,0,    0),
                    new Point3(0,-1,    0),
                    new Point3(-1,-1,   0),
                    new Point3(-1,1,    0),
                    new Point3(1,-1,    0),
                    new Point3(1,1,     0),
                    new Point3(1,0,     -1),
                    new Point3(0,1,     -1),
                    new Point3(-1,0,    -1),
                    new Point3(0,-1,    -1),
                    new Point3(-1,-1,   -1),
                    new Point3(-1,1,    -1),
                    new Point3(1,-1,    -1),
                    new Point3(1,1,     -1),
                    new Point3(1,0,     1),
                    new Point3(0,1,     1),
                    new Point3(-1,0,    1),
                    new Point3(0,-1,    1),
                    new Point3(-1,-1,   1),
                    new Point3(-1,1,    1),
                    new Point3(1,-1,    1),
                    new Point3(1,1,     1),
                };
        }

        public static Point3[] TOP_BOTTOM_NEIGHBOUR_PATTERN(bool inclusive = true)
        {
            if (inclusive)
                return new Point3[]
                {
                    new Point3(0,0,0),
                    new Point3(0,0,     1),
                    new Point3(0,0,     -1)
                };
            else
                return new Point3[]
                    {
                    new Point3(0,0,     1),
                    new Point3(0,0,     -1)
                    };
        }

        #endregion Neighbour Patterns
    }
}