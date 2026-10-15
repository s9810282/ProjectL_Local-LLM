using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem
{
    public enum AreaOutlineFlowDirection
    {
        Counterclockwise = -1,
        Clockwise = 1
    }

    /// <summary>
    /// 선택된 XZ 격자 셀의 외곽선 표현 데이터입니다.
    /// 서로 인접한 셀의 공유 변은 메시에서 제외됩니다.
    /// </summary>
    public readonly struct AreaOutlineData
    {
        public string Name { get; }
        public Vector2Int[] Cells { get; }
        public Vector3 GridOrigin { get; }
        public Vector2 CellSize { get; }
        public float YOffset { get; }
        public float Thickness { get; }
        public Color Color { get; }
        public bool IsDashed { get; }
        /// <summary>닫힌 경계 루프 전체의 점선 개수입니다. 셰이더에서 정수로 반올림됩니다. <see cref="DashLength"/>가 0보다 크면 쓰지 않습니다.</summary>
        public float DashCount { get; }
        /// <summary>
        /// 0보다 크면 점선 하나(점+틈)의 월드 길이입니다. 루프마다 둘레 ÷ 이 값(반올림, 최소 1)개로 나뉘어
        /// 테두리 크기와 상관없이 점선 길이가 같습니다. 0이면 <see cref="DashCount"/>를 루프마다 그대로 씁니다.
        /// </summary>
        public float DashLength { get; }
        public float DashFill { get; }
        public AreaOutlineFlowDirection FlowDirection { get; }
        public float FlowSpeed { get; }

        public AreaOutlineData(
            string name,
            Vector2Int[] cells,
            Vector3 gridOrigin,
            Vector2 cellSize,
            float yOffset,
            float thickness,
            Color color,
            bool isDashed = true,
            float dashCount = 4f,
            float dashFill = 0.55f,
            AreaOutlineFlowDirection flowDirection = AreaOutlineFlowDirection.Clockwise,
            float flowSpeed = 1f,
            float dashLength = 0f)
        {
            Name = name;
            Cells = cells;
            GridOrigin = gridOrigin;
            CellSize = cellSize;
            YOffset = yOffset;
            Thickness = thickness;
            Color = color;
            IsDashed = isDashed;
            DashCount = dashCount;
            DashFill = dashFill;
            FlowDirection = flowDirection;
            FlowSpeed = flowSpeed;
            DashLength = dashLength;
        }

        internal GridOutlineCreateData ToMeshData()
        {
            return new GridOutlineCreateData(Name, Cells, GridOrigin, CellSize, YOffset, Thickness, Color, DashLength);
        }
    }
}
