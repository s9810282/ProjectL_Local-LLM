using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 수락된 한 노드 요청의 표시 이동과 Buffer 위치 동기화 수명을 관리합니다. 말이 뛰는 연출은 <see cref="StageMapViewMarkerJump"/>가 맡고,
    /// 여기에는 일반 이동만의 차이(착지 때 카메라 확정·전투 착지 게시·끊긴 이동의 표시 폐기)만 둡니다(설계 §2.8 C-02).
    /// </summary>
    internal sealed class StageMapNodeMovement : IStageMapEntryPresentation, IDisposable
    {
        private readonly StageMapViewDisplay _display;
        private readonly StageMapViewMarkerJump _jump;
        private readonly MapCombatPresentationLease _combatLease;
        private readonly StageNodeClearHistory _clearHistory;
        private readonly Func<bool> _canWork;
        private readonly Func<StageMapMovementContext> _captureContext;
        private readonly Func<StageMapMovementContext, bool> _isContextCurrent;
        // 말 연출에 넘기는 문맥 확인입니다. 한 번만 만들어 이동마다 대리자를 새로 만들지 않습니다.
        private readonly Func<bool> _checkContext;
        private StageMapMovementContext _context;
        private long _generation;
        private StageViewCommand _command;
        private CombatEntryRequest _combatRequest;
        private bool _hasContext;
        private bool _completed;
        private bool _disposed;

        internal bool IsPresenting => _hasContext && !_completed && !_disposed;
        internal bool IsNoncombatPresenting => IsPresenting && _combatRequest == null;
        internal int PresentedChapterKey => _command.Data.ChapterKey;

        internal StageMapNodeMovement(StageMapViewDisplay display, StageMapViewMarkerJump jump,
            MapCombatPresentationLease combatLease, StageNodeClearHistory clearHistory, Func<bool> canWork,
            Func<StageMapMovementContext> captureMovementContext,
            Func<StageMapMovementContext, bool> isMovementContextCurrent)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _combatLease = combatLease;
            _clearHistory = clearHistory ?? throw new ArgumentNullException(nameof(clearHistory));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            _captureContext = captureMovementContext ?? throw new ArgumentNullException(nameof(captureMovementContext));
            _isContextCurrent = isMovementContextCurrent ?? throw new ArgumentNullException(nameof(isMovementContextCurrent));
            _checkContext = CheckContext;
        }

        bool IStageMapEntryPresentation.PresentAccepted(StageViewCommand command, CombatEntryRequest combatRequest) =>
            PresentAccepted(command, combatRequest);

        internal bool PresentAccepted(StageViewCommand command, CombatEntryRequest combatRequest)
        {
            CancelInvalidMovement();
            if (!CanWork()) return false;
            command.Data.Validate();
            if (command.Number <= 0)
                throw new InvalidOperationException("[StageMap] 이동 요청 번호가 필요합니다.");
            bool same = _hasContext && StageEntryIdentity.Of(_command) == StageEntryIdentity.Of(command) &&
                ReferenceEquals(_combatRequest, combatRequest);
            if (same) return _completed;
            if (_hasContext && !_completed)
                throw new InvalidOperationException("[StageMap] 이동 중 다른 요청을 표시할 수 없습니다.");
            if (combatRequest != null && (_combatLease == null ||
                StageEntryIdentity.Of(combatRequest) != StageEntryIdentity.Of(command) || !combatRequest.IsPrepared))
                throw new InvalidOperationException("[StageMap] 수락·준비 완료된 같은 전투 요청만 표시할 수 있습니다.");

            StageMapMovementContext context = _captureContext();
            if (context.MapKey != command.Data.ChapterKey)
                throw new InvalidOperationException("[StageMap] 이동 요청과 준비된 맵이 다릅니다.");
            long generation = checked(++_generation);
            _context = context;
            _command = command;
            _combatRequest = combatRequest;
            _hasContext = true;
            _completed = false;
            _jump.Begin(command.Data.NodeId.Value, StageMarkerJumpKind.Approach,
                () => !_disposed && _generation == generation && _isContextCurrent(context), _checkContext);
            return false;
        }

        internal void Tick()
        {
            if (!_hasContext || _completed || !CheckContext()) return;
            if (!_jump.TryArrive(_checkContext)) return;
            // 일반 이동은 착지 때 카메라를 확정합니다(출전은 출전 수락 때).
            _jump.CommitCamera();
            _jump.FinishMotion();
            if (!CheckContext()) return;
            _display.CompleteMarkerMove(_command.Data.NodeId.Value);
            if (!CheckContext()) return;
            // 수락된 요청의 실제 착지 뒤에만 기록하며, 외부 전투 진입 완료 게시보다 먼저 설치합니다.
            _clearHistory.Arm(new StageNodeClearKey(_context.RunId, _context.MapKey, _command.Data.NodeId.Value));
            if (_combatRequest != null && !_combatLease.TryComplete(_combatRequest))
                throw new InvalidOperationException("[StageMap] 현재 전투 표시의 착지 완료를 게시할 수 없습니다.");
            _completed = true;
        }

        internal void SynchronizeCurrentNode(int currentNodeId)
        {
            CancelInvalidMovement();
            if (!CanWork()) return;
            if (_hasContext && !_completed)
            {
                if (currentNodeId != _command.Data.NodeId.Value)
                    throw new InvalidOperationException("[StageMap] 이동 목적지와 권위 현재 위치가 다릅니다.");
                return;
            }
            _display.SetCurrentNode(currentNodeId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearMovement();
        }

        internal void CancelInvalidMovement()
        {
            if (!_hasContext || (CanWork() && _isContextCurrent(_context))) return;
            ClearMovement();
        }

        private bool CheckContext()
        {
            CancelInvalidMovement();
            return _hasContext && CanWork() && _isContextCurrent(_context);
        }

        private void ClearMovement()
        {
            bool discardDisplay = _hasContext && !_completed;
            ++_generation;
            _combatRequest = null;
            _hasContext = false;
            _completed = false;
            _command = default;
            List<Exception> errors = null;
            _jump.Cancel(false, ref errors);
            // 문맥이 끝난 이동은 정상 착지를 게시하지 않고 자기 표시까지 버립니다.
            // 다음 Show가 이전 표시의 이동 잠금을 이어받지 않게 합니다.
            if (discardDisplay) CleanupErrors.Try(_display.Clear, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 이동 문맥 정리 실패.");
        }

        private bool CanWork()
        {
            if (_disposed) return false;
            bool current = _canWork();
            return current && !_disposed;
        }
    }
}
