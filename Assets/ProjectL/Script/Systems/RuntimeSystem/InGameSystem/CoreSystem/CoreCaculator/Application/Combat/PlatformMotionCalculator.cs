using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>이미 끝난 진영 종료 정리 뒤, 다음 진영 시작 앞의 플랫폼 한 묶음을 계산한다.</summary>
    internal static class PlatformMotionCalculator
    {
        internal static CombatCommandTransition TryAdvanceBeforeTurn(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            Owner? observer = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var turn = input.State.Turn;
            var platforms = input.State.Environment.Tiles.OfType<MovingPlatformFact>().OrderBy(p => p.Id).ToArray();
            if (!PlatformMotionRules.IsPreludeDue(input.State, platforms.Length)) return null;
            MovementEnvironmentCalculator.RequireRules(rules);
            input = input.WithResolutionWindow(CombatResolutionWindow.BeforeControllerTurn);
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>(), rules: rules);
            projection.TornadoObserver = observer;
            var order = new List<MovingPlatformFact>();
            foreach (var group in platforms.GroupBy(p => p.Spec.Length).OrderByDescending(g => g.Key))
            {
                var tied = group.ToArray();
                for (int i = tied.Length - 1; i > 0; i--)
                {
                    int selected = projection.ChooseRandom(i + 1);
                    (tied[i], tied[selected]) = (tied[selected], tied[i]);
                }
                order.AddRange(tied);
            }
            long boundary = turn.CompletedControllerTurnCount;
            var draws = projection.Events.OfType<CombatEnvironmentRandomEvent>().ToArray();
            projection.Events.Insert(0, new CombatPlatformBoundaryEvent(boundary, order.Select(p => p.Id).ToArray(), draws));
            for (int index = 0; index < order.Count && !projection.Terminal; index++)
                Move(projection, (MovingPlatformFact)projection.Source(order[index].Id), boundary, index, rules);
            var result = projection.Complete();
            return result.WithFinalState(result.FinalState.With(lastPlatformBoundaryCount: boundary));
        }

        private static void Move(MovementProjection projection, MovingPlatformFact initial, long boundary, int orderIndex,
            IReadOnlyList<IMovementTileRule> rules)
        {
            int start = projection.Events.Count;
            var steps = new List<CombatPlatformStepEvent>();
            var current = initial;
            var riders = PlatformMotionRules.Occupants(projection.Damage.Characters.Values, initial)
                .Where(PlatformMotionRules.IsTransportable).Select(c => c.EntityId).OrderBy(id => id).ToHashSet();
            int remaining = initial.Spec.Length;
            var reason = PlatformMotionStopReason.BudgetConsumed;
            while (remaining > 0)
            {
                if (PlatformMotionRules.HasNontransportableOccupant(projection.Damage.Characters.Values,
                    projection.CurrentStructures, current))
                {
                    reason = PlatformMotionStopReason.NontransportableOccupant;
                    break;
                }
                var offset = PlatformMotionRules.TravelOffset(current.Spec, current.TravelSign);
                if (!CanEnter(projection, current, current.Cell + offset, riders))
                {
                    if (!CanEnter(projection, current, current.Cell - offset, riders))
                    {
                        reason = PlatformMotionStopReason.Blocked;
                        break;
                    }
                    Turn();
                    offset = PlatformMotionRules.TravelOffset(current.Spec, current.TravelSign);
                }
                var previous = current;
                var next = new MovingPlatformFact(current.Spec, current.Cell + offset, current.TravelSign);
                var carried = riders.OrderBy(id => id).Select(id => projection.Character(id)).ToArray();
                var source = projection.ApplyPlatformFrame(current, next, carried.Select(c => c.EntityId).ToArray());
                int frameIndex = projection.Events.IndexOf(source);
                current = next;
                remaining--;
                projection.ReconcilePresence(previous.OccupiedCells.Concat(current.OccupiedCells).Distinct().ToArray());
                // 앞선 효과로 운반에서 빠진 개체의 바닥도 현재 지지면으로 확인한다.
                foreach (var occupant in PlatformMotionRules.Occupants(projection.Damage.Characters.Values, previous)
                    .Where(c => !riders.Contains(c.EntityId)).OrderBy(c => c.EntityId).ToArray())
                    projection.ResolveForcedTerrainEntry(occupant.EntityId, occupant.Anchor.Value);
                foreach (var passenger in carried)
                {
                    var target = passenger.Anchor.Value + offset;
                    var actual = projection.Character(passenger.EntityId);
                    if (projection.Terminal) break;
                    if (actual.Health.IsDead || actual.Area != CharacterRuntimeArea.Field || actual.Anchor != target) continue;
                    if (!projection.ResolveForcedTerrainEntry(passenger.EntityId, target))
                    {
                        MovementEnvironmentCalculator.ResolveEntry(projection, target, rules, passenger.EntityId);
                        actual = projection.Character(passenger.EntityId);
                        if (!projection.Terminal && !actual.Health.IsDead && actual.Anchor == target)
                            projection.ResolveForcedArrival(passenger.EntityId);
                    }
                }
                if (!projection.Terminal)
                    foreach (var _ in DeathTraitApplication.ResolveSteps(projection)) { }
                projection.ResolveDeathBoundary();
                // 같은 플랫폼의 다른 칸으로 옮겨진 탑승자는 계속 운반하며 다음 칸은 그 실제 위치에서 출발한다(PF-1).
                var passengerSteps = carried.Select(before =>
                {
                    bool ended = PlatformMotionRules.EndsTransport(projection.Character(before.EntityId), current);
                    if (ended) riders.Remove(before.EntityId);
                    return new PlatformPassengerStep(before.EntityId, before.Anchor.Value, before.Anchor.Value + offset, ended);
                }).ToArray();
                var moved = new CombatPlatformStepEvent(boundary, orderIndex, steps.Count, PlatformStepKind.Translation,
                    source, passengerSteps, projection.Events.Last());
                steps.Add(moved);
                projection.Events.Insert(frameIndex, moved);
                // 끝점의 방향은 마지막 한 칸으로 예산을 다 쓴 경우에도 즉시 안쪽을 향한다.
                if (PlatformMotionRules.FacesRouteEnd(current)) Turn();
                if (projection.Terminal)
                {
                    reason = PlatformMotionStopReason.Terminal;
                    break;
                }
            }
            projection.Events.Insert(start, new CombatPlatformMotionEvent(boundary, orderIndex, initial, current, steps, reason));

            void Turn()
            {
                var next = new MovingPlatformFact(current.Spec, current.Cell, -current.TravelSign);
                var change = projection.ApplyPlatformFrame(current, next, Array.Empty<EntityId>());
                var step = new CombatPlatformStepEvent(boundary, orderIndex, steps.Count, PlatformStepKind.Turnaround,
                    change, Array.Empty<PlatformPassengerStep>(), change);
                projection.Events.Insert(projection.Events.IndexOf(change), step);
                steps.Add(step);
                current = next;
            }
        }

        private static bool CanEnter(MovementProjection projection, MovingPlatformFact platform, CellCoord anchor, ISet<EntityId> riders)
        {
            if (!platform.Spec.ContainsAnchor(anchor)) return false;
            var cells = platform.Spec.FootprintAt(anchor);
            if (GimmickOccupancyRules.Overlaps(projection.Gimmicks, cells, projection.GimmickReservations)) return false;
            var environment = projection.Environment;
            if (cells.Any(c => !projection.Input.State.IsInside(c)) ||
                cells.Any(c => !BoardSurfaceRules.IsPlatformRouteSurface(environment, projection.Bindings, c))) return false;
            if (environment.Tiles.OfType<MovingPlatformFact>().Any(p => p.Id != platform.Id && p.OccupiedCells.Any(cells.Contains)))
                return false;
            if (projection.CurrentStructures.Any(s => !s.IsDestroyed && s.OccupiedCells.Any(cells.Contains))) return false;
            return !projection.Damage.Characters.Values.Any(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                !riders.Contains(c.EntityId) && c.OccupiedCells.Any(cells.Contains));
        }
    }
}
