using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 전투 행동 버튼(공격·턴 종료·이동 되돌리기)의 표시 계약입니다(행동 버튼 설계 B1).
    /// 버튼 연결은 <see cref="CombatViewSystem"/>이, 보임·누름 가능 맞춤은 입력 표면(<see cref="CombatInputSurfaces"/>)이 전투 표시 전체가 아니라 이 계약으로만 다룹니다.
    /// </summary>
    /// <remarks>
    /// 세 번째 슬롯(옛 복귀 버튼 자리)은 이동 되돌리기 요청을 전달합니다(입력 연결 ReturnPressed → Router Return → 이동 되돌리기 입력).
    /// 구현: 행동 버튼 표시(<see cref="CombatActionButtonsDisplay"/>). 조정자·입력 표면은 <see cref="ICombatBattleDisplay.Actions"/>로 받습니다(씬 표시 나누기 설계 DS4).
    /// </remarks>
    internal interface ICombatActionDisplay
    {
        /// <summary>
        /// 세 입구(공격·턴 종료·이동 되돌리기)를 연결합니다. 세 번째 인자 이름 returnToCheckpoint는 옛 복귀 이름입니다. 모두 null이면 연결을 끊습니다.
        /// 씬 연결 해제 뒤 새 연결은 무시하지만 연결 해제는 항상 전달합니다.
        /// </summary>
        void BindActions(Action attack, Action endTurn, Action returnToCheckpoint);

        /// <summary>버튼 보임·누름 가능·공격 강조를 적용합니다. 같은 값이면 다시 쓰지 않습니다.</summary>
        void SetActionsAvailable(in CombatActionButtons buttons);
    }
}
