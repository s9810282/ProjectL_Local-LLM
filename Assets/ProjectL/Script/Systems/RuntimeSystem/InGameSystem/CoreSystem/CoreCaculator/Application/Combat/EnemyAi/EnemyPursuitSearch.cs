using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>너비 우선 탐색의 진행 위치와 공격 위치 조회를 탐색 수명 안에서 보관한다.</summary>
    internal sealed class EnemyPursuitSearch : IDisposable
    {
        private static readonly CellCoord[] Directions = { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
        private IEnumerator<int> _steps;
        private MovementPath _path;
        private bool _disposed;

        internal bool IsComplete { get; private set; }
        internal string WorkPhase { get; private set; } = "PursuitInitialize";

        internal EnemyPursuitSearch(CombatCommandInput input, EntityId targetId, EnemyDecisionDiagnostics diagnostics = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            _steps = Search(input, targetId, diagnostics).GetEnumerator();
        }

        internal void Step()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EnemyPursuitSearch));
            if (IsComplete) return;
            if (_steps.MoveNext()) return;
            IsComplete = true;
            _steps.Dispose();
            _steps = null;
        }

        internal bool TryGetPath(out MovementPath path)
        {
            if (!IsComplete) throw new InvalidOperationException("추적 탐색이 아직 끝나지 않았습니다.");
            path = _path;
            return path != null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _steps?.Dispose();
            _steps = null;
            _path = null;
        }

        internal static bool CanAttackFrom(CombatCommandInput input, CharacterCombatFacts actor, CellCoord cell, EntityId targetId)
        {
            var reach = new EnemyAttackReach(input, actor, targetId, input.State.Environment.VisibleTo(actor.Owner));
            foreach (var _ in reach.TargetSteps()) { }
            return reach.From(cell);
        }

        private IEnumerable<int> Search(CombatCommandInput input, EntityId targetId, EnemyDecisionDiagnostics diagnostics)
        {
            if (CombatMovementCalculator.Actor(input, out var actor) != MovementFailureReason.None ||
                MovementRouteValidator.ValidateActorPlacement(actor) != MovementFailureReason.None) yield break;
            MovementEnvironmentFacts environment = null;
            foreach (var _ in input.State.Environment.VisibleSteps(actor.Owner, value => environment = value)) yield return 0;
            var mines = new HashSet<CellCoord>();
            HashSet<CellCoord> collapseLandings = null;
            foreach (var tile in environment.Tiles)
            {
                if (tile.Active && tile.IsMine) mines.Add(tile.Cell);
                if (WeakGroundCalculator.CollapsesOnNextEntry(tile, input.EnvironmentBindings))
                    (collapseLandings ??= new HashSet<CellCoord>()).Add(tile.Cell);
                yield return 0;
            }
            bool leap = LeapTraitRule.AllowsIntermediate(actor);
            var traversal = new MovementRouteValidator.PursuitTraversal(input, actor);
            foreach (var _ in traversal.IndexSteps(environment)) yield return 0;
            var origin = actor.Anchor.Value;
            var queue = new Queue<MovementSearchLabel>();
            var labels = new Dictionary<CellCoord, List<MovementSearchLabel>>();
            var start = new MovementSearchLabel(origin, 0, 0);
            MovementSearchLabel.TryAdd(labels, start);
            queue.Enqueue(start);
            var attackable = new Dictionary<CellCoord, bool>();
            var reach = new EnemyAttackReach(input, actor, targetId, environment);
            foreach (var _ in reach.TargetSteps()) yield return 0;
            yield return 0;

            MovementSearchLabel best = null, feasible = null;
            int? goalDistance = null;
            while (queue.Count != 0)
            {
                WorkPhase = "PursuitFrontier";
                var current = queue.Dequeue();
                yield return 0;
                if (!labels[current.Cell].Contains(current)) continue;
                if (goalDistance.HasValue && current.Distance > goalDistance.Value) break;
                if (diagnostics != null)
                {
                    diagnostics.WorkUnits++;
                    diagnostics.ExpandedNodes++;
                }
                if (!attackable.TryGetValue(current.Cell, out bool canAttack))
                {
                    // 붕괴는 행동 끝에 일어나므로 이 칸은 통과할 수 있지만 추적의 도착점으로는 고르지 않는다.
                    if (collapseLandings?.Contains(current.Cell) != true &&
                        (!leap || !mines.Contains(current.Cell) && traversal.CanEnter(current.Cell)))
                    {
                        foreach (var phase in reach.FromSteps(current.Cell, value => canAttack = value))
                        {
                            WorkPhase = phase;
                            yield return 0;
                        }
                    }
                    attackable.Add(current.Cell, canAttack);
                }
                else if (diagnostics != null) diagnostics.CacheHits++;

                if (canAttack)
                {
                    goalDistance ??= current.Distance;
                    if (Better(current, best)) best = current;
                    if (MovementRouteValidator.StepBudget(actor, current.Cost, environment) == MovementFailureReason.None &&
                        Better(current, feasible)) feasible = current;
                    yield return 0;
                    continue;
                }

                WorkPhase = "PursuitNeighbor";
                foreach (var direction in Directions)
                {
                    var next = current.Cell + direction;
                    if ((leap || !mines.Contains(next)) && traversal.CanTraverse(next))
                    {
                        // 지형과 무관하게 한 걸음 비용은 1이다.
                        var candidate = new MovementSearchLabel(next, checked(current.Distance + 1), checked(current.Cost + 1), current);
                        if (MovementSearchLabel.TryAdd(labels, candidate)) queue.Enqueue(candidate);
                    }
                    yield return 0;
                }
            }

            WorkPhase = "PursuitRoute";
            var goal = feasible ?? best;
            if (goal == null || goal.Cell == origin) yield break;
            var route = new Stack<CellCoord>();
            for (var current = goal; current != null; current = current.Previous)
            {
                route.Push(current.Cell);
                yield return 0;
            }
            var prefix = new List<CellCoord> { route.Pop() };
            int steps = 0;
            while (route.Count != 0)
            {
                var next = route.Pop();
                if (MovementRouteValidator.StepBudget(actor, prefix.Count, environment) != MovementFailureReason.None) break;
                prefix.Add(next);
                if (traversal.CanEnter(next) && !mines.Contains(next) && collapseLandings?.Contains(next) != true)
                    steps = prefix.Count - 1;
                yield return 0;
            }
            if (steps == 0) yield break;
            var path = new MovementPath(prefix.GetRange(0, steps + 1));
            yield return 0;
            foreach (var _ in MovementRouteValidator.ValidationSteps(input, path, environment, (reason, count) =>
            {
                if (reason == MovementFailureReason.None) _path = path;
            })) yield return 0;
        }

        private static bool Better(MovementSearchLabel candidate, MovementSearchLabel best) =>
            best == null || (candidate.Cost, candidate.Distance, candidate.Cell.Y, candidate.Cell.X)
                .CompareTo((best.Cost, best.Distance, best.Cell.Y, best.Cell.X)) < 0;


    }
}
