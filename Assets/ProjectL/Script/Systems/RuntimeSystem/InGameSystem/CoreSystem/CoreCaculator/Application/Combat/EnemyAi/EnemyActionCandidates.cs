using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
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
    /// <summary>한 행위자의 공개 사실만으로 공격 조준과 이동 종점의 짧은 목록을 만든다.</summary>
    internal sealed class EnemyActionCandidates : IDisposable
    {
        private static readonly CellCoord[] Directions = { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
        private readonly EnemyDecisionContext _context;
        private readonly CharacterCombatFacts _actor;
        private readonly StageHazardForecast _hazards;
        private readonly CombatCommandInput _input;
        private readonly int _attackLimit;
        private readonly int _moveLimit;
        private readonly bool _followupOnly;
        private readonly List<EnemyDecision> _attacks = new List<EnemyDecision>();
        private readonly List<EnemyDecision> _moves = new List<EnemyDecision>();
        private readonly List<(CellCoord cell, long score)> _cheapScores = new List<(CellCoord cell, long score)>();
        private readonly List<CellCoord> _reserved = new List<CellCoord>();
        private IReadOnlyDictionary<(int kind, int id), Target> _targets;
        private IEnumerator<int> _steps;
        private EnemyEngagementField _field;
        private MovementPath _pursuitPath;
        private HashSet<CellCoord> _arrivalCells;
        private HashSet<CellCoord> _collapseLandings;
        private bool _disposed;
        private bool _faulted;

        internal bool IsComplete { get; private set; }
        internal int MandatoryExpandedNodes { get; private set; }
        internal int RefinementExpandedNodes { get; private set; }
        internal int RouteReconstructionCount { get; private set; }
        internal bool RefinementTruncated { get; private set; }
        /// <summary>확실히 치명이라 뺀 착지·대표 경로 수다. 남은 후보가 없을 때 대기 이유(LethalOnly)를 정하는 데 쓴다.</summary>
        internal int LethalLandingsDropped { get; private set; }
        internal EntityId RequiredTauntSource { get; private set; }
        internal IReadOnlyList<EnemyDecision> Attacks => Completed(_attacks);
        internal IReadOnlyList<EnemyDecision> Moves => Completed(_moves);
        internal int EngagementExpandedNodes => _field?.ExpandedNodes ?? 0;
        internal bool EngagementTruncated => _field?.IsTruncated == true;
        /// <summary>이 행위자의 교전 필드다. 이동할 수 없거나 대상이 없으면 없다.</summary>
        internal EnemyEngagementField Field => Completed(_field);
        internal CharacterCombatFacts Actor => _actor;
        /// <summary>공격 후보가 끝나 행위자 순서를 정할 수 있다(교전 필드와 이동 후보 탐색 전). 끝났으면 늘 참이다.</summary>
        internal bool OrderReady { get; private set; }
        /// <summary>
        /// 행위자 순서 열쇠다(설계 6.7을 바꾼 결정, 2026-11-10): 지금 칸에서 칠 수 있으면 먼저, 같으면 개체 번호.
        /// 필드 비용 순서는 매 결정마다 모든 행위자의 필드를 만들어야 해서 쓰지 않는다. <see cref="OrderReady"/>가 참일 때만 읽는다.
        /// </summary>
        internal (int ready, int id) OrderKey
        {
            get
            {
                RequireActive();
                if (!OrderReady) throw new InvalidOperationException("행위자 순서를 정할 준비가 끝나지 않았습니다.");
                return (_attacks.Count != 0 ? 0 : 1, _actor.EntityId.Value);
            }
        }
        /// <summary>도달할 수 있는 착지 칸마다의 저가 점수다(설계 6.6 1단계, 정밀 항 없음). 시험과 진단용이다.</summary>
        internal IReadOnlyList<(CellCoord cell, long score)> CheapScores => Completed(_cheapScores).AsReadOnly();
        /// <summary>예약으로 빔에 넣은 끝 칸이다(우선순위 순서, 중복 없음).</summary>
        internal IReadOnlyList<CellCoord> Reserved => Completed(_reserved).AsReadOnly();
        /// <summary>제자리의 저가 점수와, 빔에 들지 못한 착지 칸 중 가장 높은 저가 점수다(없으면 <see cref="long.MinValue"/>).</summary>
        internal long HoldCheapScore { get; private set; } = long.MinValue;
        internal long BestPrunedScore { get; private set; } = long.MinValue;

        /// <summary>
        /// 확실한 치명 판정 하나다(계획 R-3 치명 위험 규칙 1~3). 다음 기회까지 서 있을 때의 확실한 손실이 남은 체력 이상이거나
        /// 붕괴 착지면 치명이다. 지나가기만 하는 칸은 도착이 아니다. 경로 접촉 피해(옛 가시 장판)는 없어져 더하지 않는다(가시 폭탄 Phase 4 D7).
        /// 불확실한 손실(가능 손실, 무작위·위치 불명 공백, 토네이도 던지기)은 걸러 내지 않고 점수의 위험으로 남긴다.
        /// 위험 종류를 고르지 않고 소유자 사실(예보 노출·약한 지반 착지)만 읽는다. 후보 탐색과 결정 세션이 함께 부른다.
        /// </summary>
        internal static bool IsCertainLethal(int health, in HazardExposure arrival, bool collapseLanding) =>
            collapseLanding || arrival.KnownHpLoss >= health;

        /// <summary>
        /// 모두 치명일 때 덜 치명적인 쪽을 고르는 순위다(계획 R-3 규칙 4-4). 기존 위험 순위(<see cref="HazardRank"/>)이고,
        /// 붕괴 착지는 체력 전부를 잃는 것으로 본다. 첫 칸은 <see cref="IsCertainLethal"/>와 같은 판정이다.
        /// </summary>
        internal static (bool terminal, int possible, int known, int bind) LethalRank(int health, in HazardExposure arrival,
            bool collapseLanding)
        {
            if (collapseLanding) return (true, health, health, arrival.PossibleBindEndCount);
            var rank = HazardRank(arrival, health);
            return (IsCertainLethal(health, arrival, false), rank.possible, rank.known, rank.bind);
        }
        /// <summary>
        /// 접근 진척입니다(교전 필드의 남은 비용 감소). 필드가 칸 상한에 걸려 버려졌으면 대표 추격 경로의 끝 칸에만 그 걸음 수를 줍니다.
        /// 큰 판에서 모든 이동이 0이 되어 대기만 고르는 것을 막는 대체 규칙이며, 필드가 완성되면 쓰지 않습니다.
        /// </summary>
        internal long Progress(EnemyDecision move)
        {
            if (_field?.IsTruncated != true) return _field?.Progress(move.Path.Endpoint) ?? 0;
            return _pursuitPath?.Endpoint == move.Path.Endpoint ? _pursuitPath.StepCount : 0;
        }
        internal bool CollapsesOnLanding(CellCoord cell) => _collapseLandings?.Contains(cell) == true;

        internal EnemyActionCandidates(EnemyDecisionContext context, CharacterCombatFacts actor, StageHazardForecast hazards,
            bool followupOnly = false)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _hazards = hazards ?? throw new ArgumentNullException(nameof(hazards));
            _input = CombatCommandComposition.Intent(context.Input.WithActor(actor.EntityId), CombatCommandKind.Attack);
            _followupOnly = followupOnly;
            _attackLimit = followupOnly ? 1 : context.Settings.AttackCandidateLimit;
            _moveLimit = followupOnly ? 0 : context.Settings.MoveCandidateLimit;
            _steps = Build().GetEnumerator();
        }

        internal void Step()
        {
            RequireActive();
            if (IsComplete) return;
            try
            {
                if (_steps.MoveNext()) return;
                IsComplete = true;
                OrderReady = true;
                _steps.Dispose();
                _steps = null;
            }
            catch
            {
                _faulted = true;
                _steps?.Dispose();
                _steps = null;
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _steps?.Dispose();
            _steps = null;
            _field = null;
            _pursuitPath = null;
        }

        private void RequireActive()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EnemyActionCandidates));
            if (_faulted) throw new InvalidOperationException("실패한 후보 계산은 다시 진행할 수 없습니다.");
        }

        private IReadOnlyList<EnemyDecision> Completed(List<EnemyDecision> values) => Completed<List<EnemyDecision>>(values).AsReadOnly();

        private T Completed<T>(T value)
        {
            RequireActive();
            if (!IsComplete) throw new InvalidOperationException("후보 계산이 끝나지 않았습니다.");
            return value;
        }

        private IEnumerable<int> Build()
        {
            if (_actor.Owner != Owner.Enemy || _actor.Area != CharacterRuntimeArea.Field || _actor.Health.IsDead ||
                !_actor.Anchor.HasValue || !UnitActionUseRules.CanStartAttack(_actor.ActionUse)) yield break;
            IReadOnlyDictionary<(int kind, int id), Target> targets = null;
            foreach (var _ in _context.TargetSteps(value => targets = value)) yield return 0;
            _targets = targets;
            if (!_context.Units.TryGetValue(_actor.EntityId, out var actor) || !ReferenceEquals(_actor, actor))
                throw new ArgumentException("행위자는 이 결정 입력에서 읽은 사실이어야 합니다.", nameof(_actor));
            EnemyDecisionContext.ObservedGeometry geometry = null;
            foreach (var _ in _context.GeometrySteps(value => geometry = value)) yield return 0;
            var environment = geometry.Environment;
            Target pursuit = null;
            IReadOnlyCollection<CharacterCombatFacts> taunters = null;
            foreach (var _ in TauntTargeting.SourceSteps(_input, _actor, value => taunters = value)) yield return 0;
            var profile = CombatCommandCalculator.RequiredProfile(_input, _actor);
            var moveAllowed = _moveLimit > 0 && CombatMovementCalculator.Actor(_input, out _) == MovementFailureReason.None &&
                MovementRouteValidator.ValidateActorPlacement(_actor) == MovementFailureReason.None;
            yield return 0;

            var walls = geometry.Walls;
            var elevation = geometry.Elevation;
            bool attackAllowed = targets.Count != 0 && CombatAttackQueries.Actor(_input, out _) == AttackFailureReason.None;
            int damage = attackAllowed ? CombatAttackCalculator.CommonAttackDamage(_input, _actor, profile, CombatDamageKind.OrdinaryAttack) : 0;
            yield return 0;

            if (attackAllowed)
                foreach (var source in taunters)
                {
                    if (targets.TryGetValue((0, source.EntityId.Value), out var taunter))
                        foreach (var _ in AttackSteps(taunter, source.EntityId, profile, environment, walls, elevation, damage,
                            new HashSet<AttackAim>(), _attacks, _attackLimit)) yield return 0;
                    if (_attacks.Count == 0) continue;
                    RequiredTauntSource = source.EntityId;
                    yield break;
                }

            MovementPath tauntRoute = null;
            if (moveAllowed)
                foreach (var source in taunters)
                {
                    foreach (var _ in PursuitSteps(source.EntityId, value => tauntRoute = value)) yield return 0;
                    if (tauntRoute == null) continue;
                    RequiredTauntSource = source.EntityId;
                    pursuit = targets[(0, source.EntityId.Value)];
                    break;
                }

            if (tauntRoute == null && attackAllowed)
            {
                var seen = new HashSet<AttackAim>();
                var headquarters = new List<EnemyDecision>();
                foreach (var target in AttackOrder(targets, damage))
                {
                    if (_attackLimit == 1 && _attacks.Count == 1) break;
                    var output = target.IsUnit ? _attacks : headquarters;
                    int limit = target.IsUnit ? _attackLimit : 1;
                    if (output.Count < limit)
                        foreach (var _ in AttackSteps(target, default, profile, environment, walls, elevation, damage,
                            seen, output, limit)) yield return 0;
                    yield return 0;
                }
                if (headquarters.Count > 0)
                {
                    // 두 대상 종류를 비교할 자리가 있을 때에는 본부 조준 하나를 함께 보관한다.
                    if (_attacks.Count == _attackLimit && _attackLimit > 1) _attacks.RemoveAt(_attacks.Count - 1);
                    if (_attacks.Count < _attackLimit) _attacks.Add(headquarters[0]);
                }
            }
            // 교전 필드 전에 멈출 수 있게 한다. 결정 세션이 모든 행위자의 순서를 공격 후보만으로 먼저 정하고,
            // 필드는 평가하는 행위자에게만 만든다(설계 6.7을 바꾼 결정, 2026-11-10).
            OrderReady = true;
            yield return 0;
            if (!moveAllowed) yield break;
            if (targets.Count != 0)
            {
                // 도발 의무가 있으면 그 원본만, 아니면 살아 있는 적대 대상 전부로 교전 필드를 만든다(설계 6.3).
                var restrict = tauntRoute != null ? new[] { RequiredTauntSource } : null;
                foreach (var _ in EnemyEngagementField.BuildSteps(_context, _input, _actor, restrict, value => _field = value))
                    yield return 0;
            }
            if (tauntRoute == null && _field != null)
            {
                if (!_field.IsTruncated)
                {
                    // 추격 대상은 이 칸에서 남은 비용이 가장 작은 대상이다. 닿는 공격 위치가 없으면 추격하지 않는다.
                    if (_field.TryGetBestTarget(_actor.Anchor.Value, out var best))
                        foreach (var target in targets.Values)
                        {
                            if (target.Id == best) pursuit = target;
                            yield return 0;
                        }
                }
                else
                {
                    // 필드를 버린 큰 판에서만 직선거리 가장 가까운 대상의 대표 추격 경로로 대신한다.
                    var nearest = (int.MaxValue, long.MaxValue, int.MaxValue);
                    foreach (var target in targets.Values)
                    {
                        var rank = (target.IsUnit ? 0 : 1, Distance(_actor.Anchor.Value, target.Anchor), target.Id.Value);
                        if (rank.CompareTo(nearest) < 0) { nearest = rank; pursuit = target; }
                        yield return 0;
                    }
                }
            }
            foreach (var _ in MoveSteps(pursuit, tauntRoute, geometry)) yield return 0;
        }

        /// <summary>
        /// 끝 칸에서 다음 기회에 칠 수 있는 가장 좋은 대상의 값이다(설계 6.5). 교전 필드의 남은 비용이 이동 허용량 이하이고,
        /// 다음 기회 경계에서 그 대상을 끝 칸 기준으로 지정할 수 있을 때만 준다(하붑 재배치, <see cref="StageHazardForecast.CanDesignateAt"/>).
        /// 이동 후보의 저가 점수와 결정 세션의 점수가 같은 함수를 쓴다.
        /// </summary>
        internal static long NextTurnStrike(EnemyEngagementField field, CharacterCombatFacts actor, CellCoord endpoint,
            IReadOnlyDictionary<(int kind, int id), Target> targets, StageHazardForecast hazards)
        {
            if (field == null || field.IsTruncated || !field.TryGetCost(endpoint, out int cost) ||
                !field.TryGetBestTarget(endpoint, out var best)) return 0;
            if (!targets.TryGetValue((0, best.Value), out var target) && !targets.TryGetValue((1, best.Value), out target)) return 0;
            if (cost > (VoluntaryMovementRules.Allowance(actor) ?? 0)) return 0;
            foreach (var cell in target.Cells)
                if (hazards.CanDesignateAt(endpoint, cell, hazards.LastBoundaryCount) == true) return field.TargetValue(best);
            return 0;
        }

        // 후속 공격 추정(조준 하나)은 대상 값이 큰 대상부터 본다(설계 6.4). 같은 값이면 기존 대상 순서다. 그 밖에는 기존 순서 그대로다.
        private IEnumerable<Target> AttackOrder(IReadOnlyDictionary<(int kind, int id), Target> targets, int damage)
        {
            if (!_followupOnly) return targets.Values;
            var ordered = new List<(Target target, long value, int index)>();
            foreach (var target in targets.Values)
                ordered.Add((target, EnemyEngagementField.ValueOf(_context, target, damage), ordered.Count));
            ordered.Sort((a, b) => a.value != b.value ? b.value.CompareTo(a.value) : a.index.CompareTo(b.index));
            var result = new List<Target>(ordered.Count);
            foreach (var entry in ordered) result.Add(entry.target);
            return result;
        }

        private IEnumerable<int> AttackSteps(Target target, EntityId taunt, CombatAttackProfile profile,
            MovementEnvironmentFacts environment, WallGeometry walls, AttackElevation elevation, int damage,
            HashSet<AttackAim> seen, List<EnemyDecision> output, int limit)
        {
            using var aims = CombatAttackQueries.EnumerateAimsThatCanHit(_input, _actor, profile, target.Cells, environment);
            while (output.Count < limit && aims.MoveNext())
            {
                yield return 0;
                if (!(aims.Current is AttackAim aim) || seen.Contains(aim)) continue;
                bool reaches = false;
                foreach (var _ in CombatAttackQueries.DeliveryCheckSteps(_input, _actor, profile, aim, target.Id,
                    damage, environment, walls, elevation, value => reaches = value)) yield return 0;
                if (reaches && seen.Add(aim)) output.Add(EnemyDecision.Attack(_actor.EntityId, aim, taunt));
                yield return 0;
            }
        }

        private IEnumerable<int> MoveSteps(Target pursuit, MovementPath tauntRoute, EnemyDecisionContext.ObservedGeometry geometry)
        {
            var environment = geometry.Environment;
            var traversal = new MovementRouteValidator.PursuitTraversal(_input, _actor);
            foreach (var _ in traversal.IndexSteps(environment)) yield return 0;
            var mines = new HashSet<CellCoord>();
            _arrivalCells = new HashSet<CellCoord>();
            foreach (var tile in environment.Tiles)
            {
                if (tile.Active)
                {
                    if (tile.IsMine) mines.Add(tile.Cell);
                    else if (WeakGroundCalculator.CollapsesOnNextEntry(tile, _input.EnvironmentBindings))
                        (_collapseLandings ??= new HashSet<CellCoord>()).Add(tile.Cell);
                    else if (tile.TriggersArrivalRule)
                    {
                        _arrivalCells.Add(tile.Cell);
                    }
                }
                yield return 0;
            }
            bool leap = LeapTraitRule.AllowsIntermediate(_actor);
            var origin = new Node(_actor.Anchor.Value, null, default);
            var frontier = new Queue<Node>();
            frontier.Enqueue(origin);
            var visited = new HashSet<CellCoord> { origin.Cell };
            var destinations = new List<Destination>();
            int health = _actor.Health.CurrentHealth;
            int maxHealth = _actor.Health.MaxHealth;
            var holdExposure = _hazards.Evaluate(_actor, Array.Empty<CellCoord>(), origin.Cell);
            var currentRisk = HazardRank(holdExposure, health);
            // 제자리도 확실히 치명이면 치명 착지도 남겨 덜 치명적인 쪽을 고를 수 있게 한다(계획 R-3 규칙 4-4).
            bool holdLethal = IsCertainLethal(health, holdExposure, false);
            bool complete = _field != null && !_field.IsTruncated;
            var weights = EnemyScoreWeights.From(_context.Settings);
            EnemyThreatField threat = null;
            foreach (var _ in _context.ThreatSteps(value => threat = value)) yield return 0;
            HoldCheapScore = CheapScore(origin.Cell, holdExposure, false, false);
            while (frontier.Count > 0)
            {
                var from = frontier.Dequeue();
                MandatoryExpandedNodes++;
                yield return 0;
                int distance = checked(from.Distance + 1);
                if (MovementRouteValidator.StepBudget(_actor, distance, environment) != MovementFailureReason.None) continue;
                foreach (var direction in Directions)
                {
                    var cell = from.Cell + direction;
                    yield return 0;
                    if (!_input.State.IsInside(cell) || !visited.Add(cell)) continue;
                    bool canTraverse = (leap || !mines.Contains(cell)) && traversal.CanTraverse(cell);
                    bool canLand = !mines.Contains(cell) && (traversal.CanEnter(cell) ||
                        CombatMovementCalculator.CanLand(_input, cell, environment));
                    if (!canTraverse && !canLand) continue;
                    var next = new Node(cell, from, from.Risk.Add(Contact(cell)));
                    if (canTraverse) frontier.Enqueue(next);
                    if (!canLand) continue;

                    var exposure = _hazards.Evaluate(_actor, Array.Empty<CellCoord>(), cell, next.Risk.Gaps);
                    bool collapse = CollapsesOnLanding(cell);
                    if (!holdLethal && IsCertainLethal(health, exposure, collapse))
                    {
                        LethalLandingsDropped++;
                        continue;
                    }
                    var hazard = collapse
                        ? (true, health, health, exposure.PossibleBindEndCount)
                        : HazardRank(exposure, health);
                    long score = CheapScore(cell, exposure, collapse, true);
                    destinations.Add(new Destination(next, hazard, score, complete && _field.IsAttackPosition(cell),
                        complete ? _field.Progress(cell) : 0));
                    _cheapScores.Add((cell, score));
                    yield return 0;
                }
            }

            // 저가 점수(설계 6.6 1단계): 점수기 공식 그대로, 정밀 항 없이 착지 칸의 위험·플레이어 위험·진척·다음 기회 공격과
            // 공격 위치이면 그 칸 최선 대상의 값(후속 공격의 대리값)을 센다. 제자리는 진척·후속 공격 없이 같은 식이다.
            long CheapScore(CellCoord cell, HazardExposure exposure, bool collapse, bool moving)
            {
                var threatAt = threat.Evaluate(_actor, cell);
                long followup = moving && complete && _field.IsAttackPosition(cell) && _field.TryGetBestTarget(cell, out var best)
                    ? _field.TargetValue(best) : 0;
                var facts = new EnemyScoreFacts(CombatAssessmentGap.None,
                    actorHazardLoss: EnemyUtilityScorer.LossPermille(collapse ? health : exposure.PossibleHpLoss, health, maxHealth),
                    playerThreat: threatAt.EstimatedLossPermille, playerThreatLethal: threatAt.IsLethal,
                    engagementProgress: moving && complete ? _field.Progress(cell) : 0, subsequentAttackBenefit: followup,
                    nextTurnStrike: NextTurnStrike(_field, _actor, cell, _targets, _hazards),
                    additionalForecastBind: exposure.AdditionalBindAtNextOpportunity);
                return EnemyUtilityScorer.Score(facts, weights);
            }

            // 필드가 칸 상한에 걸린 큰 판에서만 대표 추격 경로를 최선 진척 칸으로 쓴다. 필드가 완성되면 진척 예약이 같은 일을 한다.
            MovementPath fallback = null;
            if (tauntRoute == null && pursuit != null && !complete)
                foreach (var _ in PursuitSteps(pursuit.Id, value => fallback = value)) yield return 0;
            _pursuitPath = tauntRoute ?? fallback;
            // 대표 경로(도발·추격)의 착지가 확실히 치명이면 후보에서 뺀다.
            bool obligation = tauntRoute != null;
            tauntRoute = SafeRoute(tauntRoute);
            fallback = SafeRoute(fallback);

            var beam = SelectBeam(destinations, currentRisk, obligation, tauntRoute, fallback);
            var built = new List<(EnemyDecision move, (long score, int steps, int y, int x) rank)>(beam.Count);
            foreach (var item in beam)
            {
                var path = item.Path;
                if (path == null)
                {
                    var node = item.Destination.Node;
                    if (item.Escape && node.Risk.Contacts > 0)
                        foreach (var _ in RefineSteps(origin, node, traversal, environment, mines, leap, value => node = value))
                            yield return 0;
                    foreach (var _ in PathSteps(node, value => path = value)) yield return 0;
                }
                built.Add((EnemyDecision.Move(_actor.EntityId, path, item.Taunt), item.Rank));
            }
            // 빔 안에서는 저가 점수 순서다. 결정 세션이 후속 공격을 더해 다시 줄 세우고 앞의 R개를 정밀 평가한다(설계 6.6 3·4단계).
            built.Sort((a, b) => a.rank.CompareTo(b.rank));
            foreach (var move in built) _moves.Add(move.move);

            MovementPath SafeRoute(MovementPath path)
            {
                if (path == null || holdLethal) return path;
                var arrival = _hazards.Evaluate(_actor, Array.Empty<CellCoord>(), path.Endpoint);
                if (!IsCertainLethal(health, arrival, CollapsesOnLanding(path.Endpoint))) return path;
                LethalLandingsDropped++;
                return null;
            }
        }

        internal IEnumerable<int> EntryEstimateSteps(MovementPath path, Action<long, CombatAssessmentGap> completed)
        {
            var gaps = CombatAssessmentGap.None;
            for (int i = 1; i < path.Cells.Count; i++)
            {
                var cell = path.Cells[i];
                if (_arrivalCells.Contains(cell)) gaps |= CombatAssessmentGap.UnsupportedEffect | CombatAssessmentGap.UnknownPosition;
                yield return 0;
            }
            // 경로는 같은 칸을 두 번 밟을 수 없다. 중간에 무너질 칸을 밟아도 종점에 남아 있을 때만 낙사한다.
            completed(CollapsesOnLanding(path.Endpoint) ? _actor.Health.CurrentHealth : 0, gaps);
        }

        internal static IEnumerable<int> EstimateAttackSteps(EnemyDecisionContext context, CharacterCombatFacts actor,
            EnemyDecision action, EnemyScoreFacts partial, Action<EnemyScoreFacts> completed)
        {
            EnemyDecisionContext.ObservedGeometry geometry = null;
            foreach (var _ in context.GeometrySteps(value => geometry = value)) yield return 0;
            var input = CombatCommandComposition.Intent(context.Input.WithActor(actor.EntityId), CombatCommandKind.Attack, action.Aim);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            int damage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.OrdinaryAttack);
            yield return 0;
            ResolvedAttackDelivery delivery = null;
            foreach (var _ in CombatAttackQueries.DeliverySteps(input, actor, profile, action.Aim, damage,
                geometry.Environment, geometry.Walls, geometry.Elevation, value => delivery = value)) yield return 0;
            long opponentLoss = 0, alliedLoss = 0;
            if (delivery != null)
            {
                var estimated = new Dictionary<EntityId, long>();
                foreach (var hit in delivery.Hits)
                {
                    if (hit.AppliesDamage)
                    {
                        long loss = (long)hit.BaseDamage * hit.AppliedPercent / 100;
                        estimated[hit.TargetId] = checked(estimated.GetValueOrDefault(hit.TargetId) + loss);
                    }
                    yield return 0;
                }
                var hostility = new HostilityPolicy();
                foreach (var entry in estimated)
                {
                    HealthStateSnapshot health;
                    Owner owner;
                    if (context.Units.TryGetValue(entry.Key, out var unit))
                    { health = unit.Health; owner = unit.Owner; }
                    else if (context.Structures.TryGetValue(entry.Key, out var structure) && structure.ObstacleDurability == null)
                    { health = structure.Health; owner = structure.Owner; }
                    else { yield return 0; continue; }

                    long loss = EnemyUtilityScorer.LossPermille(entry.Value, health.CurrentHealth, health.MaxHealth);
                    if (owner == actor.Owner) alliedLoss = checked(alliedLoss + loss);
                    else if (hostility.AreHostile(actor.Owner, owner)) opponentLoss = checked(opponentLoss + loss);
                    yield return 0;
                }
            }
            completed(new EnemyScoreFacts(partial.Gaps | CombatAssessmentGap.UnsupportedEffect,
                budgetExhausted: partial.BudgetExhausted, opponentHpLoss: opponentLoss, alliedHpLoss: alliedLoss,
                possibleAlliedHpLoss: Math.Max(alliedLoss, partial.PossibleAlliedHpLossPermille),
                hasUnresolvedAlliedLoss: partial.HasUnresolvedAlliedLoss));
        }

        private IEnumerable<int> PursuitSteps(EntityId target, Action<MovementPath> completed)
        {
            using var pursuit = new EnemyPursuitSearch(_input, target, _context.Diagnostics);
            yield return 0;
            while (!pursuit.IsComplete) { pursuit.Step(); yield return 0; }
            pursuit.TryGetPath(out var path);
            completed(path);
        }

        private IEnumerable<int> RefineSteps(Node origin, Node goal, MovementRouteValidator.PursuitTraversal traversal,
            MovementEnvironmentFacts environment, HashSet<CellCoord> mines, bool leap, Action<Node> completed)
        {
            int limit = Math.Min(256, _context.Settings.AlternateRouteNodeLimit);
            var frontier = new SortedDictionary<((int throws, int contacts) risk, int steps, long order), Node>();
            var ranks = new Dictionary<(CellCoord cell, int steps), (int throws, int contacts)>();
            long order = 0;
            Node best = goal;
            Enqueue(origin);
            while (frontier.Count > 0)
            {
                if (RefinementExpandedNodes == limit) { RefinementTruncated = true; break; }
                using var first = frontier.GetEnumerator();
                first.MoveNext();
                var item = first.Current;
                frontier.Remove(item.Key);
                var node = item.Value;
                yield return 0;
                if (ranks[(node.Cell, node.Distance)] != node.Risk.Rank) continue;
                RefinementExpandedNodes++;
                if (node.Cell == goal.Cell) break;
                if (MovementRouteValidator.StepBudget(_actor, checked(node.Distance + 1), environment) != MovementFailureReason.None) continue;
                foreach (var direction in Directions)
                {
                    var cell = node.Cell + direction;
                    yield return 0;
                    if (!_input.State.IsInside(cell) || !leap && mines.Contains(cell)) continue;
                    if (cell == goal.Cell)
                    {
                        if (mines.Contains(cell) || !CombatMovementCalculator.CanLand(_input, cell, environment)) continue;
                    }
                    else if (!traversal.CanTraverse(cell)) continue;
                    bool repeated = false;
                    for (var previous = node; previous != null; previous = previous.Previous)
                    {
                        if (previous.Cell == cell) { repeated = true; break; }
                        yield return 0;
                    }
                    if (repeated) continue;
                    Enqueue(new Node(cell, node, node.Risk.Add(Contact(cell))));
                }
            }
            completed(best);

            void Enqueue(Node next)
            {
                var key = (next.Cell, next.Distance);
                if (ranks.TryGetValue(key, out var prior) && prior.CompareTo(next.Risk.Rank) <= 0) return;
                ranks[key] = next.Risk.Rank;
                if (next.Cell == goal.Cell && (next.Risk.Rank, next.Distance).CompareTo((best.Risk.Rank, best.Distance)) < 0)
                    best = next;
                frontier.Add((next.Risk.Rank, next.Distance, order++), next);
            }
        }

        private IEnumerable<int> PathSteps(Node node, Action<MovementPath> completed)
        {
            var cells = new CellCoord[node.Distance + 1];
            for (var next = node; next != null; next = next.Previous)
            {
                cells[next.Distance] = next.Cell;
                yield return 0;
            }
            RouteReconstructionCount++;
            completed(new MovementPath(cells));
        }

        private EntryRisk Contact(CellCoord cell)
        {
            var gaps = _hazards.EntryGaps(_actor, cell);
            bool throws = (gaps & CombatAssessmentGap.UnknownPosition) != 0;
            bool other = _arrivalCells.Contains(cell);
            if (other) gaps |= CombatAssessmentGap.UnsupportedEffect;
            return new EntryRisk(throws ? 1 : 0, throws || other ? 1 : 0, gaps);
        }

        /// <summary>
        /// 이동 빔 선택 하나다(설계 6.6, 계획 R-4 예약 규칙). 예약을 먼저, 남은 자리를 저가 점수 순서(같으면 걸음 수·y·x)로 채우고,
        /// 같은 끝 칸은 한 자리만 쓴다. 도발 의무가 있으면 빔에는 도발 경로만 둔다(의무는 다양성이 아니라 규칙이다).
        /// 의무가 없으면 예약 우선순위는 위험 피하기(제자리보다 위험 순위가 낮음) → 최선 공격 위치 → 최선 진척이고,
        /// 예약이 K보다 많으면 낮은 우선순위부터 버린다.
        /// </summary>
        private List<BeamItem> SelectBeam(List<Destination> destinations, (bool terminal, int possible, int known, int bind) currentRisk,
            bool obligation, MovementPath tauntRoute, MovementPath progressRoute)
        {
            var byCell = new Dictionary<CellCoord, Destination>(destinations.Count);
            foreach (var destination in destinations) byCell[destination.Node.Cell] = destination;
            var ordered = new List<Destination>(destinations);
            ordered.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            var items = new List<BeamItem>();
            var used = new HashSet<CellCoord>();

            if (obligation)
            {
                if (tauntRoute != null && Take(BeamItem.Route(tauntRoute, RequiredTauntSource, byCell.GetValueOrDefault(tauntRoute.Endpoint))))
                    _reserved.Add(tauntRoute.Endpoint);
            }
            else
            {
                Destination escape = null, attack = null, progress = null;
                foreach (var destination in ordered)
                {
                    if (escape == null && destination.Hazard.CompareTo(currentRisk) < 0) escape = destination;
                    if (attack == null && destination.AttackPosition) attack = destination;
                    if (destination.Progress > 0 && (progress == null || destination.Progress > progress.Progress)) progress = destination;
                }
                var reservations = new[]
                {
                    escape == null ? null : BeamItem.Landing(escape, escape: true),
                    attack == null ? null : BeamItem.Landing(attack),
                    progressRoute != null ? BeamItem.Route(progressRoute, default, byCell.GetValueOrDefault(progressRoute.Endpoint))
                        : progress == null ? null : BeamItem.Landing(progress)
                };
                foreach (var reservation in reservations)
                    if (reservation != null && Take(reservation)) _reserved.Add(reservation.Endpoint);
                foreach (var destination in ordered) Take(BeamItem.Landing(destination));
            }

            foreach (var destination in destinations)
                if (!used.Contains(destination.Node.Cell) && destination.Score > BestPrunedScore) BestPrunedScore = destination.Score;
            return items;

            bool Take(BeamItem item)
            {
                if (items.Count >= _moveLimit || !used.Add(item.Endpoint)) return false;
                items.Add(item);
                return true;
            }
        }

        internal static (bool terminal, int possible, int known, int bind) HazardRank(HazardExposure exposure, int health) =>
            (exposure.KnownHpLoss >= health, exposure.PossibleHpLoss, exposure.KnownHpLoss, exposure.PossibleBindEndCount);

        private static long Distance(CellCoord first, CellCoord second) =>
            Math.Abs((long)first.X - second.X) + Math.Abs((long)first.Y - second.Y);

        private readonly struct EntryRisk
        {
            internal int Throws { get; }
            internal int Contacts { get; }
            internal CombatAssessmentGap Gaps { get; }
            internal (int throws, int contacts) Rank => (Throws, Contacts);

            internal EntryRisk(int throws, int contacts, CombatAssessmentGap gaps)
            {
                Throws = throws;
                Contacts = contacts;
                Gaps = gaps;
            }

            internal EntryRisk Add(EntryRisk next) => new EntryRisk(checked(Throws + next.Throws),
                checked(Contacts + next.Contacts), Gaps | next.Gaps);
        }

        private sealed class Node
        {
            internal CellCoord Cell { get; }
            internal Node Previous { get; }
            internal EntryRisk Risk { get; }
            internal int Distance { get; }

            internal Node(CellCoord cell, Node previous, EntryRisk risk)
            { Cell = cell; Previous = previous; Risk = risk; Distance = previous == null ? 0 : checked(previous.Distance + 1); }
        }

        private sealed class Destination
        {
            internal Node Node { get; }
            internal (bool terminal, int possible, int known, int bind) Hazard { get; }
            internal long Score { get; }
            internal bool AttackPosition { get; }
            internal long Progress { get; }
            /// <summary>저가 점수가 높은 쪽이 앞이고, 같으면 걸음 수·y·x 순서다(기존 안정 순서).</summary>
            internal (long score, int steps, int y, int x) Rank => (-Score, Node.Distance, Node.Cell.Y, Node.Cell.X);

            internal Destination(Node node, (bool terminal, int possible, int known, int bind) hazard, long score,
                bool attackPosition, long progress)
            {
                Node = node;
                Hazard = hazard;
                Score = score;
                AttackPosition = attackPosition;
                Progress = progress;
            }
        }

        private sealed class BeamItem
        {
            internal Destination Destination { get; }
            internal MovementPath Path { get; }
            internal EntityId Taunt { get; }
            internal bool Escape { get; }
            internal CellCoord Endpoint => Path?.Endpoint ?? Destination.Node.Cell;
            /// <summary>착지 칸 기록이 없는 대표 경로(필드를 버린 판의 추격 경로 등)는 빔 안에서 맨 뒤다.</summary>
            internal (long score, int steps, int y, int x) Rank => Destination?.Rank ??
                (long.MaxValue, Path.StepCount, Path.Endpoint.Y, Path.Endpoint.X);

            private BeamItem(Destination destination, MovementPath path, EntityId taunt, bool escape)
            {
                Destination = destination;
                Path = path;
                Taunt = taunt;
                Escape = escape;
            }

            internal static BeamItem Landing(Destination destination, bool escape = false) => new BeamItem(destination, null, default, escape);
            internal static BeamItem Route(MovementPath path, EntityId taunt, Destination endpoint) => new BeamItem(endpoint, path, taunt, false);
        }
    }
}
