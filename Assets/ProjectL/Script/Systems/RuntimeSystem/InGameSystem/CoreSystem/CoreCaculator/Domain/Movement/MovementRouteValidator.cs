using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    /// <summary>경로 전체를 검증한다. 유효 접두 길이는 편집 정보이며 부분 실행 승인이 아니다.</summary>
    internal static class MovementRouteValidator
    {
        internal static MovementFailureReason Validate(CombatCommandInput input, MovementPath path, out int validStepCount,
            MovementEnvironmentFacts environment = null)
        {
            validStepCount = 0;
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (path == null) return MovementFailureReason.InvalidPath;
            var actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (actor == null) return MovementFailureReason.EntityNotFound;
            if (actor.Anchor != path.Origin) return MovementFailureReason.PathOriginMismatch;
            if (!input.State.IsInside(path.Origin)) return MovementFailureReason.OutOfBounds;
            var ledger = ValidateLedger(actor);
            if (ledger != MovementFailureReason.None) return ledger;
            if (path.StepCount == 0) return MovementFailureReason.SameAnchor;
            var visited = new HashSet<CellCoord> { path.Origin };
            long actionCost = 0;
            for (int i = 1; i < path.Cells.Count; i++)
            {
                var cell = path.Cells[i];
                if (!input.State.IsInside(cell)) return MovementFailureReason.OutOfBounds;
                var previous = path.Cells[i - 1];
                if (Math.Abs((long)cell.X - previous.X) + Math.Abs((long)cell.Y - previous.Y) != 1)
                    return MovementFailureReason.InvalidPath;
                if (!visited.Add(cell)) return MovementFailureReason.RepeatedCell;
                if (!CanEnter(input, cell, environment)) return MovementFailureReason.DestinationOccupied;
                try
                {
                    actionCost = checked(actionCost + EnvironmentTerrainRules.EdgeActionCost(environment ?? input.State.Environment,
                        input.EnvironmentBindings, previous, cell, actor.Stats.MoveCost));
                }
                catch (OverflowException) { return MovementFailureReason.StateInconsistent; }
                var budget = StepBudget(actor, i, actionCost);
                if (budget != MovementFailureReason.None) return budget;
                validStepCount = i;
            }
            return MovementFailureReason.None;
        }

        internal static MovementFailureReason ValidateLedger(CharacterCombatFacts actor)
        {
            long spent = (long)actor.CommittedMoveDistance + actor.PendingMoveDistance;
            if (spent > actor.Stats.MoveRange || actor.Stats.RemainingMoveRange != actor.Stats.MoveRange - spent ||
                (actor.PendingMoveDistance == 0 && actor.PendingMoveActionCost != 0) ||
                actor.AttackRefundEligibleMoveCost < actor.PendingMoveActionCost ||
                !actor.Anchor.HasValue || actor.OccupiedCells.Count != 1 || actor.OccupiedCells[0] != actor.Anchor.Value)
                return MovementFailureReason.StateInconsistent;
            return MovementFailureReason.None;
        }

        internal static MovementFailureReason StepBudget(CharacterCombatFacts actor, int distance, long cost)
        {
            if (distance > actor.Stats.RemainingMoveRange) return MovementFailureReason.MoveRangeExceeded;
            if (cost > actor.Stats.CurrentActionCost) return MovementFailureReason.ActionCostInsufficient;
            if ((long)actor.PendingMoveActionCost + cost > int.MaxValue ||
                (long)actor.AttackRefundEligibleMoveCost + cost > int.MaxValue)
                return MovementFailureReason.StateInconsistent;
            return MovementFailureReason.None;
        }

        internal static bool CanEnter(CombatCommandInput input, CellCoord cell, MovementEnvironmentFacts environment = null) =>
            input.State.IsInside(cell) &&
            !EnvironmentTerrainRules.IsWallOccupied(environment ?? input.State.Environment, input.EnvironmentBindings, cell) &&
            !input.Characters.Any(c => c.EntityId != input.ActorId && c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && c.OccupiedCells.Contains(cell)) &&
            !input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell));
    }
}
