using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal sealed class MeteorGimmickRule : IStageGimmickRule, IStageGimmickEntryRule
    {
        private readonly IReadOnlyList<IMovementTileRule> _rules;
        internal MeteorGimmickRule(IReadOnlyList<IMovementTileRule> rules = null) { _rules = rules; }
        public StageGimmickKey Key => StageGimmickKey.Meteor;

        public bool RunsAt(StageGimmickBoundaryPhase phase) =>
            phase is StageGimmickBoundaryPhase.Expiry or StageGimmickBoundaryPhase.Scheduled or
                StageGimmickBoundaryPhase.EndOccupancy or StageGimmickBoundaryPhase.Warning;

        public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
            StageGimmickId id, ref uint randomState)
        {
            var spec = RequireSpec(definition);
            long firstWarning = checked(initialState.Turn.CompletedControllerTurnCount +
                spec.CycleControllerTurns - spec.WarningLeadControllerTurns);
            return new MeteorGimmickState(id, firstWarning, Array.Empty<MeteorFootprint>(), Array.Empty<MeteorFlameArea>());
        }

        public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition, StageGimmickId id,
            StageGimmickBoundary? boundary)
        {
            var spec = RequireSpec(definition);
            var end = boundary ?? throw new InvalidOperationException("운석에는 진영 종료 경계가 필요합니다.");
            var state = (MeteorGimmickState)input.State.Gimmicks.Get(id);
            long count = end.CompletedControllerTurnCount;
            if (end.Phase == StageGimmickBoundaryPhase.Expiry) return Expire(input, state, end);
            if (end.Phase == StageGimmickBoundaryPhase.EndOccupancy) return RefreshOccupants(input, spec, state, end.EndedSide);
            if (count < state.NextBoundaryCount) return null;
            if (count > state.NextBoundaryCount) throw new InvalidOperationException("운석의 예정 경계를 건너뛸 수 없습니다.");
            if (end.Phase == StageGimmickBoundaryPhase.Warning && state.Warnings.Count == 0) return Warn(input, spec, state, end);
            if (end.Phase == StageGimmickBoundaryPhase.Scheduled && state.Warnings.Count > 0) return Impact(input, spec, state, end);
            return null;
        }

        internal static int HealthDamage(int maximumHealth, MeteorGimmickSpec spec) =>
            checked((int)((long)maximumHealth * spec.MaximumHealthDamagePercent / 100));

        private static MeteorGimmickSpec RequireSpec(StageGimmickSpec definition) =>
            definition as MeteorGimmickSpec ?? throw new InvalidOperationException("운석 설정 형식이 다릅니다.");

        private static CombatCommandTransition Expire(CombatCommandInput input, MeteorGimmickState state, StageGimmickBoundary end)
        {
            var active = state.Flames.Where(f => f.ExpiresAtBoundaryCount > end.CompletedControllerTurnCount).ToArray();
            if (active.Length == state.Flames.Count) return null;
            var final = new MeteorGimmickState(state.Id, state.NextBoundaryCount, state.Warnings, active);
            return StateChange(input, new CombatMeteorEvent(MeteorGimmickStep.Expired, end, state, final));
        }

        private static CombatCommandTransition Warn(CombatCommandInput input, MeteorGimmickSpec spec,
            MeteorGimmickState state, StageGimmickBoundary end)
        {
            var progress = input.State.EnvironmentProgress;
            uint random = progress.RandomState;
            var blocked = BlockedCells(input.State.Structures, input.State.Environment, input.EnvironmentBindings, input.State.Gimmicks);
            var warned = Plan(input.State.BoardWidth, input.State.BoardHeight, blocked, spec,
                checked(end.CompletedControllerTurnCount + spec.WarningLeadControllerTurns), ref random);
            bool skipped = warned.Count == 0;
            long next = skipped ? checked(end.CompletedControllerTurnCount + spec.CycleControllerTurns) : warned[0].ImpactBoundaryCount;
            var final = new MeteorGimmickState(state.Id, next, warned, state.Flames);
            return StateChange(input, new CombatMeteorEvent(skipped ? MeteorGimmickStep.CycleSkipped : MeteorGimmickStep.Warning,
                end, state, final, progress.RandomState, random));
        }

        private static CombatCommandTransition StateChange(CombatCommandInput input, CombatMeteorEvent change)
        {
            var progress = input.State.EnvironmentProgress;
            var state = input.State.With(gimmicks: input.State.Gimmicks.Replace(change.Final),
                environmentProgress: change.FinalRandomState is uint random
                    ? new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, random) : progress);
            return CombatTurnCalculator.Changed(input, input.Characters.ToDictionary(c => c.EntityId), state, new CombatEvent[] { change });
        }

        internal static IReadOnlyList<MeteorFootprint> Plan(int width, int height, HashSet<CellCoord> blocked,
            MeteorGimmickSpec spec, long impactAt, ref uint random)
        {
            int wanted = checked(spec.MinimumMeteorCount + EnvironmentRandom.Choose(ref random,
                checked(spec.MaximumMeteorCount - spec.MinimumMeteorCount + 1)));
            int side = spec.FootprintSideLength;
            long availableCells = (long)width * height - blocked.Count;
            if (availableCells / ((long)side * side) < spec.MinimumMeteorCount) return Array.Empty<MeteorFootprint>();
            var candidates = new List<CellCoord[]>();
            for (int y = 0; y <= height - side; y++)
            for (int x = 0; x <= width - side; x++)
            {
                var cells = new CellCoord[checked(side * side)];
                for (int row = 0; row < side; row++)
                for (int col = 0; col < side; col++) cells[row * side + col] = new CellCoord(x + col, y + row);
                if (!cells.Any(blocked.Contains)) candidates.Add(cells);
            }
            if (candidates.Count < spec.MinimumMeteorCount) return Array.Empty<MeteorFootprint>();
            var selected = new List<CellCoord[]>();
            var remaining = candidates.ToList();
            while (selected.Count < wanted && remaining.Count > 0)
            {
                var cells = remaining[EnvironmentRandom.Choose(ref random, remaining.Count)];
                selected.Add(cells);
                var occupied = cells.ToHashSet();
                remaining.RemoveAll(c => c.Any(occupied.Contains));
            }
            if (selected.Count < spec.MinimumMeteorCount)
            {
                // 첫 선택에 길이 막혀도 최소 개수를 놓을 수 있는 판을 건너뛰지 않는다.
                // 경고가 없는 상태에서만 원래 후보 순서로 최소 배치를 찾으며 난수를 더 쓰지 않는다.
                selected.Clear();
                if (!FitMinimum(0, new HashSet<CellCoord>())) return Array.Empty<MeteorFootprint>();
            }
            return selected.Select((cells, index) => new MeteorFootprint(impactAt, index + 1, cells)).ToArray();

            bool FitMinimum(int start, HashSet<CellCoord> occupied)
            {
                int needed = spec.MinimumMeteorCount - selected.Count;
                if (needed == 0) return true;
                for (int index = start; index <= candidates.Count - needed; index++)
                {
                    var cells = candidates[index];
                    if (cells.Any(occupied.Contains)) continue;
                    selected.Add(cells);
                    occupied.UnionWith(cells);
                    if (FitMinimum(index + 1, occupied)) return true;
                    occupied.ExceptWith(cells);
                    selected.RemoveAt(selected.Count - 1);
                }
                return false;
            }
        }

        /// <summary>
        /// 운석 칸이 될 수 없는 칸이다: 살아 있는 구조물, 일반 진입이 막힌 지형, 기믹 확정 점유(사슬 머리 등).
        /// 유닛 점유는 막지 않는다(피해 대상). 경고 선정과 충돌 직전 재검증이 같은 판정을 쓴다.
        /// </summary>
        internal static HashSet<CellCoord> BlockedCells(IEnumerable<StructureCombatFacts> structures,
            MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, StageGimmickStateSet gimmicks)
        {
            var blocked = structures.Where(s => !s.Health.IsDead).SelectMany(s => s.OccupiedCells).ToHashSet();
            foreach (var cell in EnvironmentTerrainRules.VoluntaryBlockedCellSteps(environment, bindings))
                if (cell.HasValue) blocked.Add(cell.Value);
            blocked.UnionWith(GimmickOccupancyRules.BlockingCells(gimmicks));
            return blocked;
        }

        private CombatCommandTransition Impact(CombatCommandInput input, MeteorGimmickSpec spec,
            MeteorGimmickState state, StageGimmickBoundary end)
        {
            // 환경 피해가 턴 종료 요청의 행위자에게 귀속되지 않게 한다.
            var projection = MovementProjection.ForArrival(input.WithActor(EntityId.Empty),
                input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>(), rules: _rules);
            var blocked = BlockedCells(input.State.Structures, input.State.Environment, input.EnvironmentBindings, input.State.Gimmicks);
            var flames = state.Flames.ToList();
            long count = end.CompletedControllerTurnCount;
            foreach (var footprint in state.Warnings)
            {
                bool cancelled = footprint.Cells.Any(blocked.Contains);
                var victims = cancelled ? Array.Empty<EntityId>() : projection.Damage.Characters.Values.Where(c =>
                    c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.OccupiedCells.Any(footprint.Cells.Contains))
                    .Select(c => c.EntityId).OrderBy(id => id).ToArray();
                var impact = new CombatMeteorImpactEvent(state.Id, footprint, cancelled, victims);
                projection.Events.Add(impact);
                if (cancelled) continue;
                var fireOrigin = footprint.FireOrigin(state.Id);
                var damageOrigin = new CombatDamageOrigin(null, null, projection.NextAttackGroup());
                foreach (var victim in victims)
                {
                    int amount = HealthDamage(projection.Character(victim).Health.MaxHealth, spec);
                    foreach (var _ in projection.ChangeHealthSteps(new MovementHealthRequest(victim, amount), false,
                        damageOrigin, contextEvent: impact, effectOrigin: fireOrigin)) { }
                    PeriodicStatusApplication.RefreshFire(projection.Damage.Characters, projection.Events, victim, fireOrigin, spec.FireAffectedSideStarts);
                }
                flames.Add(new MeteorFlameArea(footprint, checked(count + spec.FlameControllerTurns)));
            }
            var final = new MeteorGimmickState(state.Id, checked(count + spec.CycleControllerTurns - spec.WarningLeadControllerTurns),
                Array.Empty<MeteorFootprint>(), flames);
            projection.ReplaceGimmick(state, final);
            projection.Events.Add(new CombatMeteorEvent(MeteorGimmickStep.Resolved, end, state, final));
            return CombatCommandComposition.Merge(input, new[] { projection.Complete() });
        }

        private static CombatCommandTransition RefreshOccupants(CombatCommandInput input, MeteorGimmickSpec spec, MeteorGimmickState state, Owner side)
        {
            if (state.Flames.Count == 0) return null;
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            foreach (var current in input.Characters.Where(c => c.Owner == side &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).OrderBy(c => c.EntityId))
            {
                var area = state.FlameAt(current.OccupiedCells);
                if (area != null) PeriodicStatusApplication.RefreshFire(characters, events, current.EntityId,
                    area.Footprint.FireOrigin(state.Id), spec.FireAffectedSideStarts);
            }
            return events.Count == 0 ? null : CombatTurnCalculator.Changed(input, characters, input.State, events);
        }

        /// <summary>불길 칸에 도착한 생존자에게 화상을 부여·갱신한다(설계 §5.5 진입 갱신).</summary>
        public void ApplyEntry(MovementProjection projection, StageGimmickSpec definition, StageGimmickId id, EntityId arriver)
        {
            var spec = RequireSpec(definition);
            if (projection.Terminal ||
                projection.Gimmicks.Entries.FirstOrDefault(e => e.Id == id) is not MeteorGimmickState state || state.Flames.Count == 0)
                return;
            var character = projection.Character(arriver);
            if (character.Health.IsDead || character.Area != CharacterRuntimeArea.Field) return;
            var flame = state.FlameAt(character.OccupiedCells);
            if (flame == null) return;
            PeriodicStatusApplication.RefreshFire(projection.Damage.Characters, projection.Events, arriver,
                flame.Footprint.FireOrigin(state.Id), spec.FireAffectedSideStarts);
        }

        /// <summary>
        /// 이 이동에서 불길 진입이 새로 부여·연장한 화상만 되돌림 영수증으로 인정한다.
        /// 더 긴 기존 화상이 남은 경우와 다른 출처의 화상은 이동의 몫이 아니다.
        /// </summary>
        public void CollectEntryReceipts(CombatCommandInput input, StageGimmickSpec definition, StageGimmickId id,
            MovementEffectId movement, CharacterCombatFacts before, CharacterCombatFacts after, bool requireContact,
            ICollection<MovementEffectReceipt> receipts)
        {
            var spec = RequireSpec(definition);
            var expected = after.StatusEffects.Entries.OfType<FireStatus>().SingleOrDefault();
            if (expected?.Origin is not EffectOriginKey origin || origin.Kind != EffectOwnerKind.StageGimmick ||
                origin.OwnerInstanceId != id.Value || origin.DefinitionKey != (int)Key ||
                expected.RemainingAffectedSideStarts != spec.FireAffectedSideStarts) return;
            var previous = before.StatusEffects.Entries.OfType<FireStatus>().SingleOrDefault();
            if (expected.SameValues(previous) || previous?.RemainingAffectedSideStarts > expected.RemainingAffectedSideStarts) return;
            if (input.State.Gimmicks.Entries.FirstOrDefault(e => e.Id == id) is not MeteorGimmickState state) return;
            bool fromFlame = requireContact
                ? state.FlameAt(after.OccupiedCells)?.Footprint.FireOrigin(state.Id) == origin
                : state.Flames.Any(f => f.Footprint.FireOrigin(state.Id) == origin);
            if (fromFlame) receipts.Add(new MovementFireReceipt(movement, after.EntityId, previous, expected));
        }

        public IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount)
        {
            var spec = RequireSpec(definition);
            var state = (MeteorGimmickState)input.State.Gimmicks.Get(id);
            foreach (var flame in state.Flames)
                foreach (var cell in flame.Footprint.Cells)
                    yield return new HazardCellFact(id, Key, input.State.Turn.CompletedControllerTurnCount, cell, meteorFlame: flame);
            long next = state.NextBoundaryCount;
            if (state.Warnings.Count != 0)
            {
                if (next > lastBoundaryCount) yield break;
                foreach (var footprint in state.Warnings)
                    foreach (var cell in footprint.Cells)
                        yield return new HazardCellFact(id, Key, next, cell, gaps: CombatAssessmentGap.UnsupportedEffect,
                            meteor: spec, meteorFootprint: footprint);
                next = checked(next + spec.CycleControllerTurns - spec.WarningLeadControllerTurns);
            }
            for (; next <= lastBoundaryCount; next = checked(next + spec.CycleControllerTurns))
                yield return new HazardCellFact(id, Key, next, null, gaps: CombatAssessmentGap.FutureWarning);
        }
    }
}
