using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>확정된 이동 사건의 재생만 담당하는 계약입니다.</summary>
    public interface ICombatMovementPlaybackPresenter
    {
        void BeginStep(CombatCharacterEvent step);
        void ShowReturn(CombatCharacterEvent returned);
        // 현재 BeginStep의 완료만 뜻한다. CancelMovement는 이전 애니메이션 콜백도 무효화해야 한다.
        bool IsMovementComplete { get; }
        void CancelMovement();
    }
}
