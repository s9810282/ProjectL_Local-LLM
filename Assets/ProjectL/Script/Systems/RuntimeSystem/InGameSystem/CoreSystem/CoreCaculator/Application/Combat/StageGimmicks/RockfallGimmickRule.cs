using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal sealed class RockfallGimmickRule : IStageGimmickRule
    {
        private readonly IReadOnlyList<IMovementTileRule> _rules;
        internal RockfallGimmickRule(IReadOnlyList<IMovementTileRule> rules = null) { _rules = rules; }
        public StageGimmickKey Key => StageGimmickKey.Rockfall;

        public bool RunsAt(StageGimmickBoundaryPhase phase) => phase == StageGimmickBoundaryPhase.Scheduled;

        public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
            StageGimmickId id, ref uint randomState)
        {
            var spec = definition as RockfallGimmickSpec ?? throw new InvalidOperationException("낙석 설정 형식이 다릅니다.");
            return new RockfallGimmickState(id, RockfallGimmickPhase.Cooldown,
                checked(initialState.Turn.CompletedControllerTurnCount + spec.CooldownControllerTurns), Array.Empty<CellCoord>());
        }

        internal static int HealthLoss(int maxHealth, RockfallGimmickSpec spec) =>
            checked((int)((long)maxHealth * spec.MaximumHealthLossPercent / 100));

        internal static int BindEndCount(Owner victimOwner, Owner endedSide, RockfallGimmickSpec spec) =>
            checked(spec.BindAffectedSideTurns + (victimOwner == endedSide ? 1 : 0));

        public IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount)
        {
            var spec = definition as RockfallGimmickSpec ?? throw new InvalidOperationException("낙석 설정 형식이 다릅니다.");
            var state = (RockfallGimmickState)input.State.Gimmicks.Get(id);
            if (state.NextBoundaryCount <= input.State.Turn.CompletedControllerTurnCount)
                throw new InvalidOperationException("낙석의 예정 경계가 현재 종료 누계보다 뒤여야 합니다.");
            long warning = state.NextBoundaryCount;
            if (state.Phase == RockfallGimmickPhase.Warning)
            {
                if (state.NextBoundaryCount <= lastBoundaryCount)
                    foreach (var cell in state.WarnedCells)
                        yield return new HazardCellFact(id, Key, state.NextBoundaryCount, cell, rockfall: spec);
                if (state.NextBoundaryCount > lastBoundaryCount) yield break;
                warning = checked(state.NextBoundaryCount + spec.CooldownControllerTurns);
            }
            if (warning <= lastBoundaryCount)
            {
                yield return new HazardCellFact(id, Key, warning, null, gaps: CombatAssessmentGap.FutureWarning);
                long impact = checked(warning + spec.WarningControllerTurns);
                if (impact <= lastBoundaryCount)
                    yield return new HazardCellFact(id, Key, impact, null, rockfall: spec,
                        gaps: CombatAssessmentGap.FutureWarning | CombatAssessmentGap.RandomOutcome);
            }
        }

        public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, StageGimmickBoundary? boundary)
        {
            var spec = definition as RockfallGimmickSpec ?? throw new InvalidOperationException("낙석 설정 형식이 다릅니다.");
            var end = boundary ?? throw new InvalidOperationException("낙석에는 완료 진영 경계가 필요합니다.");
            var before = (RockfallGimmickState)input.State.Gimmicks.Get(id);
            if (end.CompletedControllerTurnCount < before.NextBoundaryCount) return null;
            if (end.CompletedControllerTurnCount != before.NextBoundaryCount)
                throw new InvalidOperationException("낙석의 예정 경계를 건너뛸 수 없습니다.");
            if (before.Phase == RockfallGimmickPhase.Warning) return Impact(input, spec, before, end);

            uint random = input.State.EnvironmentProgress.RandomState;
            int count = checked(spec.MinimumRockCount + EnvironmentRandom.Choose(ref random,
                checked(spec.MaximumRockCount - spec.MinimumRockCount + 1)));
            var blocked = input.State.Structures.Where(s => !s.Health.IsDead).SelectMany(s => s.OccupiedCells).ToHashSet();
            var candidates = new List<CellCoord[]>();
            int anchorWidth = input.State.BoardWidth - spec.FootprintOffsets.Max(o => o.X);
            int anchorHeight = input.State.BoardHeight - spec.FootprintOffsets.Max(o => o.Y);
            for (int y = 0; y < anchorHeight; y++)
            for (int x = 0; x < anchorWidth; x++)
            {
                var cells = spec.FootprintOffsets.Select(o => new CellCoord(checked(x + o.X), checked(y + o.Y))).ToArray();
                if (cells.All(c => c.X < input.State.BoardWidth && c.Y < input.State.BoardHeight && !blocked.Contains(c) &&
                    !EnvironmentTerrainRules.BlocksVoluntaryEntry(input.State.Environment, input.EnvironmentBindings, c))) candidates.Add(cells);
            }
            var warned = new HashSet<CellCoord>();
            int available = Math.Min(count, candidates.Count);
            for (int i = 0; i < available; i++)
            {
                int index = EnvironmentRandom.Choose(ref random, candidates.Count);
                warned.UnionWith(candidates[index]);
                candidates.RemoveAt(index);
            }
            var after = new RockfallGimmickState(id, RockfallGimmickPhase.Warning,
                checked(end.CompletedControllerTurnCount + spec.WarningControllerTurns), warned.ToArray());
            var progress = input.State.EnvironmentProgress;
            var state = input.State.With(gimmicks: input.State.Gimmicks.Replace(after),
                environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, random));
            return CombatTurnCalculator.Changed(input, input.Characters.ToDictionary(c => c.EntityId), state,
                new CombatEvent[] { new CombatRockfallEvent(RockfallGimmickStep.Warning, before, after, Array.Empty<EntityId>()) });
        }

        private CombatCommandTransition Impact(CombatCommandInput input, RockfallGimmickSpec spec,
            RockfallGimmickState before, StageGimmickBoundary end)
        {
            var cells = before.WarnedCells.ToHashSet();
            var victims = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                c.OccupiedCells.Any(cells.Contains)).OrderBy(c => c.EntityId).ToArray();
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var activations = new List<CombatEvent>();
            var outcomes = new List<(CombatCharacterEvent Evidence, HealthStateSnapshot AfterLoss)>();
            var origin = before.BindOrigin;
            foreach (var victim in victims)
            {
                // 수치 피해 경로를 거치지 않아 방어·방어막·피해 감소와 공격 귀속을 만들지 않는다.
                int loss = HealthLoss(victim.Health.MaxHealth, spec);
                var hurt = CombatCommandCalculator.Hurt(victim, loss);
                var afterLoss = hurt.Health;
                CombatCharacterEvent evidence;
                if (hurt.Health.IsDead && ReconstructionTraitRule.CanTrigger(victim))
                {
                    // 낙석 치명도 재구성이 버틴다(적 전용 특성 설계 Q-ED2 A). 사망이 아니므로 생존자처럼 속박을 받는다.
                    // 발동 사건은 수신자 사건이 모두 끝난 뒤에 둔다. 판독기는 충돌 바로 뒤에 수신자 사건이 차례로 오기를 요구한다.
                    hurt = ReconstructionTraitRule.Trigger(victim);
                    evidence = new CombatCharacterEvent(CombatEventKind.Reconstructed, victim, hurt);
                    events.Add(evidence);
                    CombatCommandEvents.Trait(activations, victim.EntityId, TraitKey.Reconstruction, TraitActivationKind.ReconstructionStarted,
                        victim.Health.CurrentHealth, new[] { victim.EntityId });
                }
                else evidence = CombatCommandEvents.Health(events, victim, hurt);
                if (evidence != null) outcomes.Add((evidence, afterLoss));
                if (!hurt.Health.IsDead)
                {
                    int ends = BindEndCount(victim.Owner, end.EndedSide, spec);
                    var bound = hurt.WithStatusEffects(AppliedStatusRules.Apply(hurt.StatusEffects,
                        new ActionRestrictionStatus(StatusEffectKey.Bind, origin, ends)));
                    // 같은 값으로 갱신되어도 충돌의 속박 적용 근거를 남긴다(체력 감소가 0일 수 있다).
                    events.Add(new CombatTraitStateEvent(hurt, bound, expectedStats: CombatEffectiveStatsCalculator.Snapshot(hurt),
                        finalStats: CombatEffectiveStatsCalculator.Snapshot(bound)));
                    hurt = bound;
                }
                projected[victim.EntityId] = hurt;
            }
            events.AddRange(activations);
            bool terminal = BattleOutcomeCalculator.Resolve(projected.Values.ToArray(), input.State.Structures, input.State.Objective).IsStageEnded;
            var after = terminal ? null : new RockfallGimmickState(before.Id, RockfallGimmickPhase.Cooldown,
                checked(end.CompletedControllerTurnCount + spec.CooldownControllerTurns), Array.Empty<CellCoord>());
            var impact = new CombatRockfallEvent(RockfallGimmickStep.Impact, before, after, victims.Select(c => c.EntityId).ToArray());
            events.Insert(0, impact);
            var state = input.State.With(gimmicks: terminal ? input.State.Gimmicks.Without(before.Id) : input.State.Gimmicks.Replace(after));
            // 사망·영혼·보상·승패 정산은 이동 투영의 명령 완료 정산 한 번으로 끝낸다. 자폭·공포 확인도 여기서 탄다(적 전용 특성 설계 D7·P17).
            // 환경 효과라 행위자를 비운다. 충돌로 죽은 유닛의 영혼 자리는 맞기 전 위치로 정산된다(완료 정산의 기본 위치와 같음).
            var projection = MovementProjection.ForArrival(input.WithActor(EntityId.Empty), projected, events, state, rules: _rules);
            var source = new CombatHealthSource(CombatHealthOperationKind.Environment, contextEvent: impact,
                damageCause: CombatDamageCause.NumericDamage);
            foreach (var (evidence, afterLoss) in outcomes)
            {
                var expected = evidence.Expected;
                int actualLoss = expected.Health.CurrentHealth - afterLoss.CurrentHealth;
                if (evidence.Kind == CombatEventKind.Reconstructed)
                {
                    var steps = CombatHealthRecording.Reconstruction(evidence, afterLoss, source, actualLoss);
                    projection.Damage.HealthExecutions.Add(new CombatHealthExecution(steps, new[] { evidence }));
                }
                else CombatHealthRecording.Add(projection.Damage.HealthExecutions, expected.EntityId, expected.Health, afterLoss,
                    source, evidence, loss: actualLoss);
            }
            return CombatCommandComposition.Merge(input, new[] { projection.Complete() });
        }
    }
}
