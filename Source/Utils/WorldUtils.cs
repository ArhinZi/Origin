using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Origin.Source.Model.Map;
using Origin.Source.Model.NewWorld;
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

        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level, Site site)
        {
            return MouseScreenToMap(cam, mousePos, level, site, false, false);
        }

        public static Point3 MouseScreenToMap(Camera2D cam, Point mousePos, int level, Site site,
            bool onFloor, bool clip)
        {
            Vector3 worldPos = Global.GraphicsDevice.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1), cam.Projection, cam.Transformation, cam.WorldMatrix);
            worldPos += new Vector3(0, level * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset) +
                (onFloor ? GlobalResources.Settings.FloorYoffset : 0), 0);

            var cellPosX = worldPos.X / GlobalResources.Settings.TileSize.X - 0.5;
            var cellPosY = worldPos.Y / GlobalResources.Settings.TileSize.Y - 0.5;

            Point3 rotatedCellPos = new()
            {
                X = (int)Math.Round(cellPosX + cellPosY),
                Y = (int)Math.Round(cellPosY - cellPosX),
                Z = level
            };

            Point3 cellPos = InverseRotatePosition(rotatedCellPos, site.Size, site.Rotation);
            if (clip && (cellPos.LessOr(Point3.Zero) || cellPos.GraterEqualOr(site.Size)))
                return Point3.Null;

            return cellPos;
        }

        public static Point3 MouseScreenToMapSurface(Camera2D cam, Point mousePos, int level, Site site)
        {
            return MouseScreenToMapSurface(cam, mousePos, level, site, false);
        }

        public static Point3 MouseScreenToMapSurface(Camera2D cam, Point mousePos, int level, Site site,
            bool onFloor)
        {
            Span<int> probeOffsets = stackalloc int[]
            {
                0,
                -GlobalResources.Settings.FloorYoffset,
                -(GlobalResources.Settings.TileSize.Y / 2),
                -GlobalResources.Settings.TileSize.Y
            };

            int tlevel = level;
            for (int i = 0; i < Global.ONE_MOMENT_DRAW_LEVELS; i++)
            {
                for (int p = 0; p < probeOffsets.Length; p++)
                {
                    Point probeMousePos = new(mousePos.X, mousePos.Y + probeOffsets[p]);
                    Point3 pos = MouseScreenToMap(cam, probeMousePos, tlevel, site, onFloor, false);
                    if (pos.LessOr(Point3.Zero) || pos.GraterEqualOr(site.Size))
                        continue;

                    Tile tile = site.Map[pos];
                    if (!tile.Exists || !tile.HasConstruction)
                        continue;

                    return pos;
                }

                tlevel--;
            }

            return Point3.Null;
        }

        public static Point3 ProjectToSurface(Point3 position, Site site)
        {
            Point3 pos = position;
            if (position.LessOr(Point3.Zero) || position.GraterEqualOr(site.Size))
            {
                return pos;
            }
            while (pos.Z - 1 >= 0)
            {
                Tile below = site.Map[pos.X, pos.Y, pos.Z - 1];
                if (!below.Exists || below.HasConstruction)
                    break;

                pos = pos - new Point3(1, 1, 1);
                if (pos.X < 0 || pos.X >= site.Size.X || pos.Y < 0 || pos.Y >= site.Size.Y)
                    return pos;
            }
            return pos;
        }

        public static Point GetSpritePositionByCellPosition(Point3 cellPos, Site site)
        {
            Point3 rotated = RotatePosition(cellPos, site.Size, site.Rotation);
            var vertexX = (rotated.X - rotated.Y) * GlobalResources.Settings.TileSize.X / 2;
            var vertexY = ((rotated.X + rotated.Y) * GlobalResources.Settings.TileSize.Y / 2)
                    - rotated.Z * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset);
            return new Point(vertexX, vertexY);
        }

        public static float GetSpriteZOffsetByCellPos(Point3 cellPos, Site site)
        {
            Point3 rotated = RotatePosition(cellPos, site.Size, site.Rotation);
            var vertexZ = (rotated.X + rotated.Y) * Global.Z_DIAGONAL_OFFSET - 100;
            return (float)vertexZ;
        }

        public static Point3 GetChunkByCell(Point3 cellPos, Point3 chunkSize)
        {
            Point3 chunkPos = new(
                cellPos.X / chunkSize.X,
                cellPos.Y / chunkSize.Y,
                cellPos.Z);
            return chunkPos;
        }

        public static Point3 RotatePosition(Point3 pos, Point3 size, WorldRotation rotation)
        {
            return rotation switch
            {
                WorldRotation.TR => pos,
                WorldRotation.TL => new Point3(pos.Y, size.X - pos.X - 1, pos.Z),
                WorldRotation.BL => new Point3(size.X - pos.X - 1, size.Y - pos.Y - 1, pos.Z),
                WorldRotation.BR => new Point3(size.Y - pos.Y - 1, pos.X, pos.Z),
                _ => pos
            };
        }

        public static Point3 InverseRotatePosition(Point3 pos, Point3 size, WorldRotation rotation)
        {
            return rotation switch
            {
                WorldRotation.TR => pos,
                WorldRotation.TL => new Point3(size.X - pos.Y - 1, pos.X, pos.Z),
                WorldRotation.BL => new Point3(size.X - pos.X - 1, size.Y - pos.Y - 1, pos.Z),
                WorldRotation.BR => new Point3(pos.Y, size.Y - pos.X - 1, pos.Z),
                _ => pos
            };
        }

        public static WorldRotation InverseRotation(WorldRotation rotation)
        {
            return rotation switch
            {
                WorldRotation.TR => WorldRotation.TR,
                WorldRotation.TL => WorldRotation.BR,
                WorldRotation.BL => WorldRotation.BL,
                WorldRotation.BR => WorldRotation.TL,
                _ => WorldRotation.TR
            };
        }

        public static Global.Direction RotateDirection(Global.Direction dir, WorldRotation rotation)
        {
            if (dir == Global.Direction.NONE)
                return dir;

            Point3 delta = Point3.PointByDir(dir);
            Point3 rotated = rotation switch
            {
                WorldRotation.TR => delta,
                WorldRotation.TL => new Point3(delta.Y, -delta.X, delta.Z),
                WorldRotation.BL => new Point3(-delta.X, -delta.Y, delta.Z),
                WorldRotation.BR => new Point3(-delta.Y, delta.X, delta.Z),
                _ => delta
            };

            return Point3.DirByPoint(rotated);
        }

        #region Neighbour Patterns

        // Inclusive - includes (0,0,0)
        // Exclusive - excludes (0,0,0)

        public static Point3[] PLUS_NEIGHBOUR_PATTERN_1L(bool inclusive = true)
        {
            if (inclusive)
                return
                [
                    new(0,0,0),
                    new(1,0,0),
                    new(0,1,0),
                    new(-1,0,0),
                    new(0,-1,0)
                ];
            else
                return
                [
                    new(1,0,0),
                    new(0,1,0),
                    new(-1,0,0),
                    new(0,-1,0)
                ];
        }

        public static Point3[] PLUS_NEIGHBOUR_PATTERN_3L(bool inclusive = true)
        {
            if (inclusive)
                return
                [
                    new(0,0,1),
                    new(1,0,1),
                    new(0,1,1),
                    new(-1,0,1),
                    new(0,-1,1),

                    new(0,0,0),
                    new(1,0,0),
                    new(0,1,0),
                    new(-1,0,0),
                    new(0,-1,0),

                    new(0,0,-1),
                    new(1,0,-1),
                    new(0,1,-1),
                    new(-1,0,-1),
                    new(0,-1,-1)
                ];
            else
                return
                [
                    new(0,0,1),
                    new(1,0,1),
                    new(0,1,1),
                    new(-1,0,1),
                    new(0,-1,1),

                    new(1,0,0),
                    new(0,1,0),
                    new(-1,0,0),
                    new(0,-1,0),

                    new(0,0,-1),
                    new(1,0,-1),
                    new(0,1,-1),
                    new(-1,0,-1),
                    new(0,-1,-1)
                ];
        }

        public static Point3[] FULL_NEIGHBOUR_PATTERN_1L(bool inclusive = true)
        {
            if (inclusive)
                return
                [
                    new(0,0,0),
                    new(1,0,0),
                    new(0,1,0),
                    new(-1,0,0),
                    new(0,-1,0),
                    new(-1,-1,0),
                    new(-1,1,0),
                    new(1,-1,0),
                    new(1,1,0),
                ];
            else
                return
                [
                    new(1,0,0),
                    new(0,1,0),
                    new(-1,0,0),
                    new(0,-1,0),
                    new(-1,-1,0),
                    new(-1,1,0),
                    new(1,-1,0),
                    new(1,1,0),
                ];
        }

        public static Point3[] FULL_NEIGHBOUR_PATTERN_1L_4x4(bool inclusive = true)
        {
            if (inclusive)
                return
                [
            new(0, 0, 0),
            new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), new(-1, 1, 0),
            new(-1, 0, 0), new(-1, -1, 0), new(0, -1, 0), new(1, -1, 0),
            new(2, 0, 0), new(2, 1, 0), new(2, 2, 0), new(1, 2, 0), new(0, 2, 0), new(-1, 2, 0),
            new(-2, 0, 0), new(-2, -1, 0), new(-2, -2, 0), new(-1, -2, 0), new(0, -2, 0), new(1, -2, 0)
                ];
            else
                return
                [
            new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), new(-1, 1, 0),
            new(-1, 0, 0), new(-1, -1, 0), new(0, -1, 0), new(1, -1, 0),
            new(2, 0, 0), new(2, 1, 0), new(2, 2, 0), new(1, 2, 0), new(0, 2, 0), new(-1, 2, 0),
            new(-2, 0, 0), new(-2, -1, 0), new(-2, -2, 0), new(-1, -2, 0), new(0, -2, 0), new(1, -2, 0)
                ];
        }

        public static Point3[] FULL_NEIGHBOUR_PATTERN_1L_5x5(bool inclusive = true)
        {
            if (inclusive)
                return
                [
            new(0, 0, 0),
            new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), new(-1, 1, 0),
            new(-1, 0, 0), new(-1, -1, 0), new(0, -1, 0), new(1, -1, 0),
            new(2, 0, 0), new(2, 1, 0), new(2, 2, 0), new(1, 2, 0), new(0, 2, 0), new(-1, 2, 0),
            new(-2, 0, 0), new(-2, -1, 0), new(-2, -2, 0), new(-1, -2, 0), new(0, -2, 0), new(1, -2, 0),
            new(3, 0, 0), new(3, 1, 0), new(3, 2, 0), new(3, 3, 0), new(2, 3, 0), new(1, 3, 0), new(0, 3, 0), new(-1, 3, 0), new(-2, 3, 0), new(-3, 3, 0),
            new(-3, 0, 0), new(-3, -1, 0), new(-3, -2, 0), new(-3, -3, 0), new(-2, -3, 0), new(-1, -3, 0), new(0, -3, 0), new(1, -3, 0), new(2, -3, 0), new(3, -3, 0)
                ];
            else
                return
                [
            new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), new(-1, 1, 0),
            new(-1, 0, 0), new(-1, -1, 0), new(0, -1, 0), new(1, -1, 0),
            new(2, 0, 0), new(2, 1, 0), new(2, 2, 0), new(1, 2, 0), new(0, 2, 0), new(-1, 2, 0),
            new(-2, 0, 0), new(-2, -1, 0), new(-2, -2, 0), new(-1, -2, 0), new(0, -2, 0), new(1, -2, 0),
            new(3, 0, 0), new(3, 1, 0), new(3, 2, 0), new(3, 3, 0), new(2, 3, 0), new(1, 3, 0), new(0, 3, 0), new(-1, 3, 0), new(-2, 3, 0), new(-3, 3, 0),
            new(-3, 0, 0), new(-3, -1, 0), new(-3, -2, 0), new(-3, -3, 0), new(-2, -3, 0), new(-1, -3, 0), new(0, -3, 0), new(1, -3, 0), new(2, -3, 0), new(3, -3, 0)
                ];
        }

        public static Point3[] STAR_NEIGHBOUR_PATTERN_3L(bool inclusive = true)
        {
            if (inclusive)
                return
                [
                    new(0,0,0),
                    new(1,0,0),
                    new(0,1,0),
                    new(0,0,1),
                    new(-1,0,0),
                    new(0,-1,0),
                    new(0,0,-1),
                ];
            else
                return
                [
                    new(1,0,0),
                    new(0,1,0),
                    new(0,0,1),
                    new(-1,0,0),
                    new(0,-1,0),
                    new(0,0,-1),
                ];
        }

        public static Point3[] FULL_NEIGHBOUR_PATTERN_3L(bool inclusive = true)
        {
            if (inclusive)
                return
                [
                    new(0,0,0),
                    new(1,0,     0),
                    new(0,1,     0),
                    new(-1,0,    0),
                    new(0,-1,    0),
                    new(-1,-1,   0),
                    new(-1,1,    0),
                    new(1,-1,    0),
                    new(1,1,     0),
                    new(1,0,     -1),
                    new(0,1,     -1),
                    new(-1,0,    -1),
                    new(0,-1,    -1),
                    new(-1,-1,   -1),
                    new(-1,1,    -1),
                    new(1,-1,    -1),
                    new(1,1,     -1),
                    new(1,0,     1),
                    new(0,1,     1),
                    new(-1,0,    1),
                    new(0,-1,    1),
                    new(-1,-1,   1),
                    new(-1,1,    1),
                    new(1,-1,    1),
                    new(1,1,     1),
                ];
            else
                return
                [
                    new(1,0,     0),
                    new(0,1,     0),
                    new(-1,0,    0),
                    new(0,-1,    0),
                    new(-1,-1,   0),
                    new(-1,1,    0),
                    new(1,-1,    0),
                    new(1,1,     0),
                    new(1,0,     -1),
                    new(0,1,     -1),
                    new(-1,0,    -1),
                    new(0,-1,    -1),
                    new(-1,-1,   -1),
                    new(-1,1,    -1),
                    new(1,-1,    -1),
                    new(1,1,     -1),
                    new(1,0,     1),
                    new(0,1,     1),
                    new(-1,0,    1),
                    new(0,-1,    1),
                    new(-1,-1,   1),
                    new(-1,1,    1),
                    new(1,-1,    1),
                    new(1,1,     1),
                ];
        }

        public static Point3[] TOP_BOTTOM_NEIGHBOUR_PATTERN(bool inclusive = true)
        {
            if (inclusive)
                return
                [
                    new(0,0,0),
                    new(0,0,     1),
                    new(0,0,     -1)
                ];
            else
                return
                    [
                    new(0,0,     1),
                    new(0,0,     -1)
                    ];
        }

        #endregion Neighbour Patterns

        public static bool TryGetFrontLeftSideIndex(Point3 pos, Point3 size, WorldRotation rotation, out int sideIndex)
        {
            sideIndex = -1;
            switch (rotation)
            {
                case WorldRotation.TR:
                    if (pos.Y == size.Y - 1)
                    {
                        sideIndex = pos.X;
                        return true;
                    }
                    break;
                case WorldRotation.TL:
                    if (pos.X == 0)
                    {
                        sideIndex = pos.Y;
                        return true;
                    }
                    break;
                case WorldRotation.BL:
                    if (pos.Y == 0)
                    {
                        sideIndex = size.X - 1 - pos.X;
                        return true;
                    }
                    break;
                case WorldRotation.BR:
                    if (pos.X == size.X - 1)
                    {
                        sideIndex = size.Y - 1 - pos.Y;
                        return true;
                    }
                    break;
            }

            return false;
        }

        public static bool TryGetFrontRightSideIndex(Point3 pos, Point3 size, WorldRotation rotation, out int sideIndex)
        {
            sideIndex = -1;
            switch (rotation)
            {
                case WorldRotation.TR:
                    if (pos.X == size.X - 1)
                    {
                        sideIndex = size.Y - 1 - pos.Y;
                        return true;
                    }
                    break;
                case WorldRotation.TL:
                    if (pos.Y == size.Y - 1)
                    {
                        sideIndex = pos.X;
                        return true;
                    }
                    break;
                case WorldRotation.BL:
                    if (pos.X == 0)
                    {
                        sideIndex = pos.Y;
                        return true;
                    }
                    break;
                case WorldRotation.BR:
                    if (pos.Y == 0)
                    {
                        sideIndex = size.X - 1 - pos.X;
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}