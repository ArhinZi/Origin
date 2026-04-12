using MessagePack;
using Microsoft.Xna.Framework;
using Origin.Source.Resources;
using System;
using static Origin.Source.Resources.Global;

namespace Origin.Source.Utils
{
    /// <summary>
    /// Lightweight integer 3D point used across map, tile and render code.
    /// </summary>
    [MessagePackObject]
    public struct Point3 : IComparable, IEquatable<Point3>
    {
        [Key(0)]
        public int X;

        [Key(1)]
        public int Y;

        [Key(3)]
        public int Z;

        public Point3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3(Point xy, int z)
        {
            X = xy.X;
            Y = xy.Y;
            Z = z;
        }

        public Point3(Vector3 v3)
        {
            X = (int)v3.X;
            Y = (int)v3.Y;
            Z = (int)v3.Z;
        }

        public static Point3 Null = new(int.MinValue, int.MinValue, int.MinValue);

        public static Point3 Zero = new(0, 0, 0);
        public static Point3 Up = new(0, 0, 1);
        public static Point3 Down = new(0, 0, -1);
        public static Point3 North = new(0, -1, 0);
        public static Point3 South = new(0, 1, 0);
        public static Point3 West = new(-1, 0, 0);
        public static Point3 East = new(1, 0, 0);
        public static Point3 NorthWest = new(-1, -1, 0);
        public static Point3 NorthEast = new(1, -1, 0);
        public static Point3 SouthWest = new(-1, 1, 0);
        public static Point3 SouthEast = new(1, 1, 0);

        public static Point3 PointByDir(Direction dir)
        {
            switch (dir)
            {
                case Direction.NORTH:
                    return North;
                case Direction.SOUTH:
                    return South;
                case Direction.WEST:
                    return West;
                case Direction.EAST:
                    return East;
                case Direction.NORTHWEST:
                    return NorthWest;
                case Direction.NORTHEAST:
                    return NorthEast;
                case Direction.SOUTHWEST:
                    return SouthWest;
                case Direction.SOUTHEAST:
                    return SouthEast;
                default:
                    return Zero;
            }
        }

        public static Direction DirByPoint(Point3 point)
        {
            if (point == North)
                return Direction.NORTH;
            if (point == South)
                return Direction.SOUTH;
            if (point == West)
                return Direction.WEST;
            if (point == East)
                return Direction.EAST;
            if (point == NorthWest)
                return Direction.NORTHWEST;
            if (point == NorthEast)
                return Direction.NORTHEAST;
            if (point == SouthWest)
                return Direction.SOUTHWEST;
            if (point == SouthEast)
                return Direction.SOUTHEAST;

            return Direction.NONE;
        }

        public readonly Point XY()
        {
            return new Point(X, Y);
        }

        public readonly Point3 XYdiv(int packSize)
        {
            return new Point3(X / packSize, Y / packSize, Z);
        }

        public readonly Point3 XYmod(int packSize)
        {
            return new Point3(X % packSize, Y % packSize, Z);
        }

        /// <summary>
        /// Checks whether the point is inside the given bounds.
        /// </summary>
        public readonly bool InBounds(Point3 lower, Point3 higher, bool includeLower = true, bool includeHigher = false)
        {
            bool lowerOk = includeLower
                ? X >= lower.X && Y >= lower.Y && Z >= lower.Z
                : X > lower.X && Y > lower.Y && Z > lower.Z;

            bool higherOk = includeHigher
                ? X <= higher.X && Y <= higher.Y && Z <= higher.Z
                : X < higher.X && Y < higher.Y && Z < higher.Z;

            return lowerOk && higherOk;
        }

        public static bool operator ==(Point3 first, Point3 second)
        {
            return first.Equals(second);
        }

        public readonly bool Equals(Point3 point)
        {
            return X == point.X && Y == point.Y && Z == point.Z;
        }

        public readonly bool Equals(ref Point3 point)
        {
            return X == point.X && Y == point.Y && Z == point.Z;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is Point3 point && Equals(point);
        }

        public readonly bool GraterOr(Point3 other)
        {
            return X > other.X || Y > other.Y || Z > other.Z;
        }

        public readonly bool LessOr(Point3 other)
        {
            return X < other.X || Y < other.Y || Z < other.Z;
        }

        public readonly bool GraterEqualOr(Point3 other)
        {
            return X >= other.X || Y >= other.Y || Z >= other.Z;
        }

        public readonly bool LessEqualOr(Point3 other)
        {
            return X <= other.X || Y <= other.Y || Z <= other.Z;
        }

        public static bool operator !=(Point3 first, Point3 second)
        {
            return !first.Equals(second);
        }

        public static bool operator <(Point3 first, Point3 second)
        {
            return first.X < second.X &&
                first.Y < second.Y &&
                first.Z < second.Z;
        }

        public static bool operator <=(Point3 first, Point3 second)
        {
            return first.X <= second.X &&
                first.Y <= second.Y &&
                first.Z <= second.Z;
        }

        public static bool operator >(Point3 first, Point3 second)
        {
            return first.X > second.X &&
                first.Y > second.Y &&
                first.Z > second.Z;
        }

        public static bool operator >=(Point3 first, Point3 second)
        {
            return first.X >= second.X &&
                first.Y >= second.Y &&
                first.Z >= second.Z;
        }

        public static Point3 operator +(Point3 first, Point3 second)
        {
            return new Point3(first.X + second.X, first.Y + second.Y, first.Z + second.Z);
        }

        public static Point3 operator *(Point3 first, Point3 second)
        {
            return new Point3(first.X * second.X, first.Y * second.Y, first.Z * second.Z);
        }

        public static Point3 operator -(Point3 first, Point3 second)
        {
            return new Point3(first.X - second.X, first.Y - second.Y, first.Z - second.Z);
        }

        public static Point3 operator *(Point3 first, int second)
        {
            return new Point3(first.X * second, first.Y * second, first.Z * second);
        }

        public override readonly string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public readonly Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public readonly int CompareTo(object obj)
        {
            if (obj is not Point3 p2)
                throw new ArgumentException($"Object must be of type {nameof(Point3)}", nameof(obj));

            return (X + Y + Z).CompareTo(p2.X + p2.Y + p2.Z);
        }
    }
}