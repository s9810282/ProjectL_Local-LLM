using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback
{
    /// <summary>단계 전환 사건과 스텐실의 완료를 모으고, 자신이 대여한 Scop을 반환합니다.</summary>
    internal sealed class CombatPhaseTransitionPresenter
    {
        private readonly IScreenTransitionSystemService _transitions;
        private readonly CombatPhaseTransitionSettings _settings;
        private readonly Action<CombatEvent, Action> _beginEvent;
        private IScreenTransitionScop _scop;
        private Action _completed;
        private bool _eventCompleted;
        private int _generation;

        internal CombatPhaseTransitionPresenter(IScreenTransitionSystemService transitions,
            CombatPhaseTransitionSettings settings, Action<CombatEvent, Action> beginEvent)
        {
            _transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _beginEvent = beginEvent ?? throw new ArgumentNullException(nameof(beginEvent));
        }

        internal bool IsPlaying => _scop != null;

        internal bool TryBegin(CombatEvent item, Action completed)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            // 자동 화면 전환은 현재 비활성입니다. 아래는 남은 아침 전환의 연결 예제입니다.
            // 어떤 사건이 전환을 거는지는 사건 형식 표가 답합니다(View 2라운드 W7-3).
            //CombatPhaseTransitionKind transition = CombatEventTypeTable.TransitionOf(item);
            //if (transition != CombatPhaseTransitionKind.Morning)
            //    return false;
            //if (IsPlaying)
            //    throw new InvalidOperationException("이전 단계의 화면 전환이 아직 끝나지 않았습니다.");

            //int generation = ++_generation;
            //_completed = completed;
            //_eventCompleted = false;
            //try
            //{
            //    _scop = _transitions.CreateScop()
            //        ?? throw new InvalidOperationException("화면 전환 Scop을 대여하지 못했습니다.");
            //    _scop.Play(_settings.Morning);
            //    if (generation != _generation || _scop == null)
            //        return true;
            //    // BGM 등 기존 사건 처리는 그대로 실행하고 완료 통지만 전환과 합류합니다.
            //    _beginEvent(item, () =>
            //    {
            //        if (generation == _generation && _scop != null)
            //            _eventCompleted = true;
            //    });
            //    return true;
            //}
            //catch (Exception error)
            //{
            //    CleanupFailed(generation, error);
            //    throw;
            //}
            return false;
        }

        /// <summary>Scop의 진행은 Render Tick이 담당하며 여기서는 완료 여부만 확인합니다.</summary>
        internal void Tick()
        {
            if (_scop == null || !_eventCompleted)
                return;
            int generation = _generation;
            try
            {
                if (!_scop.IsCompleted)
                    return;
            }
            catch (Exception error)
            {
                CleanupFailed(generation, error);
                throw;
            }

            Action completed = _completed;
            Cancel();
            completed();
        }

        internal void Cancel()
        {
            ++_generation;
            IScreenTransitionScop scop = _scop;
            _scop = null;
            _completed = null;
            _eventCompleted = false;
            scop?.Dispose();
        }

        private void CleanupFailed(int generation, Exception error)
        {
            if (generation != _generation)
                return;
            try
            {
                Cancel();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(error, cleanup);
            }
        }
    }
}
