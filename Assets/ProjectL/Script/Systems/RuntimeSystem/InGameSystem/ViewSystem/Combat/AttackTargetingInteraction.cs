using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 단계의 공격 조준 입력입니다(설계 §7·§21.1, W21, 계획 E2, 구조 개선 VR3).
    /// 공격 모드·행위자·기록 모드 행위자, 공격 범위 응답(조준 목록·AP 판정), 공격 호버와 그 미리보기 요청 번호를 소유합니다.
    /// </summary>
    /// <remarks>
    /// 조준은 Core 공격 범위 응답의 조준 목록에서만 고르며 그 종류(칸·방향·전체)로 입력 방식을 정합니다. 비용·유효 범위는 계산하지 않습니다.
    /// 범위 응답은 이후 호버 요청이 Buffer의 요청을 바꾸므로 모드 동안만 보관하고, 결과 후처리·모드 종료에서 버립니다.
    /// 명령은 Gate로 제출합니다. 미리보기는 Buffer의 최신 요청이 자기 번호일 때만 비웁니다.
    /// </remarks>
    internal sealed class AttackTargetingInteraction
    {
        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatViewBufferReadSink _requests;
        private readonly ICombatPointerPicker _picker;
        private readonly ICombatInteractionFeedback _feedback;
        private readonly CombatCommandGate _gate;
        private readonly CombatHoverTracker _hover;

        private bool _active;
        private EntityId _actor;
        private EntityId _recordActor;

        // 조준 모드의 범위 응답(모드 수명 동안의 입력·표시 캐시, 원본은 Core Buffer)
        private int _rangeRequestNumber;
        private bool _rangeAnswered;
        private AttackAim[] _attackAims = Array.Empty<AttackAim>();
        private readonly Dictionary<CellCoord, List<AttackBlockedContact>> _rangeBlockers = new();
        private bool _canAffordAttack;

        // 호버 칸에 대한 마지막 공격 미리보기 요청 번호
        private int _hoverRequestNumber;

        internal AttackTargetingInteraction(
            ICombatViewBufferWriteSink input,
            ICombatViewBufferReadSink requests,
            ICombatPointerPicker picker,
            ICombatInteractionFeedback feedback,
            CombatCommandGate gate)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _hover = new CombatHoverTracker(feedback);
        }

        /// <summary>공격 모드인지입니다.</summary>
        internal bool IsActive => _active;

        /// <summary>현재 모드의 범위 응답을 받았는지입니다. 받기 전에는 호버 요청과 기록을 하지 않습니다.</summary>
        internal bool HasRangeAnswer => _rangeAnswered;

        /// <summary>
        /// 공격 모드에 들어갈 수 있는지입니다. 행위자가 있고 공격 범위 미리보기를 요청할 수 있어야 합니다.
        /// 들어갈 수 있을 때만 호출자가 다른 소유자의 표시를 정리한 뒤 <see cref="Enter"/>를 부릅니다.
        /// </summary>
        internal bool CanEnter(in CombatInputFacts facts, out EntityId actor)
        {
            actor = ResolveActor(facts);
            return !actor.IsEmpty && CombatInputPolicy.CanPreview(facts, _gate.HasPending, CombatViewPreviewKind.AttackRange);
        }

        /// <summary>이전 공격 모드를 정리하고 새 행위자로 모드에 들어가 범위를 요청합니다.</summary>
        internal void Enter(in CombatInputFacts facts, EntityId actor)
        {
            Exit();
            _hover.Clear();
            _active = true;
            _actor = actor;
            RequestRange(facts);
        }

        internal void PointerMoved(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (!_active)
                return;

            if (!_picker.TryPickCell(screenX, screenY, out CellCoord cell))
            {
                _hover.Clear();
                _feedback.ClearAffectedCells();
                _feedback.ClearAttackBlockers();
                InvalidateHoverPreview();
                return;
            }

            if (_hover.HasHover && _hover.Cell == cell)
                return;

            _hover.Set(cell, CombatHoverState.Neutral);
            RequestHoverForCell(facts, characters, cell);
        }

        /// <summary>기록 모드는 Core가 배치 단계라 행위자가 없으므로, 누른 아군 유닛을 기록용 행위자로 고릅니다(설계 §8.1).</summary>
        internal void SelectPressed(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (facts.ExecutionMode != CombatExecutionMode.RecordInputOnly || !facts.ActiveActorId.IsEmpty)
                return;
            if (!_picker.TryPickCell(screenX, screenY, out CellCoord cell))
                return;

            CharacterData unit = CombatCharacterQueries.FindPlayerAt(characters, cell);
            if (unit != null)
                _recordActor = unit.EntityId;
        }

        internal void SelectReleased(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (_active && _picker.TryPickCell(screenX, screenY, out CellCoord cell))
                SubmitTarget(facts, characters, cell);
        }

        /// <summary>
        /// 자기 요청의 응답이면 반영하고 true입니다. 범위 응답이면 조준 목록·범위·AP 판정을 보관·표시하고,
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

            if (_hoverRequestNumber == 0 || answer.RequestNumber != _hoverRequestNumber)
                return false;

            if (_hover.HasHover && answer.Attack.HasValue)
                ApplyAttackAnswer(answer.Attack.Value);
            return true;
        }

        /// <summary>
        /// 결과 후처리: 행위자·Layer가 맞지 않으면 모드를 끝내고, 남은 모드는 보관한 범위 응답을 버리고 새 번호로 다시 요청합니다.
        /// 이전 행위자로는 재요청하지 않습니다.
        /// </summary>
        internal void AfterResult(in CombatInputFacts facts)
        {
            if (!_active)
                return;

            if (!IsModeStillValid(facts))
            {
                Exit();
                return;
            }

            ClearOwnedPreview();
            _hoverRequestNumber = 0;
            ResetRangeAnswer();
            _hover.SetStateIfHovered(CombatHoverState.Neutral);
            RequestRange(facts);
        }

        /// <summary>공격 모드·범위·영향 칸·공격 호버·자기 미리보기를 정리합니다. 모드가 아니면 아무것도 하지 않습니다.</summary>
        internal void Exit()
        {
            if (!_active)
                return;

            _active = false;
            _actor = EntityId.Empty;
            ResetRangeAnswer();
            _hover.Clear();
            ClearOwnedPreview();
            _rangeRequestNumber = 0;
            _hoverRequestNumber = 0;
        }

        /// <summary>오류 정지·씬 연결 해제 때 기록 모드 행위자를 잊습니다.</summary>
        internal void ClearRecordActor()
        {
            _recordActor = EntityId.Empty;
        }

        /// <summary>현재 모드의 범위를 요청하고 번호를 기억합니다. 응답을 받기 전까지 호버 요청·기록을 하지 않습니다.</summary>
        private void RequestRange(in CombatInputFacts facts)
        {
            CombatViewPreviewData range = CombatViewPreviewData.AttackRange(_actor);

            _rangeRequestNumber = CombatInputPolicy.CanPreview(facts, _gate.HasPending, range.Kind)
                ? _input.SetPreview(range)
                : 0;
        }

        private void ApplyRangeAnswer(CombatPreviewResult answer)
        {
            _rangeAnswered = true;
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
                // 공격 코스트(특성·효과 할인 반영)가 남은 AP 이하인지는 Core가 판정한다. 범위는 계속 보여준다.
                _canAffordAttack = range.CanAffordAttack;
                _feedback.ShowRangeCells(range.RangeCells.ToArray());
                return;
            }

            // 범위 응답이 실패면 고를 수 있는 것이 없다. 표시만 비운다.
            _attackAims = Array.Empty<AttackAim>();
            _canAffordAttack = false;
            _feedback.ClearRangeCells();
        }

        private void ApplyAttackAnswer(AttackPreview attack)
        {
            if (attack.CanAttack)
            {
                _feedback.ShowAffectedCells(attack.AffectedCells.ToArray());
                _feedback.ShowAttackCostPreview(attack.AttackerId, attack.AttackCost);
            }
            else
            {
                _feedback.ClearAffectedCells();
            }

            _hover.SetStateIfHovered(attack.CanAttack ? CombatHoverState.Valid : CombatHoverState.Invalid);
            _feedback.ShowAttackBlockers(attack.BlockedContacts);
        }

        /// <summary>범위 응답을 받은 뒤에만 호버 칸의 공격 미리보기를 요청합니다. 목록에 없거나 AP가 부족하면 불가로 표시합니다.</summary>
        private void RequestHoverForCell(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            InvalidateHoverPreview();
            _feedback.ClearAffectedCells();
            _feedback.ClearAttackBlockers();
            if (!CanChooseTarget(facts))
                return;

            // 불가 조준은 상세 미리보기를 요청하지 않는다. 범위 응답의 해당 접촉 증거만 표시한다.
            if (_rangeBlockers.TryGetValue(cell, out var contacts))
                _feedback.ShowAttackBlockers(contacts);

            AttackAim? aim = AimFor(facts, characters, cell);
            if (!aim.HasValue || !CanAffordAttack(facts))
            {
                _hover.SetStateIfHovered(CombatHoverState.Invalid);
                return;
            }

            CombatViewPreviewData preview = CombatViewPreviewData.Attack(_actor, aim.Value);
            if (CombatInputPolicy.CanPreview(facts, _gate.HasPending, preview.Kind))
                _hoverRequestNumber = _input.SetPreview(preview);
        }

        private void SubmitTarget(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            // 범위 응답을 받기 전에는 무엇을 고를 수 있는지 모르므로 기록하지 않는다.
            if (_actor.IsEmpty || !CanChooseTarget(facts))
                return;

            // AP가 부족하면 Core가 반드시 거절하므로 제출하지 않는다.
            AttackAim? aim = AimFor(facts, characters, cell);
            if (aim.HasValue && CanAffordAttack(facts))
                _gate.TrySubmit(facts, CombatViewCommandData.Attack(_actor, aim.Value));
        }

        /// <summary>
        /// 조준을 고를 수 있는지입니다. Live는 범위 응답을 받은 뒤에만 고릅니다.
        /// RecordInputOnly는 Core가 미리보기에 답하지 않으므로(처리 Job 미등록) 응답 없이 칸 기준으로 기록합니다(설계 §8.1).
        /// </summary>
        private bool CanChooseTarget(in CombatInputFacts facts)
        {
            return _rangeAnswered || facts.ExecutionMode == CombatExecutionMode.RecordInputOnly;
        }

        /// <summary>
        /// 공격 범위 응답의 AP 판정(<see cref="AttackRangeResult.CanAffordAttack"/>)입니다. View는 코스트를 계산하지 않습니다.
        /// RecordInputOnly에서 응답이 없으면 판정할 수 없으므로 막지 않습니다(설계 §8.1).
        /// </summary>
        private bool CanAffordAttack(in CombatInputFacts facts)
        {
            if (facts.ExecutionMode == CombatExecutionMode.RecordInputOnly && !_rangeAnswered)
                return true;
            return _canAffordAttack;
        }

        /// <summary>
        /// 공격 범위 응답의 조준 목록에서 칸에 맞는 조준을 고릅니다(설계 §21.1).
        /// 칸 조준이면 같은 칸, 방향 조준이면 행위자 → 칸 각도에 가장 가까운 목록 방향, 전체 조준이면 그 하나입니다.
        /// 목록에 맞는 것이 없으면 null입니다. RecordInputOnly는 응답이 없으므로 칸 조준으로 기록합니다.
        /// </summary>
        private AttackAim? AimFor(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            if (facts.ExecutionMode == CombatExecutionMode.RecordInputOnly && !_rangeAnswered)
                return AttackAim.ForCell(cell);
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

        private void ResetRangeAnswer()
        {
            _rangeAnswered = false;
            _attackAims = Array.Empty<AttackAim>();
            _rangeBlockers.Clear();
            _canAffordAttack = false;
            _feedback.ClearRangeCells();
            _feedback.ClearAffectedCells();
            _feedback.ClearAttackBlockers();
        }

        /// <summary>새 호버가 미리보기를 요청하지 않아도 이전 응답은 다시 적용할 수 없게 한다.</summary>
        private void InvalidateHoverPreview()
        {
            if (_hoverRequestNumber != 0 && _requests.HasPreview && _requests.Preview.Number == _hoverRequestNumber)
                _input.ClearPreview();
            _hoverRequestNumber = 0;
        }

        /// <summary>Buffer의 최신 미리보기 요청이 자기 범위·호버 요청일 때만 비웁니다.</summary>
        private void ClearOwnedPreview()
        {
            if (!_requests.HasPreview)
                return;

            int latest = _requests.Preview.Number;
            if ((_rangeRequestNumber != 0 && latest == _rangeRequestNumber) || (_hoverRequestNumber != 0 && latest == _hoverRequestNumber))
                _input.ClearPreview();
        }

        private bool IsModeStillValid(in CombatInputFacts facts)
        {
            return facts.CurrentLayer == CombatInputPolicy.LayerOf(StagePhase.Combat)
                && !_actor.IsEmpty
                && ResolveActor(facts) == _actor;
        }

        /// <summary>Live는 Core의 현재 행위자, RecordInputOnly는 행위자가 없을 때 누른 아군 유닛입니다.</summary>
        private EntityId ResolveActor(in CombatInputFacts facts)
        {
            if (!facts.ActiveActorId.IsEmpty)
                return facts.ActiveActorId;

            return facts.ExecutionMode == CombatExecutionMode.RecordInputOnly ? _recordActor : EntityId.Empty;
        }
    }
}
