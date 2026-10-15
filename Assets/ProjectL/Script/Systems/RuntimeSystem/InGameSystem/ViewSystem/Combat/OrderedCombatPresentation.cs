using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 결과 하나의 사건을 기존 View Tick 안에서 순서대로 표시에 넘기는 연출입니다(설계 §9·§9.2).
    /// 이동 사건은 <see cref="CombatMovementPlayback"/>이 보간하고, 그 밖의 사건은 <see cref="ICombatResultPresenter"/>가 완료를 알립니다.
    /// </summary>
    /// <remarks>
    /// Buffer 확인·Flow·최종 표시 반영은 CombatViewSystem의 책임입니다. 세대 번호로 취소 뒤 늦게 온 완료 통지를 무시합니다.
    /// </remarks>
    internal sealed class OrderedCombatPresentation : ICombatPresentation
    {
        private ICombatMovementPlaybackPresenter _movementPresenter;
        private ICombatResultPresenter _resultPresenter;
        private ICombatPresentationControl _control;
        private CombatMovementPlayback _movement;
        private CombatResult _result;
        private Action<CombatPresentationOutcome> _finished;
        private int _index;
        private int _generation;
        private bool _started;
        private bool _completed;
        private bool _isCancelling;

        public void Attach(ICombatBattleDisplay display)
        {
            if (display == null)
                throw new ArgumentNullException(nameof(display));

            Cancel();
            _movementPresenter = display;
            _resultPresenter = display;
            _control = display;
            _movement = new CombatMovementPlayback(_movementPresenter);
        }

        public void Play(CombatResult result, Action<CombatPresentationOutcome> onFinished)
        {
            if (_resultPresenter == null || _result != null)
                throw new InvalidOperationException("연출 연결/순서가 잘못되었습니다.");

            foreach (var e in result.Events)
            {
                if (!IsMovement(e) && !_resultPresenter.CanPresent(e))
                    throw new NotSupportedException("지원하지 않는 전투 사건: " + e.Kind);
            }

            _movement ??= new CombatMovementPlayback(_movementPresenter);
            _result = result;
            _finished = onFinished;
            _index = 0;
            _started = false;
            Tick(0);
        }

        public void Tick(float deltaTime)
        {
            if (_result == null)
                return;

            _control.TickPresentation(deltaTime);
            while (_result != null && _index < _result.Events.Count)
            {
                var e = _result.Events[_index];
                if (!_started)
                {
                    _started = true;
                    _completed = false;
                    int generation = _generation;
                    int index = _index;
                    if (e is CombatCharacterEvent c && c.Kind == CombatEventKind.Moved)
                    {
                        _movement.BeginStep(c);
                    }
                    else if (IsMovement(e))
                    {
                        _movement.ShowReturn((CombatCharacterEvent)e);
                    }
                    else
                    {
                        _resultPresenter.BeginEvent(e, () =>
                        {
                            if (_generation == generation && _result != null && _index == index)
                                _completed = true;
                        });
                    }

                    // 사건 시작 안에서 취소·다시 연결되면 이 Tick을 멈춘다.
                    if (_generation != generation || _result == null)
                        return;
                }

                if (IsMovement(e))
                {
                    _movement.Tick(deltaTime);
                    _completed = _movement.IsComplete;
                }

                if (!_completed)
                    return;

                _index++;
                _started = false;
            }

            if (_result == null)
                return;

            var finished = _finished;
            _result = null;
            _finished = null;
            finished(CombatPresentationOutcome.Completed);
        }

        public void Detach()
        {
            try
            {
                Cancel();
            }
            finally
            {
                _movementPresenter = null;
                _resultPresenter = null;
                _control = null;
                _movement = null;
            }
        }

        public void Cancel()
        {
            if (_isCancelling)
                return;

            _isCancelling = true;
            _generation++;
            _result = null;
            _finished = null;
            _started = false;
            var movement = _movement;
            _movement = null;
            List<Exception> errors = null;
            try
            {
                // 한 자원의 해제가 실패해도 다른 연출 정리를 시도한다. 늦은 완료는 이미 무효화했다.
                try
                {
                    movement?.Detach();
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }

                try
                {
                    _control?.CancelPresentation();
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
            }
            finally
            {
                _isCancelling = false;
            }

            if (errors != null)
                throw new AggregateException("[OrderedCombatPresentation] 연출 취소 중 정리에 실패했습니다.", errors);
        }

        /// <summary>이동 보간으로 표시하는 사건입니다: 이동 한 걸음, 필드 안에서의 체크포인트 복귀.</summary>
        private static bool IsMovement(CombatEvent e)
        {
            return e is CombatCharacterEvent c
                && (c.Kind == CombatEventKind.Moved
                    || (c.Kind == CombatEventKind.Returned
                        && c.Expected.Area == CharacterRuntimeArea.Field
                        && c.Final.Area == CharacterRuntimeArea.Field));
        }
    }
}
