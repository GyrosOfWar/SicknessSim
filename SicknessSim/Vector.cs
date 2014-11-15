using System;

namespace SicknessSim {
    internal struct Vector {
        public static readonly Vector Zero = new Vector(0, 0);

        public static readonly Vector One = new Vector(1, 1);
        public readonly double X, Y;

        public Vector(double X, double Y) {
            this.X = X;
            this.Y = Y;
        }

        public override bool Equals(object obj) {
            if (obj is Vector) {
                var p = (Vector) obj;
                return (p.X == X) && (p.Y == Y);
            }
            return false;
        }

        public override int GetHashCode() {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public override string ToString() {
            return "[" + X + ", " + Y + "]";
        }

        public double Dot(Vector that) {
            return X * that.X + Y * that.Y;
        }

        public static Vector operator +(Vector a, Vector b) {
            return new Vector(a.X + b.X, a.Y + b.Y);
        }

        public static Vector operator -(Vector a, Vector b) {
            return new Vector(a.X - b.X, a.Y - b.Y);
        }

        public static Vector operator *(Vector a, Vector b) {
            return new Vector(a.X * b.X, a.Y * b.Y);
        }

        public static Vector operator *(Vector a, double b) {
            return new Vector(a.X * b, a.Y * b);
        }

        public static Vector operator +(Vector a, double b) {
            return new Vector(a.X + b, a.Y + b);
        }

        public static bool operator ==(Vector a, Vector b) {
            return a.Equals(b);
        }

        public static bool operator !=(Vector a, Vector b) {
            return !(a.Equals(b));
        }

        public double DistanceTo(Vector other) {
            var xx = (X - other.X) * (X * other.X);
            var yy = (Y - other.Y) * (Y * other.Y);
            return Math.Sqrt(xx + yy);
        }
    }
}