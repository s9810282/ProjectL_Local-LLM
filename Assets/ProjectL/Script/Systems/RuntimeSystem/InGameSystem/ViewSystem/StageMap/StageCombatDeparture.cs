using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 출전 전의 미제출 목적지와 표시 이동만 소유합니다. 진행 Buffer를 쓰거나 Core를 호출하지 않습니다.
    /// 말이 뛰는 연출은 <see cref="StageMapViewMarkerJump"/>가 맡고, 여기에는 출전만의 차이(수락 때 카메라 확정·착지 확정 게시 전 취소면
    /// 카메라 되돌림·덱 창 닫기와 현재 노드 다시 놓기)만 둡니다(설계 §2.8 C-02).
    /// </summary>
    internal sealed class StageCombatDeparture : IDisposable
    {
        /// <summary>노드 누름을 출전이 어떻게 받았는지입니다. 안내는 출전 부품이 아니라 Scope가 띄웁니다.</summary>
        internal enum StartResult
        {
            /// <summary>전투 노드가 아니라 가로채지 않았습니다. 기존 노드 명령 경로로 갑니다.</summary>
            NotIntercepted,
            /// <summary>출전 미리 이동을 시작했습니다.</summary>
            Started,
            /// <summary>지금은 받을 수 없어 클릭을 삼켰습니다(바쁨·문맥 다름).</summary>
            Blocked,
            /// <summary>현재 노드와 연결되지 않은 전투 노드라 클릭을 삼켰습니다.</summary>
            NotConnected
        }

        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly IStageViewBufferReadSink _requests;
        private readonly IStageViewBufferWriteSink _writer;
        private readonly StageMapViewReader _reader;
        private readonly StageMapViewDisplay _display;
        private readonly StageMapViewMarkerJump _jump;
        private readonly StageNodeClearHistory _clearHistory;
        private readonly DeckEditPopupController _deck;
        private readonly Func<bool> _canWork;
        private readonly Func<StageMapMovementContext> _captureContext;
        private readonly Func<StageMapMovementContext, bool> _isContextCurrent;
        // 말 연출에 넘기는 문맥 확인입니다. 한 번만 만들어 이동마다 대리자를 새로 만들지 않습니다.
        private readonly Func<bool> _checkContext;
        private StageMapMovementContext _context;
        private long _generation;
        private int _chapter;
        private int _node;
        private int _origin;
        private StageMarkerLanding _originLanding;
        private int _submitted;
        private bool _arrived;
        private bool _landingPublished;
        private bool _disposed;

        internal bool IsBusy => _node != 0 && !_disposed;

        internal StageCombatDeparture(StageMapViewEntryServices services, StageMapViewReader reader,
            StageMapViewDisplay display, StageMapViewMarkerJump jump, DeckEditPopupController deck, Func<bool> canWork,
            Func<StageMapMovementContext> captureMovementContext,
            Func<StageMapMovementContext, bool> isMovementContextCurrent)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            _current = services.Current;
            _stages = services.Stages;
            _requests = services.Requests;
            _writer = services.RequestWriter;
            _clearHistory = services.ClearHistory;
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            _captureContext = captureMovementContext ?? throw new ArgumentNullException(nameof(captureMovementContext));
            _isContextCurrent = isMovementContextCurrent ?? throw new ArgumentNullException(nameof(isMovementContextCurrent));
            _checkContext = CheckContext;
        }

        /// <summary>전투 노드만 출전 대기로 가로챕니다. 나머지는 기존 명령 경로를 유지합니다.</summary>
        internal StartResult TryBegin(int chapter, int nodeId)
        {
            CancelInvalidMovement();
            if (_disposed || !_canWork() || IsBusy) return StartResult.Blocked;
            if (_current.CurrentMapKey != chapter || !_reader.TryReadCurrentNode(out int origin)) return StartResult.Blocked;
            bool combat = false;
            foreach (var node in _stages.GetStages(chapter))
                if (node.NodeId == nodeId) combat = !node.IsStart && node.StageType == StageType.Combat;
            if (!combat) return StartResult.NotIntercepted;
            // 선택 가능한지 최종 판단하는 곳은 출전 후 Core입니다. 여기서는 표시된 연결 밖의 미리 이동만 막습니다.
            bool connected = false;
            foreach (var node in _stages.GetStages(chapter))
                if (node.NodeId == origin)
                    foreach (int next in node.NextNodeIds)
                        if (next == nodeId) connected = true;
            if (!connected) return StartResult.NotConnected;
            StageMapMovementContext context = _captureContext();
            if (context.MapKey != chapter)
                throw new InvalidOperationException("[StageCombatDeparture] 출전 목적지와 준비된 맵이 다릅니다.");
            long generation = checked(++_generation);
            _context = context;
            _chapter = chapter;
            _node = nodeId;
            _origin = origin;
            _originLanding = _display.CurrentLanding;
            _jump.Begin(nodeId, StageMarkerJumpKind.Approach,
                () => !_disposed && _generation == generation && _isContextCurrent(context), _checkContext);
            return StartResult.Started;
        }

        internal void Tick()
        {
            if (!IsBusy || !CheckContext()) return;
            if (_current.CurrentMapKey != _chapter ||
                (_submitted == 0 && (!_reader.TryReadCurrentNode(out int current) || current != _origin)))
            {
                Cancel();
                return;
            }
            if (_submitted != 0)
            {
                // 거절·취소의 결과 처리는 기존 Observer가 입력을 제거한 뒤 완료됩니다.
                if (!_requests.TryGetNextCommand(0, out var command) || command.Number != _submitted)
                {
                    // 수락·착지를 게시한 뒤 입력이 지워진 것은 정상 완료(전투 진입)입니다. 덱 닫기·마커 되돌림 같은 취소 처리를 하지 않습니다(평가 N-80).
                    if (_landingPublished) ClearMovement(false, false);
                    else Cancel();
                }
                return;
            }
            if (_arrived)
            {
                // 출전 확정 뒤 닫힘 연출이 밖에서 끊기면 덱 창의 출전·취소 후속이 모두 버려집니다.
                // 그래서 "도착했고 제출하지 않았는데 덱 창이 닫혀 있다"는 사실로 스스로 취소합니다(정상 출전은 닫는 호출 안에서 바로 제출).
                if (!_deck.IsOpen) Cancel();
                return;
            }
            if (!_jump.TryArrive(_checkContext)) return;
            // 출전은 카메라를 착지가 아니라 출전 수락 때 확정합니다(CompleteAccepted).
            _jump.FinishMotion();
            _display.CompleteMarkerMove(_node);
            if (!CheckContext()) return;
            _arrived = true;
            if (!CheckContext()) return;
            if (!_deck.OpenForCombat(Submit, Cancel)) Cancel();
        }

        internal bool CompleteAccepted(StageViewCommand command, CombatEntryRequest request,
            MapCombatPresentationLease lease)
        {
            if (!IsBusy || !CheckContext() || !_arrived || _submitted != command.Number) return false;
            if (request == null || !request.IsPrepared || command.Data.ChapterKey != _chapter ||
                command.Data.NodeId.Value != _node || StageEntryIdentity.Of(command) != StageEntryIdentity.Of(request))
                throw new InvalidOperationException("[StageCombatDeparture] 출전 요청과 전투 준비 결과가 일치하지 않습니다.");
            if (!_landingPublished)
            {
                _jump.CommitCamera();
                // 미리 이동/덱 취소에는 기록하지 않습니다. 수락·준비·착지가 모두 확인된 경계입니다.
                _clearHistory.Arm(new StageNodeClearKey(_context.RunId, _chapter, _node));
                // 전환 Job이 실행되기 전 Observer가 다시 보더라도 착지는 한 번만 게시합니다.
                _landingPublished = true;
                if (lease == null || !lease.TryComplete(request))
                    throw new InvalidOperationException("[StageCombatDeparture] 출전한 전투의 착지 완료를 게시할 수 없습니다.");
            }
            return true;
        }

        private void Submit()
        {
            if (!IsBusy || !CheckContext() || !_arrived || _submitted != 0) return;
            if (_current.CurrentMapKey != _chapter || !_reader.TryReadCurrentNode(out int current) ||
                current != _origin || _requests.Commands.Count != 0)
            {
                Cancel();
                return;
            }
            _submitted = _writer.Submit(StageViewCommandData.SelectStageNode(_chapter, new StageNodeId(_node)));
        }

        private void Cancel()
        {
            if (!IsBusy) return;
            if (!CheckContext()) return;
            int node = _node;
            bool arrived = _arrived;
            bool restore = !_landingPublished;
            ClearMovement(restore, false);
            if (!_canWork() || _disposed || !_isContextCurrent(_context)) return;
            if (!arrived) _display.CompleteMarkerMove(node);
            if (!_canWork() || _disposed || !_isContextCurrent(_context)) return;
            _deck.CloseCombat();
            if (_canWork() && !_disposed && _isContextCurrent(_context) && _reader.TryReadCurrentNode(out int current))
                _display.PlaceCurrentMarker(current,
                    current == _origin ? _originLanding : _display.GetRestingLanding(current));
        }

        internal void CancelInvalidMovement()
        {
            if (_node == 0 || (!_disposed && _canWork() && _isContextCurrent(_context))) return;
            List<Exception> errors = null;
            CleanupErrors.Try(() => ClearMovement(false, true), ref errors);
            // 자기 출전 팝업만 닫고 새 Run의 현재 위치나 카메라는 복원하지 않습니다.
            CleanupErrors.Try(_deck.CloseCombat, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageCombatDeparture] 무효 출전 정리 실패.");
        }

        private bool CheckContext()
        {
            CancelInvalidMovement();
            return IsBusy && _canWork() && _isContextCurrent(_context);
        }

        private void ClearMovement(bool restoreCamera, bool discardDisplay)
        {
            bool interrupted = _node != 0 && !_arrived;
            ++_generation;
            _node = 0;
            _submitted = 0;
            _arrived = false;
            _landingPublished = false;
            List<Exception> errors = null;
            // 출전만 취소 때 카메라를 출발 자리로 되돌립니다(착지 확정 게시 전의 유효한 취소, 설계 §2.8).
            _jump.Cancel(restoreCamera, ref errors);
            // 일반 덱 취소는 기존 복원 경로를 쓰고, 무효화된 이동의 표시만 폐기합니다.
            if (discardDisplay && interrupted) CleanupErrors.Try(_display.Clear, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageCombatDeparture] 출전 표시 정리 실패.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearMovement(false, true);
        }
    }
}
