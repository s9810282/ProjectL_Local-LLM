using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 전투 행동 버튼(공격·턴 종료)의 표시 계약입니다(행동 버튼 설계 B1).
    /// 버튼 연결은 <see cref="CombatViewSystem"/>이, 보임·누름 가능 맞춤은 입력 표면(<see cref="CombatInputSurfaces"/>)이 전투 표시 전체가 아니라 이 계약으로만 다룹니다.
    /// </summary>
    /// <remarks>
    /// 폐지된 복귀 슬롯은 요청을 전달하지 않습니다.
    /// 구현: <see cref="CombatSceneDisplay"/>(<see cref="ICombatBattleDisplay"/>를 통해), 테스트 가짜.
    /// </remarks>
    internal interface ICombatActionDisplay
    {
        /// <summary>
        /// 기존 세 인자 계약으로 입구를 연결합니다. 세 번째 복귀 인자는 호환용이며 호출하지 않습니다. 모두 null이면 연결을 끊습니다.
        /// 씬 연결 해제 뒤 새 연결은 무시하지만 연결 해제는 항상 전달합니다.
        /// </summary>
        void BindActions(Action attack, Action endTurn, Action returnToCheckpoint);

        /// <summary>버튼 보임·누름 가능·공격 강조를 적용합니다. 같은 값이면 다시 쓰지 않습니다.</summary>
        void SetActionsAvailable(in CombatActionButtons buttons);
    }
}
