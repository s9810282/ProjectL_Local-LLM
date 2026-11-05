using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 행동 간격·머묾 대기 칸의 역할 계약입니다(연출 조립 설계 B12 보완 1). 걸음·대기 재생기가 구현하고, 사건 Presenter(사건 간격·머묾)와
    /// 이동 표시(걸음 전 간격)는 이 계약만 받습니다. 그래서 Presenter가 재생기를 이름으로 아는 곳은 조립 하나로 남습니다.
    /// </summary>
    internal interface ICombatWaitSlot
    {
        /// <summary>간격을 시작합니다. 걸음 레인이 진행 중이면 예외입니다(간격 동안 모든 레인이 멈추므로).</summary>
        void StartGap(float seconds);

        /// <summary>간격이 끝나면 처리할 사건을 겁니다. <paramref name="shownBeforeGap"/>이면 간격 뒤 완료만 알립니다.</summary>
        void HoldAfterGap(CombatEvent e, Action completed, bool shownBeforeGap);

        /// <summary>보인 사건을 정한 시간만큼 머물게 한 뒤 완료합니다. 간격 시작과 달리 레인 전제를 보지 않습니다.</summary>
        void HoldShown(CombatEvent e, Action completed, float seconds);
    }
}
