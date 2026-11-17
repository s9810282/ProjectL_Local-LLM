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
    internal sealed class LavaChainGimmickRule : IStageGimmickRule
    {
        private readonly IReadOnlyList<IMovementTileRule> _rules;
        internal LavaChainGimmickRule(IReadOnlyList<IMovementTileRule> rules) { _rules = rules; }
        public StageGimmickKey Key => StageGimmickKey.LavaChain;
        public bool RunsAt(StageGimmickBoundaryPhase phase) =>
            phase is StageGimmickBoundaryPhase.Scheduled or StageGimmickBoundaryPhase.Warning;

        public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
            StageGimmickId id, ref uint randomState)
        {
            var spec = (LavaChainGimmickSpec)definition;
            var state = new LavaChainGimmickState(id, spec.InitialHeadCell, checked(initialState.Turn.CompletedControllerTurnCount +
                spec.CycleControllerTurns - spec.WarningLeadControllerTurns));
            state.ValidateBoard(initialState.BoardWidth, initialState.BoardHeight);
            // 머리와 벽·낭떠러지·구조물·캐릭터의 초기 겹침은 지형 분류를 아는 초기 배치 검증
            // (CellEnvironmentQuery.ValidateInitialPlacement)이 모든 기믹 점유와 함께 한 곳에서 거른다.
            return state;
        }

        public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, StageGimmickBoundary? boundary)
        {
            var end = boundary ?? throw new InvalidOperationException("사슬에는 진영 종료 경계가 필요합니다.");
            var spec = (LavaChainGimmickSpec)definition;
            var before = (LavaChainGimmickState)input.State.Gimmicks.Get(id);
            if (end.CompletedControllerTurnCount < before.NextBoundaryCount) return null;
            if (end.CompletedControllerTurnCount != before.NextBoundaryCount)
                throw new InvalidOperationException("사슬의 예정 경계를 건너뛸 수 없습니다.");
            if (end.Phase == StageGimmickBoundaryPhase.Warning && !before.IsWarning) return Warn(input, spec, before, end);
            if (end.Phase != StageGimmickBoundaryPhase.Scheduled || !before.IsWarning) return null;
            if (_rules == null) throw new InvalidOperationException("사슬 실행에는 이동 규칙 목록이 필요합니다.");
            return Extend(input, spec, before, end);
        }

        /// <summary>머리를 직전 칸에서 멈추게 하는 칸이다: 벽(지형 분류 판정 공유), 살아 있는 구조물(기둥 포함), 기믹 확정 점유.</summary>
        internal static HashSet<CellCoord> HardBlockers(CombatCommandState state, MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, IEnumerable<StructureCombatFacts> structures)
        {
            var blocked = new HashSet<CellCoord>();
            foreach (var cell in EnvironmentTerrainRules.TerrainCellSteps(environment, bindings, StaticTerrainKind.Wall))
                if (cell.HasValue) blocked.Add(cell.Value);
            foreach (var structure in structures)
                if (!structure.Health.IsDead) blocked.UnionWith(structure.OccupiedCells);
            // 원점도 예약하지만 다음 칸부터 확장하므로 현재 머리가 스스로를 막지는 않는다.
            foreach (var cell in GimmickOccupancyRules.BlockingCells(state.Gimmicks)) blocked.Add(cell);
            return blocked;
        }

        internal static CellCoord? ChooseHeading(CombatCommandState state, CellCoord head, HashSet<CellCoord> blocked, ref uint random)
        {
            // 후보는 방향 수(8)만큼이다. 전체 보드나 난수 재시도 루프를 만들지 않는다.
            var legal = new CellCoord[LavaChainGeometry.Directions.Count];
            int count = 0;
            foreach (var direction in LavaChainGeometry.Directions)
            {
                var next = head + direction;
                if (state.IsInside(next) && !blocked.Contains(next)) legal[count++] = direction;
            }
            return count == 0 ? null : legal[EnvironmentRandom.Choose(ref random, count)];
        }

        private static CombatCommandTransition Warn(CombatCommandInput input, LavaChainGimmickSpec spec,
            LavaChainGimmickState before, StageGimmickBoundary end)
        {
            var progress = input.State.EnvironmentProgress;
            uint random = progress.RandomState;
            var heading = ChooseHeading(input.State, before.HeadCell,
                HardBlockers(input.State, input.State.Environment, input.EnvironmentBindings, input.State.Structures), ref random);
            var final = new LavaChainGimmickState(before.Id, before.HeadCell,
                checked(end.CompletedControllerTurnCount + spec.WarningLeadControllerTurns), true, heading);
            var change = new CombatLavaChainEvent(LavaChainGimmickStep.Warning, end, before, final,
                new[] { before.HeadCell }, progress.RandomState, random);
            return CombatTurnCalculator.Changed(input, input.Characters.ToDictionary(c => c.EntityId),
                input.State.With(gimmicks: input.State.Gimmicks.Replace(final), environmentProgress:
                    new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, random)), new CombatEvent[] { change });
        }

        private CombatCommandTransition Extend(CombatCommandInput input, LavaChainGimmickSpec spec,
            LavaChainGimmickState before, StageGimmickBoundary end)
        {
            var projection = MovementProjection.ForArrival(input.WithActor(EntityId.Empty),
                input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>(), rules: _rules);
            var path = new List<CellCoord> { before.HeadCell };
            var reservations = projection.GimmickReservations = new HashSet<CellCoord>();
            reservations.Add(before.HeadCell);
            var occupants = new Dictionary<CellCoord, List<EntityId>>();
            RefreshOccupants();
            var blocked = HardBlockers(input.State, projection.Environment, input.EnvironmentBindings, input.State.Structures);
            if (before.Heading is CellCoord direction)
            {
                int limit = LavaChainGeometry.StepsToBoundary(input.State.BoardWidth, input.State.BoardHeight, before.HeadCell, direction);
                for (int step = 1; step <= limit; step++)
                {
                    var next = path[path.Count - 1] + direction;
                    if (blocked.Contains(next)) break;
                    reservations.Add(next);
                    if (occupants.TryGetValue(next, out var victims))
                    {
                        // 같은 머리 단계에서 한 번씩 처리한다. 다음 단계의 실제 재접촉은 다시 처리한다.
                        foreach (var victim in victims)
                            if (!projection.Character(victim).Health.IsDead)
                                ResolveContact(projection, spec, before, next, path.Count, direction, victim);
                        RefreshOccupants();
                        // 재구성·면역 생존자가 남으면 머리는 직전 칸에서 멈춘다.
                        if (occupants.ContainsKey(next)) break;
                        var current = projection.CurrentInput();
                        blocked = HardBlockers(current.State, projection.Environment, current.EnvironmentBindings, current.State.Structures);
                    }
                    path.Add(next);
                }
            }
            reservations.Clear();
            var final = new LavaChainGimmickState(before.Id, path[path.Count - 1],
                checked(end.CompletedControllerTurnCount + spec.CycleControllerTurns - spec.WarningLeadControllerTurns));
            projection.ReplaceGimmick(before, final);
            projection.Events.Add(new CombatLavaChainEvent(LavaChainGimmickStep.Retracted, end, before, final, path));
            return CombatCommandComposition.Merge(input, new[] { projection.Complete() });

            void RefreshOccupants()
            {
                // 빈 경로 칸마다 전체 캐릭터를 재탐색하지 않는다. 접촉 효과 뒤에만 현재 점유를 갱신한다.
                occupants.Clear();
                foreach (var character in projection.Damage.Characters.Values.OrderBy(c => c.EntityId))
                {
                    if (character.Health.IsDead || character.Area != CharacterRuntimeArea.Field) continue;
                    foreach (var cell in character.OccupiedCells)
                    {
                        if (!occupants.TryGetValue(cell, out var ids)) occupants.Add(cell, ids = new List<EntityId>());
                        ids.Add(character.EntityId);
                    }
                }
            }
        }

        private void ResolveContact(MovementProjection projection, LavaChainGimmickSpec spec, LavaChainGimmickState state,
            CellCoord cell, int pathIndex, CellCoord heading, EntityId victim)
        {
            var original = projection.Character(victim);
            var input = projection.CurrentInput();
            var landing = FindLanding(input, original, heading, spec.PushDistance, projection.GimmickReservations, out var failure);
            int contactIndex = projection.Events.Count;
            if (landing is CellCoord target)
            {
                var departed = PositionTileModifierCalculator.Replace(original, PositionTileModifier.None);
                PositionTileModifierCalculator.RecordNormalization(projection.Events, original, departed, projection.Damage.HealthExecutions);
                projection.MergeCharacter(new CharacterStateChange(original, departed));
                var step = new CellCoord(Math.Sign(target.X - original.Anchor.Value.X), Math.Sign(target.Y - original.Anchor.Value.Y));
                for (int index = 0; index < spec.PushDistance; index++)
                {
                    var current = projection.Character(victim);
                    var destination = current.Anchor.Value + step;
                    var moved = current.WithPlacement(current.Area, current.ReserveSlotIndex, destination, current.Rotation,
                        current.OccupiedCells.Select(occupied => occupied + step).ToArray());
                    projection.RecordForcedMove(current, moved);
                    projection.ArrivalCells.Remove(victim);
                    if (projection.ResolveForcedTerrainEntry(victim, destination)) break;
                    projection.ReconcilePresence(new[] { current.Anchor.Value, destination });
                    foreach (var _ in MovementEnvironmentCalculator.ResolveEntrySteps(projection, destination, _rules, victim)) { }
                    var arrived = projection.Character(victim);
                    if (arrived.Health.IsDead || arrived.Anchor != destination) break;
                    if (!projection.ArrivalCells.TryGetValue(victim, out var settled) || settled != destination)
                        projection.ResolveForcedArrival(victim);
                }
            }
            else
            {
                projection.Damage.Defeat(victim, new CombatDamageOrigin(null, null, projection.NextAttackGroup()),
                    new EffectOriginKey(EffectOwnerKind.StageGimmick, state.Id.Value, (int)StageGimmickKey.LavaChain, "lava-chain"));
            }
            var final = projection.Character(victim);
            // 접촉은 원인 앞에 삽입하고, 실제 밀림·지형·체력 사건은 기존 순서를 보존한다.
            projection.Events.Insert(contactIndex, new CombatLavaChainContactEvent(state.Id, pathIndex, cell, victim,
                original.Anchor.Value, landing, final.Health.IsDead ? null : final.Anchor, failure, !final.Health.IsDead));
        }

        internal static CellCoord? FindLanding(CombatCommandInput input, CharacterCombatFacts recipient, CellCoord heading,
            int pushDistance, IReadOnlyCollection<CellCoord> reservations, out LavaChainPushFailure failure)
        {
            failure = UnitBehaviorRules.IsForcedMovementImmune(input, recipient)
                ? LavaChainPushFailure.Immune : LavaChainPushFailure.NoLanding;
            if (failure == LavaChainPushFailure.Immune) return null;
            foreach (var side in LavaChainGeometry.LateralPriority(input.State.BoardWidth, input.State.BoardHeight, recipient.Anchor.Value, heading))
            {
                if (LavaChainGeometry.StepsToBoundary(input.State.BoardWidth, input.State.BoardHeight, recipient.Anchor.Value, side) < pushDistance)
                    continue;
                bool allowed = true;
                var from = recipient.Anchor.Value;
                for (int step = 0; step < pushDistance; step++)
                {
                    var to = from + side;
                    if (!AttackForcedMovement.CanEnterUnit(input, input.State.Environment, input.EnvironmentBindings,
                        input.Characters, input.State.Structures, recipient.EntityId, from, to, reservations))
                    { allowed = false; break; }
                    from = to;
                }
                if (!allowed) continue;
                failure = LavaChainPushFailure.None;
                return from;
            }
            return null;
        }

        public IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount)
        {
            var state = (LavaChainGimmickState)input.State.Gimmicks.Get(id);
            if (state.NextBoundaryCount > lastBoundaryCount) yield break;
            if (!state.IsWarning)
            {
                yield return new HazardCellFact(id, Key, state.NextBoundaryCount, null, gaps: CombatAssessmentGap.FutureWarning);
                yield break;
            }
            if (state.Heading is not CellCoord heading) yield break;
            var blocked = HardBlockers(input.State, input.State.Environment, input.EnvironmentBindings, input.State.Structures);
            var cell = state.HeadCell;
            int distance = LavaChainGeometry.StepsToBoundary(input.State.BoardWidth, input.State.BoardHeight, cell, heading);
            for (int i = 0; i < distance; i++)
            {
                cell += heading;
                if (blocked.Contains(cell)) yield break;
                yield return new HazardCellFact(id, Key, state.NextBoundaryCount, cell, gaps: CombatAssessmentGap.UnknownPosition);
            }
        }
    }
}
