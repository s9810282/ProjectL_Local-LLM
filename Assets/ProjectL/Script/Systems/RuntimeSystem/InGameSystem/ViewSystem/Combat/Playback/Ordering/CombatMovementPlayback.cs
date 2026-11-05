using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 순차 연출과 표시의 이동 재생(<see cref="ICombatMovementPlaybackPresenter"/>) 사이의 연결 수명입니다. 걸음 시작·복귀 표시를 넘기고,
    /// 연결을 끊을 때 표시의 이동을 한 번 취소합니다. 걸음별 사건 완료는 시작 때 넘긴 통지로 받습니다(동시 처리 설계 C6).
    /// </summary>
    /// <remarks>Detach 뒤에는 다시 쓰지 않습니다. 진행 중 Detach는 표시의 이동을 취소합니다. 여러 유닛의 걸음이 함께 돌 수 있습니다.</remarks>
    internal sealed class CombatMovementPlayback
    {
        private readonly ICombatMovementPlaybackPresenter _presenter;
        private bool _detached;

        internal CombatMovementPlayback(ICombatMovementPlaybackPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        internal void BeginStep(CombatCharacterEvent step, Action completed)
        {
            RequireAvailable(step, CombatEventKind.Moved);
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            _presenter.BeginStep(step, completed);
        }

        internal void ShowReturn(CombatCharacterEvent returned)
        {
            RequireAvailable(returned, CombatEventKind.Returned);
            _presenter.ShowReturn(returned);
        }

        internal void Detach()
        {
            if (_detached)
                return;

            _detached = true;
            _presenter.CancelMovement();
        }

        private void RequireAvailable(CombatCharacterEvent step, CombatEventKind kind)
        {
            if (_detached)
                throw new ObjectDisposedException(nameof(CombatMovementPlayback));
            if (step == null || step.Kind != kind)
                throw new ArgumentException("이동 연출 종류가 다릅니다.", nameof(step));
        }
    }
}
