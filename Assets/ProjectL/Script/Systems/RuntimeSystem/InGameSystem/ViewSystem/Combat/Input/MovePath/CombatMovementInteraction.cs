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

        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatViewBufferReadSink _requests;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly float _innerCellInset;
        private readonly ICombatActionRejectionSink _rejections;
        private readonly List<CellCoord> _crossed = new List<CellCoord>();
        private readonly List<CellCoord> _unaccepted = new List<CellCoord>();
        private Vector2? _lastPointer;
        private bool _attached;
        private bool _frozen;
        private bool _previewDeferred;
        private long _lifetime;
        private long _battle;
        private long _requestLifetime;
        private int _requestNumber;
        private int _submittedNumber;
        private long _requestRevision = -1;
        private MovementPath _requestPath;
        private MovementPreviewContext _context;
        private MovementPath _retainedPath;
        private MovementPath _retainedAgainst;
        private readonly List<CellCoord> _prefixCells = new List<CellCoord>();
        private bool _resumeAdjacent;
        private bool _requiresEndpointReentry;
        private CellCoord? _rejectedCell;

        // 경로 편집은 이 입력 소유자가 담당하며 표시 측은 Snapshot과 상태를 읽는다.
        private MovementDraft Draft { get; } = new MovementDraft();

        internal MovementPath Snapshot() => Draft.Snapshot();

        // 후보 전체 경로는 Core 검증용이며, 강조에는 확인된 접두 경로만 사용한다.
        internal MovementPath RetainedPath => _retainedPath;

        internal CellCoord? CandidateCell { get; private set; }

        internal long DraftRevision => Draft.Revision;

        internal bool PointerValid => Draft.PointerValid;

        internal bool IsActive => Draft.IsActive;

        internal long ValidatedRevision { get; private set; } = -1;

        internal CombatPreviewResult Preview { get; private set; }

        internal bool IsAwaitingPreview => _frozen && _submittedNumber == 0;

        internal bool IsSubmitted => _submittedNumber != 0;

        // 재진입 조건을 충족하지 않은 포인터는 숨긴다. Core가 거절한 후보는 무효색으로 남긴다.
        internal bool CanShowPointer => CandidateCell.HasValue &&
            (Draft.PointerValid || _rejectedCell == CandidateCell);

        internal CombatMovementInteraction(ICombatViewBufferWriteSink input, ICombatViewBufferReadSink requests,
            ICombatStateBufferReadSink combat, float innerCellInset, ICombatActionRejectionSink rejections)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
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
            _lifetime++;
            _attached = true;
        }

        internal void Detach()
        {
            _lifetime++;
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
                (_submittedNumber != 0 && _combat.HandledCommandNumber < _submittedNumber))
                return false;

            Cancel();
            _submittedNumber = 0;
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

        internal void PointerMoved(Vector2? grid, bool overCancelArea, bool allowed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMoveDragSample = _profileInteractionMoveDrag.Auto();
#endif

            if (!EnsureLive(allowed) || _frozen || IsSubmitted)
                return;

            if (!grid.HasValue || overCancelArea)
            {
                CandidateCell = null;
                _requiresEndpointReentry = true;
                SuspendSampling();
                return;
            }

            var point = grid.Value;
            var cell = MovementPointerSampler.CellAt(point);
            CandidateCell = cell;
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
                if (Draft.Accept(_unaccepted[i]) == DraftEditResult.Rejected)
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

        internal void OnPreview(CombatPreviewResult result, bool allowed)
        {
            if (!EnsureLive(allowed) || IsSubmitted || result == null || !result.Move.HasValue ||
                result.RequestNumber != _requestNumber || _requestLifetime != _lifetime ||
                _requestRevision != Draft.Revision || !OwnsLatestRequest())
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePreview = CombatInputProfiler.Apply(CombatViewPreviewKind.Move).Auto();
#endif
            var move = result.Move.Value;
            if (!move.CanMove)
            {
                // 정규화 단계의 점유 거절은 유효 접두 길이를 주지 않으므로 거절 위치를 추측하지 않는다.
                // 편집 후보는 남겨 다음 칸까지의 전체 경로를 다시 묻고, 강조는 기존 확인 경로만 유지한다.
                if (!_frozen && move.FailureReason == MovementFailureReason.DestinationOccupied)
                {
                    if (ReferenceEquals(Preview, result))
                        return;

                    ValidatedRevision = Draft.Revision;
                    Preview = result;
                    _context = null;
                    _rejectedCell = _requestPath.Endpoint;
                    // 같은 초안은 다시 게시하지 않도록 요청 소유를 유지한다. 놓은 뒤 같은 거절을
                    // 받으면 위 편집 분기를 건너뛰고 기존 확정 거절 처리로 지정을 끝낸다.
                    return;
                }

                bool wasConfirming = _frozen;
                ClearOwnedPreview();
                int validSteps = Math.Min(move.ValidStepCount, _requestPath.StepCount);
                _rejectedCell = validSteps < _requestPath.StepCount ? _requestPath.Cells[validSteps + 1] : CandidateCell;
                CandidateCell = _rejectedCell;
                Draft.RetainPrefix(validSteps);
                _retainedPath = Draft.Snapshot();
                _retainedAgainst = _retainedPath;
                ValidatedRevision = Draft.Revision;
                Preview = result;
                _context = null;
                _requestNumber = 0;
                _requestPath = null;
                _lastPointer = null;
                _unaccepted.Clear();
                // 불가 후보를 실행하지 않고 끝점에 인접한 다른 후보를 곧바로 받을 수 있다.
                _resumeAdjacent = !_requiresEndpointReentry;
                _frozen = false;
                if (wasConfirming)
                    _rejections.MovementRejected(move.FailureReason);
                return;
            }

            var context = result.MovementContext;
            if (context == null || !context.HasExpectedFacts || context.BattleInstanceId != _battle ||
                context.ActorId != Draft.Actor || context.Observer != Owner.Player || context.IntentKind != CombatCommandKind.Move ||
                !SamePath(context.Path, _requestPath) || move.ValidStepCount != _requestPath.StepCount ||
                move.PathMovement == null || !SamePath(move.PathMovement.RequestedPath, _requestPath))
            {
                Cancel();
                return;
            }

            ValidatedRevision = Draft.Revision;
            Preview = result;
            _context = context;
            _rejectedCell = null;
            _retainedPath = _requestPath;
            _retainedAgainst = _requestPath;
            if (_frozen)
                SubmitFrozen();
        }

        internal void Confirm(CellCoord? cell, bool overCancelArea, bool allowed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMoveConfirmSample = _profileInteractionMoveConfirm.Auto();
#endif

            if (!EnsureLive(allowed) || IsSubmitted || _frozen)
                return;

            if (overCancelArea || !cell.HasValue || !Draft.PointerValid || _unaccepted.Count > 0 ||
                Draft.StepCount == 0 || cell.Value != Draft.Endpoint)
            {
                if (!overCancelArea && cell.HasValue && cell == _rejectedCell && Preview?.Move != null)
                    _rejections.MovementRejected(Preview.Move.Value.FailureReason);
                return;
            }

            _frozen = true;
            RequestPreview();
            if (_context != null && ValidatedRevision == Draft.Revision && OwnsLatestRequest())
                SubmitFrozen();
        }

        internal void Cancel()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMoveCancelSample = _profileInteractionMoveCancel.Auto();
#endif

            ClearOwnedPreview();
            Draft.Cancel();
            _lastPointer = null;
            _unaccepted.Clear();
            _requestNumber = 0;
            _requestRevision = -1;
            _requestPath = null;
            _context = null;
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
                ClearOwnedPreview();
                _requestNumber = 0;
                _context = null;
                Preview = null;
                ValidatedRevision = -1;
                return;
            }

            if (_previewDeferred || (_requestNumber != 0 && _requestRevision == Draft.Revision && OwnsLatestRequest()))
                return;

            _context = null;
            Preview = null;
            _requestPath = Draft.Snapshot();
            _requestRevision = Draft.Revision;
            _requestLifetime = _lifetime;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _requestNumber = CombatInputProfiler.SetPreview(_input, CombatViewPreviewData.Move(Draft.Actor, _requestPath));
#else
            _requestNumber = _input.SetPreview(CombatViewPreviewData.Move(Draft.Actor, _requestPath));
#endif
        }

        // 범위 응답을 받은 뒤 최신 초안(확정 대기 중이면 동결된 초안) 하나만 게시한다.
        internal void ResumePreview(bool allowed)
        {
            if (!_previewDeferred)
                return;

            _previewDeferred = false;
            if (EnsureLive(allowed) && !IsSubmitted)
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

        private bool OwnsLatestRequest() =>
            _requestNumber != 0
            && _requests.HasPreview
            && _requests.Preview.Number == _requestNumber
            && _requests.Preview.Data.Kind == CombatViewPreviewKind.Move
            && _requests.Preview.Data.EntityId == Draft.Actor
            && SamePath(_requests.Preview.Data.Path, _requestPath);

        // 자기 요청이 슬롯에 있을 때만 비웁니다(판단은 Buffer의 번호 있는 비우기, View 2라운드 W5-4).
        private void ClearOwnedPreview() => _input.ClearPreview(_requestNumber);

        private void SubmitFrozen()
        {
            var command = CombatViewCommandData.Move(Draft.Actor, _requestPath, _context);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _submittedNumber = CombatInputProfiler.Submit(_input, command);
#else
            _submittedNumber = _input.Submit(command);
#endif
            ClearOwnedPreview();
        }

        // 경로 Drag 중 매 프레임 불려 열거자 없이 칸을 차례로 비교합니다(GC 끊김 설계 GH4). 뜻은 SequenceEqual과 같습니다.
        private static bool SamePath(MovementPath a, MovementPath b)
        {
            if (a == null || b == null)
                return false;
            IReadOnlyList<CellCoord> left = a.Cells;
            IReadOnlyList<CellCoord> right = b.Cells;
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
                if (!left[i].Equals(right[i]))
                    return false;
            return true;
        }
    }
}
