using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    public static class MeteorGimmickEvidenceValidator
    {
        public static void Validate(CombatCommandTransition transition)
        {
            var state = transition.Input.State.Gimmicks.Entries.OfType<MeteorGimmickState>().SingleOrDefault();
            bool hasEvents = transition.Events.Any(e => e is CombatMeteorEvent or CombatMeteorImpactEvent);
            if (state == null && !hasEvents) return;
            var definitions = transition.Input.Battle?.GimmickDefinitions;
            Require(state != null && definitions != null && definitions.Count >= state.Id.Value &&
                definitions[state.Id.Value - 1] is MeteorGimmickSpec, "등록된 운석 상태와 정의");
            new Validation(transition, state, (MeteorGimmickSpec)definitions[state.Id.Value - 1]).Run();
        }

        private sealed class Validation
        {
            private readonly CombatCommandTransition _transition;
            private readonly MeteorGimmickSpec _spec;
            private readonly StageGimmickId _initialId;
            private readonly GimmickEvidenceWorld _world;
            private readonly EnvironmentRandomTrace _random;
            private CombatMeteorEvent _planning;
            private readonly List<CombatMeteorImpactEvent> _impacts = new();
            private readonly HashSet<CombatMeteorImpactEvent> _hitEvents = new();
            private readonly HashSet<EntityId> _fireRecipients = new();
            private readonly ILookup<(CombatEvent, EntityId), CombatHealthStep> _lossSteps;
            private readonly CombatTurnEvent _turn;
            private readonly long _count;
            private MeteorGimmickState _state;
            private int _lastPhase = -1;
            private (MovementEnvironmentFacts Environment, HashSet<CellCoord> Blocked) _impactBoard;
            private CharacterCombatFacts[] _captured = Array.Empty<CharacterCombatFacts>();
            private int _impactStart = -1;

            internal Validation(CombatCommandTransition transition, MeteorGimmickState state, MeteorGimmickSpec spec)
            {
                _transition = transition;
                _state = state;
                _initialId = state.Id;
                _spec = spec;
                _world = new GimmickEvidenceWorld(transition.Input);
                _random = new EnvironmentRandomTrace(transition.Input.State.EnvironmentProgress.RandomState);
                _lossSteps = transition.HealthExecutions.SelectMany(e => e.Steps).Where(s => s.ActualLoss > 0 &&
                    s.Source.ContextEvent is CombatMeteorImpactEvent).ToLookup(s => (s.Source.ContextEvent, s.RecipientId));
                _turn = transition.Events.OfType<CombatTurnEvent>().SingleOrDefault(e =>
                    e.Kind == CombatEventKind.PlayerTurnEnded || e.Kind == CombatEventKind.EnemyPhaseEnded);
                _count = transition.FinalState.Turn.CompletedControllerTurnCount;
            }

            internal void Run()
            {
                for (int index = 0; index < _transition.Events.Count; index++)
                {
                    var occurrence = _transition.Events[index];
                    if (occurrence is CombatTraitStateEvent status) ObserveFire(status);
                    else if (occurrence is CombatMeteorImpactEvent hit) ValidateImpact(hit, index);
                    else if (occurrence is CombatMeteorEvent change) ValidateAdvance(change, index);
                    _world.Apply(occurrence);
                    _random.Observe(occurrence);
                }
                // 경고 뒤에 난수를 쓴 사건이 없으면 전이의 최종 난수는 경고가 남긴 값이다.
                Require(_planning == null || _random.EndsWith(_planning, _transition.FinalState.EnvironmentProgress.RandomState),
                    "운석 경고 뒤 최종 난수");
                ValidateCompletion();
                ValidateHealthSources();
                ValidatePublishedFire();
            }

            private void ObserveFire(CombatTraitStateEvent change)
            {
                if (Fire(change.Final)?.Origin is EffectOriginKey origin &&
                    origin.Kind == EffectOwnerKind.StageGimmick && origin.DefinitionKey == (int)_state.Key &&
                    !SameFire(Fire(change.Expected), Fire(change.Final)))
                    _fireRecipients.Add(change.Final.EntityId);
            }

            private void ValidateImpact(CombatMeteorImpactEvent hit, int index)
            {
                ValidatePriorImpact(index);
                Require(_turn != null && _turn.Final.CompletedControllerTurnCount == _count && _state.NextBoundaryCount == _count &&
                    hit.GimmickId == _state.Id && _impacts.Count < _state.Warnings.Count &&
                    _state.Warnings[_impacts.Count].SameValues(hit.Footprint), "충돌의 현재 경고와 경계");
                // 한 충돌 묶음의 보드는 첫 충돌 직전 사실로 고정한다.
                if (_impactBoard.Blocked == null) _impactBoard = ReadBoard();
                Require(hit.Cancelled == hit.Footprint.Cells.Any(_impactBoard.Blocked.Contains), "운석 충돌 취소와 차단 칸");
                _captured = hit.Cancelled ? Array.Empty<CharacterCombatFacts>() : _world.Characters.Values.Where(c =>
                    c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.OccupiedCells.Any(hit.Footprint.Cells.Contains))
                    .OrderBy(c => c.EntityId).ToArray();
                Require(hit.RecipientIds.SequenceEqual(_captured.Select(c => c.EntityId)), "운석 충돌 직전의 전체 수신자");
                _impactStart = index;
                _impacts.Add(hit);
                _hitEvents.Add(hit);
            }

            private void ValidateAdvance(CombatMeteorEvent change, int index)
            {
                ValidateBoundary(change);
                switch (change.Step)
                {
                    case MeteorGimmickStep.Expired:
                        ValidateExpiry(change);
                        break;
                    case MeteorGimmickStep.Warning:
                    case MeteorGimmickStep.CycleSkipped:
                        ValidateWarningSchedule(change);
                        ValidateWarningSelection(change, index);
                        break;
                    case MeteorGimmickStep.Resolved:
                        ValidateResolution(change, index);
                        break;
                }
                _state = change.Final;
            }

            private void ValidateBoundary(CombatMeteorEvent change)
            {
                Require(_turn != null && _turn.Final.CompletedControllerTurnCount == _count &&
                    change.Boundary.CompletedControllerTurnCount == _count &&
                    (_turn.Kind == CombatEventKind.PlayerTurnEnded ? change.Boundary.EndedSide == Owner.Player :
                        change.Boundary.EndedSide == Owner.Enemy) &&
                    (int)change.Boundary.Phase > _lastPhase && _state.SameValues(change.Expected),
                    "운석의 실제 종료 단계와 기대 상태");
                _lastPhase = (int)change.Boundary.Phase;
                change.Final.ValidateDefinition(_spec, _transition.FinalState.BoardWidth, _transition.FinalState.BoardHeight);
            }

            private void ValidateExpiry(CombatMeteorEvent change)
            {
                Require(_impacts.Count == 0 && _state.NextBoundaryCount == change.Final.NextBoundaryCount &&
                    SameWarnings(_state, change.Final) &&
                    SameFlames(_state.Flames.Where(f => f.ExpiresAtBoundaryCount > _count), change.Final.Flames) &&
                    change.Final.Flames.Count < _state.Flames.Count, "운석 불길의 만료 경계");
            }

            private void ValidateWarningSchedule(CombatMeteorEvent change)
            {
                bool skipped = change.Step == MeteorGimmickStep.CycleSkipped;
                Require(_impacts.Count == 0 && _state.NextBoundaryCount == _count && _state.Warnings.Count == 0 &&
                    (change.Final.Warnings.Count == 0) == skipped &&
                    change.Final.NextBoundaryCount == checked(_count + (skipped ? _spec.CycleControllerTurns : _spec.WarningLeadControllerTurns)) &&
                    SameFlames(_state.Flames, change.Final.Flames),
                    "운석 경고 또는 건너뛴 주기의 일정·난수");
            }

            private void ValidateWarningSelection(CombatMeteorEvent warning, int index)
            {
                uint random = warning.ExpectedRandomState.Value;
                // 앞선 사건이 남긴 난수와 잇는다. 값을 남기지 않는 난수 사용 뒤에는 공용 추적이 판단을 넘긴다.
                Require(_random.Matches(random), "운석 경고의 시작 난수");
                var final = _transition.FinalState;
                var board = ReadBoard();
                var sites = MeteorGimmickRule.Plan(final.BoardWidth, final.BoardHeight, board.Blocked, _spec,
                    checked(warning.Boundary.CompletedControllerTurnCount + _spec.WarningLeadControllerTurns), ref random);
                Require(sites.Count == warning.Final.Warnings.Count &&
                    sites.Zip(warning.Final.Warnings, (a, b) => a.SameValues(b)).All(equal => equal) &&
                    random == warning.FinalRandomState, "운석 경고의 후보와 난수 선택");
                _planning = warning;
            }

            private void ValidateResolution(CombatMeteorEvent change, int index)
            {
                ValidatePriorImpact(index);
                Require(_state.NextBoundaryCount == _count && _impacts.Count > 0 && _impacts.Count == _state.Warnings.Count &&
                    change.Final.Warnings.Count == 0 &&
                    change.Final.NextBoundaryCount == checked(_count + _spec.CycleControllerTurns - _spec.WarningLeadControllerTurns) &&
                    SameFlames(_state.Flames.Concat(_impacts.Where(hit => !hit.Cancelled)
                        .Select(hit => new MeteorFlameArea(hit.Footprint, checked(_count + _spec.FlameControllerTurns)))),
                        change.Final.Flames), "운석 충돌과 생성된 불길");
                _impacts.Clear();
            }

            private void ValidatePriorImpact(int endIndex)
            {
                var hit = _impacts.LastOrDefault();
                if (hit == null || hit.Cancelled) return;
                var origin = hit.Footprint.FireOrigin(_state.Id);
                var effects = _transition.Events.Skip(_impactStart + 1).Take(endIndex - _impactStart - 1).ToArray();
                foreach (var before in _captured)
                {
                    var id = before.EntityId;
                    var losses = _lossSteps[(hit, id)].ToArray();
                    Require(losses.Length <= 1 && losses.All(step => effects.Contains(step.EvidenceEvent)),
                        "운석 하나의 수신자별 체력 실행");
                    if (losses.Length == 0 && MeteorGimmickRule.HealthDamage(before.Health.MaxHealth, _spec) > 0)
                        Require(HasMitigation(_transition.Input, before, _spec, _impactBoard.Environment, effects),
                            "체력 손실이 없는 운석 피해의 보호 근거");
                    var character = _world.Characters[id];
                    if (character.Health.IsDead) continue;
                    var fire = Fire(character);
                    Require(fire != null && (fire.RemainingAffectedSideStarts > _spec.FireAffectedSideStarts ||
                        fire.RemainingAffectedSideStarts == _spec.FireAffectedSideStarts && fire.Origin == origin),
                        "운석 충돌 생존자의 화상");
                }
            }

            private void ValidateCompletion()
            {
                Require(_impacts.Count == 0, "불길 상태로 연결되지 않은 운석 충돌");
                if (_turn != null && !_transition.Outcome.IsStageEnded)
                    Require(_state.NextBoundaryCount > _count && _state.Flames.All(f => f.ExpiresAtBoundaryCount > _count),
                        "예정된 운석 경고·충돌·만료");
            }

            private void ValidateHealthSources()
            {
                foreach (var step in _transition.HealthExecutions.SelectMany(e => e.Steps))
                {
                    if (step.Source.ContextEvent is not CombatMeteorImpactEvent hit) continue;
                    Require(_hitEvents.Contains(hit) && !hit.Cancelled && hit.RecipientIds.Contains(step.RecipientId) &&
                        step.Source.DamageKind == CombatDamageKind.EnvironmentDamage && step.Source.EffectOrigin == hit.Footprint.FireOrigin(_initialId),
                        "운석 충돌의 수치 환경 피해 출처");
                }
            }

            private void ValidatePublishedFire()
            {
                var changes = _transition.Characters.Characters.ToDictionary(change => change.Final.EntityId, change => change.Final);
                foreach (var id in _fireRecipients)
                {
                    var published = changes.TryGetValue(id, out var final) ? final :
                        _transition.Characters.Created.Concat(_transition.Input.Characters).First(c => c.EntityId == id);
                    Require(SameFire(Fire(_world.Characters[id]), Fire(published)), "운석 화상의 사건과 게시 상태");
                }
            }

            private (MovementEnvironmentFacts Environment, HashSet<CellCoord> Blocked) ReadBoard() =>
                (_world.Environment, MeteorGimmickRule.BlockedCells(_world.Structures.Values, _world.Environment, _world.Bindings,
                    _world.Gimmicks));
        }

        private static bool HasMitigation(CombatCommandInput input, CharacterCombatFacts recipient, MeteorGimmickSpec spec,
            MovementEnvironmentFacts environment, IReadOnlyList<CombatEvent> effects)
        {
            var id = recipient.EntityId;
            int raw = MeteorGimmickRule.HealthDamage(recipient.Health.MaxHealth, spec);
            if (CombatDamageCalculator.Environment(input, raw, new CombatDamageOrigin(null, null),
                recipient.Anchor, environment).FinalDamage == 0) return true;
            return effects.Any(effect => effect switch
            {
                CombatItemEvent item => item.HolderId == id && item.Stage == ItemEventStage.BeforeDamage &&
                    item.DamageKind == CombatDamageKind.EnvironmentDamage && item.AppliedMagnitude > 0,
                CombatHeldEnvironmentEvent held => held.CharacterId == id && held.Origin != null,
                CombatTraitStateEvent shield => shield.Final.EntityId == id && shield.Origin != null &&
                    shield.Expected.StatusEffects.Entries.OfType<ShieldStatus>().Sum(s => (long)s.RemainingBudget) >
                    shield.Final.StatusEffects.Entries.OfType<ShieldStatus>().Sum(s => (long)s.RemainingBudget),
                CombatEffectsEvent traits => traits.Traits.Any(t => t.TargetId == id &&
                    t.TraitActivationKind == TraitActivationKind.DamageModified && t.Magnitude > 0),
                _ => false
            });
        }

        private static FireStatus Fire(CharacterCombatFacts character) => character.StatusEffects.Entries.OfType<FireStatus>().SingleOrDefault();
        private static bool SameFire(FireStatus a, FireStatus b) => a == null ? b == null : a.SameValues(b);
        private static bool SameWarnings(MeteorGimmickState a, MeteorGimmickState b) => a.Warnings.Count == b.Warnings.Count &&
            a.Warnings.Zip(b.Warnings, (x, y) => x.SameValues(y)).All(equal => equal);
        private static bool SameFlames(IEnumerable<MeteorFlameArea> expected, IReadOnlyList<MeteorFlameArea> final)
        {
            var copy = expected.ToArray();
            return copy.Length == final.Count && copy.Zip(final, (x, y) => x.SameValues(y)).All(equal => equal);
        }

        private static void Require(bool condition, string fact)
        {
            if (!condition) throw new InvalidOperationException("운석 게시 근거가 잘못되었습니다. " + fact);
        }
    }
}
