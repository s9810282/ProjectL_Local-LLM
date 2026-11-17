using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using Target = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi.EnemyDecisionContext.HostileTarget;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>
    /// 한 행위자의 여러 턴 교전 거리다(적 AI 근간 개편 설계 6.3). 살아 있는 적대 유닛 모두를 대상으로, 각 대상의 공격 위치에서 거꾸로 정수 비용을 퍼뜨린다.
    /// 공격 위치는 공용 도달 판정(<see cref="EnemyAttackReach"/>)으로 찾고, 칸마다 남은 비용·가장 가까운 대상·공격 위치 여부를 보관한다.
    /// 아군이 선 칸은 비용을 더해 지나갈 수 있고(<see cref="EnemyAiTuning.AllySoftCost"/>), 적대 유닛·벽·낭떠러지·구조물·기믹 확정 점유는 막는다.
    /// 이번 턴 이동은 여전히 단단한 점유 규칙을 쓴다. 본부는 닿는 유닛 공격 위치가 없을 때만 대상이다.
    /// </summary>
    internal sealed class EnemyEngagementField
    {
        /// <summary>보관할 이동 가능 칸 수의 상한이다. 예비 최대 필드 17×17(289칸)이 빈 판이어도 거리가 완성되게 맞춘다.</summary>
        internal const int CellLimit = 289;
        private static readonly CellCoord[] Directions = { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
        private readonly CellCoord _origin;
        private readonly Dictionary<CellCoord, int> _costs;
        private readonly Dictionary<CellCoord, EntityId> _best;
        private readonly Dictionary<EntityId, long> _values;

        internal bool IsTruncated { get; }
        internal int ExpandedNodes { get; }

        private EnemyEngagementField(CellCoord origin, Dictionary<CellCoord, int> costs, Dictionary<CellCoord, EntityId> best,
            Dictionary<EntityId, long> values, int expandedNodes, bool truncated)
        {
            _origin = origin;
            _costs = costs;
            _best = best;
            _values = values;
            ExpandedNodes = expandedNodes;
            IsTruncated = truncated;
        }

        internal bool TryGetCost(CellCoord cell, out int cost) => _costs.TryGetValue(cell, out cost);
        internal bool IsAttackPosition(CellCoord cell) => _costs.TryGetValue(cell, out int cost) && cost == 0;
        internal bool TryGetBestTarget(CellCoord cell, out EntityId target) => _best.TryGetValue(cell, out target);
        internal long TargetValue(EntityId target) => _values.GetValueOrDefault(target);

        /// <summary>출발 칸에서 종점까지 줄어든 남은 비용이다. 둘 중 하나라도 거리가 없으면 0이다.</summary>
        internal long Progress(CellCoord endpoint) =>
            _costs.TryGetValue(_origin, out int before) && _costs.TryGetValue(endpoint, out int after) ? (long)before - after : 0;

        /// <param name="restrictTo">이 대상만 쓴다(도발 의무). 비우면 살아 있는 적대 대상 전부다.</param>
        internal static IEnumerable<int> BuildSteps(EnemyDecisionContext context, CombatCommandInput input, CharacterCombatFacts actor,
            IReadOnlyCollection<EntityId> restrictTo, Action<EnemyEngagementField> completed)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var costs = new Dictionary<CellCoord, int>();
            var best = new Dictionary<CellCoord, EntityId>();
            var values = new Dictionary<EntityId, long>();
            var origin = actor.Anchor.GetValueOrDefault();
            if (CombatMovementCalculator.Actor(input, out _) != MovementFailureReason.None ||
                MovementRouteValidator.ValidateActorPlacement(actor) != MovementFailureReason.None)
            {
                completed(new EnemyEngagementField(origin, costs, best, values, 0, false));
                yield break;
            }

            IReadOnlyDictionary<(int kind, int id), Target> all = null;
            foreach (var _ in context.TargetSteps(value => all = value)) yield return 0;
            var units = new List<Target>();
            var headquarters = new List<Target>();
            foreach (var target in all.Values)
            {
                if (restrictTo == null || Contains(restrictTo, target.Id)) (target.IsUnit ? units : headquarters).Add(target);
                yield return 0;
            }
            EnemyDecisionContext.ObservedGeometry geometry = null;
            foreach (var _ in context.GeometrySteps(value => geometry = value)) yield return 0;
            var environment = geometry.Environment;

            // 여러 턴 추정 전용 색인이다. 이번 턴 경로의 PursuitTraversal과 달리 아군 칸을 따로 둔다.
            // 막는 칸은 PursuitTraversal과 같다: 기믹 확정 점유(사슬 머리 등)와 자발 진입을 막는 지형.
            var terrain = new HashSet<CellCoord>(GimmickOccupancyRules.BlockingCells(input.State.Gimmicks));
            foreach (var cell in EnvironmentTerrainRules.VoluntaryBlockedCellSteps(environment, input.EnvironmentBindings))
            {
                if (cell.HasValue) terrain.Add(cell.Value);
                yield return 0;
            }
            var hostile = new HashSet<CellCoord>();
            var allied = new HashSet<CellCoord>();
            foreach (var unit in input.Characters)
            {
                if (unit.EntityId != actor.EntityId && unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead)
                    foreach (var cell in unit.OccupiedCells)
                    {
                        (unit.Owner == actor.Owner ? allied : hostile).Add(cell);
                        yield return 0;
                    }
                yield return 0;
            }
            var structures = new HashSet<CellCoord>();
            foreach (var structure in input.State.Structures)
            {
                if (!structure.Health.IsDead)
                    foreach (var cell in structure.OccupiedCells)
                    {
                        structures.Add(cell);
                        yield return 0;
                    }
                yield return 0;
            }
            var mines = new HashSet<CellCoord>();
            var collapseLandings = new HashSet<CellCoord>();
            foreach (var tile in environment.Tiles)
            {
                if (tile.Active && tile.IsMine) mines.Add(tile.Cell);
                if (WeakGroundCalculator.CollapsesOnNextEntry(tile, input.EnvironmentBindings)) collapseLandings.Add(tile.Cell);
                yield return 0;
            }
            bool leap = LeapTraitRule.AllowsIntermediate(actor);

            // 도약은 유닛·지형을 건너고, 그 밖에는 아군만 비용을 더해 지난다. 지뢰는 도약이 아니면 지나지 않는다.
            bool Passable(CellCoord cell) => input.State.IsInside(cell) && !structures.Contains(cell) &&
                (leap || !terrain.Contains(cell) && !hostile.Contains(cell) && !mines.Contains(cell));
            bool Landable(CellCoord cell) => input.State.IsInside(cell) && !terrain.Contains(cell) && !hostile.Contains(cell) &&
                !allied.Contains(cell) && !structures.Contains(cell) && !mines.Contains(cell) && !collapseLandings.Contains(cell);
            int StepCost(CellCoord cell) => 1 + (!leap && allied.Contains(cell) ? EnemyAiTuning.AllySoftCost : 0);

            var component = new HashSet<CellCoord> { origin };
            var anchors = new List<CellCoord> { origin };
            int expanded = 0;
            for (int index = 0; index < anchors.Count; index++)
            {
                var current = anchors[index];
                expanded++;
                yield return 0;
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    yield return 0;
                    if (component.Contains(next) || !Passable(next)) continue;
                    // 일부만 본 그래프로 점수를 주면 탐색 순서에 따라 다른 후보가 유리해진다.
                    if (anchors.Count == CellLimit)
                    {
                        completed(new EnemyEngagementField(origin, costs, best, values, expanded, truncated: true));
                        yield break;
                    }
                    component.Add(next);
                    anchors.Add(next);
                }
            }

            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            int damage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.OrdinaryAttack);
            int radius = BoundRadius(actor, profile);
            var relocations = new EnemyAttackReach.Relocations(input, actor, geometry);
            yield return 0;
            foreach (var group in new[] { units, headquarters })
            {
                if (group.Count == 0) continue;
                costs.Clear();
                best.Clear();
                foreach (var target in group)
                {
                    values[target.Id] = ValueOf(context, target, damage);
                    var reach = new EnemyAttackReach(input, actor, target.Id, environment, geometry, relocations);
                    foreach (var _ in reach.TargetSteps()) yield return 0;
                    foreach (var cell in anchors)
                    {
                        yield return 0;
                        if (!Landable(cell) || radius >= 0 && Chebyshev(cell, target.Cells) > radius) continue;
                        bool goal = false;
                        foreach (var _ in reach.FromSteps(cell, value => goal = value)) yield return 0;
                        if (!goal) continue;
                        if (!best.TryGetValue(cell, out var known) || Better(target, known, values, group)) best[cell] = target.Id;
                        costs[cell] = 0;
                    }
                }

                var buckets = new List<Queue<CellCoord>> { new Queue<CellCoord>() };
                foreach (var cell in anchors)
                {
                    if (costs.ContainsKey(cell)) buckets[0].Enqueue(cell);
                    yield return 0;
                }
                for (int level = 0; level < buckets.Count; level++)
                {
                    var queue = buckets[level];
                    while (queue.Count != 0)
                    {
                        var current = queue.Dequeue();
                        yield return 0;
                        if (costs[current] != level) continue;
                        expanded++;
                        // 출발 칸을 떠날 수 있어도 지뢰·지형 때문에 재진입하지 못할 수 있으므로 들어가는 쪽 허용을 확인한다.
                        if (!Passable(current)) continue;
                        int cost = checked(level + StepCost(current));
                        foreach (var direction in Directions)
                        {
                            var next = current + direction;
                            yield return 0;
                            if (!component.Contains(next)) continue;
                            if (costs.TryGetValue(next, out int known))
                            {
                                // 같은 비용이면 값이 큰 대상을 남긴다. 더 싼 제안은 이 칸이 나오기 전에 모두 도착한다.
                                if (known == cost && Better(Find(group, best[current]), best[next], values, group)) best[next] = best[current];
                                if (known <= cost) continue;
                            }
                            costs[next] = cost;
                            best[next] = best[current];
                            while (buckets.Count <= cost) buckets.Add(new Queue<CellCoord>());
                            buckets[cost].Enqueue(next);
                        }
                    }
                }
                if (costs.ContainsKey(origin)) break;
            }
            completed(new EnemyEngagementField(origin, costs, best, values, expanded, false));
        }

        // 대상 하나의 기본 공격 한 번이 줄 공통 피해 손실과, 처치면 처치 가중치다. 공격 위치가 같은 거리면 값이 큰 대상을 고른다.
        internal static long ValueOf(EnemyDecisionContext context, Target target, int damage)
        {
            HealthStateSnapshot health;
            if (context.Units.TryGetValue(target.Id, out var unit)) health = unit.Health;
            else if (context.Structures.TryGetValue(target.Id, out var structure) && structure.ObstacleDurability == null)
                health = structure.Health;
            else return 0;
            long loss = EnemyUtilityScorer.LossPermille(damage, health.CurrentHealth, health.MaxHealth);
            return damage >= health.CurrentHealth ? checked(loss + EnemyUtilityScorer.OpposingEliminationWeight) : loss;
        }

        private static bool Better(Target candidate, EntityId known, Dictionary<EntityId, long> values, List<Target> group)
        {
            var other = Find(group, known);
            return (-values[candidate.Id], candidate.IsUnit ? 0 : 1, candidate.Id.Value)
                .CompareTo((-values[other.Id], other.IsUnit ? 0 : 1, other.Id.Value)) < 0;
        }

        private static Target Find(List<Target> group, EntityId id)
        {
            foreach (var target in group)
                if (target.Id == id) return target;
            throw new InvalidOperationException("교전 필드의 대상 표에 없는 대상입니다.");
        }

        private static bool Contains(IReadOnlyCollection<EntityId> ids, EntityId id)
        {
            foreach (var value in ids)
                if (value == id) return true;
            return false;
        }

        /// <summary>
        /// 공격 위치 후보 칸의 대상까지 체비쇼프 반경이다. <see cref="CombatAttackQueries.EnumerateAimsThatCanHit"/>와 같은 기준을 쓴다:
        /// 선택 사거리(지형은 줄이기만 하고 끌어오기만 늘림)와 지정 모양의 너비, 스플래시 한 칸을 봅니다.
        /// 관통과 전체 지정은 판 끝까지 닿아 -1(전체 탐색)입니다.
        /// </summary>
        private static int BoundRadius(CharacterCombatFacts actor, CombatAttackProfile profile)
        {
            var method = profile.Method;
            if (profile.UsesPiercingShot || method.ChoiceMode == TargetingChoiceMode.All) return -1;
            int range = PullAttackCalculator.SelectionRange(actor, profile, actor.Stats.AppointRange);
            int maximumDistance = AppointShapeCalculator.MaximumBaseDistance(method, range);
            return checked(maximumDistance + (method.Splash == TargetingSplash.None ? 0 : 1));
        }

        private static int Chebyshev(CellCoord cell, IReadOnlyCollection<CellCoord> cells)
        {
            int nearest = int.MaxValue;
            foreach (var other in cells)
                nearest = Math.Min(nearest, Math.Max(Math.Abs(cell.X - other.X), Math.Abs(cell.Y - other.Y)));
            return nearest;
        }
    }
}
