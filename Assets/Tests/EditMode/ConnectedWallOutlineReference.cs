using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    /// <summary>셀 조각 클리핑과 독립적으로 외곽선·감김 수·유리수 교차로 벽 내부를 검사한다.</summary>
    internal sealed class ConnectedWallOutlineReference
    {
        private readonly List<(Point A, Point B)> _edges = new List<(Point, Point)>();

        internal ConnectedWallOutlineReference(CellCoord[] cells, int cut)
        {
            var occupied = new HashSet<CellCoord>(cells);
            var next = new Dictionary<Point, Point>();
            foreach (var cell in cells)
            {
                long x = (long)cell.X * 1024, y = (long)cell.Y * 1024;
                var bl = new Point(x - 512, y - 512);
                var br = new Point(x + 512, y - 512);
                var tr = new Point(x + 512, y + 512);
                var tl = new Point(x - 512, y + 512);
                if (!occupied.Contains(new CellCoord(cell.X, cell.Y - 1))) next.Add(bl, br);
                if (!occupied.Contains(new CellCoord(cell.X + 1, cell.Y))) next.Add(br, tr);
                if (!occupied.Contains(new CellCoord(cell.X, cell.Y + 1))) next.Add(tr, tl);
                if (!occupied.Contains(new CellCoord(cell.X - 1, cell.Y))) next.Add(tl, bl);
            }
            while (next.Count != 0)
            {
                var loop = new List<Point>();
                Point start = next.Keys.First(), point = start;
                do
                {
                    loop.Add(point);
                    var target = next[point];
                    next.Remove(point);
                    point = target;
                } while (!point.Equals(start));
                var polygon = new List<Point>();
                for (int i = 0; i < loop.Count; i++)
                {
                    Point a = loop[(i + loop.Count - 1) % loop.Count], b = loop[i], c = loop[(i + 1) % loop.Count];
                    long cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
                    if (cross > 0 && cut != 0)
                    {
                        polygon.Add(new Point(b.X - Math.Sign(b.X - a.X) * cut, b.Y - Math.Sign(b.Y - a.Y) * cut));
                        polygon.Add(new Point(b.X + Math.Sign(c.X - b.X) * cut, b.Y + Math.Sign(c.Y - b.Y) * cut));
                    }
                    else polygon.Add(b);
                }
                for (int i = 0; i < polygon.Count; i++) _edges.Add((polygon[i], polygon[(i + 1) % polygon.Count]));
            }
        }

        internal bool Intersects(CellCoord a, CellCoord b, out Rational entry)
        {
            long ax = (long)a.X * 1024, ay = (long)a.Y * 1024;
            long dx = ((long)b.X - a.X) * 1024, dy = ((long)b.Y - a.Y) * 1024;
            entry = Rational.Zero;
            if (dx == 0 && dy == 0) return Inside(ax, ay);
            var cuts = new List<Rational> { Rational.Zero, Rational.One };
            foreach (var edge in _edges)
            {
                long ex = edge.B.X - edge.A.X, ey = edge.B.Y - edge.A.Y;
                BigInteger determinant = (BigInteger)dx * ey - (BigInteger)dy * ex;
                if (determinant.IsZero)
                {
                    if ((BigInteger)(edge.A.X - ax) * dy != (BigInteger)(edge.A.Y - ay) * dx) continue;
                    AddCut(cuts, new Rational(dx != 0 ? edge.A.X - ax : edge.A.Y - ay, dx != 0 ? dx : dy));
                    AddCut(cuts, new Rational(dx != 0 ? edge.B.X - ax : edge.B.Y - ay, dx != 0 ? dx : dy));
                    continue;
                }
                var t = new Rational((BigInteger)(edge.A.X - ax) * ey - (BigInteger)(edge.A.Y - ay) * ex, determinant);
                var u = new Rational((BigInteger)(edge.A.X - ax) * dy - (BigInteger)(edge.A.Y - ay) * dx, determinant);
                if (u.CompareTo(Rational.Zero) >= 0 && u.CompareTo(Rational.One) <= 0) AddCut(cuts, t);
            }
            var sorted = cuts.Distinct().OrderBy(value => value).ToArray();
            for (int i = 1; i < sorted.Length; i++)
            {
                var midpoint = (sorted[i - 1] + sorted[i]) / 2;
                if (!Inside(ax + dx * midpoint, ay + dy * midpoint)) continue;
                entry = sorted[i - 1];
                return true;
            }
            return false;
        }

        private static void AddCut(List<Rational> cuts, Rational t)
        {
            if (t.CompareTo(Rational.Zero) >= 0 && t.CompareTo(Rational.One) <= 0) cuts.Add(t);
        }

        private bool Inside(Rational x, Rational y)
        {
            int winding = 0;
            foreach (var edge in _edges)
            {
                var cross = (edge.B.X - edge.A.X) * (y - edge.A.Y) - (edge.B.Y - edge.A.Y) * (x - edge.A.X);
                if (cross.Numerator.IsZero && x.CompareTo(Math.Min(edge.A.X, edge.B.X)) >= 0 &&
                    x.CompareTo(Math.Max(edge.A.X, edge.B.X)) <= 0 && y.CompareTo(Math.Min(edge.A.Y, edge.B.Y)) >= 0 &&
                    y.CompareTo(Math.Max(edge.A.Y, edge.B.Y)) <= 0) return false;
                if (y.CompareTo(edge.A.Y) >= 0 && y.CompareTo(edge.B.Y) < 0 && cross.Numerator.Sign > 0) winding++;
                if (y.CompareTo(edge.B.Y) >= 0 && y.CompareTo(edge.A.Y) < 0 && cross.Numerator.Sign < 0) winding--;
            }
            return winding != 0;
        }

        private readonly struct Point : IEquatable<Point>
        {
            internal long X { get; }
            internal long Y { get; }
            internal Point(long x, long y)
            {
                X = x;
                Y = y;
            }
            public bool Equals(Point other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is Point other && Equals(other);
            public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
        }

        internal readonly struct Rational : IComparable<Rational>, IEquatable<Rational>
        {
            internal static Rational Zero => new Rational(0, 1);
            internal static Rational One => new Rational(1, 1);
            internal BigInteger Numerator { get; }
            internal BigInteger Denominator { get; }
            internal Rational(BigInteger numerator, BigInteger denominator)
            {
                if (denominator.Sign < 0)
                {
                    numerator = -numerator;
                    denominator = -denominator;
                }
                BigInteger divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
                Numerator = numerator / divisor;
                Denominator = denominator / divisor;
            }
            public int CompareTo(Rational other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
            public bool Equals(Rational other) => CompareTo(other) == 0;
            public override bool Equals(object obj) => obj is Rational other && Equals(other);
            public override int GetHashCode() => Numerator.GetHashCode() ^ Denominator.GetHashCode();
            public static implicit operator Rational(long value) => new Rational(value, 1);
            public static Rational operator +(Rational a, Rational b) => new Rational(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
            public static Rational operator -(Rational a, Rational b) => new Rational(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);
            public static Rational operator *(Rational a, Rational b) => new Rational(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
            public static Rational operator /(Rational a, Rational b) => new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
        }
    }
}
