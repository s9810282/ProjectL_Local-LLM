#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath
{
    /// <summary>
    /// 이동 경로 초안의 편집·Core 견적 요청·확정 제출을 맡습니다. 허용 판단은 호출자가 넘기고,
    /// 전투·요청 번호·Core context 일치 같은 수명·정체성 검사만 직접 합니다.
    /// </summary>
    internal sealed class CombatMovementInteraction
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMoveBegin = new("Combat.Action.Interaction.Move.Begin");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMoveDrag = new("Combat.Action.Interaction.Move.Drag");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMoveConfirm = new("Combat.Action.Interaction.Move.Confirm");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMoveCancel = new("Combat.Action.Interaction.Move.Cancel");
#endif

        private readonly CombatMovementQuote _quote;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly float _innerCellInset;
        private readonly ICombatActionRejectionSink _rejections;
        private readonly List<CellCoord> _crossed = new List<CellCoord>();
        private readonly List<CellCoord> _unaccepted = new List<CellCoord>();
        private Vector2? _lastPointer;
        private bool _attached;
        private bool _frozen;
        private bool _previewDeferred;
        private long _battle;
        private bool _submitted;
        private MovementPath _retainedPath;
        private MovementPath _retainedAgainst;
        private readonly List<CellCoord> _prefixCells = new List<CellCoord>();
        private bool _resumeAdjacent;
        private bool _requiresEndpointReentry;
        private bool _awaitingBoardReentry;
        private Vector2? _boardReentryPoint;
        private CellCoord? _rejectedCell;
        // 새 칸을 이어 그린 초안만 최단 경로로 복구한다. 되짚기·경로 교체는 다른 revision이다.
        private long _extendedRevision;

        // 경로 편집은 이 입력 소유자가 담당하며 표시 측은 Snapshot과 상태를 읽는다.
        private MovementDraft Draft { get; } = new MovementDraft();

        internal MovementPath Snapshot() => Draft.Snapshot();

        // 후보 전체 경로는 Core 검증용이며, 강조에는 확인된 접두 경로만 사용한다.
        internal MovementPath RetainedPath => _retainedPath;

        internal CellCoord? CandidateCell { get; private set; }

        internal long DraftRevision => Draft.Revision;

        // 범위를 기다리는 재진입 칸은 중립으로 표시한다. Release의 이동 자격은 Confirm이 별도로 검사한다.
        internal bool PointerValidForDisplay => Draft.PointerValid || (_previewDeferred && _boardReentryPoint.HasValue);

        internal bool IsActive => Draft.IsActive;

        internal long ValidatedRevision { get; private set; } = -1;

        internal CombatPreviewResult Preview { get; private set; }

        internal bool IsAwaitingPreview => _frozen && !_submitted;

        internal bool IsSubmitted => _submitted;

        internal bool CanShowPointer => !CandidateCell.HasValue || PointerValidForDisplay || _rejectedCell == CandidateCell;

        internal CombatMovementInteraction(CombatMovementQuote quote,
            ICombatStateBufferReadSink combat, float innerCellInset, ICombatActionRejectionSink rejections)
        {
            _quote = quote ?? throw new ArgumentNullException(nameof(quote));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (float.IsNaN(innerCellInset) || innerCellInset < 0 || innerCellInset >= .5f)
                throw new ArgumentOutOfRangeException(nameof(innerCellInset));
            _innerCellInset = innerCellInset;
            // 거절 안내는 경로 입력이 늘 넘깁니다(평가 C5-02).
            _rejections = rejections ?? throw new ArgumentNullException(nameof(rejections));
        }

        internal void Attach()
        {
            Cancel();
            _attached = true;
        }

        internal void Detach()
        {
            _attached = false;
            Cancel();
        }

        internal void FocusLost() => Cancel();

        internal void CaptureLost() => Cancel();

        // 이동 가능 캐릭터 선택과 화면 좌표 변환은 씬 View가 담당하고, 적법성은 Core가 검증한다.
        // allowed는 호출자(CombatPathInput)가 이번 호출에 새로 평가한 입력 허용 판단이다. 저장하지 않는다.
        // 포인터는 행위자 칸이나 상하좌우로 붙은 칸에서 시작할 수 있다. 붙은 칸이면 그 칸이 첫 걸음이 되고 곧바로 견적을 요청한다.
        // 붙은 칸 시작은 칸 경계 떨림으로 시작되지 않도록 안쪽 영역에 들어왔을 때만 받는다.
        internal bool Begin(EntityId actor, CellCoord origin, Vector2 pointerGrid, bool allowed, bool deferPreview = false)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMoveBeginSample = _profileInteractionMoveBegin.Auto();
#endif

            CellCoord start = MovementPointerSampler.CellAt(pointerGrid);
            if (!_attached || !_combat.HasBattle || IsAwaitingPreview || !allowed ||
                !MovementDraft.CanStartAt(origin, start) || actor.IsEmpty ||
                (start != origin && !MovementPointerSampler.IsInside(pointerGrid, _innerCellInset)) ||
                _quote.HasUnhandled)
                return false;

            Cancel();
            _submitted = false;
            _previewDeferred = deferPreview;
            _battle = _combat.BattleInstanceId;
            Draft.Begin(actor, origin);
            _lastPointer = pointerGrid;
            _retainedPath = Draft.Snapshot();
            _retainedAgainst = _retainedPath;
            CandidateCell = start;
            if (start != origin)
            {
                Draft.Accept(start);
                RequestPreview();
            }

            return true;
        }

        internal void PointerMoved(Vector2? grid, bool overCancelArea, bool allowed, in MovementRangeResult range)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMoveDragSample = _profileInteractionMoveDrag.Auto();
#endif

            if (!EnsureLive(allowed) || _frozen || IsSubmitted)
                return;

            if (!grid.HasValue || overCancelArea)
            {
                if (!grid.HasValue && !_awaitingBoardReentry)
                {
                    _awaitingBoardReentry = true;
                    _quote.Clear();
                    Preview = null;
                    ValidatedRevision = -1;
                }
                _boardReentryPoint = null;
                CandidateCell = null;
                _requiresEndpointReentry = true;
                SuspendSampling();
                return;
            }

            var point = grid.Value;
            var cell = MovementPointerSampler.CellAt(point);
            CandidateCell = cell;
            if (_awaitingBoardReentry)
            {
                _boardReentryPoint = MovementPointerSampler.IsInside(point, _innerCellInset) ? point : (Vector2?)null;
                TryResumeOnBoard(range);
                return;
            }
            if (!_lastPointer.HasValue)
            {
                if ((_resumeAdjacent && _rejectedCell == cell) ||
                    !Draft.CanResumeAt(cell, _resumeAdjacent) || !MovementPointerSampler.IsInside(point, _innerCellInset))
                    return;

                Draft.ResumeAt(cell, _resumeAdjacent);
                _lastPointer = point;
                _resumeAdjacent = false;
                _rejectedCell = null;
                _requiresEndpointReentry = false;
                RequestPreview();
                return;
            }

            _crossed.Clear();
            MovementPointerSampler.AppendCrossedCells(_lastPointer.Value, point, _crossed);
            _lastPointer = point;
            foreach (var crossed in _crossed)
            {
                // 아직 수락하지 않은 경계 왕복은 히스테리시스 구간 안에서 상쇄한다.
                var previous = _unaccepted.Count > 1 ? _unaccepted[_unaccepted.Count - 2] : Draft.Endpoint;
                if (_unaccepted.Count > 0 && crossed == previous)
                    _unaccepted.RemoveAt(_unaccepted.Count - 1);
                else if (crossed != (_unaccepted.Count > 0 ? _unaccepted[_unaccepted.Count - 1] : Draft.Endpoint))
                    _unaccepted.Add(crossed);
            }

            bool inside = MovementPointerSampler.IsInside(point, _innerCellInset);
            int acceptCount = inside ? _unaccepted.Count : Math.Max(0, _unaccepted.Count - 1);
            for (int i = 0; i < acceptCount; i++)
            {
                var edit = Draft.Accept(_unaccepted[i]);
                if (edit == DraftEditResult.Added)
                    _extendedRevision = Draft.Revision;
                if (edit == DraftEditResult.Rejected)
                {
                    SuspendSampling();
                    RequestPreview();
                    return;
                }
            }

            if (acceptCount > 0)
                _unaccepted.RemoveRange(0, acceptCount);
            // 칸 경계선 위(안쪽 영역 밖)라 아직 받지 않은 칸은 오류가 아니라 미정이다. 무효로 보이지 않고 받은 끝점을
            // 그대로 가리킨다. 미정 칸이 남아 있는 동안 확정(Confirm)은 막힌다.
            if (_unaccepted.Count > 0)
                CandidateCell = Draft.Endpoint;
            else if (inside && cell == Draft.Endpoint)
                Draft.Accept(cell);
            RequestPreview();
        }

        // 보드 밖 구간은 샘플링하지 않고, 돌아온 칸에서 승인된 범위의 경로로 새로 이어갑니다.
        private bool TryResumeOnBoard(in MovementRangeResult range)
        {
            if (!_boardReentryPoint.HasValue)
                return false;

            var point = _boardReentryPoint.Value;
            var cell = MovementPointerSampler.CellAt(point);
            if (cell == Draft.Snapshot().Origin)
                Draft.ResumeAt(cell, false);
            else if (range.Succeeded && range.TryGetRoute(cell, out var shortest))
                AdoptRangeRoute(shortest);
            else
                return false;

            _awaitingBoardReentry = false;
            _boardReentryPoint = null;
            _requiresEndpointReentry = false;
            _lastPointer = point;
            RequestPreview();
            return true;
        }

        // 범위가 제공한 경로를 즉시 표시하고 실행 승인은 새 Move 견적으로 받습니다.
        private void AdoptRangeRoute(MovementPath shortest)
        {
            Draft.ReplacePath(shortest);
            _retainedPath = shortest;
            _retainedAgainst = shortest;
            _rejectedCell = null;
        }

        // 이번 초안에 응답을 적용했을 때만 true다. Release 뒤에는 같은 응답으로도 확정을 마칠 수 있다.
        internal bool TryApplyPreview(CombatPreviewResult result, bool allowed, in MovementRangeResult range)
        {
            if (!EnsureLive(allowed) || IsSubmitted || !_quote.Matches(result, Draft.Revision))
                return false;
            if (!_frozen && ReferenceEquals(Preview, result))
                return false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePreview = CombatInputProfiler.Apply(CombatViewPreviewKind.Move).Auto();
#endif
            var move = result.Move.Value;
            bool approved = _quote.Accept(result);
            if (!move.CanMove)
            {
                if (move.FailureReason == MovementFailureReason.DestinationOccupied ||
                    move.FailureReason == MovementFailureReason.MoveRangeExceeded)
                {
                    // 도약 등으로 통과할 수 있는 칸은 착지 목록에 없을 수 있다. 수동 경로 전체가 거절된 뒤에만 복구한다.
                    // Release가 먼저 와도 교체 경로의 새 견적을 기다린다.
                    if (Draft.PointerValid && _extendedRevision == Draft.Revision &&
                        range.TryGetRoute(Draft.Endpoint, out var shortest) && !_quote.HasSamePath(shortest))
                    {
                        AdoptRangeRoute(shortest);
                        RequestPreview();
                        return true;
                    }

                    if (!_frozen)
                    {
                        ValidatedRevision = Draft.Revision;
                        Preview = result;
                        _rejectedCell = _quote.Path.Endpoint;
                        return true;
                    }
                }

                bool wasConfirming = _frozen;
                int validSteps = Math.Min(move.ValidStepCount, _quote.Path.StepCount);
                _rejectedCell = validSteps < _quote.Path.StepCount ? _quote.Path.Cells[validSteps + 1] : CandidateCell;
                CandidateCell = _rejectedCell;
                Draft.RetainPrefix(validSteps);
                _retainedPath = Draft.Snapshot();
                _retainedAgainst = _retainedPath;
                ValidatedRevision = Draft.Revision;
                Preview = result;
                _quote.Clear();
                _lastPointer = null;
                _unaccepted.Clear();
                // 불가 후보를 실행하지 않고 끝점에 인접한 다른 후보를 곧바로 받을 수 있다.
                _resumeAdjacent = !_requiresEndpointReentry;
                _frozen = false;
                if (wasConfirming)
                    _rejections.MovementRejected(move.FailureReason);
                return true;
            }

            if (!approved)
            {
                Cancel();
                return true;
            }

            ValidatedRevision = Draft.Revision;
            Preview = result;
            _rejectedCell = null;
            _retainedPath = _quote.Path;
            _retainedAgainst = _quote.Path;
            if (_frozen)
                _submitted = _quote.TrySubmit();
            return true;
        }

        internal void Confirm(CellCoord? cell, bool overCancelArea, bool allowed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMoveConfirmSample = _profileInteractionMoveConfirm.Auto();
#endif

            if (!EnsureLive(allowed) || IsSubmitted || _frozen)
                return;

            if (_awaitingBoardReentry)
            {
                // 범위가 아직 도착하지 않았으면 재진입 후 실제로 놓은 칸만 확정 대상으로 보존합니다.
                _frozen = _previewDeferred && !overCancelArea && _boardReentryPoint.HasValue &&
                    cell == MovementPointerSampler.CellAt(_boardReentryPoint.Value);
                return;
            }

            if (overCancelArea || !cell.HasValue || !Draft.PointerValid || _unaccepted.Count > 0 ||
                Draft.StepCount == 0 || cell.Value != Draft.Endpoint)
            {
                if (!overCancelArea && cell.HasValue && cell == _rejectedCell && Preview?.Move != null)
                    _rejections.MovementRejected(Preview.Move.Value.FailureReason);
                return;
            }

            _frozen = true;
            RequestPreview();
            if (ValidatedRevision == Draft.Revision && _quote.IsApproved)
                _submitted = _quote.TrySubmit();
        }

        internal void Cancel()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMoveCancelSample = _profileInteractionMoveCancel.Auto();
#endif

            _quote.Clear();
            Draft.Cancel();
            _lastPointer = null;
            _unaccepted.Clear();
            Preview = null;
            ValidatedRevision = -1;
            _frozen = false;
            _previewDeferred = false;
            _retainedPath = null;
            _retainedAgainst = null;
            CandidateCell = null;
            _rejectedCell = null;
            _resumeAdjacent = false;
            _requiresEndpointReentry = false;
            _awaitingBoardReentry = false;
            _boardReentryPoint = null;
            _prefixCells.Clear();
            // 이미 제출한 명령은 취소하거나 연출 완료로 간주하지 않는다.
        }

        private void SuspendSampling()
        {
            Draft.Suspend();
            _lastPointer = null;
            _unaccepted.Clear();
            _resumeAdjacent = false;
            _rejectedCell = null;
        }

        // 허용 규칙(연출·미처리 명령·국면)은 호출자가 Policy로 판단해 넘긴다. 여기서는 전투 정체성과 수명만 본다.
        private bool EnsureLive(bool allowed)
        {
            if (!_attached || !Draft.IsActive)
                return false;

            if (!_combat.HasBattle || _combat.BattleInstanceId != _battle || (!IsSubmitted && !allowed))
            {
                Cancel();
                return false;
            }

            return true;
        }

        private void RequestPreview()
        {
            TrimRetainedPath();
            if (!Draft.IsActive || Draft.StepCount == 0)
            {
                _quote.Clear();
                Preview = null;
                ValidatedRevision = -1;
                return;
            }

            if (_previewDeferred || _quote.IsCurrent(Draft.Revision))
                return;

            Preview = null;
            _quote.Request(Draft.Actor, Draft.Snapshot(), Draft.Revision);
        }

        // 범위 응답을 받은 뒤 최신 초안(확정 대기 중이면 동결된 초안) 하나만 게시한다.
        internal void ResumePreview(bool allowed, in MovementRangeResult range)
        {
            if (!_previewDeferred)
                return;

            _previewDeferred = false;
            if (!EnsureLive(allowed) || IsSubmitted)
                return;

            if (_awaitingBoardReentry)
            {
                if (!TryResumeOnBoard(range) && _frozen)
                    Cancel();
                return;
            }

            RequestPreview();
        }

        // 되돌리기/분기에서 이전에 확인한 경로와 공통인 부분만 표시한다.
        // 변경 없는 포인터 입력에서는 불변 Snapshot을 그대로 재사용한다.
        private void TrimRetainedPath()
        {
            if (!Draft.IsActive || _retainedPath == null)
                return;

            MovementPath candidate = Draft.Snapshot();
            if (ReferenceEquals(candidate, _retainedAgainst))
                return;

            _retainedAgainst = candidate;
            int count = 0;
            while (count < candidate.Cells.Count && count < _retainedPath.Cells.Count &&
                candidate.Cells[count] == _retainedPath.Cells[count])
                count++;
            if (count == _retainedPath.Cells.Count)
                return;

            _prefixCells.Clear();
            for (int i = 0; i < count; i++)
                _prefixCells.Add(candidate.Cells[i]);
            _retainedPath = count == candidate.Cells.Count ? candidate : new MovementPath(_prefixCells);
        }
    }
}
