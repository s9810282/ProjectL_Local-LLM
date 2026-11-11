using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath
{
    // 경로 입력·이동 모드 범위의 View 수명만 담당한다. 배치·공격 입력과 결과 확인은 담당하지 않는다.
    // 이동 범위는 이동 모드와 Drag 지정이 같이 쓴다. 수명 규칙 원본은 이동 모드 범위 설계(2026-10-19) MR2~MR4다.
    // 범위를 어떻게 받아 보이는지는 CombatMovementRangeTracker가, 언제·무엇을(원하는 사실·수명 시점·재진입 검사)은 이 클래스가 맡는다
    // (경로 입력 분리 설계 RT1).
    internal sealed class CombatPathInput : ICombatAttackDragTarget, ICombatMoveModeReader
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMovePathEnter = new("Combat.Action.Interaction.MovePath.Enter");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMovePathPointerMoved = new("Combat.Action.Interaction.MovePath.PointerMoved");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMovePathSelect = new("Combat.Action.Interaction.MovePath.Select");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMovePathRelease = new("Combat.Action.Interaction.MovePath.Release");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMovePathCancel = new("Combat.Action.Interaction.MovePath.Cancel");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMovePathExit = new("Combat.Action.Interaction.MovePath.Exit");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionMovePathPoll = new("Combat.Action.Interaction.MovePath.Poll");
#endif

        private readonly ICombatStateBufferReadSink _state;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICombatViewBufferReadSink _input;
        private readonly ICombatViewBufferWriteSink _write;
        private readonly ICombatPointerMapping _mapping;
        private readonly ICombatMovementFeedback _presenter;
        // 입력 가용성·대상·대기·연출 상태를 담은 사실 공급자. 판단할 때마다 새로 읽고 저장하지 않는다.
        private readonly Func<CombatInputFacts> _readFacts;
        // 씬 입력이 소유합니다. 배경 이동 범위보다 먼저 같은 슬롯의 선택 가용성 조회를 맞춥니다.
        private readonly CombatActionAvailabilityTracker _actionAvailability;
        private readonly ICombatActionRejectionSink _rejections;
        private readonly Action _reportBlockedInput;
        private readonly CombatMovementInteraction _interaction;
        private bool _attached = true;
        private bool _draftShown;
        private bool _planningShown;
        private readonly long _battle;
        private bool _waitingForOrigin;
        // Drag를 놓아 확정을 기다리는 중인지입니다. 놓은 뒤에는 포인터 이동을 무시하고 승인 결과만 받습니다(이동 Drag 입력 설계 C2).
        private bool _released;
        private EntityId _pathActor;
        private CellCoord _pathOrigin;
        private int _pathResultSequence;
        private int _planningVersion;
        // 제한 중 누른 아군에서 다른 칸에 놓은 시도만 안내합니다. 단순 정보 선택·호버는 보고하지 않습니다.
        private EntityId _blockedMoveActor;
        private CellCoord _blockedMoveOrigin;
        private int _blockedMoveResultSequence;
        // 이동 범위 하나의 요청·응답·표시 수명입니다(경로 입력 분리 설계 RT2). 이동 모드와 Drag 지정이 같이 씁니다.
        private readonly CombatMovementRangeTracker _range;
        // 이동 입력 정리 단계들입니다. 정리는 매 프레임 불릴 수 있어 생성 때 한 번 만들어 둡니다(GC 끊김 설계 GH4).
        private readonly Action _cancelInteraction;
        private readonly Action _keepRange;
        private readonly Action _clearRange;
        private readonly Action _endPlanning;
        private readonly Action _clearDraft;
        private readonly Action _syncPlanning;
        private const string CleanupMessage = "[CombatPathInput] 이동 입력 정리 중 실패했습니다.";
        private bool _movementShown;
        private Vector2? _movementPointer;

        /// <summary>경로 지정 중(Drag 시작부터 확정·취소까지, 시작 칸 대기 포함)인지입니다. 커서 그리드 스냅의 활성 조건이 읽습니다.</summary>
        internal bool IsPlanning => _attached && !_pathActor.IsEmpty;

        /// <summary>
        /// 연결이 끊겼는지(<see cref="Detach"/> 뒤)입니다. 정지 뒤 다시 부르지 않는 판단은 이 사실 하나로 합니다(View 2라운드 TP-5).
        /// <see cref="Detach"/>가 정리 콜백보다 먼저 세우므로, 정리 안에서 다시 들어와도 같은 뜻입니다.
        /// </summary>
        internal bool IsDetached => !_attached;

        /// <summary>입력 기능 묶음의 공통 수명에 참여하는 창구입니다(View 2라운드 W5-1).</summary>
        internal CombatInputOwner InputOwner { get; }

        /// <summary>
        /// 현재 공개 전투 사실과 맞는 성공한 빈 범위 응답인지입니다. Facts 조립에서도 읽으므로 입력 정책·Facts 공급자를 다시 호출하지 않습니다.
        /// </summary>
        internal bool HasCurrentAcceptedEmptyMovementRange(EntityId selectedActor)
        {
            CharacterData actor = _attached && _state.HasBattle && _state.BattleInstanceId == _battle
                ? FindActor(selectedActor)
                : null;
            // 필드 유닛의 칸·방향은 Core 사실이 보장합니다(CharacterCombatFacts 위치 검사, 평가 C5-02). FindActor는 필드 유닛만 돌려줍니다.
            CombatMovementRangeFacts facts = actor != null ? RangeFactsOf(actor) : default;
            return _range.HasAcceptedEmptyRange(facts);
        }

        internal CombatPathInput(ICombatStateBufferReadSink state, ICharacterBufferReadSink characters,
            ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write,
            ICombatPointerMapping mapping, ICombatMovementFeedback presenter, Func<CombatInputFacts> readFacts,
            ICombatActionRejectionSink rejections,
            CombatActionAvailabilityTracker actionAvailability, Action reportBlockedInput)
        {
            // 거절 안내·가용성 추적은 운영 조립이 늘 넘깁니다(평가 C5-02). 빠지면 사전 거절이 조용히 사라지므로 생성 때 막습니다.
            _actionAvailability = actionAvailability ?? throw new ArgumentNullException(nameof(actionAvailability));
            _rejections = rejections ?? throw new ArgumentNullException(nameof(rejections));
            _reportBlockedInput = reportBlockedInput ?? throw new ArgumentNullException(nameof(reportBlockedInput));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _battle = state.BattleInstanceId;
            _range = new CombatMovementRangeTracker(input, write, presenter);
            _interaction = new CombatMovementInteraction(write, input, state, mapping.InnerCellInset, rejections);
            _interaction.Attach();
            _cancelInteraction = _interaction.Cancel;
            _keepRange = KeepRangeForMoveMode;
            _clearRange = _range.Clear;
            _endPlanning = EndMovementPlanning;
            _clearDraft = ClearDraft;
            _syncPlanning = SyncPlanning;
            InputOwner = new OwnerAdapter(this);
        }

        // 입구 검사: 이 객체의 수명·전투 일치 → 입력 가용성·대상 → 공통 경로 정책(Policy 한 곳).
        // 외부 표시 콜백 뒤에는 이 함수를 다시 불러 사실을 새로 읽는다.
        private bool AllowsInput()
        {
            return AllowsInput(CombatInputRouter.PointerTarget.Path);
        }

        private bool AllowsInput(CombatInputRouter.PointerTarget expectedTarget)
        {
            if (!_attached || !_state.HasBattle || _state.BattleInstanceId != _battle)
                return false;

            CombatInputFacts facts = _readFacts();
            return facts.IsInputAvailable
                && facts.PointerTarget == expectedTarget
                && CombatInputPolicy.CanEditPath(facts);
        }

        // 포인터 대상이 경로 입력인지는 사실에 담긴 대상 하나로 본다. 그 대상은 입력 기능 묶음 한 곳이 판정식으로 정한다(평가 C4-13).
        private CharacterData PlanningActor(CombatInputRouter.PointerTarget expectedTarget)
        {
            if (!AllowsInput(expectedTarget) || _state.Phase != CombatFlowPhase.PlayerInput)
                return null;
            var actor = ActiveActor();
            return actor != null && actor.Stats.MoveMethod != MoveMethod.None ? actor : null;
        }

        internal bool TryGetAttackDragOrigin(float x, float y, out CombatAttackDragGesture.Origin origin)
        {
            origin = default;
            int version = _planningVersion;
            if (!_pathActor.IsEmpty || !Map(x, y, out var grid, out var cancel) || cancel)
                return false;
            var actor = PlanningActor(CombatInputRouter.PointerTarget.Attack);
            if (version != _planningVersion || actor == null
                || actor.Anchor != MovementPointerSampler.CellAt(grid))
                return false;
            // 이동할 수 없는 행위자(이동한 뒤·이동 AP 부족)는 이동으로 넘기지 않습니다. 넘기면 공격 모드만 끝나고 이동은 거절됩니다.
            // 판정은 Core 가용성 응답 하나입니다. 운영 씬은 응답이 있어야 공격 모드에 들어가므로 여기서는 응답이 와 있습니다.
            CombatActionAvailability availability = _actionAvailability.Read(_readFacts());
            if (availability != null && !availability.CanMove)
                return false;
            origin = new CombatAttackDragGesture.Origin(_battle, actor.EntityId, actor.Anchor.Value,
                _state.LatestResultSequence, new Vector2(x, y));
            return true;
        }

        internal bool IsAttackDragOriginCurrent(in CombatAttackDragGesture.Origin origin,
            CombatInputRouter.PointerTarget expectedTarget)
        {
            var actor = PlanningActor(expectedTarget);
            return actor != null
                && origin.BattleId == _battle && actor.EntityId == origin.ActorId
                && actor.Anchor == origin.Anchor && _state.LatestResultSequence == origin.ResultSequence;
        }

        internal bool TryBeginTransferredDrag(in CombatAttackDragGesture.Origin origin)
        {
            if (!_pathActor.IsEmpty || !IsAttackDragOriginCurrent(origin, CombatInputRouter.PointerTarget.Path)
                || !TryBeginPlanning(origin))
                return false;
            int version = _planningVersion;
            PointerMoved(origin.PressedScreen.x, origin.PressedScreen.y);
            return version == _planningVersion && IsPlanning
                && IsAttackDragOriginCurrent(origin, CombatInputRouter.PointerTarget.Path);
        }

        internal void Detach()
        {
            if (!_attached)
                return;

            _attached = false;
            _blockedMoveActor = EntityId.Empty;
            _waitingForOrigin = false;
            _released = false;
            _pathActor = EntityId.Empty;
            _planningVersion++;
            _range.InvalidateAcceptedRange();
            RunCleanup(_interaction.Detach, _range.Clear, EndMovementPlanning, ClearDraft, SyncPlanning);
        }

        internal void OnPreview(CombatPreviewResult preview)
        {
            // Poll은 새 응답이 없어도 마지막 응답을 전달합니다. 새 Preview를 내지 않는 금지 시도를 그 응답으로 취소하지 않습니다.
            if (!_blockedMoveActor.IsEmpty && IsBlockedMoveOriginCurrent(_readFacts(), _blockedMoveActor))
                return;
            bool allowed = AllowsInput();
            if (!allowed || !ValidatePlanning(allowed))
            {
                CancelInputs();
                return;
            }

            // 지정 중이 아닌 이동 모드 범위 요청이 슬롯을 잃었으면 내려놓고 다른 응답(복귀 등)을 처리한다(이동 모드 범위 설계 MR4).
            // 지정 중이면 위 ValidatePlanning이 슬롯 소유를 이미 확인했다.
            _range.DropIfSlotLost();

            if (_range.IsPending)
            {
                ConsumeMovementRange(preview);
                return;
            }

            _interaction.OnPreview(preview, allowed);
            // 놓은 뒤 승인이 "이동 불가"로 와 확정이 풀렸으면 앞부분으로 이어 그리지 않고 취소한다(M3).
            if (_released && _interaction.IsActive && !_interaction.IsAwaitingPreview && !_interaction.IsSubmitted)
            {
                CancelInputs();
                return;
            }
            ShowDraft();
        }

        public void FocusLost() => CancelInputs();

        public void CaptureLost() => CancelInputs();

        private CharacterData ActiveActor() => FindActor(_readFacts().SelectedActorId);

        private CharacterData FindActor(EntityId selectedActor) =>
            _state.HasBattle
            && _characters.TryGet(selectedActor, out var actor)
            && actor.Owner == Owner.Player
            && actor.Area == CharacterRuntimeArea.Field
            && !actor.Health.IsDead
                ? actor
                : null;

        // 경로 지정의 시작이다. Drag 누름(SelectPressed)이 부른다(이동 Drag 입력 설계). 범위를 요청하고 시작 칸을 기다린다.
        internal void BeginPlanning()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMovePathEnterSample = _profileInteractionMovePathEnter.Auto();
#endif

            TryBeginPlanning(null);
        }

        private bool TryBeginPlanning(CombatAttackDragGesture.Origin? requiredOrigin)
        {
            if (!_attached)
                return false;

            int version = _planningVersion;
            CancelInputs();
            if (_planningVersion != version + 1 || !_pathActor.IsEmpty)
                return false;
            version = _planningVersion;
            var actor = PlanningActor(CombatInputRouter.PointerTarget.Path);
            if (actor == null || (requiredOrigin.HasValue
                && !IsAttackDragOriginCurrent(requiredOrigin.Value, CombatInputRouter.PointerTarget.Path)))
                return false;

            // 현재 Core 응답이 있을 때만 사전 거절합니다. Unknown은 기존 범위/상세 승인 경로를 유지합니다.
            CombatActionAvailability availability = _actionAvailability.Read(_readFacts());
            if (availability != null && !availability.CanMove)
            {
                _rejections.MovementRejected(availability.MoveFailure);
                return false;
            }

            // 이동 모드에서 같은 사실로 보이거나 기다리는 범위는 그대로 쓴다(이동 모드 범위 설계 MR3). 다르면 지우고 새로 요청한다.
            CombatMovementRangeFacts facts = RangeFactsOf(actor);
            bool reuse = _range.Matches(facts);
            if (!reuse)
            {
                _range.Clear();
                if (!_attached || version != _planningVersion || !_pathActor.IsEmpty)
                    return false;
            }

            _pathActor = actor.EntityId;
            _pathOrigin = actor.Anchor.Value;
            _released = false;
            _pathResultSequence = _state.LatestResultSequence;
            _waitingForOrigin = true;
            if (!reuse)
                _range.Request(facts);
            SyncPlanning();
            return _attached && version == _planningVersion && !_pathActor.IsEmpty;
        }

        // 지금 보여야 할 이동 범위의 사실입니다(이동 모드 범위 설계 MR2). 지정 중이면 그 지정, 아니면 이동 모드 행위자입니다. 없으면 false입니다.
        private bool TryGetWantedRangeFacts(out CombatMovementRangeFacts facts)
        {
            if (!_pathActor.IsEmpty)
            {
                facts = CurrentRangeFacts();
                return true;
            }

            return TryGetMoveModeRangeFacts(out facts);
        }

        // 이동 모드(행위자 칸을 눌러 Drag를 시작할 수 있는 상태)면 그 행위자의 범위 사실입니다. 조건은 Drag 시작과 같은 PlanningActor 하나입니다.
        private bool TryGetMoveModeRangeFacts(out CombatMovementRangeFacts facts)
        {
            var actor = PlanningActor(CombatInputRouter.PointerTarget.Path);
            facts = actor != null ? RangeFactsOf(actor) : default;
            return actor != null;
        }

        private CombatMovementRangeFacts RangeFactsOf(CharacterData actor) =>
            new CombatMovementRangeFacts(_state.BattleInstanceId, actor.EntityId, actor.Anchor.Value, _state.LatestResultSequence);

        // 슬롯이 한가한지입니다(배경 요청 규칙 R-a). 답을 받지 않은 남의 요청이 있으면 배경 요청은 기다립니다.
        private bool IsPreviewSlotIdle() => CombatPreviewSlotRule.IsIdle(
            _input.HasPreview, _input.HasPreview ? _input.Preview.Number : 0, _state.AnsweredPreviewNumber);

        // 지정 중인 행위자·시작 칸 수명 검사. allowed는 같은 호출에서 이미 평가한 입구 판단이다.
        private bool ValidatePlanning(bool allowed)
        {
            if (_pathActor.IsEmpty)
                return true;

            var actor = ActiveActor();
            return allowed && _state.Phase == CombatFlowPhase.PlayerInput &&
                actor != null && actor.EntityId == _pathActor && actor.Anchor.Value == _pathOrigin &&
                _state.LatestResultSequence == _pathResultSequence &&
                (!_range.IsPending || _range.Matches(CurrentRangeFacts()));
        }

        private CombatMovementRangeFacts CurrentRangeFacts() =>
            new CombatMovementRangeFacts(_state.BattleInstanceId, _pathActor, _pathOrigin, _state.LatestResultSequence);

        private void ConsumeMovementRange(CombatPreviewResult preview)
        {
            // 이동 모드 범위면 지금 이동 모드의 사실로, 지정 중이면 지정의 사실로 대조한다(MR2).
            if (!TryGetWantedRangeFacts(out CombatMovementRangeFacts facts))
                return;

            // 소유자가 자기 응답만 소비하고 자기 슬롯을 비운 뒤 보인다. 외부 표시 콜백 뒤에는 새 지정 수명을 다시 확인한다.
            int version = _planningVersion;
            try
            {
                if (!_range.TryShowAnswer(preview, facts))
                    return;
            }
            catch (Exception displayError)
            {
                try
                {
                    // 표시가 실패한 범위는 이동 모드에도 남기지 않는다.
                    if (version == _planningVersion)
                        LeaveMoveMode();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(displayError, cleanupError);
                }
                throw;
            }

            if (!_attached || version != _planningVersion || _pathActor.IsEmpty)
                return;
            bool allowed = AllowsInput();
            if (!ValidatePlanning(allowed))
            {
                CancelInputs();
                return;
            }
            _interaction.ResumePreview(allowed);
            ShowDraft();
        }

        public void PointerMoved(float x, float y)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMovePathPointerMovedSample = _profileInteractionMovePathPointerMoved.Auto();
#endif

            if (!_attached || _pathActor.IsEmpty || _released)
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
                    !_interaction.Begin(_pathActor, _pathOrigin, grid, allowed, _range.IsPending))
                    return;

                _waitingForOrigin = false;
                _movementPointer = new Vector2(x, y);
                var actor = ActiveActor();
                int version = _planningVersion;
                _movementShown = true;
                try
                {
                    _presenter.BeginMovementPlanning(actor.EntityId, actor.CharacterKey, actor.Rotation.Value);
                }
                catch (Exception displayError)
                {
                    try { if (version == _planningVersion) LeaveMoveMode(); }
                    catch (Exception cleanupError) { throw new AggregateException(displayError, cleanupError); }
                    throw;
                }
                if (!_attached || version != _planningVersion || _pathActor.IsEmpty)
                    return;
                ShowDraft();
                return;
            }

            bool frozen = _interaction.IsAwaitingPreview;
            _interaction.PointerMoved(mapped ? grid : (Vector2?)null, cancel, allowed);
            if (!frozen)
                _movementPointer = mapped && !cancel ? new Vector2(x, y) : (Vector2?)null;
            ShowDraft();
        }

        /// <summary>
        /// Drag 시작입니다(이동 Drag 입력 설계 §3·C1). 지정 중이 아니고 누른 칸이 활성 아군 행위자의 앵커일 때만
        /// 경로 지정을 시작하고(범위 요청 포함) 같은 좌표로 시작 칸을 처리합니다. 그 밖의 누름은 무시합니다(M2).
        /// 지정 중(놓은 뒤 승인 대기 포함)의 새 누름도 무시합니다.
        /// </summary>
        public void SelectPressed(float x, float y)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMovePathSelectSample = _profileInteractionMovePathSelect.Auto();
#endif

            _blockedMoveActor = EntityId.Empty;
            if (!_attached || !_pathActor.IsEmpty)
                return;
            if (!Map(x, y, out var grid, out var cancel) || cancel)
                return;

            var actor = ActiveActor();
            if (actor == null || actor.Anchor != MovementPointerSampler.CellAt(grid))
                return;

            CombatInputFacts facts = _readFacts();
            if (IsTutorialMoveBlocked(facts))
            {
                _blockedMoveActor = actor.EntityId;
                _blockedMoveOrigin = actor.Anchor.Value;
                _blockedMoveResultSequence = _state.LatestResultSequence;
                return;
            }

            // 허용 판단·행위자 조건은 BeginPlanning이 기존 입구 검사로 한다.
            if (!TryBeginPlanning(null))
                return;

            PointerMoved(x, y);
        }

        /// <summary>
        /// Drag 종료입니다(이동 Drag 입력 설계 §3·M3). 놓은 칸을 끝점으로 확정하고, 확정이 성립하지 않으면
        /// (시작 칸·필드 밖·UI·미정 칸·끝점 아님) 지정을 취소해 이동하지 않습니다. 성립하면 승인을 기다린 뒤 제출합니다.
        /// </summary>
        public void SelectReleased(float x, float y)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMovePathReleaseSample = _profileInteractionMovePathRelease.Auto();
#endif

            if (!_blockedMoveActor.IsEmpty)
            {
                ReportBlockedMoveRelease(x, y);
                return;
            }
            if (!_attached || _pathActor.IsEmpty || _released)
                return;
            if (_waitingForOrigin || !ValidatePlanning(AllowsInput()))
            {
                CancelInputs();
                return;
            }

            // 놓은 위치까지 경로에 반영한 뒤 놓은 상태로 바꾼다. 이후 포인터 이동은 무시한다(C2).
            PointerMoved(x, y);
            if (_pathActor.IsEmpty)
                return;

            _released = true;
            bool mapped = Map(x, y, out var grid, out var cancel);
            // 위 PointerMoved가 표시 콜백을 불렀으므로 확정 직전에 사실을 다시 읽는다.
            _interaction.Confirm(mapped && !cancel ? MovementPointerSampler.CellAt(grid) : (CellCoord?)null, cancel, AllowsInput());
            if (!_interaction.IsAwaitingPreview && !_interaction.IsSubmitted)
            {
                CancelInputs();
                return;
            }

            ShowDraft();
        }

        private bool IsTutorialMoveBlocked(in CombatInputFacts facts) =>
            _attached && _state.HasBattle && _state.BattleInstanceId == _battle
                && facts.PointerTarget == CombatInputRouter.PointerTarget.Path
                && facts.IsTutorialPracticeGoalPending && CombatInputPolicy.CanSelectUnit(facts)
                && !CombatInputPolicy.AllowsTutorialCommand(facts, CombatViewCommandKind.Move);

        private bool IsBlockedMoveOriginCurrent(in CombatInputFacts facts, EntityId actor) =>
            IsTutorialMoveBlocked(facts) && facts.SelectedActorId == actor
                && _state.LatestResultSequence == _blockedMoveResultSequence;

        private void ReportBlockedMoveRelease(float x, float y)
        {
            EntityId actor = _blockedMoveActor;
            _blockedMoveActor = EntityId.Empty;
            CombatInputFacts facts = _readFacts();
            if (!IsBlockedMoveOriginCurrent(facts, actor))
                return;

            // 같은 칸 클릭·필드 밖·UI 위 놓기는 이동 시도가 아닙니다. 수명 정리/중복 release도 알림을 재사용하지 않습니다.
            if (Map(x, y, out var grid, out var cancel) && !cancel
                && MovementPointerSampler.CellAt(grid) != _blockedMoveOrigin)
                _reportBlockedInput();
        }

        public void Cancel()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMovePathCancelSample = _profileInteractionMovePathCancel.Auto();
#endif

            if (!_attached)
                return;

            CancelInputs();
            Refresh();
        }

        /// <summary>
        /// 경로 지정·복귀 요청을 취소합니다. 이동 범위는 이동 모드가 같은 사실로 이어지면 남기고 아니면 지웁니다(새로 요청하지 않음, 이동 모드 범위 설계 MR3).
        /// </summary>
        internal void CancelInputs() => CancelInputs(keepMoveModeRange: true);

        /// <summary>
        /// 이동 모드를 끝냅니다: 경로 지정·복귀 요청을 취소하고 이동 범위도 지웁니다(이동 모드 범위 설계 MR3).
        /// 공격 모드 진입·턴 종료처럼 새 의도를 시작하기 전에 부릅니다. 의도가 거절되면 다음 <see cref="Refresh"/>가 범위를 다시 맞춥니다.
        /// </summary>
        internal void LeaveMoveMode()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMovePathExitSample = _profileInteractionMovePathExit.Auto();
#endif
            CancelInputs(keepMoveModeRange: false);
        }

        private void CancelInputs(bool keepMoveModeRange)
        {
            _blockedMoveActor = EntityId.Empty;
            _waitingForOrigin = false;
            _released = false;
            _pathActor = EntityId.Empty;
            _planningVersion++;
            int version = _planningVersion;
            // 정리 안에서 다시 들어온 취소가 판 번호를 올렸으면 남은 단계는 그 취소에 맡깁니다(단계마다 부르기 직전에 봄).
            // 경로 입력이 허용되지 않는 동안 매 프레임 불리므로 람다·배열 없이 미리 만든 대리자를 차례로 부릅니다
            // (GC 끊김 설계 GH4). 순서·실패 모으기·문구는 그대로입니다.
            List<Exception> errors = null;
            CleanupErrors.Try(_cancelInteraction, ref errors);
            if (version == _planningVersion)
                CleanupErrors.Try(keepMoveModeRange ? _keepRange : _clearRange, ref errors);
            if (version == _planningVersion)
                CleanupErrors.Try(_endPlanning, ref errors);
            if (version == _planningVersion)
                CleanupErrors.Try(_clearDraft, ref errors);
            if (version == _planningVersion)
                CleanupErrors.Try(_syncPlanning, ref errors);
            CleanupErrors.ThrowIfAny(errors, CleanupMessage);
        }

        private static void RunCleanup(params Action[] actions) =>
            CleanupErrors.RunAll(CleanupMessage, actions);

        // 지정 취소 뒤에도 이동 모드가 같은 사실로 이어지면 범위(보이는 것·기다리는 요청)를 남기고, 아니면 지운다. 새로 요청하지 않는다(MR3).
        private void KeepRangeForMoveMode()
        {
            if (_range.IsEmpty)
                return;
            if (TryGetMoveModeRangeFacts(out CombatMovementRangeFacts facts) && _range.Matches(facts))
                return;

            _range.Clear();
        }

        private void EndMovementPlanning()
        {
            _movementPointer = null;
            if (!_movementShown) return;
            _movementShown = false;
            _presenter.EndMovementPlanning();
        }

        // 경로 지정 중(Drag 중, 시작 칸 대기 포함)인지 표시에 알린다. 값이 바뀔 때만 부른다.
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
                // 커서 추적을 먼저 하고, 그 표시 콜백 뒤에는 지정 세대를 다시 본다.
                int versionBeforePointer = _planningVersion;
                CombatPreviewResult quote = _interaction.Preview;
                CombatHoverState tint = !_interaction.PointerValid ? CombatHoverState.Invalid
                    : quote?.Move == null || _interaction.ValidatedRevision != _interaction.DraftRevision ? CombatHoverState.Neutral
                    : quote.Move.Value.CanMove ? CombatHoverState.Valid : CombatHoverState.Invalid;
                try
                {
                    _presenter.UpdateMovementPointer(_interaction.CanShowPointer ? _movementPointer : null,
                        tint == CombatHoverState.Neutral ? (bool?)null : tint == CombatHoverState.Valid,
                        _interaction.IsAwaitingPreview);
                }
                catch (Exception displayError)
                {
                    try { if (versionBeforePointer == _planningVersion) LeaveMoveMode(); }
                    catch (Exception cleanupError) { throw new AggregateException(displayError, cleanupError); }
                    throw;
                }
                if (!_attached || versionBeforePointer != _planningVersion || !_interaction.IsActive || _interaction.IsSubmitted)
                    return;

                // 표시는 같은 값을 다시 받아도 화면이 바뀌지 않으므로(멱등) 입력은 무엇을 그렸는지 기억하지 않고 매번 보낸다.
                // 그려진 상태의 원본은 표시 쪽 하나다(경로 입력 분리 설계 RT8).
                _draftShown = true;
                _presenter.ShowDraft(_interaction.RetainedPath, _interaction.Preview?.Move, _interaction.PointerValid,
                    _interaction.CandidateCell);
            }
            else
            {
                if (!_waitingForOrigin && !_pathActor.IsEmpty)
                    CancelInputs();
                else
                {
                    ClearDraft();
                    SyncPlanning();
                }
            }
        }

        // 그린 초안이 있을 때만 지운다. 다른 기능이 쓰는 호버 칸을 지우지 않으려는 소유 표지다(RT8).
        private void ClearDraft()
        {
            if (!_draftShown)
                return;

            _draftShown = false;
            _presenter.ClearDraft();
        }

        /// <summary>
        /// 현재 사실로 경로 입력을 맞춥니다: ① 경로 지정 수명 감시(<see cref="WatchPlanning"/>) → ② 이동 모드 범위(<see cref="RefreshMoveModeRange"/>)
        /// 매 Tick(<see cref="CombatInteraction.Tick"/>)과 결과 확인 뒤(<see cref="CombatInteraction.AfterAcknowledged"/>), 경로 취소 뒤에 부릅니다.
        /// </summary>
        internal void Refresh()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionMovePathPollSample = _profileInteractionMovePathPoll.Auto();
#endif

            bool allowed = AllowsInput();
            if (!WatchPlanning(allowed))
                return;

            RefreshMoveModeRange();
        }

        /// <summary>
        /// 지정 중이 아닐 때 이동 모드 범위를 지금 사실에 맞춥니다(이동 모드 범위 설계 MR3·MR4). 이동 모드가 아니면 지우고,
        /// 같은 사실로 보이거나 기다리는 중이면 그대로 두고, 다르면 지운 뒤 새로 요청합니다. 같은 사실로 받은 범위는 슬롯을 잃어도 다시 요청하지 않습니다.
        /// 슬롯에 답을 받지 않은 남의 요청(복귀 버튼 응답 대기·호버 조회 등)이 있으면 요청만 미룹니다. 지정 중 범위는 지정 수명이 맡습니다.
        /// 두 판단은 배경 요청 규칙(<see cref="CombatPreviewSlotRule"/>, 호버 공격 범위 설계 UR4) R-b·R-a입니다.
        /// </summary>
        private void RefreshMoveModeRange()
        {
            if (!_pathActor.IsEmpty)
                return;
            if (!TryGetMoveModeRangeFacts(out CombatMovementRangeFacts facts))
            {
                _range.Clear();
                return;
            }
            if (_range.Matches(facts))
                return;

            int version = _planningVersion;
            _range.Clear();
            if (!_attached || version != _planningVersion || !_pathActor.IsEmpty)
                return;
            // Cancel/Q/확인 뒤 Refresh에서도 선택 가용성 → 배경 이동 범위 순서를 유지합니다.
            // 지정 중 범위는 이 메서드를 거치지 않아 Pending 가용성을 바로 선점할 수 있습니다.
            _actionAvailability.Sync(_readFacts(), _characters.Characters, false, _state.AnsweredPreviewNumber);
            if (!_attached || version != _planningVersion || !_pathActor.IsEmpty || !IsPreviewSlotIdle())
                return;
            _range.Request(facts);
        }

        /// <summary>
        /// 경로 지정 중이면 입력 허용·국면·행위자·시작 칸·결과 번호·범위 요청이 아직 맞는지 보고(<see cref="ValidatePlanning"/>),
        /// 깨졌으면 입력을 취소합니다. 지정 중이 아니면 언제나 계속합니다. 계속해도 되면 true입니다.
        /// </summary>
        private bool WatchPlanning(bool allowed)
        {
            if (ValidatePlanning(allowed))
                return true;

            CancelInputs();
            return false;
        }

        // 공격 Drag 넘겨받기(CombatAttackDragHandover)가 보는 좁은 계약입니다(View 2라운드 W5-5).
        // 출발점 셋·CancelInputs·IsDetached는 internal이라 명시적 구현으로 넘깁니다(계획 검증 F5).
        // public인 PointerMoved·SelectReleased도 계약 전체가 한 곳에 보이도록 같은 꼴로 넘깁니다.
        bool ICombatAttackDragTarget.IsDetached => IsDetached;

        // 유닛 호버가 보는 좁은 계약입니다(호버 공격 범위 설계 §9). 조건은 이동 모드 범위·Drag 시작과 같은 PlanningActor 하나입니다.
        EntityId ICombatMoveModeReader.MoveModeActorId =>
            PlanningActor(CombatInputRouter.PointerTarget.Path)?.EntityId ?? EntityId.Empty;

        bool ICombatAttackDragTarget.TryGetAttackDragOrigin(float x, float y, out CombatAttackDragGesture.Origin origin) =>
            TryGetAttackDragOrigin(x, y, out origin);

        bool ICombatAttackDragTarget.IsAttackDragOriginCurrent(in CombatAttackDragGesture.Origin origin,
            CombatInputRouter.PointerTarget expectedTarget) => IsAttackDragOriginCurrent(origin, expectedTarget);

        bool ICombatAttackDragTarget.TryBeginTransferredDrag(in CombatAttackDragGesture.Origin origin) =>
            TryBeginTransferredDrag(origin);

        void ICombatAttackDragTarget.PointerMoved(float screenX, float screenY) => PointerMoved(screenX, screenY);

        void ICombatAttackDragTarget.SelectReleased(float screenX, float screenY) => SelectReleased(screenX, screenY);

        void ICombatAttackDragTarget.CancelInputs() => CancelInputs();

        /// <summary>
        /// 공통 수명의 단계마다 경로 입력의 어느 메서드인지만 답합니다. 경로는 표현 정리(<see cref="CombatInputOwner.CancelPresentation"/>)에서 빠집니다.
        /// 연결이 끊긴 뒤에는 모든 단계를 무시합니다(지금까지 묶음이 경로 참조를 놓던 것과 같은 뜻, View 2라운드 TP-5).
        /// </summary>
        private sealed class OwnerAdapter : CombatInputOwner
        {
            private readonly CombatPathInput _path;

            internal OwnerAdapter(CombatPathInput path) => _path = path;

            internal override CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.Path;
            internal override bool IsActive => _path.IsPlanning;

            internal override void PointerMoved(in CombatPointerInput input)
            {
                if (!_path.IsDetached) _path.PointerMoved(input.ScreenX, input.ScreenY);
            }

            internal override void SelectPressed(in CombatPointerInput input)
            {
                if (!_path.IsDetached) _path.SelectPressed(input.ScreenX, input.ScreenY);
            }

            internal override void SelectReleased(in CombatPointerInput input)
            {
                if (!_path.IsDetached) _path.SelectReleased(input.ScreenX, input.ScreenY);
            }

            internal override void SelectCanceled() => CancelInputs();

            /// <summary>
            /// 매 Tick 감시입니다. 마지막 응답을 넘긴 뒤 이동 모드 범위·복귀 견적을 맞춥니다(경로의 응답 소비·수명 감시·복귀 재요청).
            /// 응답 처리 안에서 정지가 다시 들어와 연결이 끊겼으면 끊긴 경로를 다시 부르지 않습니다.
            /// </summary>
            internal override void Poll(CombatPreviewResult latestPreview)
            {
                if (_path.IsDetached) return;
                if (latestPreview != null)
                    _path.OnPreview(latestPreview);
                if (!_path.IsDetached)
                    _path.Refresh();
            }

            internal override void BeforeResult() => CancelInputs();

            internal override void AfterAcknowledged()
            {
                if (!_path.IsDetached) _path.Refresh();
            }

            internal override void CancelInputs()
            {
                if (!_path.IsDetached) _path.CancelInputs();
            }

            internal override void Stop() => _path.Detach();
        }
    }
}
