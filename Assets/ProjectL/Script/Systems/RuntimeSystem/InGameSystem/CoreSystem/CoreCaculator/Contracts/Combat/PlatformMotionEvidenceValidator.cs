using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>이동 계산을 반복하지 않고 플랫폼·탑승자 프레임, 난수 소비, 진영 경계의 사건 보존을 검사한다.</summary>
    public static class PlatformMotionEvidenceValidator
    {
        public static void Validate(CombatCommandTransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            var before = transition.Input.State.Environment.Tiles.OfType<MovingPlatformFact>().ToDictionary(p => p.Id);
            var final = transition.FinalState.Environment.Tiles.OfType<MovingPlatformFact>().ToDictionary(p => p.Id);
            Require(before.Count == final.Count && before.All(p => final.TryGetValue(p.Key, out var next) &&
                p.Value.Spec.SameValues(next.Spec) && p.Value.KnownTo.SequenceEqual(next.KnownTo)), "정지 플랫폼 식별·정의·공개 범위 보존");
            var boundaries = transition.Events.OfType<CombatPlatformBoundaryEvent>().ToArray();
            if (boundaries.Length == 0)
            {
                if (before.Any(p => !p.Value.SameValues(final[p.Key])) ||
                    transition.Events.OfType<CombatMovementSourceEvent>().Any(e => e.Expected is MovingPlatformFact || e.Final is MovingPlatformFact))
                    throw new InvalidOperationException("정지 플랫폼 원본은 사전 이동 근거 없이 추가·삭제·이동할 수 없습니다.");
                Require(transition.Input.State.LastPlatformBoundaryCount == transition.FinalState.LastPlatformBoundaryCount &&
                    !transition.Events.Any(e => e is CombatPlatformMotionEvent or CombatPlatformStepEvent or CombatEnvironmentRandomEvent),
                    "사전 이동 사건 없는 경계 변경");
                if (transition.Accepted && Due(transition.Input, before.Count))
                    Require(!transition.Events.Any(e => e.Kind == CombatEventKind.ControllerTurnStarted), "플랫폼 처리 전 진영 시작 금지");
                return;
            }
            Require(boundaries.Length == 1 && transition.Accepted && Due(transition.Input, before.Count) &&
                transition.Input.ProcessingKind == CombatProcessingKind.Advance &&
                transition.Input.Kind == CombatCommandKind.StartControllerTurn, "진영 시작 전 한 번의 사전 이동");
            var boundary = boundaries[0];
            Require(ReferenceEquals(transition.Events[0], boundary) &&
                boundary.BoundaryCount == transition.Input.State.Turn.CompletedControllerTurnCount &&
                transition.FinalState.LastPlatformBoundaryCount == boundary.BoundaryCount &&
                transition.Input.State.Turn.SameValues(transition.FinalState.Turn) &&
                transition.FinalState.Phase is CombatFlowPhase.Automatic or CombatFlowPhase.Finished &&
                transition.ActionUseSettlement == null &&
                !transition.Events.Any(e => e.Kind == CombatEventKind.ControllerTurnStarted), "턴 시작과 분리된 처리 경계");
            ValidateOrder(boundary, before);
            new Validation(transition, boundary, before, final).Run();
        }

        private static bool Due(CombatCommandInput input, int count) => PlatformMotionRules.IsPreludeDue(input.State, count);

        private static void ValidateOrder(CombatPlatformBoundaryEvent boundary, Dictionary<long, MovingPlatformFact> platforms)
        {
            int drawIndex = 0;
            var ordered = new List<long>();
            foreach (var group in platforms.Values.OrderBy(p => p.Id).GroupBy(p => p.Spec.Length).OrderByDescending(g => g.Key))
            {
                var tied = group.Select(p => p.Id).ToArray();
                for (int i = tied.Length - 1; i > 0; i--)
                {
                    Require(drawIndex < boundary.OrderingDraws.Count, "동률 순서 추첨 근거 누락");
                    var draw = boundary.OrderingDraws[drawIndex++];
                    Require(draw.ChoiceCount == i + 1, "동률 추첨 후보 수");
                    (tied[i], tied[draw.ChoiceIndex]) = (tied[draw.ChoiceIndex], tied[i]);
                }
                ordered.AddRange(tied);
            }
            Require(drawIndex == boundary.OrderingDraws.Count && ordered.SequenceEqual(boundary.OrderedPlatformIds),
                "큰 플랫폼 우선 및 기록된 동률 순서");
        }

        private sealed class Validation
        {
            private readonly CombatCommandTransition _transition;
            private readonly CombatPlatformBoundaryEvent _boundary;
            private readonly Dictionary<long, MovingPlatformFact> _platforms;
            private readonly Dictionary<long, MovingPlatformFact> _final;
            private readonly Dictionary<EntityId, CharacterCombatFacts> _characters;
            private readonly Dictionary<EntityId, StructureCombatFacts> _structures;
            private CombatPlatformMotionEvent _motion;
            private CombatPlatformStepEvent _frame;
            private HashSet<EntityId> _riders;
            private bool _sourcePending;
            private int _passengerIndex;
            private int _stepIndex;
            private int _motionCount;
            private int _orderingDraws;
            private uint _random;
            private bool _nontransportableAtStop;

            internal Validation(CombatCommandTransition transition, CombatPlatformBoundaryEvent boundary,
                Dictionary<long, MovingPlatformFact> platforms, Dictionary<long, MovingPlatformFact> final)
            {
                _transition = transition;
                _boundary = boundary;
                _platforms = platforms;
                _final = final;
                _characters = transition.Input.Characters.ToDictionary(c => c.EntityId);
                _structures = transition.Input.State.Structures.ToDictionary(s => s.EntityId);
                _random = transition.Input.State.EnvironmentProgress.RandomState;
            }

            internal void Run()
            {
                foreach (var occurrence in _transition.Events.Skip(1))
                {
                    if (_sourcePending)
                    {
                        Require(ReferenceEquals(occurrence, _frame.SourceChange), "위치 프레임의 플랫폼 원본 변경 순서");
                        _sourcePending = false;
                    }
                    else if (_frame != null && _passengerIndex < _frame.Passengers.Count)
                    {
                        var passenger = _frame.Passengers[_passengerIndex++];
                        var move = occurrence as CombatCharacterEvent;
                        Require(move != null && move.Kind == CombatEventKind.Moved && move.Final.EntityId == passenger.EntityId &&
                            move.MovementCause?.Agency == CombatMovementAgency.Forced &&
                            _characters.TryGetValue(passenger.EntityId, out var current) && current.SameValues(move.Expected) &&
                            move.Expected.Anchor == passenger.From && move.Final.Anchor == passenger.To &&
                            move.Expected.WithPlacement(move.Expected.Area, move.Expected.ReserveSlotIndex, passenger.To,
                                move.Expected.Rotation, new[] { passenger.To }).SameValues(move.Final),
                            "효과 앞에서 함께 옮기는 모든 탑승자");
                    }
                    switch (occurrence)
                    {
                        case CombatEnvironmentRandomEvent draw:
                            Require(draw.ExpectedState == _random, "환경 난수 소비의 기대값");
                            _random = draw.FinalState;
                            if (_motionCount == 0)
                            {
                                Require(_orderingDraws < _boundary.OrderingDraws.Count &&
                                    ReferenceEquals(draw, _boundary.OrderingDraws[_orderingDraws++]), "이동 전 순서 추첨");
                            }
                            break;
                        case CombatPlatformMotionEvent motion:
                            EndMotion();
                            Require(_orderingDraws == _boundary.OrderingDraws.Count && motion.BoundaryCount == _boundary.BoundaryCount &&
                                motion.OrderIndex == _motionCount && _motionCount < _boundary.OrderedPlatformIds.Count &&
                                motion.PlatformSourceId == _boundary.OrderedPlatformIds[_motionCount] &&
                                _platforms[motion.PlatformSourceId].SameValues(motion.Expected), "순차 플랫폼 시작 기대값");
                            _motion = motion;
                            _nontransportableAtStop = HasNontransportableOccupant(motion.Expected);
                            if (_nontransportableAtStop)
                                Require(motion.Steps.Count == 0 && motion.Expected.SameValues(motion.Final) &&
                                    motion.StopReason == PlatformMotionStopReason.NontransportableOccupant,
                                    "운반 불가 점유자의 지지면과 방향 보존");
                            _motionCount++;
                            _stepIndex = 0;
                            _riders = PlatformMotionRules.Occupants(_characters.Values, motion.Expected)
                                .Where(PlatformMotionRules.IsTransportable).Select(c => c.EntityId).ToHashSet();
                            break;
                        case CombatPlatformStepEvent step:
                            Require(_motion != null && _frame == null && _stepIndex < _motion.Steps.Count &&
                                ReferenceEquals(step, _motion.Steps[_stepIndex++]) &&
                                _platforms[step.PlatformSourceId].SameValues(step.Expected), "요약과 일치하는 한 번의 이동 단계");
                            Require(step.Final.OccupiedCells.All(_transition.Input.State.IsInside), "보드 안의 플랫폼 전체 모양");
                            // 계산기는 각 이동 칸 앞에서만 점유를 검사한다. 끝점 반전은 효과 뒤에 바로 일어나므로 점유를 검사하지 않는다.
                            if (step.StepKind == PlatformStepKind.Translation)
                            {
                                Require(!HasNontransportableOccupant(step.Expected), "운반 불가 점유자가 있는 플랫폼 이동 금지");
                                Require(step.Passengers.Select(p => p.EntityId).ToHashSet().SetEquals(_riders) &&
                                    step.Passengers.All(p => _characters.TryGetValue(p.EntityId, out var rider) &&
                                        PlatformMotionRules.IsTransportable(rider) && !rider.Health.IsDead && rider.Anchor == p.From),
                                    "고정된 탑승자와 실제 출발 칸");
                            }
                            _frame = step;
                            _sourcePending = true;
                            _passengerIndex = 0;
                            break;
                        case CombatMovementSourceEvent source when source.Expected is MovingPlatformFact || source.Final is MovingPlatformFact:
                            Require(_frame != null && ReferenceEquals(source, _frame.SourceChange), "단계 밖의 플랫폼 변경 금지");
                            _platforms[_frame.PlatformSourceId] = _frame.Final;
                            break;
                        case CombatCharacterEvent character:
                            _characters[character.Final.EntityId] = character.Final;
                            break;
                        case CombatTraitStateEvent trait:
                            _characters[trait.Final.EntityId] = trait.Final;
                            break;
                        case CombatStructureEvent structure:
                            _structures[structure.Final.EntityId] = structure.Final;
                            break;
                        case CombatObstacleDurabilityEvent obstacle:
                            _structures[obstacle.ObstacleId] = obstacle.Final;
                            break;
                        case CombatHeldEnvironmentEvent held:
                            _characters[held.CharacterId] = _characters[held.CharacterId].With(heldEnvironment: held.Final);
                            break;
                        case CombatSoulEvent soul when soul.Characters != null:
                            foreach (var change in soul.Characters.Characters) _characters[change.Final.EntityId] = change.Final;
                            break;
                        case CombatSummonEvent summon:
                            _characters[summon.Created.EntityId] = summon.Created;
                            break;
                        case CombatReinforcementEvent reinforcement:
                            _characters[reinforcement.Created.EntityId] = reinforcement.Created;
                            break;
                    }
                    if (_frame != null && ReferenceEquals(occurrence, _frame.CompletionEvent)) EndFrame();
                }
                EndMotion();
                Require(_motionCount == _boundary.OrderedPlatformIds.Count || _transition.Outcome.IsStageEnded,
                    "종료 전 모든 플랫폼 처리");
                Require(_platforms.All(p => p.Value.SameValues(_final[p.Key])), "단계와 최종 플랫폼 위치·방향");
                Require(_random == _transition.FinalState.EnvironmentProgress.RandomState, "난수 소비와 최종 환경 진행값");
            }

            private bool HasNontransportableOccupant(MovingPlatformFact platform) =>
                PlatformMotionRules.HasNontransportableOccupant(_characters.Values, _structures.Values, platform);

            private void EndFrame()
            {
                Require(!_sourcePending && _passengerIndex == _frame.Passengers.Count, "모든 위치 변경 뒤의 프레임 완료");
                foreach (var passenger in _frame.Passengers)
                {
                    bool ended = PlatformMotionRules.EndsTransport(_characters[passenger.EntityId], _frame.Final);
                    Require(passenger.TransportEnded == ended, "진입 효과 뒤 운반 종료 여부");
                    if (ended) _riders.Remove(passenger.EntityId);
                }
                // 이후 플랫폼이나 후속 반응이 점유자를 바꿔도 이 플랫폼이 멈춘 순간의 근거는 유지한다.
                _nontransportableAtStop = HasNontransportableOccupant(_frame.Final);
                _frame = null;
            }

            private void EndMotion()
            {
                Require(_frame == null && !_sourcePending, "다음 플랫폼 전 위치 프레임 완료");
                if (_motion == null) return;
                int moved = _motion.Steps.Count(s => s.StepKind == PlatformStepKind.Translation);
                Require(_stepIndex == _motion.Steps.Count && _platforms[_motion.PlatformSourceId].SameValues(_motion.Final) &&
                    moved <= _motion.Expected.Spec.Length, "플랫폼 경로와 이동 예산 보존");
                if (_motion.StopReason == PlatformMotionStopReason.BudgetConsumed)
                    Require(moved == _motion.Expected.Spec.Length, "전체 이동 예산 사용");
                if (_motion.StopReason == PlatformMotionStopReason.Blocked)
                    Require(moved < _motion.Expected.Spec.Length, "남은 이동량이 있는 막힘");
                if (_motion.StopReason == PlatformMotionStopReason.NontransportableOccupant)
                    Require(_nontransportableAtStop, "정지 시점의 운반 불가 점유자");
                if (_motion.StopReason == PlatformMotionStopReason.Terminal)
                    Require(_transition.Outcome.IsStageEnded, "실제 전투 종료");
                // 경로 끝점에 닿은 이동 칸 바로 뒤에는 반전이 온다. 전투가 끝난 경우만 예외로 둔다(검토 PF-10).
                for (int index = 0; index < _motion.Steps.Count; index++)
                {
                    var step = _motion.Steps[index];
                    if (step.StepKind != PlatformStepKind.Translation || !PlatformMotionRules.FacesRouteEnd(step.Final)) continue;
                    Require(_transition.Outcome.IsStageEnded || index + 1 < _motion.Steps.Count &&
                        _motion.Steps[index + 1].StepKind == PlatformStepKind.Turnaround, "끝점 도착 뒤의 즉시 반전");
                }
                _motion = null;
            }
        }

        private static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException("플랫폼 이동 근거가 올바르지 않습니다: " + reason);
        }
    }
}
