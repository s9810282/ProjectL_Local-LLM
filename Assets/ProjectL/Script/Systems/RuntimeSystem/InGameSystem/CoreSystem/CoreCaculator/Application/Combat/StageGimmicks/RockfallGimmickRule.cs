using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal sealed class RockfallGimmickRule : IStageGimmickRule
    {
        public StageGimmickKey Key => StageGimmickKey.Rockfall;

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

        private static CombatCommandTransition Impact(CombatCommandInput input, RockfallGimmickSpec spec,
            RockfallGimmickState before, StageGimmickBoundary end)
        {
            var cells = before.WarnedCells.ToHashSet();
            var victims = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                c.OccupiedCells.Any(cells.Contains)).OrderBy(c => c.EntityId).ToArray();
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var deaths = new Dictionary<EntityId, CellCoord>();
            var origin = before.BindOrigin;
            foreach (var victim in victims)
            {
                // 수치 피해 경로를 거치지 않아 방어·방어막·피해 감소와 공격 귀속을 만들지 않는다.
                int loss = (int)((long)victim.Health.MaxHealth * spec.MaximumHealthLossPercent / 100);
                var hurt = CombatCommandCalculator.Hurt(victim, loss);
                CombatCommandEvents.Health(events, victim, hurt);
                if (hurt.Health.IsDead) deaths.Add(victim.EntityId, victim.Anchor.Value);
                else
                {
                    int ends = checked(spec.BindAffectedSideTurns + (victim.Owner == end.EndedSide ? 1 : 0));
                    var bound = hurt.WithStatusEffects(AppliedStatusRules.Apply(hurt.StatusEffects,
                        new ActionRestrictionStatus(StatusEffectKey.Bind, origin, ends)));
                    // 같은 값으로 갱신되어도 충돌의 속박 적용 근거를 남긴다(체력 감소가 0일 수 있다).
                    events.Add(new CombatTraitStateEvent(hurt, bound, expectedStats: CombatEffectiveStatsCalculator.Snapshot(hurt),
                        finalStats: CombatEffectiveStatsCalculator.Snapshot(bound)));
                    hurt = bound;
                }
                projected[victim.EntityId] = hurt;
            }
            bool terminal = BattleOutcomeCalculator.Resolve(projected.Values.ToArray(), input.State.Structures, input.State.Objective).IsStageEnded;
            var after = terminal ? null : new RockfallGimmickState(before.Id, RockfallGimmickPhase.Cooldown,
                checked(end.CompletedControllerTurnCount + spec.CooldownControllerTurns), Array.Empty<CellCoord>());
            events.Insert(0, new CombatRockfallEvent(RockfallGimmickStep.Impact, before, after, victims.Select(c => c.EntityId).ToArray()));
            var state = input.State.With(gimmicks: terminal ? input.State.Gimmicks.Without(before.Id) : input.State.Gimmicks.Replace(after));
            // 기존 사망·영혼·보상·승패 정산을 한 번만 실행한다. 환경 효과는 대체 공격자도 두지 않는다.
            var settled = CombatCommandCalculator.Finish(input.WithActor(EntityId.Empty), projected,
                input.SoulCounts.ToDictionary(p => p.Key, p => p.Value), input.Souls,
                input.State.ThornFields.ToDictionary(f => f.Cell), input.State.Structures.ToArray(), new List<long>(),
                deaths, new SoulEffectCalculator(input.Artifacts), null, proposedState: state, events: events);
            return CombatCommandComposition.Merge(input, new[] { settled });
        }
    }
}
