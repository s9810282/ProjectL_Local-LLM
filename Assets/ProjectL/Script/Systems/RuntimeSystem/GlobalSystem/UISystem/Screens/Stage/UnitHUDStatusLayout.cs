using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>좌하단에 고정한 묶음에서 위 행부터 채우는 동일 크기 상태 슬롯 배치입니다.</summary>
    internal readonly struct UnitHUDStatusLayout : IEquatable<UnitHUDStatusLayout>
    {
        private readonly float _width;
        private readonly float _cellWidth;
        private readonly float _cellHeight;
        private readonly float _horizontalSpacing;
        private readonly float _verticalSpacing;

        internal UnitHUDStatusLayout(float width, float cellWidth, float cellHeight,
            float horizontalSpacing, float verticalSpacing, int count)
        {
            if (!Positive(width)) throw new ArgumentOutOfRangeException(nameof(width));
            if (!Positive(cellWidth)) throw new ArgumentOutOfRangeException(nameof(cellWidth));
            if (!Positive(cellHeight)) throw new ArgumentOutOfRangeException(nameof(cellHeight));
            if (!NonNegative(horizontalSpacing)) throw new ArgumentOutOfRangeException(nameof(horizontalSpacing));
            if (!NonNegative(verticalSpacing)) throw new ArgumentOutOfRangeException(nameof(verticalSpacing));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (width < cellWidth) throw new ArgumentException("상태 영역 폭은 아이콘 한 칸 이상이어야 합니다.", nameof(width));

            _width = width;
            _cellWidth = cellWidth;
            _cellHeight = cellHeight;
            _horizontalSpacing = horizontalSpacing;
            _verticalSpacing = verticalSpacing;
            Count = count;
            Columns = (int)Math.Min(int.MaxValue, Math.Floor(((double)width + horizontalSpacing) / ((double)cellWidth + horizontalSpacing)));
            Rows = count == 0 ? 0 : 1 + (count - 1) / Columns;
            Height = Rows == 0 ? 0f : (float)((double)Rows * cellHeight + (Rows - 1d) * verticalSpacing);
            if (!NonNegative(Height)) throw new ArgumentOutOfRangeException(nameof(count), "상태 영역 높이가 유효 범위를 넘었습니다.");
        }

        internal float Width => _width;
        internal int Count { get; }
        internal int Columns { get; }
        internal int Rows { get; }
        internal float Height { get; }

        /// <summary>항목의 왼쪽 아래 좌표입니다. 첫 항목은 항상 최상단 행의 첫 칸입니다.</summary>
        internal void PositionOf(int index, out float x, out float y)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            x = (float)((index % Columns) * ((double)_cellWidth + _horizontalSpacing));
            y = (float)((Rows - 1 - index / Columns) * ((double)_cellHeight + _verticalSpacing));
        }

        public bool Equals(UnitHUDStatusLayout other) => _width == other._width && _cellWidth == other._cellWidth
            && _cellHeight == other._cellHeight && _horizontalSpacing == other._horizontalSpacing
            && _verticalSpacing == other._verticalSpacing && Count == other.Count;

        public override bool Equals(object obj) => obj is UnitHUDStatusLayout other && Equals(other);
        public override int GetHashCode() => _width.GetHashCode() ^ _cellWidth.GetHashCode() ^ _cellHeight.GetHashCode() ^ _horizontalSpacing.GetHashCode() ^ _verticalSpacing.GetHashCode() ^ Count;

        private static bool Positive(float value) => value > 0f && !float.IsInfinity(value);
        private static bool NonNegative(float value) => value >= 0f && !float.IsInfinity(value);
    }
}