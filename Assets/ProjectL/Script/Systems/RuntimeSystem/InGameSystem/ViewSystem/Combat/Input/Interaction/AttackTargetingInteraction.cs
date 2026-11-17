#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 전투 단계의 공격 조준 입력입니다(설계 §7·§21.1, W21, 계획 E2, 구조 개선 VR3).
        /// 공격 모드·행위자, 공격 범위 응답의 조준 목록, 공격 호버와 그 미리보기 요청 번호를 소유합니다.
    /// </summary>
    /// <remarks>
    /// 조준은 Core 공격 범위 응답의 조준 목록에서만 고르며 그 종류(칸·방향·전체)로 입력 방식을 정합니다. 유효 범위는 계산하지 않습니다.
    /// 범위 응답은 이후 호버 요청이 Buffer의 요청을 바꾸므로 모드 동안만 보관하고, 결과 후처리·모드 종료에서 버립니다.
    /// 명령은 Gate로 제출합니다. 미리보기는 Buffer의 최신 요청이 자기 번호일 때만 비웁니다.
    /// </remarks>
    internal sealed class AttackTargetingInteraction
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackEnter = new("Combat.Action.Interaction.Attack.Enter");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackAim = new("Combat.Action.Interaction.Attack.Aim");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackCancel = new("Combat.Action.Interaction.Attack.Cancel");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackRelease = new("Combat.Action.Interaction.Attack.Release");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackConfirm = new("Combat.Action.Interaction.Attack.Confirm");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackExit = new("Combat.Action.Interaction.Attack.Exit");
#endif

        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatViewBufferReadSink _requests;
        private readonly ICombatPointerPicker _picker;
        // 공격 범위·영향·막힘·체력 계약입니다. 호버 칸은 호버 추적이 따로 받습니다(씬 표시 나누기 설계 DS2, 결정 Q-DS2 A).
        private readonly ICombatAttackRangeFeedback _feedback;
        private readonly CombatCommandGate _gate;
        private readonly CombatHoverTracker _hover;
        private readonly ICombatActionRejectionSink _rejections;
        // 공격 진입 가능 여부(Ready.CanAttack)와 진입 거절 이유를 읽는 가용성 조회입니다. 필수입니다(평가 C5-02).
        private readonly CombatActionAvailabilityTracker _actionAvailability;
        private AttackFailureReason? _rangeFailure;
        // 궤적 표시 의도입니다. 필수이고, 보일지는 조준 종류가 정합니다(ShowsTrajectory, 평가 C5-02).
        private readonly ICombatAttackTrajectoryFeedback _trajectory;
        // 넉백 미리보기 표시 의도입니다. 필수입니다(넉백 미리보기 결정 Q-KB4 A).
        private readonly ICombatKnockbackPreviewFeedback _knockback;
        // 공격 모드 대상(즉시 처치 대상) 표시 의도입니다. 범위 응답 동안 보이고 모드 끝·결과 재생 직전에 지웁니다(이펙트 끝 규칙 계획 TP-E16, 결정 Q-FS4 A — 호버 중 유지).
        private readonly ICombatAttackModeTargets _modeTargets;

        private bool _active;
        private int _modeGeneration;
        private EntityId _actor;
        // 취소 효과음 창구입니다(View 3라운드 W9-2 검토). 필수입니다(평가 C5-02).
        private readonly CombatInputSounds _sounds;
        private long _requestBattleId;
        private long _requestTurnSequence;

        // 조준 모드의 범위 응답(모드 수명 동안의 입력·표시 캐시, 원본은 Core Buffer)
        private int _rangeRequestNumber;
        private bool _rangeAnswered;
        private AttackAim[] _attackAims = Array.Empty<AttackAim>();
        private readonly Dictionary<CellCoord, List<AttackBlockedContact>> _rangeBlockers = new();

        // 호버 칸에 대한 마지막 공격 미리보기 요청 번호
        private int _hoverRequestNumber;

        // 목록에 없는 칸을 눌렀을 때 그 이유를 묻는 미리보기 요청 번호입니다(하붑 View 계획 Q-HP1 A). 답이 오면 안내만 하고 호버 표시는 건드리지 않습니다.
        private int _rejectionRequestNumber;

        // 이 모드가 낸 공격 명령 번호와, 그 명령이 수락돼 결과 후처리에서 모드를 끝낼지입니다(2026-10-28 사용자 "공격 후에는 공격 모드를 자동으로 풀어줘").
        // 공격한 유닛은 더 행동할 수 없어서(Core 행동 규칙) 모드를 남기면 다른 칸 누름이 공격 거절 안내가 됩니다.
        private int _submittedAttackNumber;
        private bool _attackAccepted;

        // 조준 호버 그림의 지우기 목록 둘입니다(평가 C1-31). 새 호버 그림은 맞는 목록 한 곳에만 더하면 호버 표시 지우기(ClearHoverDisplay)·
        // 결과 재생 직전(ClearShotPreview)·모드 정리(ResetRangeAnswer)가 함께 따릅니다. 지울 때마다 대리자를 만들지 않도록 생성자에서 한 번 만듭니다.
        // 결과 후처리까지 남는 그림(영향 칸·막힌 벽)
        private readonly Action[] _hoverClears;
        // 결과 재생 직전에도 지우는 그림(넉백·궤적)
        private readonly Action[] _shotPreviewClears;

        internal AttackTargetingInteraction(
            ICombatViewBufferWriteSink input,
            ICombatViewBufferReadSink requests,
            ICombatPointerPicker picker,
            ICombatAttackRangeFeedback feedback,
            ICombatHoverCellFeedback hover,
            CombatCommandGate gate, ICombatKnockbackPreviewFeedback knockback, ICombatAttackModeTargets modeTargets, CombatInputSounds sounds,
            ICombatActionRejectionSink rejections,
            ICombatAttackTrajectoryFeedback trajectory, CombatActionAvailabilityTracker actionAvailability)
        {
            _actionAvailability = actionAvailability ?? throw new ArgumentNullException(nameof(actionAvailability));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _knockback = knockback ?? throw new ArgumentNullException(nameof(knockback));
            _modeTargets = modeTargets ?? throw new ArgumentNullException(nameof(modeTargets));
            _hover = new CombatHoverTracker(hover, CombatHoverKind.Attack);
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
            _rejections = rejections ?? throw new ArgumentNullException(nameof(rejections));
            _trajectory = trajectory ?? throw new ArgumentNullException(nameof(trajectory));
            _hoverClears = new Action[] { _feedback.ClearAffectedCells, _feedback.ClearAttackBlockers };
            _shotPreviewClears = new Action[] { _knockback.ClearKnockbackPreview, ClearTrajectory };
            InputOwner = new OwnerAdapter(this);
        }

        /// <summary>입력 기능 묶음의 공통 수명에 참여하는 창구입니다(View 2라운드 W5-1).</summary>
        internal CombatInputOwner InputOwner { get; }

        /// <summary>공격 모드인지입니다.</summary>
        internal bool IsActive => _active;

        /// <summary>현재 모드의 범위 응답을 받았는지입니다. 받기 전에는 호버 요청과 기록을 하지 않습니다.</summary>
        internal bool HasRangeAnswer => _rangeAnswered;

        /// <summary>
        /// 공격 모드에 들어갈 수 있는지입니다. 현재 키의 Ready.CanAttack도 요구합니다.
        /// 들어갈 수 있을 때만 호출자가 다른 소유자의 표시를 정리한 뒤 <see cref="Enter"/>를 부릅니다.
        /// </summary>
        internal bool CanEnter(in CombatInputFacts facts, out EntityId actor)
        {
            actor = ResolveActor(facts);
            return !actor.IsEmpty && CombatInputPolicy.CanPreview(facts, _gate.HasPending, CombatViewPreviewKind.AttackRange)
                && _actionAvailability.Read(facts)?.CanAttack == true;
        }

        /// <summary>실제 진입 시도에서만 현재 Core 불가 이유를 알립니다. 버튼 갱신/대기 응답에서는 호출하지 않습니다.</summary>
        internal void NotifyEntryRejected(in CombatInputFacts facts)
        {
            if (ResolveActor(facts).IsEmpty || !CombatInputPolicy.CanPreview(facts, _gate.HasPending, CombatViewPreviewKind.AttackRange))
                return;
            CombatActionAvailability availability = _actionAvailability.Read(facts);
            if (availability != null && !availability.CanAttack)
                _rejections.AttackRejected(availability.AttackFailure);
        }

        /// <summary>이전 공격 모드를 정리하고 새 행위자로 모드에 들어가 범위를 요청합니다.</summary>
        internal void Enter(in CombatInputFacts facts, EntityId actor)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackEnterSample = _profileInteractionAttackEnter.Auto();
#endif

            int generation = ++_modeGeneration;
            Exit(generation);
            if (generation != _modeGeneration)
                return;
            _hover.Clear();
            if (generation != _modeGeneration)
                return;
            _active = true;
            _actor = actor;
            _submittedAttackNumber = 0;
            _attackAccepted = false;
            _requestBattleId = facts.BattleInstanceId;
            _requestTurnSequence = facts.ControllerTurnSequence;
            RequestRange(facts);
        }

        internal void PointerMoved(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackAimSample = _profileInteractionAttackAim.Auto();
#endif

            if (!_active)
                return;

            if (!CanShowHealthPreview(facts))
                _feedback.ClearAttackHealthPreview();

            if (!_picker.TryPickCell(screenX, screenY, out CellCoord cell))
            {
                _hover.Clear();
                ClearHoverDisplay();
                InvalidateHoverPreview();
                return;
            }

            if (_hover.HasHover && _hover.Cell == cell)
                return;

            _hover.Set(cell, CombatHoverState.Neutral);
            RequestHoverForCell(facts, characters, cell);
        }

        internal void Cancel(IReadOnlyList<CharacterData> characters)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackCancelSample = _profileInteractionAttackCancel.Auto();
#endif

            // 부르는 곳은 Buffer 캐릭터 목록을 늘 넘깁니다(평가 C5-02).
            var selected = !_active ? null : CombatCharacterQueries.Find(characters, _actor);
            if (selected != null)
                _sounds.Play(UnitEvent.UnitCancel, _sounds.PresetOf(selected));
            Exit();
        }

        internal void SelectReleased(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackReleaseSample = _profileInteractionAttackRelease.Auto();
#endif

            if (_active && _picker.TryPickCell(screenX, screenY, out CellCoord cell))
                SubmitTarget(facts, characters, cell);
            else if (_active && _rangeAnswered)
                NotifyRejectedTarget(facts);
        }

        /// <summary>
        /// 자기 요청의 응답이면 반영하고 true입니다. 범위 응답이면 조준 목록·범위을 보관·표시하고,
        /// 호버 요청의 응답이면 가능 여부·영향 칸을 표시합니다. 거절 응답은 표시만 비웁니다(정상 Core 결과).
        /// </summary>
        internal bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            CombatPreviewResult answer = facts.PreviewAnswer;
            if (!_active || answer == null || !_requests.HasPreview || _requests.Preview.Number != answer.RequestNumber)
                return false;

            if (!_rangeAnswered && _rangeRequestNumber != 0 && answer.RequestNumber == _rangeRequestNumber)
            {
                ApplyRangeAnswer(answer);
                // 범위를 받기 전부터 올려 둔 칸이 있으면 이제 그 칸을 요청한다.
                if (_hover.HasHover)
                    RequestHoverForCell(facts, characters, _hover.Cell);
                return true;
            }

            if (_rejectionRequestNumber != 0 && answer.RequestNumber == _rejectionRequestNumber)
            {
                ApplyRejectionAnswer(facts, answer);
                return true;
            }

            if (_hoverRequestNumber == 0 || answer.RequestNumber != _hoverRequestNumber)
                return false;

            if (_hover.HasHover && answer.Attack.HasValue && answer.Attack.Value.AttackerId == _actor)
                ApplyAttackAnswer(facts, answer.Attack.Value);
            else
            {
                ClearHoverDisplay();
                _hover.SetStateIfHovered(CombatHoverState.Invalid);
                InvalidateHoverPreview();
            }
            return true;
        }

        /// <summary>
        /// 결과 재생 전에 상세 응답과 예상 체력만 무효화하고 모드·범위·명령을 유지합니다.
        /// 궤적·넉백 미리보기는 지웁니다(<see cref="ClearShotPreview"/>) — 실제 발사체·밀림 재생 위에 겹치지 않게 합니다.
        /// 공격 모드 대상 표시도 지웁니다. 모드가 남으면 결과 후처리의 새 범위 응답이 다시 보입니다.
        /// </summary>
        internal void BeforeResult()
        {
            if (!_active)
                return;
            InvalidateHoverPreview();
            ClearShotPreview();
            _modeTargets.ClearAttackModeTargets();
        }

        /// <summary>
        /// 결과를 그린 직후(후처리 전) 명령 결과를 받습니다. 이 모드가 낸 공격 명령이 수락됐으면 후처리(<see cref="AfterResult"/>)에서 모드를 끝내도록 표시합니다.
        /// 거절이면 그 번호만 잊고 모드를 남겨 다시 고를 수 있게 합니다. 다른 결과는 무시합니다.
        /// </summary>
        internal void CommandResolved(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!_active || _submittedAttackNumber == 0
                || result.Kind != CombatProcessingKind.Command || result.CommandNumber != _submittedAttackNumber)
                return;
            _submittedAttackNumber = 0;
            _attackAccepted = result.Accepted;
        }

        /// <summary>
        /// 결과 후처리: 이 모드의 공격이 수락됐거나 행위자·Layer가 맞지 않으면 모드를 끝내고, 남은 모드는 보관한 범위 응답을 버리고 새 번호로 다시 요청합니다.
        /// 이전 행위자로는 재요청하지 않습니다.
        /// </summary>
        internal void AfterResult(in CombatInputFacts facts)
        {
            if (!_active)
                return;

            if (_attackAccepted || !IsModeStillValid(facts))
            {
                Exit();
                return;
            }

            ClearOwnedPreview();
            _hoverRequestNumber = 0;
            _rejectionRequestNumber = 0;
            ResetRangeAnswer();
            _hover.SetStateIfHovered(CombatHoverState.Neutral);
            RequestRange(facts);
        }

        /// <summary>공격 모드·범위·영향 칸·공격 호버·자기 미리보기를 정리합니다. 모드가 아니면 아무것도 하지 않습니다.</summary>
        internal void Exit()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackExitSample = _profileInteractionAttackExit.Auto();
#endif

            Exit(++_modeGeneration);
        }

        private void Exit(int generation)
        {
            if (!_active)
                return;

            _active = false;
            _actor = EntityId.Empty;
            _submittedAttackNumber = 0;
            _attackAccepted = false;
            // 외부 표시 콜백 전에 옛 요청을 놓습니다. 콜백에서 열린 새 공격 수명은 옛 정리가 건드리지 않습니다.
            ClearOwnedPreview();
            _rangeRequestNumber = 0;
            _hoverRequestNumber = 0;
            _rejectionRequestNumber = 0;
            CleanupErrors.RunAll("[AttackTargetingInteraction] 공격 정리 중 실패했습니다.",
                () => ResetRangeAnswer(generation),
                () =>
                {
                    if (generation == _modeGeneration)
                        _hover.Clear();
                });
        }

        /// <summary>현재 모드의 범위를 요청하고 번호를 기억합니다. 응답을 받기 전까지 호버 요청·기록을 하지 않습니다.</summary>
        private void RequestRange(in CombatInputFacts facts)
        {
            CombatViewPreviewData range = CombatViewPreviewData.AttackRange(_actor);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _rangeRequestNumber = CombatInputPolicy.CanPreview(facts, _gate.HasPending, range.Kind)
                ? CombatInputProfiler.SetPreview(_input, range)
                : 0;
#else
            _rangeRequestNumber = CombatInputPolicy.CanPreview(facts, _gate.HasPending, range.Kind)
                ? _input.SetPreview(range)
                : 0;
#endif
        }

        private void ApplyRangeAnswer(CombatPreviewResult answer)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePreview = CombatInputProfiler.Apply(CombatViewPreviewKind.AttackRange).Auto();
#endif

            _rangeAnswered = true;
            _rangeFailure = answer.Range?.FailureReason;
            _rangeBlockers.Clear();
            if (answer.Range.HasValue && answer.Range.Value.Succeeded)
            {
                AttackRangeResult range = answer.Range.Value;
                _attackAims = range.AttackAims.ToArray();
                foreach (var contact in range.BlockedContacts)
                {
                    if (!_rangeBlockers.TryGetValue(contact.Contact, out var contacts))
                        _rangeBlockers.Add(contact.Contact, contacts = new List<AttackBlockedContact>());
                    contacts.Add(contact);
                }
                _feedback.ShowRangeCells(range.RangeCells.ToArray(), range.BasicAttackCells.ToArray());
                // 즉시 처치 대상의 판정 원본은 Core 범위 응답입니다. 그대로 넘기고, 무엇을 낼지는 특성 외형이 정합니다(마무리 사격 FS-b).
                _modeTargets.ShowAttackModeTargets(_actor, range.InstantDefeatTargetIds);
                return;
            }

            // 범위 응답이 실패면 고를 수 있는 것이 없다. 표시만 비운다.
            _attackAims = Array.Empty<AttackAim>();
            _feedback.ClearRangeCells();
            _modeTargets.ClearAttackModeTargets();
        }

        private void ApplyAttackAnswer(in CombatInputFacts facts, AttackPreview attack)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePreview = CombatInputProfiler.Apply(CombatViewPreviewKind.Attack).Auto();
#endif

            if (attack.CanAttack)
            {
                var cells = attack.AffectedCells.ToArray();
                // 연쇄 폭발이 맞히는 칸도 영향 칸으로 보입니다. 범위는 Core가 폭발마다 주는 칸 그대로입니다(가시 폭탄 Phase 4 D4).
                if (attack.ThornBombExplosions.Count > 0)
                    cells = cells.Concat(attack.ThornBombExplosions.SelectMany(explosion => explosion.BlastCells)).Distinct().ToArray();
                _feedback.ShowAffectedCells(cells, attack.Pull, closeOutline: attack.ProjectedCategory.Key == CategoryKey.Cannon);
            }
            else
                _feedback.ClearAffectedCells();

            _hover.SetStateIfHovered(attack.CanAttack ? CombatHoverState.Valid : CombatHoverState.Invalid);
            _feedback.ShowAttackBlockers(attack.BlockedContacts);

            // 궤적: 가능 응답은 끝까지, 불가 응답은 막힘 증거가 있을 때만 막힘으로(공격 궤적 설계 TJ5). 끊는 위치·모양은 표시가 정한다.
            if (!ShowsTrajectory)
                ClearTrajectory();
            else if (attack.CanAttack)
                _trajectory.ShowAttackTrajectory(_actor, attack);
            else if (attack.BlockedContacts.Count > 0)
                _trajectory.ShowBlockedAttackTrajectory(_actor, _hover.Cell, attack.BlockedContacts);
            else
                ClearTrajectory();

            // 결과 예상(체력·넉백)은 같은 조건 하나로 보입니다. 결과 재생 중에도 미리보기 답은 오므로, 재생 중에는 그리지 않아
            // 움직이는 유닛 위에 화살표가 겹치지 않게 합니다(넉백 미리보기 결정 Q-KBb1 A). 넉백은 이 요청을 보낼 때 이미 지웠습니다.
            if (attack.CanAttack && CanShowHealthPreview(facts))
            {
                _feedback.ShowAttackHealthPreview(attack);
                _knockback.ShowKnockbackPreview(attack);
            }
            else
                InvalidateHoverPreview();
        }

        /// <summary>범위 응답을 받은 뒤에만 호버 칸의 공격 미리보기를 요청합니다. 목록에 없으면 불가로 표시합니다.</summary>
        private void RequestHoverForCell(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            InvalidateHoverPreview();
            ClearHoverDisplay();
            // 범위 응답을 받은 뒤에만 고를 수 있다.
            if (!_rangeAnswered)
                return;

            // 불가 조준은 상세 미리보기를 요청하지 않는다. 범위 응답의 해당 접촉 증거만 표시한다(막힌 궤적 포함).
            if (_rangeBlockers.TryGetValue(cell, out var contacts))
            {
                _feedback.ShowAttackBlockers(contacts);
                if (ShowsTrajectory)
                    _trajectory.ShowBlockedAttackTrajectory(_actor, cell, contacts);
            }

            AttackAim? aim = AimFor(characters, cell);
            if (!aim.HasValue)
            {
                _hover.SetStateIfHovered(CombatHoverState.Invalid);
                return;
            }

            CombatViewPreviewData preview = CombatViewPreviewData.Attack(_actor, aim.Value);
            if (CombatInputPolicy.CanPreview(facts, _gate.HasPending, preview.Kind))
            {
                DropRejectionRequest(facts);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _hoverRequestNumber = CombatInputProfiler.SetPreview(_input, preview);
#else
                _hoverRequestNumber = _input.SetPreview(preview);
#endif
            }
        }

        private void SubmitTarget(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackConfirmSample = _profileInteractionAttackConfirm.Auto();
#endif

            // 범위 응답을 받기 전에는 무엇을 고를 수 있는지 모르므로 기록하지 않는다.
            if (_actor.IsEmpty || !_rangeAnswered)
            {
                if (!CanShowHealthPreview(facts))
                    _feedback.ClearAttackHealthPreview();
                return;
            }

            AttackAim? aim = AimFor(characters, cell);
            if (aim.HasValue)
            {
                int number = _gate.TrySubmit(facts, CombatViewCommandData.Attack(_actor, aim.Value, _requestBattleId, _requestTurnSequence));
                if (number > 0)
                {
                    _submittedAttackNumber = number;
                    InvalidateHoverPreview();
                }
                else if (!CanShowHealthPreview(facts))
                {
                    _feedback.ClearAttackHealthPreview();
                }
            }
            else
            {
                InvalidateHoverPreview();
                if (!TryRequestRejectionReason(facts, cell))
                    NotifyRejectedTarget(facts);
            }
        }

        /// <summary>
        /// 칸 조준에서 목록에 없는 칸을 눌렀을 때 그 칸의 공격 미리보기로 이유를 한 번 묻습니다(하붑 View 계획 Q-HP1 A). Core가 막힌 칸을 범위와 목록에서 빼서,
        /// 묻지 않으면 일반 문구만 나옵니다. 조준 목록이 비었으면 종류를 알 수 없지만, 구름 때문에 범위가 모두 막힌 경우이므로 칸으로 묻습니다(다른 종류면 Core가 다른 이유로 답합니다).
        /// 물을 수 없으면 false이고 부르는 쪽이 바로 일반 안내를 냅니다.
        /// </summary>
        private bool TryRequestRejectionReason(in CombatInputFacts facts, CellCoord cell)
        {
            bool asksCell = _attackAims.Length == 0 ? _rangeFailure == AttackFailureReason.None : _attackAims[0].Kind == AttackAimKind.Cell;
            if (!asksCell)
                return false;

            CombatViewPreviewData preview = CombatViewPreviewData.Attack(_actor, AttackAim.ForCell(cell));
            if (!CombatInputPolicy.CanPreview(facts, _gate.HasPending, preview.Kind))
                return false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _rejectionRequestNumber = CombatInputProfiler.SetPreview(_input, preview);
#else
            _rejectionRequestNumber = _input.SetPreview(preview);
#endif
            return true;
        }

        /// <summary>
        /// 답을 받기 전의 이유 요청을 호버 요청이 덮기 직전에 놓습니다(슬롯에는 요청이 하나뿐이라 답이 오지 않습니다). 클릭이 조용히 지나가지 않게 일반 안내를 냅니다.
        /// </summary>
        private void DropRejectionRequest(in CombatInputFacts facts)
        {
            if (_rejectionRequestNumber == 0)
                return;
            _rejectionRequestNumber = 0;
            NotifyRejectedTarget(facts);
        }

        /// <summary>
        /// 이유 요청의 답입니다. 하붑으로 막혔으면 그 안내를, 다른 이유면 지금까지처럼 일반 안내를 냅니다. 사거리 밖처럼 안내 표가 모르는 이유가 조용히 지나가지 않게 합니다.
        /// </summary>
        private void ApplyRejectionAnswer(in CombatInputFacts facts, CombatPreviewResult answer)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var previewSample = CombatInputProfiler.Apply(CombatViewPreviewKind.Attack).Auto();
#endif

            _rejectionRequestNumber = 0;
            _input.ClearPreview(answer.RequestNumber);
            bool haboob = answer.Attack.HasValue && answer.Attack.Value.AttackerId == _actor
                && answer.Attack.Value.FailureReason == AttackFailureReason.HaboobDesignationBlocked;
            if (!haboob)
                NotifyRejectedTarget(facts);
            else if (CombatInputPolicy.CanSubmit(facts, _gate.HasPending, CombatViewCommandKind.Attack))
                _rejections.AttackRejected(AttackFailureReason.HaboobDesignationBlocked);
        }

        private void NotifyRejectedTarget(in CombatInputFacts facts)
        {
            if (_rangeFailure.HasValue && CombatInputPolicy.CanSubmit(facts, _gate.HasPending, CombatViewCommandKind.Attack))
                _rejections.AttackRejected(_rangeFailure.Value != AttackFailureReason.None ? _rangeFailure.Value :
                    AttackFailureReason.InvalidAttackAim);
        }

        /// <summary>
        /// 공격 범위 응답의 조준 목록에서 칸에 맞는 조준을 고릅니다(설계 §21.1).
        /// 칸 조준이면 같은 칸, 방향 조준이면 행위자 → 칸 각도에 가장 가까운 목록 방향, 전체 조준이면 그 하나입니다.
        /// 목록에 맞는 것이 없으면 null입니다.
        /// </summary>
        private AttackAim? AimFor(IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            if (_attackAims.Length == 0)
                return null;

            switch (_attackAims[0].Kind)
            {
                case AttackAimKind.All:
                    return _attackAims[0];
                case AttackAimKind.Cell:
                    for (int i = 0; i < _attackAims.Length; i++)
                    {
                        if (_attackAims[i].Kind == AttackAimKind.Cell && _attackAims[i].Cell == cell)
                            return _attackAims[i];
                    }

                    return null;
                case AttackAimKind.Direction:
                    CharacterData actor = CombatCharacterQueries.Find(characters, _actor);
                    return actor?.Anchor is CellCoord anchor ? NearestDirectionAim(anchor, cell) : null;
                default:
                    return null;
            }
        }

        private AttackAim? NearestDirectionAim(CellCoord origin, CellCoord cell)
        {
            int dx = cell.X - origin.X;
            int dy = cell.Y - origin.Y;
            if (dx == 0 && dy == 0)
                return null;

            double pointer = Math.Atan2(dy, dx);
            AttackAim? best = null;
            double bestDelta = double.MaxValue;
            for (int i = 0; i < _attackAims.Length; i++)
            {
                AttackAim aim = _attackAims[i];
                if (aim.Kind != AttackAimKind.Direction || !TryGetDirectionVector(aim.Direction, out int vx, out int vy))
                    continue;

                double delta = Math.Abs(Math.Atan2(vy, vx) - pointer);
                if (delta > Math.PI)
                    delta = (2.0 * Math.PI) - delta;
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = aim;
                }
            }

            return best;
        }

        /// <summary>Core의 방향 정의와 같은 칸 변화입니다(Up = +Y, AppointShapeCalculator).</summary>
        private static bool TryGetDirectionVector(AttackDirection direction, out int x, out int y)
        {
            switch (direction)
            {
                case AttackDirection.Up: x = 0; y = 1; return true;
                case AttackDirection.UpRight: x = 1; y = 1; return true;
                case AttackDirection.Right: x = 1; y = 0; return true;
                case AttackDirection.DownRight: x = 1; y = -1; return true;
                case AttackDirection.Down: x = 0; y = -1; return true;
                case AttackDirection.DownLeft: x = -1; y = -1; return true;
                case AttackDirection.Left: x = -1; y = 0; return true;
                case AttackDirection.UpLeft: x = -1; y = 1; return true;
                default: x = 0; y = 0; return false;
            }
        }

        private void ResetRangeAnswer() => ResetRangeAnswer(_modeGeneration);

        private void ResetRangeAnswer(int generation)
        {
            _rangeAnswered = false;
            _rangeFailure = null;
            _attackAims = Array.Empty<AttackAim>();
            _rangeBlockers.Clear();
            // 그림마다 실패를 떼어 두고(하나가 실패해도 나머지를 시도), 그림마다 세대를 다시 봅니다(정리 콜백 안에서 열린 새 모드의 표시는 건드리지 않음).
            // 호버 그림은 그 목록 한 곳을 돕니다(평가 C1-31).
            List<Exception> errors = null;
            ClearIfSameMode(generation, _feedback.ClearRangeCells, ref errors);
            for (int i = 0; i < _hoverClears.Length; i++)
                ClearIfSameMode(generation, _hoverClears[i], ref errors);
            for (int i = 0; i < _shotPreviewClears.Length; i++)
                ClearIfSameMode(generation, _shotPreviewClears[i], ref errors);
            ClearIfSameMode(generation, _modeTargets.ClearAttackModeTargets, ref errors);
            ClearIfSameMode(generation, _feedback.ClearAttackHealthPreview, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[AttackTargetingInteraction] 공격 표시 정리 중 실패했습니다.");
        }

        /// <summary>모드 정리의 그림 하나를 지웁니다. 세대가 그대로일 때만 지우고, 실패는 <paramref name="errors"/>에 모읍니다.</summary>
        private void ClearIfSameMode(int generation, Action clear, ref List<Exception> errors)
        {
            if (generation == _modeGeneration)
                CleanupErrors.Try(clear, ref errors);
        }

        /// <summary>
        /// 지금 조준 종류가 궤적을 보이는지입니다. 칸·방향 조준은 보이고 전체 조준은 보이지 않습니다(공격 궤적 설계 TJ4, 결정 Q-T1 A).
        /// 이 규칙의 원본은 이곳 한 곳입니다. 조준 목록은 범위 응답 하나에서 같은 종류만 옵니다.
        /// </summary>
        private bool ShowsTrajectory => _attackAims.Length > 0 && _attackAims[0].Kind != AttackAimKind.All;

        private void ClearTrajectory() => _trajectory.ClearAttackTrajectory();

        /// <summary>
        /// 호버 칸 하나에 대한 그림(영향 칸·막힌 벽·넉백·궤적)을 지웁니다(공격 궤적 구현 평가 P-V2).
        /// 지울 그림은 생성자의 두 목록 한 곳이 정하고, 모드 정리(<see cref="ResetRangeAnswer(int)"/>)도 같은 목록을 돕니다(평가 C1-31).
        /// </summary>
        private void ClearHoverDisplay()
        {
            RunClears(_hoverClears);
            ClearShotPreview();
        }

        /// <summary>
        /// 결과 재생 직전에도 지우는 호버 그림(넉백·궤적)입니다. 실제 재생과 겹치지 않게 합니다(공격 궤적 계획 P8, 넉백 미리보기 결정 Q-KB3 A).
        /// 영향 칸·막힌 벽은 결과 후처리까지 남습니다. 호버 표시 지우기와 결과 재생 직전이 같이 부릅니다.
        /// </summary>
        private void ClearShotPreview() => RunClears(_shotPreviewClears);

        /// <summary>목록을 차례로 지웁니다. 하나가 실패하면 그 자리에서 던집니다(그림마다 실패를 떼어 두는 것은 모드 정리뿐입니다).</summary>
        private static void RunClears(Action[] clears)
        {
            for (int i = 0; i < clears.Length; i++)
                clears[i]();
        }

        /// <summary>새 호버가 미리보기를 요청하지 않아도 이전 응답은 다시 적용할 수 없게 한다.</summary>
        private void InvalidateHoverPreview()
        {
            _feedback.ClearAttackHealthPreview();
            _input.ClearPreview(_hoverRequestNumber);
            _hoverRequestNumber = 0;
        }

        /// <summary>
        /// Buffer의 최신 미리보기 요청이 자기 범위·호버·이유 요청일 때만 비웁니다(판단은 Buffer). 슬롯에는 요청이 하나뿐이라
        /// 셋 중 많아야 하나만 비워집니다.
        /// </summary>
        private void ClearOwnedPreview()
        {
            if (!_input.ClearPreview(_rangeRequestNumber) && !_input.ClearPreview(_hoverRequestNumber))
                _input.ClearPreview(_rejectionRequestNumber);
        }

        private bool IsModeStillValid(in CombatInputFacts facts)
        {
            return facts.CurrentLayer == CombatInputPolicy.LayerOf(StagePhase.Combat)
                && !_actor.IsEmpty
                && ResolveActor(facts) == _actor
                && facts.BattleInstanceId == _requestBattleId && facts.ControllerTurnSequence == _requestTurnSequence;
        }

        /// <summary>
        /// 결과 예상(체력·넉백)을 그려도 되는지의 원본입니다. 결과 재생 중이 아니고 모드가 그대로이며 공격 미리보기를 낼 수 있어야 합니다.
        /// 그리는 조건만 정합니다. 지우는 때는 따로입니다(체력은 <see cref="InvalidateHoverPreview"/>, 넉백은 <see cref="ClearShotPreview"/>).
        /// </summary>
        private bool CanShowHealthPreview(in CombatInputFacts facts)
        {
            return !facts.IsPresenting
                && IsModeStillValid(facts)
                && CombatInputPolicy.CanPreview(facts, _gate.HasPending, CombatViewPreviewKind.Attack);
        }

        /// <summary>공격 행위자는 View가 선택한 아군입니다. 없으면 Empty입니다.</summary>
        private static EntityId ResolveActor(in CombatInputFacts facts) => facts.SelectedActorId;

        /// <summary>
        /// 공통 수명의 단계마다 공격 조준의 어느 메서드인지만 답합니다. 조준 제출은 뗄 때라 누름에는 할 일이 없고,
        /// 강제 선택 종료에는 모드를 유지합니다(V2-b 전 묶음과 같음).
        /// </summary>
        private sealed class OwnerAdapter : CombatInputOwner
        {
            private readonly AttackTargetingInteraction _attack;

            internal OwnerAdapter(AttackTargetingInteraction attack) => _attack = attack;

            internal override CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.Attack;
            internal override bool IsActive => _attack.IsActive;

            internal override void PointerMoved(in CombatPointerInput input) =>
                _attack.PointerMoved(input.Facts, input.Characters, input.ScreenX, input.ScreenY);

            internal override void SelectReleased(in CombatPointerInput input) =>
                _attack.SelectReleased(input.Facts, input.Characters, input.ScreenX, input.ScreenY);

            internal override bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _attack.PreviewAnswered(facts, characters);

            internal override void BeforeResult() => _attack.BeforeResult();

            internal override void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _attack.AfterResult(facts);

            internal override void CancelPresentation() => _attack.Exit();
        }
    }
}
