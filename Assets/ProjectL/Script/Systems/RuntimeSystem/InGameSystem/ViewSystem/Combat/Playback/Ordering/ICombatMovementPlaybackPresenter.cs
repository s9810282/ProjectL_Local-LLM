using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>확정된 이동 사건의 재생만 담당하는 계약입니다. 이동 연결(<see cref="CombatMovementPlayback"/>)이 받습니다.</summary>
    internal interface ICombatMovementPlaybackPresenter
    {
        /// <summary>
        /// 걸음 하나를 그 유닛의 걸음 레인에서 시작합니다(동시 처리 설계 C5·C6). 다른 유닛의 걸음과 함께 돌 수 있습니다.
        /// <paramref name="completed"/>는 이 걸음의 사건 완료(다음 사건으로 넘어가도 됨)입니다. 접촉 묶음이 있으면 첫 묶음 해제 때 오고
        /// 남은 보간은 계속됩니다(이동 접촉 설계 M3). 취소는 완료가 아니며, 취소 뒤에는 부르지 않습니다.
        /// </summary>
        void BeginStep(CombatCharacterEvent step, Action completed);

        void ShowReturn(CombatCharacterEvent returned);

        /// <summary>모든 걸음 레인을 취소합니다. 이전 걸음의 완료 통지도 무효화해야 합니다.</summary>
        void CancelMovement();
    }
}
