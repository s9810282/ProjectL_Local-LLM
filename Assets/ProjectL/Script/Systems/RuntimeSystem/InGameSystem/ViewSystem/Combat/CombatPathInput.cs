using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    // 경로 입력과 복귀 요청의 View 수명만 담당한다. 배치·공격 입력과 결과 확인은 담당하지 않는다.
    internal sealed class CombatPathInput
    {
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICombatViewBufferReadSink _input;
        private readonly ICombatViewBufferWriteSink _write;
        private readonly ICombatPointerMapping _mapping;
        private readonly ICombatMovementFeedback _presenter;
        // 입력 가용성·대상·대기·연출 상태를 담은 사실 공급자. 판단할 때마다 새로 읽고 저장하지 않는다.
        private readonly Func<CombatInputFacts> _readFacts;
        private readonly CombatMovementInteraction _interaction;
        private bool _attached = true;
        private bool _returnWanted;
        private bool _draftShown;
        private bool _planningShown;
        private readonly long _battle;
        private int _returnRequest;
        private EntityId _returnActor;
        private MovementPath _shownPath;
        private CombatPreviewResult _shownPreview;
        private bool _shownPointerValid;
        private CellCoord? _shownCandidate;
        private int _draftDisplayVersion;
        private bool _waitingForOrigin;
        private EntityId _pathActor;
        private CellCoord _pathOrigin;

        internal CombatPathInput(ICombatStateBufferReadSink state, ICharacterBufferReadSink characters,
            ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write,
            ICombatPointerMapping mapping, ICombatMovementFeedback presenter, Func<CombatInputFacts> readFacts)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _battle = state.BattleInstanceId;
            _interaction = new CombatMovementInteraction(write, input, state, mapping.InnerCellInset);
            _interaction.Attach();
        }

        // 입구 검사: 이 객체의 수명·전투 일치 → 입력 가용성·대상 → 공통 경로 정책(Policy 한 곳).
        // 외부 표시 콜백 뒤에는 이 함수를 다시 불러 사실을 새로 읽는다.
        private bool AllowsInput()
        {
            if (!_attached || !_state.HasBattle || _state.BattleInstanceId != _battle)
                return false;

            CombatInputFacts facts = _readFacts();
            return facts.IsInputAvailable && facts.IsPathTarget && CombatInputPolicy.CanEditPath(facts);
        }

        internal void Detach()
        {
            if (!_attached)
                return;

            _attached = false;
            _waitingForOrigin = false;
            _pathActor = EntityId.Empty;
            InvalidateDraftDisplay();
            List<Exception> errors = null;
            try
            {
                _interaction.Detach();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            try
            {
                ClearReturnRequest();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            try
            {
                ClearDraft();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            try
            {
                SyncPlanning();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            if (errors != null)
                throw new AggregateException("[CombatPathInput] 이동 입력 정리 중 실패했습니다.", errors);
        }

        internal void OnPreview(CombatPreviewResult preview)
        {
            bool allowed = AllowsInput();
            if (!allowed || !ValidatePlanning(allowed))
            {
                CancelInputs();
                return;
            }

            // 이동 제출이 일어날 수 있으므로 Return 처리는 자기 입구에서 사실을 다시 읽는다.
            _interaction.OnPreview(preview, allowed);
            HandleReturnPreview(preview);
            ShowDraft();
        }

        public void FocusLost() => CancelInputs();

        public void CaptureLost() => CancelInputs();

        private CharacterData ActiveActor() =>
            _state.HasBattle
            && _characters.TryGet(_state.Current.CommandState.ActiveActorId, out var actor)
            && actor.Owner == Owner.Player
            && actor.Area == CharacterRuntimeArea.Field
            && !actor.Health.IsDead
                ? actor
                : null;

        // 이동 모드와 별개로 Q에서 명시한 경로 지정의 수명을 관리한다.
        internal void BeginPlanning()
        {
            if (!_attached)
                return;

            CancelInputs();
            if (!AllowsInput() || _state.Phase != CombatFlowPhase.PlayerInput)
                return;

            var actor = ActiveActor();
            if (actor == null || actor.Stats.MoveMethod == MoveMethod.None)
                return;

            _pathActor = actor.EntityId;
            _pathOrigin = actor.Anchor.Value;
            _waitingForOrigin = true;
            SyncPlanning();
        }

        // 지정 중인 행위자·시작 칸 수명 검사. allowed는 같은 호출에서 이미 평가한 입구 판단이다.
        private bool ValidatePlanning(bool allowed)
        {
            if (_pathActor.IsEmpty)
                return true;

            var actor = ActiveActor();
            return allowed && _state.Phase == CombatFlowPhase.PlayerInput &&
                actor != null && actor.EntityId == _pathActor && actor.Anchor.Value == _pathOrigin;
        }

        public void PointerMoved(float x, float y)
        {
            if (!_attached || _pathActor.IsEmpty)
                return;

            bool allowed = AllowsInput();
            if (!ValidatePlanning(allowed))
            {
                CancelInputs();
                return;
            }

            bool mapped = Map(x, y, out var grid, out var cancel);
            if (_waitingForOrigin)
            {
                if (!mapped || cancel || !MovementDraft.CanStartAt(_pathOrigin, MovementPointerSampler.CellAt(grid)) ||
                    !_interaction.Begin(_pathActor, _pathOrigin, grid, allowed))
                    return;

                _waitingForOrigin = false;
                InvalidateDraftDisplay();
                ShowDraft();
                return;
            }

            _interaction.PointerMoved(mapped ? grid : (Vector2?)null, cancel, allowed);
            ShowDraft();
        }

        public void SelectPressed(float x, float y)
        {
            if (!_attached || _pathActor.IsEmpty || _waitingForOrigin)
                return;

            if (!ValidatePlanning(AllowsInput()))
            {
                CancelInputs();
                return;
            }

            // UI/필드 밖 클릭은 확정하지 않는다. 재진입은 기존 경로 끝점 규칙을 따른다.
            PointerMoved(x, y);
            if (_pathActor.IsEmpty || !Map(x, y, out var grid, out var cancel) || cancel)
                return;

            // 위 PointerMoved가 표시 콜백을 불렀으므로 확정 직전에 사실을 다시 읽는다.
            _interaction.Confirm(MovementPointerSampler.CellAt(grid), false, AllowsInput());
            ShowDraft();
        }

        // 확정은 누름에서만 처리한다. 놓기/드래그 종료로 명령을 제출하지 않는다.
        public void SelectReleased(float x, float y)
        {
        }

        public void SelectCanceled() => CaptureLost();

        public void Cancel()
        {
            if (!_attached)
                return;

            CancelInputs();
            RefreshReturnPreview();
        }

        internal void CancelInputs()
        {
            _waitingForOrigin = false;
            _pathActor = EntityId.Empty;
            InvalidateDraftDisplay();
            _interaction.Cancel();
            ClearReturnRequest();
            ClearDraft();
            SyncPlanning();
        }

        // Q로 경로 지정 중(시작 칸 대기 포함)인지 표시에 알린다. 값이 바뀔 때만 부른다.
        private void SyncPlanning()
        {
            bool planning = _attached && !_pathActor.IsEmpty;
            if (planning == _planningShown)
                return;

            _planningShown = planning;
            _presenter.SetPathPlanning(planning);
        }

        private bool Map(float x, float y, out Vector2 grid, out bool cancel)
        {
            if (!_mapping.TryMapPointer(new Vector2(x, y), out grid, out cancel))
                return false;

            var cell = MovementPointerSampler.CellAt(grid);
            return cell.X >= 0 && cell.Y >= 0 && cell.X < _state.Current.BoardWidth && cell.Y < _state.Current.BoardHeight;
        }

        private void ShowDraft()
        {
            if (_interaction.IsActive && !_interaction.IsSubmitted)
            {
                MovementPath path = _interaction.RetainedPath;
                CellCoord? candidate = _interaction.CandidateCell;
                CombatPreviewResult preview = _interaction.Preview;
                bool pointerValid = _interaction.PointerValid;
                if (ReferenceEquals(_shownPath, path) && ReferenceEquals(_shownPreview, preview) &&
                    _shownPointerValid == pointerValid && _shownCandidate == candidate)
                    return;

                // 표시 실패나 취소·새 표시로 재진입한 뒤에는 이전 호출의 캐시를 복원하지 않는다.
                InvalidateDraftDisplay();
                int version = _draftDisplayVersion;
                _draftShown = true;
                _presenter.ShowDraft(path, preview?.Move, pointerValid, candidate);
                if (_attached && _draftShown && version == _draftDisplayVersion)
                {
                    _shownPath = path;
                    _shownPreview = preview;
                    _shownPointerValid = pointerValid;
                    _shownCandidate = candidate;
                }
            }
            else
            {
                if (!_waitingForOrigin)
                    _pathActor = EntityId.Empty;

                ClearDraft();
                SyncPlanning();
            }
        }

        private void ClearDraft()
        {
            InvalidateDraftDisplay();
            if (!_draftShown)
                return;

            _draftShown = false;
            _presenter.ClearDraft();
        }

        private void InvalidateDraftDisplay()
        {
            _draftDisplayVersion++;
            _shownPath = null;
            _shownPreview = null;
            _shownCandidate = null;
        }

        internal void RefreshReturnPreview()
        {
            bool allowed = AllowsInput();
            if (!ValidatePlanning(allowed))
            {
                CancelInputs();
                return;
            }

            if (!_pathActor.IsEmpty)
                return;
            if (!allowed || _interaction.IsActive || _state.Phase != CombatFlowPhase.PlayerInput)
                return;
            if (_returnRequest != 0 && _input.HasPreview && _input.Preview.Number == _returnRequest)
                return;

            var actor = ActiveActor();
            // 표시 요청만 줄인다. 실제 복귀 가능 여부와 환급은 항상 Core 미리보기가 결정한다.
            if (actor == null || (actor.PendingMoveDistance == 0 && actor.PendingMoveActionCost == 0))
            {
                _presenter.SetReturnState(null);
                return;
            }

            _returnActor = actor.EntityId;
            _returnRequest = _write.SetPreview(CombatViewPreviewData.ReturnToCheckpoint(_returnActor));
        }

        public void RequestReturn()
        {
            if (!AllowsInput() || _returnWanted || _state.Phase != CombatFlowPhase.PlayerInput)
                return;

            var actor = ActiveActor();
            if (actor == null)
                return;

            CancelInputs();
            _returnActor = actor.EntityId;
            _returnWanted = true;
            _returnRequest = _write.SetPreview(CombatViewPreviewData.ReturnToCheckpoint(_returnActor));
        }

        private void HandleReturnPreview(CombatPreviewResult preview)
        {
            if (_returnRequest == 0 || preview.RequestNumber != _returnRequest || preview.CheckpointReturn == null ||
                !_input.HasPreview || _input.Preview.Number != _returnRequest || !AllowsInput())
                return;

            _presenter.SetReturnState(preview.CheckpointReturn);
            if (!_returnWanted)
                return;

            _returnWanted = false;
            var context = preview.MovementContext;
            if (!preview.CheckpointReturn.CanReturn || context == null || !context.HasExpectedFacts ||
                context.BattleInstanceId != _battle || context.ActorId != _returnActor || context.Observer != Owner.Player ||
                context.IntentKind != CombatCommandKind.ReturnToCheckpoint)
                return;

            _write.Submit(CombatViewCommandData.ReturnToCheckpoint(_returnActor, context));
            ClearReturnRequest();
        }

        private void ClearReturnRequest()
        {
            if (_returnRequest != 0 && _input.HasPreview && _input.Preview.Number == _returnRequest)
                _write.ClearPreview();

            _returnRequest = 0;
            _returnActor = EntityId.Empty;
            _returnWanted = false;
            _presenter.SetReturnState(null);
        }
    }
}
