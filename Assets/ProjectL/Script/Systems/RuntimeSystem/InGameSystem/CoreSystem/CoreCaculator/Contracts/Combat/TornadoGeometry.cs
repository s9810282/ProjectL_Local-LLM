using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public static class TornadoGeometry
    {
        internal static int InwardHorizontalSign(CellCoord center, int sideLength, int width, int sign)
        {
            int radius = sideLength / 2;
            if (width == sideLength) return sign;
            return center.X == radius ? 1 : center.X == width - 1 - radius ? -1 : sign;
        }

        // 지형·점유와 무관하게 정사각형 전체가 판 안에 있는지만 판정한다.
        public static bool Fits(CellCoord center, int sideLength, int width, int height)
        {
            if (sideLength < 1 || sideLength % 2 == 0) throw new ArgumentOutOfRangeException(nameof(sideLength));
            int radius = sideLength / 2;
            return width >= sideLength && height >= sideLength && center.X >= radius && center.Y >= radius &&
                center.X <= width - 1 - radius && center.Y <= height - 1 - radius;
        }

        public static IReadOnlyList<CellCoord> Footprint(CellCoord center, int sideLength)
            => new List<CellCoord>(FootprintSteps(center, sideLength)).AsReadOnly();

        internal static IEnumerable<CellCoord> FootprintSteps(CellCoord center, int sideLength)
        {
            if (!Fits(center, sideLength, int.MaxValue, int.MaxValue)) throw new ArgumentOutOfRangeException(nameof(center));
            int radius = sideLength / 2;
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
                for (int x = center.X - radius; x <= center.X + radius; x++)
                    yield return new CellCoord(x, y);
        }

        // 경고 때 고른 방향을 그대로 사용하며 출발 중심을 항상 포함한다.
        public static IReadOnlyList<CellCoord> Travel(CellCoord center, int sideLength, int distance,
            int width, int height, CellCoord direction)
            => new List<CellCoord>(TravelSteps(center, sideLength, distance, width, height, direction)).AsReadOnly();

        internal static IEnumerable<CellCoord> TravelSteps(CellCoord center, int sideLength, int distance,
            int width, int height, CellCoord direction)
        {
            if (!Fits(center, sideLength, width, height)) throw new ArgumentOutOfRangeException(nameof(center));
            if (distance < 1) throw new ArgumentOutOfRangeException(nameof(distance));
            if (direction != CellCoord.Zero && (Math.Abs((long)direction.X) != 1 || Math.Abs((long)direction.Y) != 1))
                throw new ArgumentOutOfRangeException(nameof(direction));
            yield return center;
            if (direction == CellCoord.Zero) yield break;
            for (int i = 0; i < distance; i++)
            {
                var next = center + direction;
                if (!Fits(next, sideLength, width, height)) yield break;
                yield return next;
                center = next;
            }
        }
    }
}
