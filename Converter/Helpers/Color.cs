using System;
using System.Diagnostics;

namespace Qoi
{
    [DebuggerDisplay("r = {r}, g = {g}, b = {b}, a = {a}")]
    internal struct Color : IEquatable<Color>
    {
        public int r;
        public int g;
        public int b;
        public int a;

        public Color(int r, int g, int b, int a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color operator -(Color a, Color b)
        {
            return new Color(
                a.r - b.r,
                a.g - b.g,
                a.b - b.b,
                a.a - b.a
            );
        }

        public override bool Equals(object obj)
        {
            return obj is Color color && Equals(color);
        }

        public bool Equals(Color other)
        {
            return r == other.r &&
                   g == other.g &&
                   b == other.b &&
                   a == other.a;
        }

        public override int GetHashCode()
        {
            return (r * 3 + g * 5 + b * 7 + a * 11) % 64;
        }

        public static bool operator ==(Color left, Color right) => left.Equals(right);
        public static bool operator !=(Color left, Color right) => !(left == right);
    }
}
