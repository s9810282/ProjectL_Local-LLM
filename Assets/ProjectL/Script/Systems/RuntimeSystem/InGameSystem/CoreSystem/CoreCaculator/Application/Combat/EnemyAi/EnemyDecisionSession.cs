using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>고정된 전투 입력에서 적의 행동 하나를 고르며, 실제 정산은 호출자가 수행한다.</summary>
    public sealed class EnemyDecisionSession : IDisposable
    {
        private IEnumerator<int> _steps;
        private EnemyDecision _selected;
        private long _version;
        private bool _disposed;
        private bool _faulted;
        private readonly bool _immediateCompatibility;
        // 연구 저장소 전용 연결: 턴 계획 AI가 일 스레드 계산을 기다리는 중인지(LLMTest/docs/2026-11-18-planner-background-parallel-design.md BP3).
        private bool _waiting;

        public EnemyDecisionSession(CombatCommandInput input, IReadOnlyList<IMovementTileRule> movementRules,
            EnemyDecisionSettings settings)
            : this(input, movementRules, settings, (EnemyDecisionDiagnostics)null, null)
        {
        }

        /// <param name="sideCache">
        /// 같은 적 진영의 결정끼리 공유 구조(위험 예보·위협 필드·목표 판정)를 다시 쓰는 캐시다(설계 6.8). null이면 쓰지 않는다.
        /// 캐시를 쓰든 안 쓰든 결정은 같다.
        /// </param>
        public EnemyDecisionSession(CombatCommandInput input, IReadOnlyList<IMovementTileRule> movementRules,
            EnemyDecisionSettings settings, EnemyDecisionSideCache sideCache)
            : this(input, movementRules, settings, null, sideCache)
        {
        }

        internal EnemyDecisionSession(CombatCommandInput input, IReadOnlyList<IMovementTileRule> movementRules,
            EnemyDecisionSettings settings, EnemyDecisionDiagnostics diagnostics, EnemyDecisionSideCache sideCache = null)
            : this(input, movementRules, settings, diagnostics, immediateCompatibility: false, sideCache)
        {
        }

        internal static EnemyDecisionSession BeginImmediateCompatibility(CombatCommandInput input,
            IReadOnlyList<IMovementTileRule> movementRules, EnemyDecisionSettings settings, EnemyDecisionDiagnostics diagnostics = null) =>
            new EnemyDecisionSession(input, movementRules, settings, diagnostics, immediateCompatibility: true, null);

        private EnemyDecisionSession(CombatCommandInput input, IReadOnlyList<IMovementTileRule> movementRules,
            EnemyDecisionSettings settings, EnemyDecisionDiagnostics diagnostics, bool immediateCompatibility,
            EnemyDecisionSideCache sideCache)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (movementRules == null) throw new ArgumentNullException(nameof(movementRules));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (input.ProcessingKind != CombatProcessingKind.Advance || input.State.Phase != CombatFlowPhase.Automatic ||
                input.State.Turn.StagePhase != StagePhase.Combat || input.State.Turn.ControllerPhase != CombatControllerPhase.Enemy)
                throw new InvalidOperationException("자동 적 진영 턴의 입력이 아닙니다.");
            Diagnostics = diagnostics ?? new EnemyDecisionDiagnostics();
            _immediateCompatibility = immediateCompatibility;
            _steps = (immediateCompatibility ? SelectImmediate(input, movementRules, settings) :
                settings.DecisionModel == EnemyDecisionModel.TurnPlanner
                    // 연구 저장소 전용 연결: 턴 계획 AI(Assets/ResearchEnemyPlanner, LLMTest/docs/2026-11-18-core-turn-planner-port-design.md CP3).
                    ? EnemyPlanner.EnemyTurnPlanner.Steps(input, movementRules, sideCache, Diagnostics, decision => _selected = decision,
                        () => SelectUtility(input, movementRules, settings, sideCache), waiting => _waiting = waiting)
                    : SelectUtility(input, movementRules, settings, sideCache)).GetEnumerator();
        }

        public bool IsReady => _selected != null;
        /// <summary>
        /// 결정이 다른 스레드의 계산을 기다리는 중이다(연구 저장소 전용, 턴 계획 AI만). 이때 <see cref="Step"/>을 더 불러도 진행하지 않으므로
        /// 준비하는 쪽은 그 프레임을 멈춰도 된다.
        /// </summary>
        public bool IsWaiting => _waiting;
        public long WorkVersion => _version;
        internal string WorkPhase { get; private set; } = "DecisionInitialize";
        public EnemyDecision Decision => _selected ?? throw new InvalidOperationException("행동 선택이 끝나지 않았습니다.");
        public EnemyDecisionDiagnostics Diagnostics { get; }

        public void Step()
        {
            RequireActive();
            if (IsReady) return;
            try
            {
                if (!_steps.MoveNext() && !IsReady)
                    throw new InvalidOperationException("행동 선택이 결과 없이 끝났습니다.");
                _version++;
                if (!_immediateCompatibility) Diagnostics.WorkUnits++;
            }
            catch
            {
                _faulted = true;
                Release();
                throw;
            }
        }

        /// <summary>같은 입력에서 거절된 행동의 다음 대안부터 이어간다.</summary>
        public void RejectSelected() => RejectSelected(null);

        internal void RejectSelected(string reason)
        {
            RequireActive();
            if (Decision.Kind == EnemyDecisionKind.EndEnemyPhase)
                throw new InvalidOperationException("진영 종료에는 대체 행동이 없습니다.");
            Diagnostics.RecordRejection(_selected, reason);
            _selected = null;
            _version++;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Release();
        }

        private void RequireActive()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EnemyDecisionSession));
            if (_faulted) throw new InvalidOperationException("실패한 행동 선택은 다시 진행할 수 없습니다.");
        }

        private void Release()
        {
            _steps?.Dispose();
            _steps = null;
            _selected = null;
        }

        private IEnumerable<int> SelectUtility(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, EnemyDecisionSettings settings,
            EnemyDecisionSideCache sideCache)
        {
            var context = new EnemyDecisionContext(input, settings, Diagnostics, sideCache);
            foreach (var _ in context.IndexSteps()) yield return 0;
            if (context.Actors.Count == 0)
            {
                _selected = EnemyDecision.EndEnemyPhase();
                WorkPhase = "DecisionReadyEnd";
                yield break;
            }

            WorkPhase = "UtilityForecast";
            StageHazardForecast hazards = null;
            foreach (var _ in context.ForecastSteps(value => hazards = value)) yield return 0;
            WorkPhase = "UtilityThreat";
            EnemyThreatField threat = null;
            foreach (var _ in context.ThreatSteps(value => threat = value)) yield return 0;
            EnemyDecisionContext.ObservedGeometry geometry = null;
            foreach (var _ in context.GeometrySteps(value => geometry = value)) yield return 0;
            IReadOnlyDictionary<(int kind, int id), EnemyDecisionContext.HostileTarget> targets = null;
            foreach (var _ in context.TargetSteps(value => targets = value)) yield return 0;
            var world = new DecisionWorld(input, rules, context, hazards, threat, geometry, targets, EnemyScoreWeights.From(settings));

            // 1차: 모든 행위자의 공격 후보만 만들어 지금 칠 수 있는 행위자를 먼저 세운다(같으면 개체 번호, 설계 6.7을 바꾼 결정 2026-11-10).
            // 교전 필드와 이동 후보는 2차에서 평가하는 행위자에게만 만든다. 그 공격 후보는 2차에 그대로 쓴다.
            var pending = new List<EnemyActionCandidates>(context.Actors.Count);
            try
            {
                WorkPhase = "UtilityOrder";
                foreach (var actor in context.Actors.Values)
                {
                    var candidates = new EnemyActionCandidates(context, actor, hazards);
                    pending.Add(candidates);
                    yield return 0;
                    while (!candidates.IsComplete && !candidates.OrderReady) { candidates.Step(); yield return 0; }
                }
                pending.Sort((a, b) => a.OrderKey.CompareTo(b.OrderKey));

                foreach (var candidates in pending)
                {
                    WorkPhase = "UtilityCandidates";
                    while (!candidates.IsComplete) { candidates.Step(); yield return 0; }
                    var actor = candidates.Actor;
                    Diagnostics.ExpandedNodes += candidates.MandatoryExpandedNodes + candidates.RefinementExpandedNodes + candidates.EngagementExpandedNodes;
                    if (candidates.RefinementTruncated) Diagnostics.CapHits++;
                    if (candidates.EngagementTruncated) Diagnostics.CapHits++;
                    var attacks = candidates.Attacks;
                    var moves = candidates.Moves;
                    var field = candidates.Field;
                    Diagnostics.GeneratedAttacks += attacks.Count;
                    Diagnostics.GeneratedMoves += moves.Count;

                    bool mandatory = !candidates.RequiredTauntSource.IsEmpty;
                    var origin = actor.Anchor.Value;
                    var holdArrival = hazards.Evaluate(actor, Array.Empty<CellCoord>(), origin);
                    bool holdLethal = EnemyActionCandidates.IsCertainLethal(actor.Health.CurrentHealth, holdArrival, false);
                    var hold = PositionFacts(actor, new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment), null, hazards, threat, 0, 0, 0, default,
                        EnemyActionCandidates.NextTurnStrike(field, actor, origin, targets, hazards));
                    var holdRank = EnemyUtilityScorer.Rank(hold, world.Weights);
                    // 진단 기록(설계 6.9)은 요청했을 때만 쌓는다. 판단에는 쓰지 않는다.
                    var record = Diagnostics.BeginActor(actor.EntityId, EnemyUtilityScorer.Terms(hold, world.Weights), holdRank.tier, holdLethal);
                    var options = new List<Option>();
                    var moveOptions = new List<Option>();
                    foreach (var family in new[] { attacks, moves })
                    {
                        foreach (var action in family)
                        {
                            bool moving = action.Kind == EnemyDecisionKind.Move;
                            if (mandatory && action.TauntSourceId != candidates.RequiredTauntSource) { yield return 0; continue; }
                            Diagnostics.RecordAlternative(actor.EntityId, moving ? (AttackAim?)null : action.Aim, action.Path?.Endpoint);
                            // 공격은 정밀 평가를 허용한다. 이동은 먼저 간이 평가와 기하 후속 공격으로 줄 세운다(설계 6.6 3단계).
                            Option? option = null;
                            foreach (var _ in OptionSteps(world, candidates, action, !moving, value => option = value)) yield return 0;
                            if (option is Option admitted) (moving ? moveOptions : options).Add(admitted);
                            yield return 0;
                        }
                    }
                    // 앞의 R개 이동만 정밀 평가로 바꾼다(설계 6.6 4단계). 안전한 후보가 먼저이고, 그다음 점수·의도 순서다.
                    moveOptions.Sort(CompareForRefinement);
                    for (int i = 0; i < Math.Min(settings.RefinedMoveLimit, moveOptions.Count); i++)
                    {
                        Diagnostics.RefinedMoves++;
                        Option? precise = null;
                        foreach (var _ in OptionSteps(world, candidates, moveOptions[i].Action, true, value => precise = value)) yield return 0;
                        if (precise is Option refined) moveOptions[i] = refined;
                    }
                    options.AddRange(moveOptions);
                    if (record != null)
                        foreach (var option in options)
                            record.Add(new EnemyDecisionDiagnostics.CandidateRecord(option.Action,
                                EnemyUtilityScorer.Terms(option.Facts, world.Weights), option.Rank.tier, option.Lethal, option.Facts.IsPrecise));

                    // 치명적이지 않은 후보가 있으면 치명 후보를 모두 뺀다. 제자리가 치명이면 안전한 후보는 대기 점수와 비교하지 않는다
                    // (피할 수 있는 환경 죽음을 고르지 않는다). 제자리와 모든 후보가 치명이면 가장 덜 치명적인 후보만 남겨
                    // 대기와 점수로 비교한다(규칙 4-4). 제자리는 안전하고 후보만 모두 치명이면 모두 뺀다.
                    var ranked = new List<(EnemyDecision action, (int tier, long score) rank)>();
                    bool anySafe = false;
                    foreach (var option in options) anySafe |= !option.Lethal;
                    bool lethalOnly = options.Count > 0 && !anySafe;
                    if (anySafe)
                    {
                        foreach (var option in options)
                            if (!option.Lethal && (mandatory || holdLethal || option.Rank.CompareTo(holdRank) > 0))
                                ranked.Add((option.Action, option.Rank));
                    }
                    else if (lethalOnly && holdLethal)
                    {
                        var least = options[0].LethalRank;
                        foreach (var option in options)
                            if (option.LethalRank.CompareTo(least) < 0) least = option.LethalRank;
                        foreach (var option in options)
                            if (option.LethalRank.CompareTo(least) == 0 && (mandatory || option.Rank.CompareTo(holdRank) > 0))
                                ranked.Add((option.Action, option.Rank));
                    }

                    bool selectedAny = ranked.Count > 0;
                    bool attackRejected = false;
                    while (ranked.Count > 0)
                    {
                        int best = 0;
                        for (int i = 1; i < ranked.Count; i++)
                        {
                            int order = ranked[i].rank.CompareTo(ranked[best].rank);
                            if (order > 0 || order == 0 && EnemyUtilityScorer.CompareIntent(ranked[i].action, ranked[best].action) < 0)
                                best = i;
                        }
                        var choice = ranked[best].action;
                        ranked.RemoveAt(best);
                        bool attack = choice.Kind == EnemyDecisionKind.Attack;
                        if (attack && attackRejected) { yield return 0; continue; }
                        WorkPhase = attack ? "DecisionReadyAttack" : "DecisionReadyMove";
                        _selected = choice;
                        yield return 0;
                        // 공격 거절 뒤에는 이동만 남기고, 이동 거절 뒤에는 다음 행위자로 넘어간다.
                        if (!attack) break;
                        attackRejected = true;
                    }
                    // 후보 탐색이 치명 착지를 미리 빼서 남은 후보가 없으면 그것도 "모두 치명"이다.
                    bool onlyLethalFound = lethalOnly || options.Count == 0 && candidates.LethalLandingsDropped > 0;
                    // 빔에 들지 못한 칸의 저가 점수가 대기보다 높았으면 더 나은 선택지를 잘라 낸 것이다.
                    bool pruned = candidates.BestPrunedScore > holdRank.score;
                    Diagnostics.RecordHold(actor.EntityId, HoldReason(selectedAny, onlyLethalFound, pruned, attacks.Count, field, origin, options));
                }
            }
            finally
            {
                foreach (var candidates in pending) candidates.Dispose();
            }

            _selected = EnemyDecision.EndEnemyPhase();
            WorkPhase = "DecisionReadyEnd";
            yield return 0;
        }

        private static int CompareForRefinement(Option left, Option right)
        {
            if (left.Lethal != right.Lethal) return left.Lethal ? 1 : -1;
            int score = right.Rank.CompareTo(left.Rank);
            return score != 0 ? score : EnemyUtilityScorer.CompareIntent(left.Action, right.Action);
        }

        /// <summary>
        /// 행동 하나를 평가해 대기와 비교할 선택지로 만든다. 받아들여지지 않으면 없다. 이동은 정밀 투영이 없을 때 기하 후속 공격(설계 6.4)을,
        /// 정밀 투영이 있으면 그 투영의 후속 공격을 더한다. 점수와 확실한 치명 판정은 같은 끝 칸 사실로 낸다.
        /// </summary>
        private IEnumerable<int> OptionSteps(DecisionWorld world, EnemyActionCandidates candidates, EnemyDecision action, bool allowPrecise,
            Action<Option?> completed)
        {
            var actor = candidates.Actor;
            var field = candidates.Field;
            bool moving = action.Kind == EnemyDecisionKind.Move;
            EnemyScoreFacts facts;
            CombatCommandInput moved;
            CombatAssessmentGap routeGaps;
            using (var assessment = CombatActionAssessment.Begin(world.Input, action, world.Rules, world.Context, allowPrecise))
            {
                yield return 0;
                while (!assessment.IsComplete)
                {
                    WorkPhase = assessment.Phase;
                    assessment.Step();
                    yield return 0;
                }
                if (!assessment.Admitted) { completed(null); yield break; }
                facts = assessment.Facts;
                moved = assessment.MovedFacts;
                routeGaps = assessment.RouteGaps;
            }

            WorkPhase = "UtilityCoarseFacts";
            long entryLoss = 0;
            var entryGaps = CombatAssessmentGap.None;
            bool collapseLanding = false;
            if (!facts.IsPrecise)
            {
                if (moving)
                {
                    collapseLanding = candidates.CollapsesOnLanding(action.Path.Endpoint);
                    foreach (var _ in candidates.EntryEstimateSteps(action.Path, (loss, gaps) => { entryLoss = loss; entryGaps = gaps; }))
                        yield return 0;
                }
                else
                {
                    EnemyScoreFacts estimate = null;
                    foreach (var _ in EnemyActionCandidates.EstimateAttackSteps(world.Context, actor, action, facts, value => estimate = value))
                        yield return 0;
                    facts = estimate;
                }
            }

            // 정밀 이동 투영이 없으면 이동 끝 칸에서의 공격을 기하 세계로 추정한다(설계 6.4). 정밀 투영이 있으면 그것을 쓴다.
            bool projected = moved != null;
            if (moving && !projected && EnemyFollowupGeometry.IsEligible(field, action.Path, routeGaps, collapseLanding))
                moved = EnemyFollowupGeometry.Relocate(world.Input, actor, action.Path, world.Geometry.Environment);
            long followup = 0;
            if (moved != null)
            {
                WorkPhase = "UtilityFollowup";
                foreach (var _ in FollowupSteps(moved, actor.EntityId, world.Hazards, world.Rules, world.Context, projected && facts.IsPrecise,
                    value => followup = value))
                    yield return 0;
            }
            WorkPhase = "UtilityCompareWithHold";
            var endpoint = facts.Endpoint ?? action.Path?.Endpoint ?? actor.Anchor.Value;
            var scoreFacts = PositionFacts(actor, facts, action.Path, world.Hazards, world.Threat, moving ? candidates.Progress(action) : 0,
                followup, entryLoss, entryGaps, EnemyActionCandidates.NextTurnStrike(field, actor, endpoint, world.Targets, world.Hazards));
            var (lethal, lethalRank) = CandidateLethality(facts, actor, endpoint, world.Hazards, collapseLanding);
            completed(new Option(action, EnemyUtilityScorer.Rank(scoreFacts, world.Weights), scoreFacts, lethal, lethalRank));
        }

        /// <summary>한 결정 안에서 모든 행위자가 함께 쓰는 관측 사실이다.</summary>
        private sealed class DecisionWorld
        {
            internal CombatCommandInput Input { get; }
            internal IReadOnlyList<IMovementTileRule> Rules { get; }
            internal EnemyDecisionContext Context { get; }
            internal StageHazardForecast Hazards { get; }
            internal EnemyThreatField Threat { get; }
            internal EnemyDecisionContext.ObservedGeometry Geometry { get; }
            internal IReadOnlyDictionary<(int kind, int id), EnemyDecisionContext.HostileTarget> Targets { get; }
            internal EnemyScoreWeights Weights { get; }

            internal DecisionWorld(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext context,
                StageHazardForecast hazards, EnemyThreatField threat, EnemyDecisionContext.ObservedGeometry geometry,
                IReadOnlyDictionary<(int kind, int id), EnemyDecisionContext.HostileTarget> targets, EnemyScoreWeights weights)
            {
                Input = input;
                Rules = rules;
                Context = context;
                Hazards = hazards;
                Threat = threat;
                Geometry = geometry;
                Targets = targets;
                Weights = weights;
            }
        }

        /// <summary>
        /// 후보 하나의 확실한 치명 판정과 순위다(계획 R-3 치명 위험 규칙). 판정 자체는 <see cref="EnemyActionCandidates.IsCertainLethal"/> 하나다.
        /// 이번 행동으로 적 승리가 확정되면 이후 경계가 오지 않으므로 거르지 않는다(규칙 4-2). 행동 뒤 이미 죽었으면 치명이다.
        /// 간이 평가의 진입 손실은 붕괴 착지 때만 0이 아니고 그때는 붕괴 착지가 먼저 치명을 정하므로, 진입 손실은 판정에 넣지 않는다(가시 폭탄 Phase 4 D7).
        /// </summary>
        internal static (bool lethal, (bool terminal, int possible, int known, int bind) rank) CandidateLethality(EnemyScoreFacts facts,
            CharacterCombatFacts actor, CellCoord endpoint, StageHazardForecast hazards, bool collapseLanding)
        {
            if (facts.GuaranteedTerminal && facts.ProjectedOutcome == StageOutcome.Defeat) return (false, (false, 0, 0, 0));
            var positioned = facts.PostActionActor ?? actor;
            if (positioned.Health.IsDead) return (true, (true, int.MaxValue, int.MaxValue, 0));
            int health = positioned.Health.CurrentHealth;
            var arrival = hazards.Evaluate(positioned, Array.Empty<CellCoord>(), endpoint);
            return (EnemyActionCandidates.IsCertainLethal(health, arrival, collapseLanding),
                EnemyActionCandidates.LethalRank(health, arrival, collapseLanding));
        }

        internal readonly struct Option
        {
            internal EnemyDecision Action { get; }
            internal (int tier, long score) Rank { get; }
            internal EnemyScoreFacts Facts { get; }
            internal bool Lethal { get; }
            internal (bool terminal, int possible, int known, int bind) LethalRank { get; }

            internal Option(EnemyDecision action, (int tier, long score) rank, EnemyScoreFacts facts, bool lethal,
                (bool terminal, int possible, int known, int bind) lethalRank)
            {
                Action = action;
                Rank = rank;
                Facts = facts;
                Lethal = lethal;
                LethalRank = lethalRank;
            }
        }

        /// <summary>
        /// 행동하지 않은 행위자의 이유 하나다(계획 R-3 규칙 5, R-4 Pruned). 실제 정산의 거절 → 모두 치명 → 빔이 더 나은 선택지를 잘라 냄
        /// → 닿는 대상 없음(필드 상한이면 FieldTruncated)
        /// → 가장 좋은 안전 후보가 아군 피해 상한 때문에 낮아짐 → 교환 손해 순서로 정한다.
        /// </summary>
        internal static EnemyHoldReason HoldReason(bool selected, bool lethalOnly, bool pruned, int attackCount, EnemyEngagementField field,
            CellCoord origin, List<Option> options)
        {
            if (selected) return EnemyHoldReason.WorldUncertainty;
            if (lethalOnly) return EnemyHoldReason.LethalOnly;
            if (pruned) return EnemyHoldReason.Pruned;
            if (attackCount == 0)
            {
                if (field != null && field.IsTruncated) return EnemyHoldReason.FieldTruncated;
                if (field == null || !field.TryGetBestTarget(origin, out _)) return EnemyHoldReason.NoReachableTarget;
            }
            Option? best = null;
            foreach (var option in options)
                if (!option.Lethal && (best == null || option.Rank.CompareTo(best.Value.Rank) > 0)) best = option;
            if (best == null) return EnemyHoldReason.NoReachableTarget;
            var facts = best.Value.Facts;
            return facts.HasUnresolvedAlliedLoss && facts.PossibleAlliedHpLossPermille > facts.AlliedHpLossPermille
                ? EnemyHoldReason.WorldUncertainty : EnemyHoldReason.ExchangeLoss;
        }

        private IEnumerable<int> FollowupSteps(CombatCommandInput moved, EntityId actorId, StageHazardForecast hazards,
            IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext budget, bool preciseMove, Action<long> completed)
        {
            var context = new EnemyDecisionContext(moved, budget.Settings);
            foreach (var _ in context.IndexSteps()) yield return 0;
            using var candidates = new EnemyActionCandidates(context, context.Units[actorId], hazards, followupOnly: true);
            yield return 0;
            while (!candidates.IsComplete) { candidates.Step(); yield return 0; }
            if (candidates.Attacks.Count == 0) yield break;
            int before = budget.PreviewCount;
            using var assessment = CombatActionAssessment.Begin(moved, candidates.Attacks[0], rules, budget, allowPrecise: preciseMove);
            yield return 0;
            string previousPhase = null;
            while (!assessment.IsComplete)
            {
                string phase = assessment.Phase;
                if (phase != previousPhase)
                {
                    WorkPhase = "UtilityFollowup/" + phase;
                    previousPhase = phase;
                }
                assessment.Step();
                if (budget.PreviewCount != before)
                {
                    Diagnostics.FollowupPreviewCalls++;
                    before = budget.PreviewCount;
                }
                yield return 0;
            }
            if (!assessment.Admitted) yield break;
            var facts = assessment.Facts;
            if (!facts.IsPrecise)
                foreach (var _ in EnemyActionCandidates.EstimateAttackSteps(context, context.Units[actorId],
                    candidates.Attacks[0], facts, value => facts = value)) yield return 0;
            completed(EnemyUtilityScorer.AttackBenefit(facts));
        }

        private static EnemyScoreFacts PositionFacts(CharacterCombatFacts actor, EnemyScoreFacts facts, MovementPath path,
            StageHazardForecast hazards, EnemyThreatField threat, long progress, long followup, long entryLoss, CombatAssessmentGap entryGaps,
            long nextTurnStrike)
        {
            // 승패가 확정된 경우에만 이후 경계를 제외한다. 공개 투영의 승패만으로 위험을 지우지 않는다.
            if (facts.GuaranteedTerminal)
                return facts.WithPosition(entryGaps, 0, 0, 0, 0);
            var positionedActor = facts.PostActionActor ?? actor;
            int health = positionedActor.Health.CurrentHealth;
            int maxHealth = positionedActor.Health.MaxHealth;
            long immediate = EnemyUtilityScorer.LossPermille(entryLoss, health, maxHealth);
            health -= (int)Math.Min(health, entryLoss);
            if (health == 0)
                return facts.WithPosition(entryGaps, 0, 0, 0, 0, immediate);
            var remaining = immediate == 0 && health == positionedActor.Health.CurrentHealth
                ? positionedActor : positionedActor.With(health: new HealthStateSnapshot(health, maxHealth));
            CellCoord endpoint = facts.Endpoint ?? path?.Endpoint ?? actor.Anchor.Value;
            var unresolvedPath = facts.IsPrecise ? Array.Empty<CellCoord>() : path?.Cells ?? Array.Empty<CellCoord>();
            var exposure = hazards.Evaluate(remaining, unresolvedPath, endpoint, entryGaps);
            var player = threat.Evaluate(remaining, endpoint);
            return facts.WithPosition(exposure.Gaps | player.Gaps, EnemyUtilityScorer.LossPermille(exposure.PossibleHpLoss, health, maxHealth),
                player.EstimatedLossPermille, progress, followup, immediate, exposure.AdditionalBindAtNextOpportunity, nextTurnStrike,
                player.IsLethal);
        }

        private IEnumerable<int> SelectImmediate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, EnemyDecisionSettings settings)
        {
            var context = new EnemyDecisionContext(input, settings);
            var policy = new EnemyAiPolicy(new HostilityPolicy(), rules, Diagnostics);
            foreach (var _ in context.IndexSteps()) yield return 0;
            foreach (var actor in context.Actors.Values)
            {
                WorkPhase = "DecisionActor";
                var current = CombatCommandComposition.Intent(input.WithActor(actor.EntityId), CombatCommandKind.Attack);
                yield return 0;
                AttackAim? selectedAim = null;
                EntityId taunt = default;
                foreach (var phase in policy.AttackSteps(current, actor, context, null,
                    (aim, source) => { selectedAim = aim; taunt = source; }))
                {
                    WorkPhase = phase;
                    yield return 0;
                }
                if (selectedAim is AttackAim attack)
                {
                    _selected = EnemyDecision.Attack(actor.EntityId, attack, taunt);
                    WorkPhase = "DecisionReadyAttack";
                    // 실제 정산에서 거절한 경우에만 같은 행위자의 추적 단계로 넘어간다.
                    yield return 0;
                }

                if (!UnitActionUseRules.CanStartMove(actor.ActionUse)) { yield return 0; continue; }
                WorkPhase = "DecisionPursuitTarget";
                EntityId target = default;
                foreach (var _ in policy.PursuitTargetSteps(current, actor, (id, source) => { target = id; taunt = source; }))
                    yield return 0;
                if (target.IsEmpty) { yield return 0; continue; }

                MovementPath path;
                using (var search = new EnemyPursuitSearch(current, target, Diagnostics))
                {
                    yield return 0;
                    while (!search.IsComplete)
                    {
                        search.Step();
                        WorkPhase = search.WorkPhase;
                        yield return 0;
                    }
                    if (!search.TryGetPath(out path)) continue;
                }
                Diagnostics.GeneratedMoves++;
                Diagnostics.RecordAlternative(actor.EntityId, null, path.Endpoint);
                _selected = EnemyDecision.Move(actor.EntityId, path, taunt);
                WorkPhase = "DecisionReadyMove";
                // 이동까지 거절되면 다음 행위자부터 찾는다.
                yield return 0;
            }
            _selected = EnemyDecision.EndEnemyPhase();
            WorkPhase = "DecisionReadyEnd";
            yield return 0;
        }
    }
}
