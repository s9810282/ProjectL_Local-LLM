namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 필드 위에 그리는 투명 표시의 정렬 순서 표입니다(넉백 미리보기 계획 §7.5). 값이 클수록 나중에(위에) 그립니다.
    /// </summary>
    /// <remarks>
    /// 칸 채우기·테두리·궤적 셰이더는 깊이를 쓰지 않아(ZWrite Off) 높이로는 그리는 순서가 정해지지 않고, 같은 정렬 순서끼리는
    /// Unity가 카메라 거리로 순서를 정합니다. 그래서 위아래는 이 표 한 곳에서 정합니다.
    /// 칸 채우기는 겹침 깊이(<see cref="Fill"/>)마다 하나씩 올라가 위 채우기가 늘 아래 채우기 뒤에 그려집니다(겹친 강조 안쪽 축소 설계 OI4).
    /// 칸 테두리는 표시기의 테두리 칸 순서(레일 → 아군 배치 구역 → 층 표 순서 → 호버)대로 하나씩 올라가고, 모든 채우기 위입니다.
    /// 이동 경로 띠·화살촉은 불투명 머티리얼이라 정렬 순서를 주지 않습니다. 대신 깊이를 쓰고 모든 칸 채우기·테두리보다 높이 놓여,
    /// 깊이를 쓰지 않는 칸 표시가 그 위에 덧칠되지 않습니다(표시기가 높이를 정함, 겹친 강조 2단계 계획 §7.6).
    /// 반투명 바닥 표면 프리팹(약한 지반 <c>Terrain_WeakGround</c>, 특성 흔적 <c>Trail_*</c>)은 에셋 값 −2로 칸 채우기 아래에 그립니다
    /// (코드가 넣지 않음, 약한 지반 검토 R3·특성 시스템 View 계획 Te.6). 같은 칸의 흔적은 바닥 표면보다 조금 높아 카메라 거리로 그 위에 그려집니다.
    /// 이 표에 없는 투명 물체(배치 고스트·이펙트 등)는 정렬 순서 0입니다. 겹침 깊이 1 채우기(0)는 그와 카메라 거리로 순서가 정해지고,
    /// 깊이 2 이상 채우기와 모든 테두리는 늘 그 뒤에 그려집니다. 그래서 깊이를 쓰지 않는 투명 물체 위에는 칸 표시가 덧칠될 수 있습니다
    /// (겹친 강조 계획 §7.6 L4, Unity 화면 확인 항목). 그런 물체가 칸 표시 위에 보여야 하면 그 물체에 이 표의 값보다 큰 정렬 순서를 줍니다.
    /// </remarks>
    internal static class FieldSortingOrders
    {
        /// <summary>칸 채우기 깊이 0(칸 전체)입니다. 겹친 위 채우기는 <see cref="Fill"/>로 이 값에서 올라갑니다.</summary>
        internal const int CellFill = -1;

        /// <summary>칸 테두리 첫 칸의 정렬 순서입니다. 가장 깊은 채우기보다 커야 합니다(표시기가 구성 때 검사).</summary>
        internal const int OutlineBase = 16;

        /// <summary>낙석 경고 칸의 느낌표입니다. 모든 칸 테두리 위, 공격 궤적 아래입니다(낙석 View 계획 RP-18).</summary>
        internal const int RockfallWarningMark = 90;

        /// <summary>공격 궤적 점선입니다. 모든 칸 테두리 위입니다.</summary>
        internal const int Trajectory = 100;

        /// <summary>한 칸에서 아래부터 센 겹침 깊이 <paramref name="depth"/>(0부터)의 채우기 정렬 순서입니다.</summary>
        internal static int Fill(int depth) => CellFill + depth;

        /// <summary>표시기의 테두리 칸 번호 <paramref name="slotIndex"/>(0부터)의 정렬 순서입니다.</summary>
        internal static int Outline(int slotIndex) => OutlineBase + slotIndex;
    }
}
