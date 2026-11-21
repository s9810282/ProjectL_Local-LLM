using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>
    /// 턴 계획 AI의 입구(연구용, 설계 LLMTest/docs/2026-11-18-core-turn-planner-port-design.md).
    /// 결정 세션(<see cref="EnemyDecisionSession"/>)이 조정 에셋의 <c>DecisionModel = TurnPlanner</c>일 때 지금 AI 대신 이 단계를 돈다.
    /// 턴 전체를 한 번 계획해 옆 캐시에 두고, 세션(행동 하나)마다 지금 판에 맞는 행동을 꺼낸다.
    /// </summary>
    internal static class EnemyTurnPlanner
    {
        /// <summary>
        /// 세션 단계. <paramref name="select"/>로 결정을 넣고 멈춘다. 세션이 그 결정을 거절하고 다시 부르면(설계 TP-6) 다시 계획하고,
        /// 다시 계획해도 같은 결정이거나 또 거절되면 그 행동 하나만 지금 AI(<paramref name="utility"/>)로 고른다(Q-CP4 A).
        /// 계획 중 예외가 나면 그 행동은 지금 AI로 고르고 진단 <see cref="EnemyDecisionDiagnostics.CapHits"/>를 센다(게임이 멈추지 않게).
        /// 계획 계산은 일 스레드에서 돈다(LLMTest/docs/2026-11-18-planner-background-parallel-design.md BP1). 기다리는 동안
        /// <paramref name="waiting"/>(true)를 알리고, 단계는 계산이 끝났는지만 보고 바로 돌아간다.
        /// <paramref name="style"/>은 조정 에셋의 성향 프리셋 값이다(결정 설정 <see cref="EnemyDecisionSettings.PlannerStyle"/>).
        /// </summary>
        internal static IEnumerable<int> Steps(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyTurnPlannerStyle style, EnemyDecisionSideCache sideCache, EnemyDecisionDiagnostics diagnostics,
            Action<EnemyDecision> select, Func<IEnumerable<int>> utility, Action<bool> waiting = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            sideCache?.EnterScope(input);
            string signature = PlannerBoard.Signature(input);

            EnemyDecision first = null;
            bool fromCache = false;
            var stored = sideCache?.TurnPlan;
            if (stored != null && stored.TryNext(signature, out var cached))
            {
                first = cached;
                fromCache = true;
                diagnostics.CacheHits++;
            }
            else
            {
                EnemyTurnPlan made = null;
                bool failed = false;
                foreach (var _ in Background(input, rules, style, diagnostics, value => made = value, () => failed = true, waiting))
                    yield return 0;
                if (failed || made == null || !made.TryNext(signature, out first))
                {
                    diagnostics.CapHits++;
                    if (sideCache != null) sideCache.TurnPlan = null;
                    foreach (var step in utility()) yield return step;
                    yield break;
                }
                if (sideCache != null) sideCache.TurnPlan = made;
            }

            select(first);
            yield return 0;

            // 여기로 다시 오면 거절됐다(TP-6). 보관 계획에서 꺼낸 것이면 지금 판으로 다시 계획해 본다.
            if (sideCache != null) sideCache.TurnPlan = null;
            if (fromCache)
            {
                EnemyTurnPlan made = null;
                bool failed = false;
                foreach (var _ in Background(input, rules, style, diagnostics, value => made = value, () => failed = true, waiting))
                    yield return 0;
                if (!failed && made != null && made.TryNext(signature, out var second) && !second.Equals(first))
                {
                    if (sideCache != null) sideCache.TurnPlan = made;
                    select(second);
                    yield return 0;
                    if (sideCache != null) sideCache.TurnPlan = null;
                }
            }
            // 같은 판에서 다시 계획해도 같은 결정이 나오므로(계산이 결정적이다) 바로 지금 AI로 고른다.
            foreach (var step in utility()) yield return step;
        }

        /// <summary>
        /// 계획 하나를 일 스레드에서 돌리고 끝날 때까지 기다린다(설계 BP1·BP2). 결과·진단은 이 반복기를 도는 쪽(메인 스레드)에서 넣는다.
        /// 일 스레드는 공유 객체(옆 캐시·진단)에 쓰지 않는다. 계산이 예외로 끝나면 <paramref name="failed"/>를 부른다.
        /// 세션이 기다리는 도중 버려지면(반복기 Dispose) finally에서 취소 신호를 보내, 일 스레드가 다음 멈춤 자리에서 멈춘다.
        /// </summary>
        private static IEnumerable<int> Background(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyTurnPlannerStyle style, EnemyDecisionDiagnostics diagnostics, Action<EnemyTurnPlan> completed, Action failed,
            Action<bool> waiting)
        {
            var cancel = new CancellationTokenSource();
            var task = Task.Run(() => Run(input, rules, style, cancel.Token), cancel.Token);
            // 버려진 계산의 예외도 관찰해 둔다(관찰하지 않은 작업 예외 경고를 막는다). 취소 신호는 계산이 끝난 뒤 버린다.
            task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(_ => cancel.Dispose());
            try
            {
                waiting?.Invoke(true);
                while (!task.IsCompleted) yield return 0;
            }
            finally
            {
                waiting?.Invoke(false);
                if (!task.IsCompleted) cancel.Cancel();
            }
            if (task.Status != TaskStatus.RanToCompletion || task.Result == null)
            {
                failed();
                yield break;
            }
            var plan = task.Result;
            if (diagnostics != null)
            {
                diagnostics.ExpandedNodes += plan.Stats.Evaluations;
                diagnostics.PreviewCalls += plan.Stats.Simulations + plan.Stats.ReplySimulations;
            }
            completed(plan);
        }

        /// <summary>
        /// 일 스레드에서 도는 계획 하나. 계획의 멈춤 자리(행동 정산 한 번 단위)마다 취소를 확인한다.
        /// 계산 동안만 스레드 우선순위를 <see cref="EnemyTurnPlannerConfig.WorkerPriority"/>로 바꾸고 끝나면 되돌린다(우선순위 실험).
        /// 스레드 풀 스레드라 되돌리지 않으면 다른 작업이 낮은 우선순위를 물려받는다.
        /// </summary>
        private static EnemyTurnPlan Run(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, EnemyTurnPlannerStyle style,
            CancellationToken cancel)
        {
            var thread = Thread.CurrentThread;
            var previous = thread.Priority;
            bool changed = false;
            if (EnemyTurnPlannerConfig.WorkerPriority != previous)
            {
                // 실행기가 우선순위 바꾸기를 받지 않으면 그대로 계산한다(실험이 계산을 막지 않게).
                try
                {
                    thread.Priority = EnemyTurnPlannerConfig.WorkerPriority;
                    changed = true;
                }
                catch (Exception exception) when (exception is ThreadStateException || exception is NotSupportedException ||
                                                  exception is PlatformNotSupportedException)
                {
                }
            }
            try
            {
                EnemyTurnPlan made = null;
                foreach (var _ in Plan(input, rules, null, value => made = value, style)) cancel.ThrowIfCancellationRequested();
                return made;
            }
            finally
            {
                if (changed) thread.Priority = previous;
            }
        }

        /// <summary>
        /// 턴 계획 하나: 공정 입력(설계 TP-3) → 빔 탐색 → 다시 매김(빠른 V 상위 <see cref="EnemyTurnPlannerStyle.Rerank"/>개를 깊은 V로).
        /// 행동 규칙(<see cref="EnemyTurnPlannerStyle.MustAct"/>)을 켜면 다시 매김 후보를 고를 때와 깊은 V를 견줄 때도
        /// 같은 벌점(<see cref="PlannerBeam.IdleCost"/>)을 뺀다(시뮬레이터 <c>_rerank</c>와 같다). 끄면 벌점이 0이라 값이 그대로다.
        /// 결과는 행동마다 "행동 전 판 서명"을 붙인 계획이다. 시험·측정 도구는 이 함수를 예외 거름 없이 직접 부른다.
        /// style은 성향·계산 크기 값이다(null이면 <see cref="EnemyTurnPlannerStyle.Default"/>). 게임은 조정 에셋의 프리셋 값을 넘긴다.
        /// </summary>
        internal static IEnumerable<int> Plan(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyDecisionDiagnostics diagnostics, Action<EnemyTurnPlan> completed, EnemyTurnPlannerStyle style = null)
        {
            var watch = Stopwatch.StartNew();
            style ??= EnemyTurnPlannerStyle.Default;
            var fair = PlannerBoard.Fair(input);
            var root = new PlannerState(fair, StageOutcome.InProgress);
            var evaluator = new PlannerEvaluator(fair, style);
            var simulator = new PlannerSimulator(rules);
            var beam = new PlannerBeam(root, evaluator, simulator);
            foreach (var _ in beam.Run()) yield return 0;
            var chosen = beam.Best;
            int beamSimulations = simulator.Simulations;
            bool reranked = false;
            int lookaheadReplies = 0;
            if (style.Rerank > 0)
            {
                var top = beam.Pool.OrderByDescending(n => beam.PlanValue(n)).Take(style.Rerank).ToList();
                if (top.Count >= 2)
                {
                    uint seed = PlannerBoard.Seed(input);
                    PlannerBeam.Node pick = null;
                    double pickValue = double.NegativeInfinity;
                    foreach (var node in top)
                    {
                        double deep = 0.0;
                        foreach (var _ in PlannerReply.DeepSteps(node.State, evaluator, simulator, seed,
                            (value, replies) => { deep = value; lookaheadReplies += replies; }))
                            yield return 0;
                        double total = deep - beam.IdleCost(node);
                        if (pick == null || total > pickValue)
                        {
                            pick = node;
                            pickValue = total;
                        }
                    }
                    chosen = pick;
                    reranked = true;
                }
            }
            if (diagnostics != null)
            {
                diagnostics.ExpandedNodes += evaluator.Evaluations;
                diagnostics.PreviewCalls += simulator.Simulations;
            }
            var steps = new List<(EnemyDecision, string)>(chosen.Steps.Count);
            string before = PlannerBoard.Signature(input);
            foreach (var (decision, after) in chosen.Steps)
            {
                steps.Add((decision, before));
                before = after;
            }
            completed(new EnemyTurnPlan(steps, before,
                new EnemyTurnPlanStats(beamSimulations, simulator.Simulations - beamSimulations, evaluator.Evaluations, beam.BeamSteps,
                    simulator.Failures, simulator.LastFailure, reranked, !ReferenceEquals(chosen, beam.Best), watch.Elapsed.TotalMilliseconds,
                    lookaheadReplies, evaluator.DamageMemo)));
        }
    }
}
