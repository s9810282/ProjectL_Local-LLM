using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem
{
    /// <summary>격자 외곽선에서 열어 둘 바깥쪽 방향입니다. 기본값은 닫힌 고리입니다.</summary>
    public enum GridOutlineSide
    {
        None,
        Left,
        Right,
        Down,
        Up
    }

    /// <summary>
    /// 생략할 격자 외곽 변입니다. GridLine은 셀 모서리 정수 좌표입니다.
    /// 예를 들어 셀 (0, 0)의 왼쪽/아래는 0, 오른쪽/위는 1입니다.
    /// 범위 전체의 바깥 경계선을 지정하며, 깊이·높이별 부분으로 나눠도 같은 값을 전달합니다.
    /// </summary>
    public readonly struct GridOutlineOpening : IEquatable<GridOutlineOpening>
    {
        public GridOutlineSide Side { get; }
        public int GridLine { get; }
        /// <summary>테두리를 표시하지 않을 칸 영역입니다. 칸 모서리 정수 좌표를 사용합니다.</summary>
        public RectInt ClearArea { get; }
        public bool HasClearArea => ClearArea.width > 0 && ClearArea.height > 0;
        public bool IsEnabled => Side != GridOutlineSide.None || HasClearArea;

        public GridOutlineOpening(GridOutlineSide side, int gridLine, RectInt clearArea = default)
        {
            if ((uint)side > (uint)GridOutlineSide.Up)
                throw new ArgumentOutOfRangeException(nameof(side));
            Side = side;
            GridLine = side == GridOutlineSide.None ? 0 : gridLine;
            ClearArea = clearArea;
        }

        public bool Equals(GridOutlineOpening other) => Side == other.Side && GridLine == other.GridLine && ClearArea.Equals(other.ClearArea);
        public override bool Equals(object obj) => obj is GridOutlineOpening other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Side, GridLine, ClearArea);
    }

    /// <summary>완성된 고리의 UV·이음은 유지하고 열린 변을 향한 띠 조각만 생략합니다.</summary>
    internal static class GridOutlineOpeningClip
    {
        internal static bool Cuts(Vector3 from, Vector3 to, in GridOutlineCreateData data, float radius)
        {
            GridOutlineOpening opening = data.Opening;
            if (!opening.IsEnabled)
                return false;

            if (opening.HasClearArea)
            {
                Vector2 padding = Vector2.Max(Vector2.zero, data.LoopInset) + Vector2.one * Mathf.Max(0f, radius);
                Vector2 min = new(data.GridOrigin.x + (opening.ClearArea.xMin - 0.5f) * data.CellSize.x - padding.x,
                    data.GridOrigin.z + (opening.ClearArea.yMin - 0.5f) * data.CellSize.y - padding.y);
                Vector2 max = new(data.GridOrigin.x + (opening.ClearArea.xMax - 0.5f) * data.CellSize.x + padding.x,
                    data.GridOrigin.z + (opening.ClearArea.yMax - 0.5f) * data.CellSize.y + padding.y);
                float epsilon = Mathf.Max(data.CellSize.x, data.CellSize.y) * 0.00001f;
                if (Inside(from, min, max, epsilon) && Inside(to, min, max, epsilon))
                    return true;
            }
            if (opening.Side == GridOutlineSide.None)
                return false;

            bool alongX = opening.Side == GridOutlineSide.Left || opening.Side == GridOutlineSide.Right;
            float sign = opening.Side == GridOutlineSide.Left || opening.Side == GridOutlineSide.Down ? -1f : 1f;
            Vector3 outward = alongX ? new Vector3(sign, 0f, 0f) : new Vector3(0f, 0f, sign);
            // 고리 진행 방향 × 위가 바깥입니다. 직선뿐 아니라 그쪽으로 굽는 둥근 모서리도 뺍니다.
            if (Vector3.Dot(Vector3.Cross((to - from).normalized, Vector3.up), outward) <= 0.0001f)
                return false;

            float cellSize = alongX ? data.CellSize.x : data.CellSize.y;
            float origin = alongX ? data.GridOrigin.x : data.GridOrigin.z;
            float inset = Mathf.Max(0f, alongX ? data.LoopInset.x : data.LoopInset.y);
            float plane = origin + (opening.GridLine - 0.5f) * cellSize - sign * inset;
            float a = sign * ((alongX ? from.x : from.z) - plane);
            float b = sign * ((alongX ? to.x : to.z) - plane);
            float tolerance = Mathf.Max(0.000001f, cellSize * 0.00001f);
            return Mathf.Min(a, b) >= -Mathf.Max(0f, radius) - tolerance;
        }

        private static bool Inside(Vector3 point, Vector2 min, Vector2 max, float epsilon) =>
            point.x >= min.x - epsilon && point.x <= max.x + epsilon && point.z >= min.y - epsilon && point.z <= max.y + epsilon;
    }
}
