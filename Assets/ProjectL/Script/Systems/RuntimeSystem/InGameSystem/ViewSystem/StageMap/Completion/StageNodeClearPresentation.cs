using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion
{
    /// <summary>클리어 후보의 건물 유지 → 파괴 반환 → 작은 점프를 연결합니다. 진행 기록과 전환 목적지는 바꾸지 않습니다.</summary>
    internal sealed class StageNodeClearPresentation
    {
        private enum State
        {
            Idle,
            Waiting,
            Breaking,
            Settling,
            Done,
            Cancelled
        }

        private readonly StageMapViewCompletionServices _services;
        private readonly StageMapViewDisplay _display;
        private readonly StageMapViewMarkerJump _jump;
        private readonly Func<bool> _canWork;
        private readonly Func<StageMapMovementContext> _captureContext;
        private readonly Func<StageMapMovementContext, bool> _isContextCurrent;
        private readonly Action _clearPointer;
        private State _state;
        private StageNodeClearKey _key;
        private long _preparationVersion;
        private StageMapMovementContext _context;
        private long _generation;
        private long _effectGeneration;
        private bool _ownsMarker;
        private bool _ticking;

        internal StageNodeClearPresentation(StageMapViewCompletionServices services, StageMapViewDisplay display,
            StageMapViewMarkerJump jump, Func<bool> canWork, Func<StageMapMovementContext> captureContext,
            Func<StageMapMovementContext, bool> isContextCurrent, Action clearPointer)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            _captureContext = captureContext ?? throw new ArgumentNullException(nameof(captureContext));
            _isContextCurrent = isContextCurrent ?? throw new ArgumentNullException(nameof(isContextCurrent));
            _clearPointer = clearPointer ?? throw new ArgumentNullException(nameof(clearPointer));
        }

        internal bool BlocksMapInput => _state == State.Waiting || _state == State.Breaking || _state == State.Settling;

        /// <summary>ShowNodes보다 먼저 호출합니다. 아직 클리어되지 않은 방문은 팝업을 막지 않습니다.</summary>
        internal void Prepare(StageNodeClearKey key, bool isCleared, long preparationVersion)
        {
            Cancel();
            _state = State.Idle;
            if (isCleared && _services.History.IsPending(key)) Reserve(key, preparationVersion);
        }

        private void Reserve(StageNodeClearKey key, long preparationVersion)
        {
            _key = key;
            _preparationVersion = preparationVersion;
            _context = new StageMapMovementContext(key.RunId, key.MapKey, preparationVersion);
            _state = State.Waiting;
            _display.HoldCompletionBuilding(key, preparationVersion);
        }

        internal void Tick(StageNodeClearFrame frame)
        {
            if (_ticking) return;
            _ticking = true;
            try
            {
                if (!_canWork() || !frame.IsPreparedContext)
                {
                    Cancel();
                    return;
                }
                if (BlocksMapInput && (frame.Key != _key || frame.PreparationVersion != _preparationVersion || !frame.IsCleared))
                    Cancel();
                if (!BlocksMapInput)
                {
                    if (!frame.IsCleared || !_services.History.IsPending(frame.Key)) return;
                    Reserve(frame.Key, frame.PreparationVersion);
                }
                // 자기 입력 잠금은 CanPresent의 계산에 넣지 않습니다. 정지·로딩·팝업 동안 단계도 넘기지 않습니다.
                if (!frame.CanPresent) return;
                if (_state == State.Waiting)
                {
                    StartBreaking();
                    return;
                }
                long generation = _generation;
                if (!CheckCurrent(generation)) return;
                if (_state == State.Breaking)
                {
                    StageNodeBreakStatus status = _services.Effects.GetStatus(_effectGeneration);
                    if (status == StageNodeBreakStatus.Playing) return;
                    if (status == StageNodeBreakStatus.Cancelled)
                    {
                        Cancel();
                        return;
                    }
                    EndEffect(_effectGeneration);
                    if (!CheckCurrent(generation)) return;
                    _state = State.Settling;
                    _ownsMarker = true;
                    int nodeId = _key.NodeId;
                    _jump.Begin(nodeId, StageMarkerJumpKind.Settle,
                        () => IsCurrent(generation), () => CheckCurrent(generation));
                    // Begin 내부의 동기 취소보다 늦게 등록된 이동도 회수합니다. 재진입 Tick은 위에서 차단합니다.
                    if (!IsCurrent(generation)) CancelJump(nodeId);
                    return;
                }
                if (_state != State.Settling || !_jump.TryArrive(() => CheckCurrent(generation))) return;
                _jump.FinishMotion();
                if (!CheckCurrent(generation)) return;
                _display.CompleteMarkerMove(_key.NodeId);
                if (!CheckCurrent(generation)) return;
                _ownsMarker = false;
                _display.ReleaseCompletionBuilding(_key, _preparationVersion);
                if (CheckCurrent(generation)) _state = State.Done;
            }
            finally
            {
                _ticking = false;
            }
        }

        private void StartBreaking()
        {
            long generation = ++_generation;
            if (!CheckCurrent(generation)) return;
            _context = _captureContext();
            StageNodeClearTarget target = _display.GetCompletionTarget(_key, _preparationVersion);
            if (!_services.History.TryConsume(_key))
            {
                Cancel();
                return;
            }
            _state = State.Breaking;
            _clearPointer();
            if (!CheckCurrent(generation)) return;
            _display.HideCompletionBuilding(target.Key, target.PreparationVersion);
            if (!CheckCurrent(generation)) return;
            long effectGeneration = _services.Effects.Begin(target.BreakEffect, target.MapRoot, target.WorldPose);
            _effectGeneration = effectGeneration;
            // Play 중 취소는 아직 반환되지 않은 세대를 알 수 없습니다. 반환 직후 반드시 직접 회수합니다.
            if (!IsCurrent(generation))
            {
                EndEffect(effectGeneration);
                CheckCurrent(generation);
                return;
            }
        }

        private bool IsCurrent(long generation) => _generation == generation && BlocksMapInput && _canWork() &&
            _isContextCurrent(_context) && _services.RunProgress.RunId == _key.RunId &&
            _services.Current.HasMap && _services.Current.CurrentMapKey == _key.MapKey &&
            _services.Current.HasSelection && _services.Current.CurrentStage.NodeId == _key.NodeId;

        private bool CheckCurrent(long generation)
        {
            if (IsCurrent(generation)) return true;
            if (_generation == generation) Cancel();
            return false;
        }

        private void EndEffect(long effectGeneration)
        {
            _services.Effects.End(effectGeneration);
            if (_effectGeneration == effectGeneration) _effectGeneration = 0;
        }

        private void CancelJump(int nodeId)
        {
            _ownsMarker = false;
            List<Exception> errors = null;
            _jump.Cancel(false, ref errors);
            CleanupErrors.Try(() => _display.CancelSettleMove(nodeId), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageNodeClearPresentation] 작은 점프 정리 실패.");
        }

        internal void Cancel()
        {
            ++_generation;
            _state = State.Cancelled;
            StageNodeClearKey key = _key;
            long version = _preparationVersion;
            long effectGeneration = _effectGeneration;
            bool ownsMarker = _ownsMarker;
            _ownsMarker = false;
            List<Exception> errors = null;
            CleanupErrors.Try(() => EndEffect(effectGeneration), ref errors);
            if (ownsMarker) CleanupErrors.Try(() => CancelJump(key.NodeId), ref errors);
            CleanupErrors.Try(() => _display.ReleaseCompletionBuilding(key, version), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageNodeClearPresentation] 완료 연출 정리 실패.");
        }
    }
}
