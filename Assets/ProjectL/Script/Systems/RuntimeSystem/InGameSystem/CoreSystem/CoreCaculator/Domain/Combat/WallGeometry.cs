using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>한 계산에서 파생한 불변 벽 형상이다. 전투 상태나 저장소를 소유하지 않는다.</summary>
    internal sealed class WallGeometry
    {
        private readonly HashSet<CellCoord> occupied;
        public IReadOnlyList<WallRun> Runs { get; }

        internal WallGeometry(IEnumerable<WallRun> runs)
        {
            var copy = runs.OrderBy(run => run.ComponentKey).ToArray();
            Runs = Array.AsReadOnly(copy);
            occupied = new HashSet<CellCoord>(copy.SelectMany(run => run.Cells));
        }

        public bool Occupies(CellCoord cell) => occupied.Contains(cell);
    }

    /// <summary>검증된 직선 연결 성분의 경계와 원본 증거만 보관한다.</summary>
    internal sealed class WallRun
    {
        public long XMin { get; }
        public long XMax { get; }
        public long YMin { get; }
        public long YMax { get; }
        public int CornerCutTicks { get; }
        public IReadOnlyList<long> SourceIds { get; }
        public IReadOnlyList<CellCoord> Cells { get; }
        public long ComponentKey => SourceIds[0];

        internal WallRun(long xMin, long xMax, long yMin, long yMax, int cornerCutTicks,
            IEnumerable<long> sourceIds, IEnumerable<CellCoord> cells)
        {
            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
            CornerCutTicks = cornerCutTicks;
            SourceIds = Array.AsReadOnly(sourceIds.OrderBy(id => id).ToArray());
            Cells = Array.AsReadOnly(cells.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray());
        }
    }

    /// <summary>선분 매개변수의 정확한 순서만 제공한다. 교차 곱에서만 큰 정수를 사용한다.</summary>
    internal readonly struct SegmentParameter : IComparable<SegmentParameter>
    {
        public static SegmentParameter Zero => new SegmentParameter(0, 1);
        public static SegmentParameter One => new SegmentParameter(1, 1);
        public long Numerator { get; }
        public long Denominator { get; }

        public SegmentParameter(long numerator, long denominator)
        {
            if (denominator <= 0) throw new ArgumentOutOfRangeException(nameof(denominator));
            Numerator = numerator;
            Denominator = denominator;
        }

        public int CompareTo(SegmentParameter other)
        {
            if (Denominator == 0 || other.Denominator == 0)
                throw new InvalidOperationException("초기화하지 않은 선분 매개변수는 비교할 수 없습니다.");
            return ((BigInteger)Numerator * other.Denominator)
                .CompareTo((BigInteger)other.Numerator * Denominator);
        }
    }
}
