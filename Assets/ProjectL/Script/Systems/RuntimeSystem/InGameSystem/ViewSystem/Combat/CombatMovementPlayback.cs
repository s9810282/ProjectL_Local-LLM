using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 확정된 이동 사건 하나의 재생 수명입니다. 한 번에 한 걸음만 진행하고, 보간 완료는 표시(<see cref="ICombatMovementPlaybackPresenter"/>)에 묻습니다.
    /// </summary>
    /// <remarks>Detach 뒤에는 다시 쓰지 않습니다. 진행 중 Detach는 표시의 이동을 취소합니다.</remarks>
    internal sealed class CombatMovementPlayback
    {
        private readonly ICombatMovementPlaybackPresenter _presenter;
        private CombatCharacterEvent _step;
        private bool _detached;

        internal CombatMovementPlayback(ICombatMovementPlaybackPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        internal bool IsComplete { get; private set; } = true;

        internal void BeginStep(CombatCharacterEvent step)
        {
            RequireAvailable(step, CombatEventKind.Moved);
            _step = step;
            IsComplete = false;
            _presenter.BeginStep(step);
        }

        internal void ShowReturn(CombatCharacterEvent returned)
        {
            RequireAvailable(returned, CombatEventKind.Returned);
            _presenter.ShowReturn(returned);
            _step = null;
            IsComplete = true;
        }

        internal void Tick(float deltaTime)
        {
            if (!_detached && _step != null && _presenter.IsMovementComplete)
            {
                _step = null;
                IsComplete = true;
            }
        }

        internal void Detach()
        {
            if (_detached)
                return;

            _detached = true;
            _step = null;
            IsComplete = true;
            _presenter.CancelMovement();
        }

        private void RequireAvailable(CombatCharacterEvent step, CombatEventKind kind)
        {
            if (_detached)
                throw new ObjectDisposedException(nameof(CombatMovementPlayback));
            if (!IsComplete)
                throw new InvalidOperationException("이전 이동 연출이 아직 끝나지 않았습니다.");
            if (step == null || step.Kind != kind)
                throw new ArgumentException("이동 연출 종류가 다릅니다.", nameof(step));
        }
    }
}
