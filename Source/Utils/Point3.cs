using MessagePack;

using Microsoft.Xna.Framework;

using System;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Utils
{
    [MessagePackObject]
    public struct Point3 : IComparable
    {
        //[IgnoreMember]
        [Key(0)]
        public int X;

        //[IgnoreMember]
        [Key(1)]
        public int Y;

        //[IgnoreMember]
        [Key(3)]
        public int Z;

        //[Key("data")]
        //public string Data
        //{
        //    get { return $"{X},{Y},{Z}"; }
        //    set
        //    {
        //        var vals = value.Split(',');
        //        X = int.Parse(vals[0]);
        //        Y = int.Parse(vals[1]);
        //        Z = int.Parse(vals[2]);
        //    }
        //}

        public Point3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        //public Point3(string data)
        //{
        //    Data = data;
        //}

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

        public static Point3 Dir(Direction dir)
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

                default:
                    return Zero;
            }
        }

        public Point XY()
        {
            return new Point(X, Y);
        }

        public Point3 XYdiv(int PackSize)
        {
            var val = this;
            val.X /= PackSize;
            val.Y /= PackSize;
            return val;
        }

        public Point3 XYmod(int PackSize)
        {
            var val = this;
            val.X %= PackSize;
            val.Y %= PackSize;
            return val;
        }

        public bool InBounds(Point3 lower, Point3 higher, bool includeLower = true, bool includeHigher = false)
        {
            bool res = true;
            if (includeLower)
            {
                if (X < lower.X || Y < lower.Y || Z < lower.Z) res = res && false;
            }
            else
            {
                if (X <= lower.X || Y <= lower.Y || Z <= lower.Z) res = res && false;
            }
            if (includeHigher)
            {
                if (X > higher.X || Y > higher.Y || Z > higher.Z) res = res && false;
            }
            else
            {
                if (X >= higher.X || Y >= higher.Y || Z >= higher.Z) res = res && false;
            }
            return res;
        }

        public static bool operator ==(Point3 first, Point3 second)
        {
            return first.Equals(ref second);
        }

        public bool Equals(Point3 point)
        {
            return Equals(ref point);
        }

        public bool Equals(ref Point3 point)
        {
            if (point.X == X && point.Y == Y)
            {
                return point.Z == Z;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is Point3)
            {
                return Equals((Point3)obj);
            }

            return false;
        }

        public bool GraterOr(Point3 other)
        {
            if (X > other.X || Y > other.Y || Z > other.Z) return true;
            return false;
        }

        public bool LessOr(Point3 other)
        {
            if (X < other.X || Y < other.Y || Z < other.Z) return true;
            return false;
        }

        public bool GraterEqualOr(Point3 other)
        {
            if (X >= other.X || Y >= other.Y || Z >= other.Z) return true;
            return false;
        }

        public bool LessEqualOr(Point3 other)
        {
            if (X <= other.X || Y <= other.Y || Z <= other.Z) return true;
            return false;
        }

        public static bool operator !=(Point3 first, Point3 second)
        {
            return !(first == second);
        }

        public static bool operator <(Point3 first, Point3 second)
        {
            if (first.X < second.X &&
                first.Y < second.Y &&
                first.Z < second.Z)
                return true;
            return false;
        }

        public static bool operator <=(Point3 first, Point3 second)
        {
            if (first.X <= second.X &&
                first.Y <= second.Y &&
                first.Z <= second.Z)
                return true;
            return false;
        }

        public static bool operator >(Point3 first, Point3 second)
        {
            if (first.X > second.X &&
                first.Y > second.Y &&
                first.Z > second.Z)
                return true;
            return false;
        }

        public static bool operator >=(Point3 first, Point3 second)
        {
            if (first.X >= second.X &&
                first.Y >= second.Y &&
                first.Z >= second.Z)
                return true;
            return false;
        }

        public static Point3 operator +(Point3 first, Point3 second)
        {
            return new Point3(
                first.X + second.X,
                first.Y + second.Y,
                first.Z + second.Z);
        }

        public static Point3 operator -(Point3 first, Point3 second)
        {
            return new Point3(
                first.X - second.X,
                first.Y - second.Y,
                first.Z - second.Z);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + X;
            hash = hash * 31 + Y;
            hash = hash * 31 + Z;
            return hash;
        }

        public int CompareTo(object obj)
        {
            Point3 p2 = (Point3)obj;
            return (X + Y + Z).CompareTo(p2.X + p2.Y + p2.Z);
        }
    }
}