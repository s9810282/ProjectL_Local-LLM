namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item
{
    /// <summary>
    /// 같은 종류 지형이 붙은 모양을 드러난 변(이웃이 없는 쪽)의 수로 구분합니다. 이웃은 북·동·남·서 4방향만 봅니다(지형 이웃 모양 설계 TC1).
    /// 모양 프리팹은 아래 "R0 기준 드러난 쪽" 방향으로 만듭니다. 칸에 맞는 회전은 표시 쪽 외형 선택이 시계 방향 90도 단위로 정합니다.
    /// 북은 필드 +Z(Core +Y), 동은 필드 +X입니다.
    /// </summary>
    public enum TerrainEdgeShape
    {
        /// <summary>드러난 변이 없습니다(이웃 넷). 외형의 기본 프리팹이 이 모양이고, 빈 모양 칸의 대체입니다.</summary>
        Center = 0,

        /// <summary>드러난 변 하나. R0 기준 드러난 쪽: 동.</summary>
        OneSide = 1,

        /// <summary>이웃한 드러난 변 둘(바깥 모서리). R0 기준 드러난 쪽: 북·동.</summary>
        TwoSide = 2,

        /// <summary>마주 보는 드러난 변 둘(폭 1 줄). R0 기준 드러난 쪽: 동·서.</summary>
        TwoSideOpposite = 3,

        /// <summary>드러난 변 셋(끝). R0 기준 드러난 쪽: 북·동·서(남에 이웃).</summary>
        ThreeSide = 4,

        /// <summary>드러난 변 넷(외딴 칸).</summary>
        FourSide = 5
    }
}
