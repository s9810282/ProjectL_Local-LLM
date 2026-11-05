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
        {
            if (!Fits(center, sideLength, int.MaxValue, int.MaxValue)) throw new ArgumentOutOfRangeException(nameof(center));
            int radius = sideLength / 2;
            var cells = new List<CellCoord>();
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
                for (int x = center.X - radius; x <= center.X + radius; x++)
                    cells.Add(new CellCoord(x, y));
            return cells.AsReadOnly();
        }

        // 경고 때 고른 방향을 그대로 사용하며 출발 중심을 항상 포함한다.
        public static IReadOnlyList<CellCoord> Travel(CellCoord center, int sideLength, int distance,
            int width, int height, CellCoord direction)
        {
            if (!Fits(center, sideLength, width, height)) throw new ArgumentOutOfRangeException(nameof(center));
            if (distance < 1) throw new ArgumentOutOfRangeException(nameof(distance));
            if (direction != CellCoord.Zero && (Math.Abs((long)direction.X) != 1 || Math.Abs((long)direction.Y) != 1))
                throw new ArgumentOutOfRangeException(nameof(direction));
            var path = new List<CellCoord> { center };
            if (direction == CellCoord.Zero) return path.AsReadOnly();
            for (int i = 0; i < distance; i++)
            {
                var next = center + direction;
                if (!Fits(next, sideLength, width, height)) break;
                path.Add(next);
                center = next;
            }
            return path.AsReadOnly();
        }
    }
}
