using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 추적 경로 찾기 성능 작업 전의 <c>EnemyMovementPathfinder.TryFind</c>·<c>CanAttackFrom</c> 사본이다(2026-11-03 기준, 측정 구간 줄만 뺌).
    /// 최적화한 경로 찾기가 같은 결과를 내는지 비교하는 기준값으로만 쓴다(성능 설계 Q-PF4 A, 계획 TP-1). 규칙을 고칠 때 이 사본은 따라 고치지 않는다.
    /// </summary>
    internal static class EnemyPursuitReference
    {
        internal static bool TryFind(CombatCommandInput input, EntityId targetId, out MovementPath path)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            path = null;
            if (CombatMovementCalculator.Actor(input, out var actor) != MovementFailureReason.None ||
                MovementRouteValidator.ValidateActorPlacement(actor) != MovementFailureReason.None) return false;
            if (actor.Stats.MoveRange == 0) return false;
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            var mines = input.State.Environment.Tiles.Where(t => t.Active && t.IsMine && t.KnownTo.Contains(actor.Owner))
                .Select(t => t.Cell).ToHashSet();
            bool leap = LeapTraitRule.AllowsIntermediate(actor);
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
                    attackable.Add(current.Cell, canAttack = (!leap || !mines.Contains(current.Cell) && MovementRouteValidator.CanEnter(input, current.Cell, environment)) &&
                        CanAttackFrom(input, actor, current.Cell, targetId));
                if (canAttack) { goals.Add(current); continue; }
                foreach (var direction in directions)
                {
                    var next = current.Cell + direction;
                    if ((!leap && mines.Contains(next)) || !MovementRouteValidator.CanTraverse(input, next, false, environment)) continue;
                    long cost;
                    try
                    {
                        cost = checked(current.Cost + 1);
                    }
                    catch (OverflowException) { continue; }
                    var candidate = new MovementSearchLabel(next, checked(current.Distance + 1), cost, current);
                    if (MovementSearchLabel.TryAdd(labels, candidate)) queue.Enqueue(candidate);
                }
            }
            var feasible = goals.Where(g => MovementRouteValidator.StepBudget(actor, g.Cost, environment) == MovementFailureReason.None).ToArray();
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
                long cost = checked(paid + 1);
                if (MovementRouteValidator.StepBudget(actor, cost, environment) != MovementFailureReason.None) break;
                paid = cost;
                if (MovementRouteValidator.CanEnter(input, route[i], environment) && !mines.Contains(route[i])) steps = i;
            }
            if (steps == 0) return false;
            var prefix = new MovementPath(route.Take(steps + 1).ToArray());
            if (MovementRouteValidator.Validate(input, prefix, out _, environment) != MovementFailureReason.None) return false;
            path = prefix;
            return true;
        }

        internal static bool CanAttackFrom(CombatCommandInput input, CharacterCombatFacts actor, CellCoord cell, EntityId targetId)
        {
            var moved = actor;
            if (cell != actor.Anchor.Value)
            {
                var departed = PositionTileModifierCalculator.Replace(actor, PositionTileModifier.None);
                moved = CombatMovementCalculator.MoveTo(departed, cell);
                moved = PositionTileModifierCalculator.Replace(moved,
                    PositionTileModifierCalculator.ResolveAt(cell, input.State.Environment.VisibleTo(actor.Owner)));
            }
            moved = moved.With(debuffs: moved.Debuffs.WithRestrictions(null, moved.Debuffs.AnkleBreaker));
            var projected = input.WithWorld(input.State,
                input.Characters.Select(c => c.EntityId == actor.EntityId ? moved : c).ToArray(),
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings)
                .WithIntent(CombatCommandKind.Attack, default, default, null, null);
            var profile = CombatCommandCalculator.RequiredProfile(projected, moved);
            var range = CombatAttackQueries.Range(projected);
            var environment = projected.State.Environment.VisibleTo(moved.Owner);
            var walls = WallGeometryBuilder.Build(projected.State.BoardWidth, projected.State.BoardHeight,
                projected.State.Environment, projected.EnvironmentBindings, projected.State.WallBlocking);
            var elevation = AttackElevation.From(environment, projected.EnvironmentBindings);
            int commonDamage = CombatAttackCalculator.CommonAttackDamage(projected, moved, profile, CombatDamageKind.OrdinaryAttack);
            foreach (var aim in range.AttackAims.Span)
            {
                if (CombatAttackQueries.DeliversTo(projected, moved, profile, aim, targetId, commonDamage, environment, walls, elevation)) return true;
            }
            return false;
        }
    }
}
