using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
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
            var ledger = ValidateActorPlacement(actor);
            if (ledger != MovementFailureReason.None) return ledger;
            if (path.StepCount == 0) return MovementFailureReason.SameAnchor;
            var visited = new HashSet<CellCoord> { path.Origin };
            for (int i = 1; i < path.Cells.Count; i++)
            {
                var cell = path.Cells[i];
                if (!input.State.IsInside(cell)) return MovementFailureReason.OutOfBounds;
                var previous = path.Cells[i - 1];
                if (Math.Abs((long)cell.X - previous.X) + Math.Abs((long)cell.Y - previous.Y) != 1)
                    return MovementFailureReason.InvalidPath;
                if (!visited.Add(cell)) return MovementFailureReason.RepeatedCell;
                if (!CanTraverse(input, cell, i == path.Cells.Count - 1, environment)) return MovementFailureReason.DestinationOccupied;
                var budget = StepBudget(actor, i, environment ?? input.State.Environment);
                if (budget != MovementFailureReason.None) return budget;
                validStepCount = i;
            }
            return MovementFailureReason.None;
        }

        internal static MovementFailureReason ValidateActorPlacement(CharacterCombatFacts actor)
        {
            if (!actor.Anchor.HasValue || actor.OccupiedCells.Count != 1 || actor.OccupiedCells[0] != actor.Anchor.Value)
                return MovementFailureReason.StateInconsistent;
            return MovementFailureReason.None;
        }

        internal static MovementFailureReason StepBudget(CharacterCombatFacts actor, long weightedCost, MovementEnvironmentFacts environment = null)
        {
            int allowance = actor.Stats.MoveRange;
            if (allowance == 0) return MovementFailureReason.VoluntaryMoveDisabled;
            if (environment != null) allowance = checked(allowance + GroundCompactionTraitRule.MovementBonusAt(actor.Anchor.Value, environment));
            return weightedCost > allowance ? MovementFailureReason.MoveRangeExceeded : MovementFailureReason.None;
        }

        internal static bool CanTraverse(CombatCommandInput input, CellCoord cell, bool landing, MovementEnvironmentFacts environment = null)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            if (landing) return CanLand(input, cell, environment);
            if (!LeapTraitRule.AllowsIntermediate(actor)) return CanEnter(input, cell, environment);
            // 구조물 통과는 추가하지 않습니다. 중간 유닛·벽·절벽 통과만 허용합니다.
            return input.State.IsInside(cell) && !input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell));
        }

        internal static CharacterCombatFacts SwapPassenger(CombatCommandInput input, CellCoord cell)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var passenger = input.Characters.SingleOrDefault(c => c.EntityId != actor.EntityId &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.OccupiedCells.Contains(cell));
            return PositionSwapTraitRule.AllowsExchange(actor, passenger) ? passenger : null;
        }

        internal static bool CanLand(CombatCommandInput input, CellCoord cell, MovementEnvironmentFacts environment = null)
        {
            if (CanEnter(input, cell, environment)) return true;
            var passenger = SwapPassenger(input, cell);
            if (passenger == null) return false;
            var participants = new[] { input.ActorId, passenger.EntityId };
            return CanOccupy(input, cell, participants, environment) &&
                CanOccupy(input, input.Path?.Origin ?? input.Characters.Single(c => c.EntityId == input.ActorId).Anchor.Value, participants, environment);
        }

        internal static bool CanEnter(CombatCommandInput input, CellCoord cell, MovementEnvironmentFacts environment = null) =>
            CanOccupy(input, cell, new[] { input.ActorId }, environment);

        internal static bool CanOccupy(CombatCommandInput input, CellCoord cell,
            IReadOnlyList<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId> excluded,
            MovementEnvironmentFacts environment = null) =>
            input.State.IsInside(cell) &&
            !EnvironmentTerrainRules.BlocksVoluntaryEntry(environment ?? input.State.Environment, input.EnvironmentBindings, cell) &&
            !input.Characters.Any(c => !excluded.Contains(c.EntityId) && c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && c.OccupiedCells.Contains(cell)) &&
            !input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell));
    }
}
