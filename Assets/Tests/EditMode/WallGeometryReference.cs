using System;
using System.Collections.Generic;
using System.Numerics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    /// <summary>검증 전용: 다각형 변과의 교차점을 나눈 뒤 각 구간 중점의 내부 여부를 검사한다.</summary>
    internal static class WallGeometryReference
    {
        public static bool IntersectsInterior(CellCoord start, CellCoord end, WallRun run)
        {
            BigInteger left = run.XMin, right = run.XMax, bottom = run.YMin, top = run.YMax;
            BigInteger cut = run.CornerCutTicks;
            var vertices = new[]
            {
                new Point(left + cut, bottom), new Point(right - cut, bottom),
                new Point(right, bottom + cut), new Point(right, top - cut),
                new Point(right - cut, top), new Point(left + cut, top),
                new Point(left, top - cut), new Point(left, bottom + cut)
            };
            var a = new Point((BigInteger)start.X * WallBlockingSpec.TicksPerTile,
                (BigInteger)start.Y * WallBlockingSpec.TicksPerTile);
            var b = new Point((BigInteger)end.X * WallBlockingSpec.TicksPerTile,
                (BigInteger)end.Y * WallBlockingSpec.TicksPerTile);
            var direction = b - a;
            var cuts = new List<Fraction> { new Fraction(0, 1), new Fraction(1, 1) };
            for (int i = 0; i < vertices.Length; i++)
            {
                var edge = vertices[(i + 1) % vertices.Length] - vertices[i];
                var determinant = Cross(direction, edge);
                if (determinant.IsZero) continue;
                var offset = vertices[i] - a;
                var t = new Fraction(Cross(offset, edge), determinant);
                var s = new Fraction(Cross(offset, direction), determinant);
                if (t.InUnitInterval && s.InUnitInterval) cuts.Add(t);
            }

            cuts.Sort();
            for (int i = 1; i < cuts.Count; i++)
            {
                if (cuts[i - 1].CompareTo(cuts[i]) == 0) continue;
                var mid = Fraction.Midpoint(cuts[i - 1], cuts[i]);
                bool inside = true;
                for (int j = 0; j < vertices.Length; j++)
                {
                    var edge = vertices[(j + 1) % vertices.Length] - vertices[j];
                    if (edge.X.IsZero && edge.Y.IsZero) continue;
                    if (Cross(edge, a - vertices[j]) * mid.Denominator +
                        Cross(edge, direction) * mid.Numerator <= 0)
                    {
                        inside = false;
                        break;
                    }
                }
                if (inside) return true;
            }
            return false;
        }

        private static BigInteger Cross(Point a, Point b) => a.X * b.Y - a.Y * b.X;

        private readonly struct Point
        {
            public readonly BigInteger X, Y;
            public Point(BigInteger x, BigInteger y) { X = x; Y = y; }
            public static Point operator -(Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
        }

        private readonly struct Fraction : IComparable<Fraction>
        {
            public readonly BigInteger Numerator, Denominator;
            public bool InUnitInterval => Numerator >= 0 && Numerator <= Denominator;
            public Fraction(BigInteger numerator, BigInteger denominator)
            {
                if (denominator.Sign < 0) { numerator = -numerator; denominator = -denominator; }
                Numerator = numerator;
                Denominator = denominator;
            }
            public int CompareTo(Fraction other) =>
                (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
            public static Fraction Midpoint(Fraction a, Fraction b) => new Fraction(
                a.Numerator * b.Denominator + b.Numerator * a.Denominator,
                2 * a.Denominator * b.Denominator);
        }
    }
}
