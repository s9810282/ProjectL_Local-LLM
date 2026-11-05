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
    /// 서로 인접한 셀의 공유 변은 메시에서 제외됩니다(<see cref="EachCell"/>가 거짓일 때).
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
        /// <summary>
        /// 외곽선 렌더러의 정렬 순서입니다. 셰이더가 깊이를 쓰지 않아(ZWrite Off) 겹친 외곽선의 위아래는 이 값으로 정합니다.
        /// 값이 클수록 나중에(위에) 그립니다. 기본 0입니다.
        /// </summary>
        public int SortingOrder { get; }
        /// <summary>0보다 크면 외곽선이 바깥으로 튀어나온(볼록) 꺾임을 이 반지름(월드 길이)의 사분원으로 잇습니다. 안으로 꺾인 곳은 직각입니다. 기본 0(모두 직각)입니다.</summary>
        public float CornerRadius { get; }
        /// <summary>
        /// 0보다 크면 맞닿은 셀을 묶은 덩어리 경계를 변마다 안쪽으로 이 거리(월드 길이)만큼 옮겨 그립니다.
        /// x는 세로(Z 방향) 변, y는 가로(X 방향) 변을 옮기는 거리입니다. 기본 0(셀 경계 그대로)입니다.
        /// 음수는 0으로 보고, 셀 크기의 반보다 작아야 합니다(넘으면 고리가 뒤집힘).
        /// </summary>
        public Vector2 LoopInset { get; }
        /// <summary>
        /// 참이면 셀을 덩어리로 묶지 않고 셀마다 네 변을 모두 그립니다. <see cref="LoopInset"/>·<see cref="CornerRadius"/>가 모두 0이면
        /// 맞닿은 두 셀이 함께 가진 변은 한 번만 그리고, 아니면 셀마다 그 셀 경계에서 줄이고 네 모서리를 둥글게 한 고리를 따로 그립니다.
        /// 기본 거짓(맞닿은 셀은 덩어리 하나, 바깥 경계만)입니다.
        /// </summary>
        public bool EachCell { get; }

        /// <summary>이 방향의 지정된 바깥 격자 변을 생략합니다. 기본값은 닫힌 고리입니다.</summary>
        public GridOutlineOpening Opening { get; }

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
            float dashLength = 0f,
            int sortingOrder = 0,
            float cornerRadius = 0f,
            Vector2 loopInset = default,
            bool eachCell = false,
            GridOutlineOpening opening = default)
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
            SortingOrder = sortingOrder;
            CornerRadius = cornerRadius;
            LoopInset = loopInset;
            EachCell = eachCell;
            Opening = opening;
        }

        internal GridOutlineCreateData ToMeshData()
        {
            return new GridOutlineCreateData(Name, Cells, GridOrigin, CellSize, YOffset, Thickness, Color, DashLength,
                CornerRadius, LoopInset, EachCell, Opening);
        }
    }
}
