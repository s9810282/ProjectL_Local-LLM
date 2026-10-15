using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 이동 경로 초안의 편집·Core 견적 요청·확정 제출을 맡습니다. 허용 판단은 호출자가 넘기고,
    /// 전투·요청 번호·Core context 일치 같은 수명·정체성 검사만 직접 합니다.
    /// </summary>
    internal sealed class CombatMovementInteraction
    {
        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatViewBufferReadSink _requests;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly float _innerCellInset;
        private readonly List<CellCoord> _crossed = new List<CellCoord>();
        private readonly List<CellCoord> _unaccepted = new List<CellCoord>();
        private Vector2? _lastPointer;
        private bool _attached;
        private bool _frozen;
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

        internal CombatMovementInteraction(ICombatViewBufferWriteSink input, ICombatViewBufferReadSink requests,
            ICombatStateBufferReadSink combat, float innerCellInset)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (float.IsNaN(innerCellInset) || innerCellInset < 0 || innerCellInset >= .5f)
                throw new ArgumentOutOfRangeException(nameof(innerCellInset));
            _innerCellInset = innerCellInset;
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
        internal bool Begin(EntityId actor, CellCoord origin, Vector2 pointerGrid, bool allowed)
        {
            CellCoord start = MovementPointerSampler.CellAt(pointerGrid);
            if (!_attached || !_combat.HasBattle || IsAwaitingPreview || !allowed ||
                !MovementDraft.CanStartAt(origin, start) || actor.IsEmpty ||
                (start != origin && !MovementPointerSampler.IsInside(pointerGrid, _innerCellInset)) ||
                (_submittedNumber != 0 && _combat.HandledCommandNumber < _submittedNumber))
                return false;

            Cancel();
            _submittedNumber = 0;
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

            var move = result.Move.Value;
            if (!move.CanMove)
            {
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
            _retainedPath = _requestPath;
            _retainedAgainst = _requestPath;
            if (_frozen)
                SubmitFrozen();
        }

        internal void Confirm(CellCoord? cell, bool overCancelArea, bool allowed)
        {
            if (!EnsureLive(allowed) || IsSubmitted || _frozen)
                return;

            if (overCancelArea || !cell.HasValue || !Draft.PointerValid || _unaccepted.Count > 0 ||
                Draft.StepCount == 0 || cell.Value != Draft.Endpoint)
                return;

            _frozen = true;
            RequestPreview();
            if (_context != null && ValidatedRevision == Draft.Revision && OwnsLatestRequest())
                SubmitFrozen();
        }

        internal void Cancel()
        {
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

            if (_requestNumber != 0 && _requestRevision == Draft.Revision && OwnsLatestRequest())
                return;

            _context = null;
            Preview = null;
            _requestPath = Draft.Snapshot();
            _requestRevision = Draft.Revision;
            _requestLifetime = _lifetime;
            _requestNumber = _input.SetPreview(CombatViewPreviewData.Move(Draft.Actor, _requestPath));
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

        private void ClearOwnedPreview()
        {
            if (_requestNumber != 0 && _requests.HasPreview && _requests.Preview.Number == _requestNumber)
                _input.ClearPreview();
        }

        private void SubmitFrozen()
        {
            var command = CombatViewCommandData.Move(Draft.Actor, _requestPath, _context);
            _submittedNumber = _input.Submit(command);
            ClearOwnedPreview();
        }

        private static bool SamePath(MovementPath a, MovementPath b) =>
            a != null && b != null && a.Cells.SequenceEqual(b.Cells);
    }
}
