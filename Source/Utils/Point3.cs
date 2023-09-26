using Microsoft.Xna.Framework;

namespace Origin.Source.Utils
{
    public struct Point3
    {
        public int X;
        public int Y;
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

        public static Point3 Zero = new Point3(0, 0, 0);

        public Point ToPoint()
        {
            return new Point(X, Y);
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
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }
    }
}