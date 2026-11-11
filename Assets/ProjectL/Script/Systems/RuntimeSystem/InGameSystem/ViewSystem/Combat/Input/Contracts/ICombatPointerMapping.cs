using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 화면 좌표를 격자 좌표로 바꾸는 계약입니다. 경로 입력·공격 Drag 몸짓(<c>CombatAttackDragGesture</c>)·커서 그리드 스냅(<see cref="CombatCursorSnap"/>)이 씁니다.
    /// 구현은 <see cref="CombatPointerMapper"/>이고, 연결 전·해제 뒤는 매퍼 자기 상태로 false입니다(씬 표시 나누기 설계 DS2).
    /// </summary>
    /// <remarks>
    /// 옛 <c>ICombatMovementPresenter.cs</c>에서 이름을 바꾼 파일입니다(구조 개선 SV3, GUID 유지).
    /// 그 파일에 함께 있던 조립 계약 <c>ICombatMovementPresenter</c>(소비자 없음)는 지웠고 결과 사건 계약은 <c>ICombatEventPlayback</c>에 흡수했습니다.
    /// </remarks>
    internal interface ICombatPointerMapping
    {
        /// <summary>칸 안쪽 판정 여백(칸 크기 비율)입니다. 경로 입력의 포인터 샘플링 기준입니다.</summary>
        float InnerCellInset { get; }

        /// <summary>화면 좌표를 격자 좌표로 바꿉니다. 취소 영역(UI 위) 위인지도 함께 돌려줍니다. 변환할 수 없으면 false입니다.</summary>
        bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea);
    }
}
