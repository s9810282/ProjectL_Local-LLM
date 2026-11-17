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
            var failure = MovementFailureReason.None;
            int count = 0;
            foreach (var _ in ValidationSteps(input, path, environment, (reason, steps) => { failure = reason; count = steps; })) { }
            validStepCount = count;
            return failure;
        }

        internal static IEnumerable<int> ValidationSteps(CombatCommandInput input, MovementPath path,
            MovementEnvironmentFacts environment, Action<MovementFailureReason, int> completed)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (path == null) { completed(MovementFailureReason.InvalidPath, 0); yield break; }
            var actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (actor == null) { completed(MovementFailureReason.EntityNotFound, 0); yield break; }
            if (actor.Anchor != path.Origin) { completed(MovementFailureReason.PathOriginMismatch, 0); yield break; }
            if (!input.State.IsInside(path.Origin)) { completed(MovementFailureReason.OutOfBounds, 0); yield break; }
            var ledger = ValidateActorPlacement(actor);
            if (ledger != MovementFailureReason.None) { completed(ledger, 0); yield break; }
            if (path.StepCount == 0) { completed(MovementFailureReason.SameAnchor, 0); yield break; }
            var visited = new HashSet<CellCoord> { path.Origin };
            for (int i = 1; i < path.Cells.Count; i++)
            {
                var failure = ValidateStep(input, path, actor, visited, i, environment);
                if (failure != MovementFailureReason.None) { completed(failure, i - 1); yield break; }
                yield return 0;
            }
            completed(MovementFailureReason.None, path.StepCount);
        }

        private static MovementFailureReason ValidateStep(CombatCommandInput input, MovementPath path, CharacterCombatFacts actor,
            HashSet<CellCoord> visited, int index, MovementEnvironmentFacts environment)
        {
            var cell = path.Cells[index];
            if (!input.State.IsInside(cell)) return MovementFailureReason.OutOfBounds;
            var previous = path.Cells[index - 1];
            if (Math.Abs((long)cell.X - previous.X) + Math.Abs((long)cell.Y - previous.Y) != 1)
                return MovementFailureReason.InvalidPath;
            if (!visited.Add(cell)) return MovementFailureReason.RepeatedCell;
            if (!CanTraverse(input, cell, index == path.Cells.Count - 1, environment)) return MovementFailureReason.DestinationOccupied;
            return StepBudget(actor, index, environment ?? input.State.Environment);
        }

        internal static MovementFailureReason ValidateActorPlacement(CharacterCombatFacts actor)
        {
            if (!actor.Anchor.HasValue || actor.OccupiedCells.Count != 1 || actor.OccupiedCells[0] != actor.Anchor.Value)
                return MovementFailureReason.StateInconsistent;
            return MovementFailureReason.None;
        }

        internal static MovementFailureReason StepBudget(CharacterCombatFacts actor, long weightedCost, MovementEnvironmentFacts environment = null)
        {
            int? allowance = VoluntaryMovementRules.Allowance(actor, environment);
            if (!allowance.HasValue) return MovementFailureReason.VoluntaryMoveDisabled;
            return weightedCost > allowance.Value ? MovementFailureReason.MoveRangeExceeded : MovementFailureReason.None;
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
            !GimmickOccupancyRules.Blocks(input.State.Gimmicks, cell) &&
            !EnvironmentTerrainRules.BlocksVoluntaryEntry(environment ?? input.State.Environment, input.EnvironmentBindings, cell) &&
            !input.Characters.Any(c => !excluded.Contains(c.EntityId) && IsLivingFieldUnit(c) && c.OccupiedCells.Contains(cell)) &&
            !input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell));

        /// <summary>한 추적 탐색의 불변 공개 입력에서 중간 이동과 진입 판정에 쓰는 칸을 색인한다.</summary>
        internal sealed class PursuitTraversal
        {
            private readonly CombatCommandInput _input;
            private readonly bool _leap;
            private readonly HashSet<CellCoord> _units = new HashSet<CellCoord>();
            private readonly HashSet<CellCoord> _structures = new HashSet<CellCoord>();
            private readonly HashSet<CellCoord> _terrain = new HashSet<CellCoord>();

            internal PursuitTraversal(CombatCommandInput input, CharacterCombatFacts actor)
            {
                _input = input;
                _leap = LeapTraitRule.AllowsIntermediate(actor);
            }

            internal IEnumerable<int> IndexSteps(MovementEnvironmentFacts environment)
            {
                foreach (var cell in GimmickOccupancyRules.BlockingCells(_input.State.Gimmicks)) _terrain.Add(cell);
                foreach (var cell in EnvironmentTerrainRules.VoluntaryBlockedCellSteps(environment, _input.EnvironmentBindings))
                {
                    if (cell.HasValue) _terrain.Add(cell.Value);
                    yield return 0;
                }
                foreach (var unit in _input.Characters)
                {
                    if (unit.EntityId != _input.ActorId && IsLivingFieldUnit(unit))
                        foreach (var cell in unit.OccupiedCells)
                        {
                            _units.Add(cell);
                            yield return 0;
                        }
                    yield return 0;
                }
                foreach (var structure in _input.State.Structures)
                {
                    if (!structure.Health.IsDead)
                        foreach (var cell in structure.OccupiedCells)
                        {
                            _structures.Add(cell);
                            yield return 0;
                        }
                    yield return 0;
                }
            }

            internal bool CanTraverse(CellCoord cell) => _leap
                ? _input.State.IsInside(cell) && !_structures.Contains(cell)
                : CanEnter(cell);

            internal bool CanEnter(CellCoord cell) => _input.State.IsInside(cell) &&
                !_terrain.Contains(cell) && !_units.Contains(cell) && !_structures.Contains(cell);
        }

        private static bool IsLivingFieldUnit(CharacterCombatFacts unit) =>
            unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead;
    }
}
