using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
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
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyPathfind);
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
            var reach = new AttackReach(input, actor, targetId, environment);
            var directions = new[] { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
            // 첫 목적지를 찾은 거리다. 한 걸음 비용이 늘 1인 너비 우선이라 거리 순서로 꺼내므로, 이 거리 층을 다 본 뒤 멈춰도
            // 아래에서 고르는 목적지가 같다(적 추적 성능 설계 D4·§4-3: 이동 예산 판정은 비용에 대해 단조).
            int? goalDistance = null;
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                if (!labels[current.Cell].Contains(current)) continue;
                if (goalDistance.HasValue && current.Distance > goalDistance.Value) break;
                if (!attackable.TryGetValue(current.Cell, out bool canAttack))
                    attackable.Add(current.Cell, canAttack = (!leap || !mines.Contains(current.Cell) && MovementRouteValidator.CanEnter(input, current.Cell, environment)) &&
                        reach.From(current.Cell));
                if (canAttack)
                {
                    goals.Add(current);
                    goalDistance ??= current.Distance;
                    continue;
                }
                foreach (var direction in directions)
                {
                    var next = current.Cell + direction;
                    if ((!leap && mines.Contains(next)) || !MovementRouteValidator.CanTraverse(input, next, false, environment)) continue;
                    long cost;
                    try
                    {
                        // 지형은 걸음 비용을 더하지 않는다. 한 걸음은 1이다.
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

        /// <summary>
        /// 칸 하나를 탐색 문맥 없이 판정하는 시험 입구다. 동등성 시험이 성능 작업 전 판정과 칸마다 비교한다(적 추적 성능 계획 TP-3).
        /// </summary>
        internal static bool CanAttackFrom(CombatCommandInput input, CharacterCombatFacts actor, CellCoord cell, EntityId targetId) =>
            new AttackReach(input, actor, targetId, input.State.Environment.VisibleTo(actor.Owner)).From(cell);

        /// <summary>
        /// 탐색 한 번 동안 "그 칸에 서면 대상에 피해를 주는 조준이 있나"를 판정한다(적 추적 성능 설계 D1~D3).
        /// 결과는 공격 범위 전체를 조회해 조준마다 전달을 확인하던 판정과 같다(설계 §4). 대상에 닿을 수 있는 조준 후보가 없는 칸은
        /// 투영 입력을 만들지 않고 끝난다. 칸과 무관한 값(프로필·공격자 조건·벽·고저)은 처음 필요할 때 한 번만 만든다.
        /// </summary>
        private sealed class AttackReach
        {
            private readonly CombatCommandInput _input;
            private readonly CharacterCombatFacts _actor;
            private readonly EntityId _targetId;
            private readonly MovementEnvironmentFacts _environment;
            private readonly CellCoord[] _targetCells;
            private bool _prepared;
            private bool _attackerAllowed;
            private CombatAttackProfile _profile;
            private WallGeometry _walls;
            private AttackElevation _elevation;

            /// <param name="environment">행위자 진영에서 본 환경이다(경로 찾기가 만든 값을 그대로 쓴다).</param>
            internal AttackReach(CombatCommandInput input, CharacterCombatFacts actor, EntityId targetId, MovementEnvironmentFacts environment)
            {
                _input = input;
                _actor = actor;
                _targetId = targetId;
                _environment = environment;
                // 대상은 행위자 자신일 수 없으므로 행위자를 옮겨도 대상 칸은 바뀌지 않는다.
                _targetCells = input.Characters.Where(c => c.EntityId == targetId).SelectMany(c => c.OccupiedCells)
                    .Concat(input.State.Structures.Where(s => s.EntityId == targetId).SelectMany(s => s.OccupiedCells))
                    .Distinct().ToArray();
            }

            internal bool From(CellCoord cell)
            {
                using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyCanAttackFrom);
                var moved = MovedTo(cell);
                // 프로필과 공격자 조건은 서 있는 칸과 무관하다. 처음 판정하는 칸에서 지금 판정과 같은 순서(투영 → 프로필 → 공격자 조건)로 한 번 정한다.
                if (!_prepared)
                {
                    var first = Project(moved);
                    _profile = CombatCommandCalculator.RequiredProfile(first, moved);
                    _attackerAllowed = CombatAttackQueries.RangeActor(first, out _) == AttackFailureReason.None;
                    _prepared = true;
                }
                if (!_attackerAllowed) return false;
                // 사거리·지정은 다른 캐릭터를 보지 않으므로 투영 전 입력과 옮긴 행위자로 후보를 구한다(설계 D2).
                var aims = CombatAttackQueries.AimsThatCanHit(_input, moved, _profile, _targetCells, _environment);
                if (aims.Count == 0) return false;

                var projected = Project(moved);
                _walls ??= WallGeometryBuilder.Build(_input.State.BoardWidth, _input.State.BoardHeight,
                    _input.State.Environment, _input.EnvironmentBindings, _input.State.WallBlocking);
                _elevation ??= AttackElevation.From(_environment, _input.EnvironmentBindings);
                int commonDamage = CombatAttackCalculator.CommonAttackDamage(projected, moved, _profile);
                for (int i = 0; i < aims.Count; i++)
                {
                    if (CombatAttackQueries.DeliversTo(projected, moved, _profile, aims[i], _targetId, commonDamage,
                            _environment, _walls, _elevation))
                        return true;
                }
                return false;
            }

            private CharacterCombatFacts MovedTo(CellCoord cell)
            {
                var moved = _actor;
                if (cell != _actor.Anchor.Value)
                {
                    var departed = PositionTileModifierCalculator.Replace(_actor, PositionTileModifier.None);
                    moved = CombatMovementCalculator.MoveTo(departed, cell);
                    moved = PositionTileModifierCalculator.Replace(moved, PositionTileModifierCalculator.ResolveAt(cell, _environment));
                }
                // 추적 목적지의 공격 기하만 계산합니다. 현재 공격 봉쇄가 자발적 이동까지 막지는 않습니다.
                return moved.With(debuffs: moved.Debuffs.WithRestrictions(null, moved.Debuffs.AnkleBreaker));
            }

            private CombatCommandInput Project(CharacterCombatFacts moved) => _input.WithWorld(_input.State,
                    _input.Characters.Select(c => c.EntityId == _actor.EntityId ? moved : c).ToArray(),
                    _input.Main, _input.SoulCounts, _input.Souls, _input.EnvironmentBindings)
                .WithIntent(CombatCommandKind.Attack, default, default, null, null);
        }
    }
}
