using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>알고 있는 활성 지뢰를 제외한 공격 위치까지 탐색하고, 현재 자원으로 걸을 접두 경로만 반환한다.</summary>
    internal static class EnemyMovementPathfinder
    {
        internal static bool TryFind(CombatCommandInput input, EntityId targetId, out MovementPath path)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            path = null;
            if (CombatMovementCalculator.Actor(input, out var actor) != MovementFailureReason.None ||
                MovementRouteValidator.ValidateLedger(actor) != MovementFailureReason.None) return false;
            if (actor.Stats.RemainingMoveRange == 0) return false;
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            var mines = input.State.Environment.Tiles.Where(t => t.Active && t.IsMine && t.KnownTo.Contains(actor.Owner))
                .Select(t => t.Cell).ToHashSet();
            var origin = actor.Anchor.Value;
            var queue = new Queue<MovementSearchLabel>();
            var labels = new Dictionary<CellCoord, List<MovementSearchLabel>>();
            var start = new MovementSearchLabel(origin, 0, 0);
            MovementSearchLabel.TryAdd(labels, start);
            queue.Enqueue(start);
            var goals = new List<MovementSearchLabel>();
            var attackable = new Dictionary<CellCoord, bool>();
            var directions = new[] { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                if (!labels[current.Cell].Contains(current)) continue;
                if (!attackable.TryGetValue(current.Cell, out bool canAttack))
                    attackable.Add(current.Cell, canAttack = CanAttackFrom(input, actor, current.Cell, targetId));
                if (canAttack) { goals.Add(current); continue; }
                foreach (var direction in directions)
                {
                    var next = current.Cell + direction;
                    if (mines.Contains(next) || !MovementRouteValidator.CanEnter(input, next)) continue;
                    long cost;
                    try
                    {
                        cost = checked(current.Cost + EnvironmentTerrainRules.EdgeActionCost(environment,
                            input.EnvironmentBindings, current.Cell, next, actor.Stats.MoveCost));
                    }
                    catch (OverflowException) { continue; }
                    var candidate = new MovementSearchLabel(next, checked(current.Distance + 1), cost, current);
                    if (MovementSearchLabel.TryAdd(labels, candidate)) queue.Enqueue(candidate);
                }
            }
            var feasible = goals.Where(g => MovementRouteValidator.StepBudget(actor, g.Distance, g.Cost) == MovementFailureReason.None).ToArray();
            var goal = (feasible.Length == 0 ? goals : feasible.AsEnumerable())
                .OrderBy(g => g.Cost).ThenBy(g => g.Distance).ThenBy(g => g.Cell.Y).ThenBy(g => g.Cell.X).FirstOrDefault();
            if (goal == null || goal.Cell == origin) return false;
            var route = new List<CellCoord>();
            for (var current = goal; current != null; current = current.Previous) route.Add(current.Cell);
            route.Reverse();
            long paid = 0;
            int steps = 0;
            for (int i = 1; i < route.Count; i++)
            {
                long cost = checked(paid + EnvironmentTerrainRules.EdgeActionCost(environment,
                    input.EnvironmentBindings, route[i - 1], route[i], actor.Stats.MoveCost));
                if (MovementRouteValidator.StepBudget(actor, i, cost) != MovementFailureReason.None) break;
                paid = cost;
                steps = i;
            }
            if (steps == 0) return false;
            var prefix = new MovementPath(route.Take(steps + 1).ToArray());
            if (MovementRouteValidator.Validate(input, prefix, out _, environment) != MovementFailureReason.None) return false;
            path = prefix;
            return true;
        }

        private static bool CanAttackFrom(CombatCommandInput input, CharacterCombatFacts actor, CellCoord cell, EntityId targetId)
        {
            var moved = actor;
            if (cell != actor.Anchor.Value)
            {
                var departed = PositionTileModifierCalculator.Replace(actor, PositionTileModifier.None);
                moved = CombatMovementCalculator.MoveTo(departed, cell);
                moved = PositionTileModifierCalculator.Replace(moved,
                    PositionTileModifierCalculator.ResolveAt(cell, input.State.Environment.VisibleTo(actor.Owner)));
            }
            var projected = new CombatCommandInput(input.Number, CombatCommandKind.Attack, input.ActorId, default,
                input.State, input.Characters.Select(c => c.EntityId == actor.EntityId ? moved : c).ToArray(),
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                rotation: input.Rotation, battle: input.Battle, processingKind: input.ProcessingKind,
                battleInstanceId: input.BattleInstanceId, artifactItems: input.ArtifactItems,
                environmentBindings: input.EnvironmentBindings, obstacleContents: input.ObstacleContents);
            var profile = CombatCommandCalculator.RequiredProfile(projected, moved);
            // AP와 독립적인 지정·대상 기하 및 관통 제한을 조회한다. 피해 적용이나 타일 진입은 실행하지 않는다.
            var range = CombatAttackQueries.Range(projected);
            foreach (var aim in range.AttackAims.Span)
            {
                var environment = projected.State.Environment.VisibleTo(moved.Owner);
                var evaluation = CombatAttackQueries.Selection(projected, moved, profile, aim, environment);
                if (evaluation.FailureReason != AttackFailureReason.None) continue;
                var selection = evaluation.Selection;
                if (selection.IsPiercing)
                {
                    int budget = CombatCommandCalculator.PierceTargetBudget(projected, moved, profile);
                    if (PierceTargetCollector.ApplyBudget(selection.PierceTrace, budget).Any(h => h.Id == targetId)) return true;
                }
                else if (selection.Targets.Any(t => t.Key == targetId &&
                    (t.Value != AttackTargetRole.Additional || profile.Method.SplashPercent > 0))) return true;
            }
            return false;
        }
    }
}
