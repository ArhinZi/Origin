using System;

namespace Roy_T.AStar.Primitives
{
    public struct Position : IEquatable<Position>
    {
        public static Position Zero => new Position(0, 0, 0);

        public Position(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static Position FromOffset(Distance xDistanceFromOrigin, Distance yDistanceFromOrigin, Distance zDistanceFromOrigin)
            => new Position((int)xDistanceFromOrigin.Meters, (int)yDistanceFromOrigin.Meters, (int)zDistanceFromOrigin.Meters);

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public static bool operator ==(Position a, Position b)
            => a.Equals(b);

        public static bool operator !=(Position a, Position b)
            => !a.Equals(b);

        public override string ToString() => $"({this.X}, {this.Y}, {this.Z})";

        public override bool Equals(object obj) => obj is Position position && this.Equals(position);

        public bool Equals(Position other) => this.X == other.X && this.Y == other.Y && this.Z == other.Z;

        public override int GetHashCode() => -1609761766 + this.X.GetHashCode() + this.Y.GetHashCode() + this.Z.GetHashCode();
    }
}