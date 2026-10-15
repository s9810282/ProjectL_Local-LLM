using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>명시적 경로만 계산하며 실제 진입 칸별로 과금한다.</summary>
    public static class CombatMovementCalculator
    {
        public static MovePreview Preview(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            var projection = ProjectRoute(input, rules, true, out var reason, out int validStepCount);
            if (projection == null) return MovePreview.Failure(reason, validStepCount);
            var final = projection.Transition();
            var facts = CombatCommandComposition.Project(final);
            var projectedActor = facts.Characters.Single(c => c.EntityId == input.ActorId);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            int requestedCost = 0;
            for (int i = 1; i < input.Path.Cells.Count; i++)
                requestedCost = checked(requestedCost + EnvironmentTerrainRules.EdgeActionCost(environment,
                    input.EnvironmentBindings, input.Path.Cells[i - 1], input.Path.Cells[i], actor.Stats.MoveCost));
            return MovePreview.Success(projection.PathResult(), projectedActor.Stats.TraitStatuses.ToArray(), projectedActor.Stats.Category,
                projectedActor, final.Events, requestedCost,
                CheckpointReturnCalculator.Preview(facts).CanReturn);
        }

        public static MovementRangeResult Range(CombatCommandInput input)
        {
            var reason = Actor(input, out var actor);
            if (reason != MovementFailureReason.None) return MovementRangeResult.Failure(reason, input.ActorId);
            reason = MovementRouteValidator.ValidateLedger(actor);
            if (reason != MovementFailureReason.None) return MovementRangeResult.Failure(reason, input.ActorId);
            if (!input.State.IsInside(actor.Anchor.Value))
                return MovementRangeResult.Failure(MovementFailureReason.OutOfBounds, input.ActorId);
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            var labels = new Dictionary<CellCoord, List<MovementSearchLabel>>();
            var destinations = new Dictionary<CellCoord, MovementSearchLabel>();
            var queue = new Queue<MovementSearchLabel>();
            var origin = new MovementSearchLabel(actor.Anchor.Value, 0, 0);
            MovementSearchLabel.TryAdd(labels, origin);
            queue.Enqueue(origin);
            var directions = new[] { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                if (!labels[current.Cell].Contains(current)) continue;
                if (current.Distance >= actor.Stats.RemainingMoveRange) continue;
                foreach (var direction in directions)
                {
                    var cell = current.Cell + direction;
                    if (!MovementRouteValidator.CanEnter(input, cell)) continue;
                    int distance = checked(current.Distance + 1);
                    long cost;
                    try
                    {
                        cost = checked(current.Cost + EnvironmentTerrainRules.EdgeActionCost(environment,
                            input.EnvironmentBindings, current.Cell, cell, actor.Stats.MoveCost));
                    }
                    catch (OverflowException) { continue; }
                    if (MovementRouteValidator.StepBudget(actor, distance, cost) != MovementFailureReason.None) continue;
                    long displacement = Distance(actor.MovementCheckpointAnchor, cell);
                    if (displacement > int.MaxValue)
                        return MovementRangeResult.Failure(MovementFailureReason.StateInconsistent, input.ActorId);
                    var candidate = new MovementSearchLabel(cell, distance, cost);
                    if (!MovementSearchLabel.TryAdd(labels, candidate)) continue;
                    queue.Enqueue(candidate);
                    // 표시는 가능한 경로 중 AP가 적은 값을 쓰되 다른 칸 수의 탐색 상태는 유지한다.
                    if (!destinations.TryGetValue(cell, out var shown) || cost < shown.Cost ||
                        cost == shown.Cost && distance < shown.Distance)
                        destinations[cell] = candidate;
                }
            }
            return MovementRangeResult.Success(input.ActorId, destinations.Values.Select(d =>
                new MovementDestination(d.Cell, new[] { d.Cell }, d.Distance,
                    (int)Distance(actor.MovementCheckpointAnchor, d.Cell),
                    checked(actor.PendingMoveActionCost + (int)d.Cost), (int)d.Cost)).ToArray());
        }

        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            var projection = ProjectRoute(input, rules, false, out var reason, out _);
            return projection == null ? CombatCommandCalculator.Reject(input, reason.ToString()) : projection.Transition();
        }

        internal static MovementProjection ProjectRoute(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            bool preview, out MovementFailureReason failure, out int validStepCount)
        {
            validStepCount = 0;
            failure = Actor(input, out var actor);
            if (failure != MovementFailureReason.None) return null;
            var environment = preview ? input.State.Environment.VisibleTo(actor.Owner) : input.State.Environment;
            failure = MovementRouteValidator.Validate(input, input.Path, out validStepCount, environment);
            if (failure != MovementFailureReason.None) return null;
            var projection = new MovementProjection(input, preview);
            projection.ReplacePositionModifier(PositionTileModifier.None);
            for (int i = 1; i < input.Path.Cells.Count; i++)
            {
                if (!MovementRouteValidator.CanEnter(projection.CurrentInput(), input.Path.Cells[i]))
                { projection.Stop(MovementStopReason.NextStepInvalid); break; }
                int stepCost = EnvironmentTerrainRules.EdgeActionCost(projection.Environment, input.EnvironmentBindings,
                    projection.Actor.Anchor.Value, input.Path.Cells[i], projection.Actor.Stats.MoveCost);
                if (MovementRouteValidator.StepBudget(projection.Actor, 1, stepCost) != MovementFailureReason.None)
                { projection.Stop(MovementStopReason.InsufficientResources); break; }
                projection.Enter(input.Path.Cells[i], stepCost);
                MovementEnvironmentCalculator.ResolveEntry(projection, input.Path.Cells[i], rules);
                if (projection.Actor.Stats.MoveMethod != MoveMethod.LiftAndPlace)
                    projection.Stop(MovementStopReason.MovementBlocked);
                if (projection.StopReason != MovementStopReason.Completed) break;
            }
            projection.ResolvePathArrival();
            if (!projection.Actor.Health.IsDead && !projection.Terminal)
            {
                var bindings = CombatCommandEffects.Capture(projection.Actor, CombatCommandCalculator.RequiredProfile(input, projection.Actor));
                var direction = CombatCommandCalculator.AttackRotation(AttackAim.ForCell(input.Path.Cells[projection.DistanceSpent]),
                    input.Path.Cells[projection.DistanceSpent - 1]);
                var effect = new CombatEffectEngine().PlanMoveSucceeded(bindings,
                    new MoveSucceededEffectContext(projection.DistanceSpent, true, projection.ActionCostSpent, direction));
                CombatCommandEvents.Effects(projection.Events, effect);
                projection.ReplaceActor(CharacterSettlementCalculator.Copy(projection.Actor, projection.Actor.Health,
                    CombatCommandEffects.Apply(projection.Actor.Stats, effect)));
            }
            if (projection.ReturnBarrierOccurred || projection.Actor.Owner == Owner.Enemy)
                projection.ReplaceActor(SealPendingMovement(projection.Actor, projection.FinalStop));
            return projection;
        }

        internal static CharacterCombatFacts RecordEnteredStep(CharacterCombatFacts actor, int stepCost)
        {
            if (stepCost < 0) throw new ArgumentOutOfRangeException(nameof(stepCost));
            var stats = CombatCommandEffects.With(actor.Stats,
                ap: checked(actor.Stats.CurrentActionCost - stepCost),
                remainingMoveRange: checked(actor.Stats.RemainingMoveRange - 1));
            return CopyMovement(actor, stats, actor.Anchor, actor.CommittedMoveDistance,
                checked(actor.PendingMoveDistance + 1), checked(actor.PendingMoveActionCost + stepCost),
                checked(actor.AttackRefundEligibleMoveCost + stepCost), actor.MovementCheckpointAnchor);
        }

        internal static CharacterCombatFacts SealPendingMovement(CharacterCombatFacts actor, CellCoord? finalStop = null) =>
            CopyMovement(actor, actor.Stats, actor.Anchor, checked(actor.CommittedMoveDistance + actor.PendingMoveDistance),
                0, 0, actor.AttackRefundEligibleMoveCost, finalStop ?? actor.Anchor ?? actor.MovementCheckpointAnchor);

        internal static CharacterCombatFacts MoveTo(CharacterCombatFacts actor, CellCoord cell) =>
            CopyMovement(actor, actor.Stats, cell, actor.CommittedMoveDistance, actor.PendingMoveDistance,
                actor.PendingMoveActionCost, actor.AttackRefundEligibleMoveCost, actor.MovementCheckpointAnchor);

        internal static CharacterCombatFacts CopyMovement(CharacterCombatFacts actor, UnitStatsSnapshot stats,
            CellCoord? anchor, int committed, int pendingDistance, int pendingCost, int attackRefund, CellCoord checkpoint) =>
            new(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner, actor.OwnershipRole, actor.Area,
                actor.Health, stats, actor.ReserveSlotIndex, anchor, actor.Rotation,
                anchor.HasValue ? new[] { anchor.Value } : Array.Empty<CellCoord>(), checkpoint,
                committed, pendingCost, actor.TemporaryAttackBonus, actor.TemporaryMaxHealthBonus,
                pendingDistance, attackRefund, actor.PositionModifier, actor.HeldEnvironment);

        internal static MovementFailureReason Actor(CombatCommandInput input, out CharacterCombatFacts actor,
            bool requireMovement = true)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (input.State.Phase == CombatFlowPhase.Finished) return MovementFailureReason.StageEnded;
            if (input.State.Turn.StagePhase != StagePhase.Combat ||
                input.State.Phase != (input.ProcessingKind == CombatProcessingKind.Advance ? CombatFlowPhase.Automatic : CombatFlowPhase.PlayerInput))
                return MovementFailureReason.WrongPhase;
            if (actor == null) return MovementFailureReason.EntityNotFound;
            if (actor.Owner != (input.ProcessingKind == CombatProcessingKind.Advance ? Owner.Enemy : Owner.Player))
                return MovementFailureReason.WrongPhase;
            if (actor.Area != CharacterRuntimeArea.Field) return MovementFailureReason.NotInCombat;
            if (actor.Health.IsDead) return MovementFailureReason.Dead;
            if (input.State.ActiveActorId != actor.EntityId) return MovementFailureReason.NotActiveUnit;
            if (requireMovement && actor.Stats.MoveMethod != MoveMethod.LiftAndPlace) return MovementFailureReason.UnsupportedMoveMethod;
            if (actor.OccupiedCells.Count != 1) return MovementFailureReason.StateInconsistent;
            return MovementFailureReason.None;
        }

        internal static long Distance(CellCoord a, CellCoord b) => Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y);
    }
}
