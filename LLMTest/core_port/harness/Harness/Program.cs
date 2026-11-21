using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Tests.Core;

public static class HarnessProgram
{
    public static int Main(string[] args)
    {
        // --no-memo: 예상 피해 기억을 끈다(기억 설계 DM-a의 기준 결정 기록 만들기). 어느 모드에나 붙일 수 있다.
        if (args.Contains("--no-memo"))
        {
            PlannerDamageMemo.Disabled = true;
            args = args.Where(a => a != "--no-memo").ToArray();
        }
        if (args.Length > 0 && args[0] == "memocheck")
            return MemoCheck(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids,
                args.Length > 2 ? args[2].Split(',').Select(int.Parse).ToArray() : new[] { 1, 7 });
        if (args.Length > 0 && args[0] == "tests")
            return new NUnitLite.AutoRun(typeof(HarnessProgram).Assembly).Execute(args.Skip(1).ToArray());
        if (args.Length > 0 && args[0] == "turns") return Turns(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "breakdown") return Breakdown(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "profile") return Profile(args.Length > 1 ? args[1] : "N8");
        if (args.Length > 0 && args[0] == "cancel") return Cancel(args.Length > 1 ? args[1] : "G16");
        if (args.Length > 0 && args[0] == "air") return Air(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "allocparts") return AllocParts(args.Length > 1 ? args[1] : "N8");
        if (args.Length > 0 && args[0] == "alloc") return Alloc(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "allocbytype") return AllocByType(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "settlerepeat") return SettleRepeat(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "allocbysite") return AllocBySite(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "bench") return Bench(args.Length > 1 ? args[1].Split(',') : new[] { "N5", "N8", "T8", "G16" },
            args.Length > 2 ? int.Parse(args[2]) : 3);
        if (args.Length > 0 && args[0] == "settle") return Settle(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "style") return Style(args.Length > 1 ? args[1] : "N8");
        if (args.Length > 0 && args[0] == "valuefix")
            return ValueFix(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids,
                args.Length > 2 ? args[2].Split(',').Select(int.Parse).ToArray() : new[] { 1, 7 },
                args.Length > 3 && args[3] == "show");
        if (args.Length > 0 && args[0] == "seq")
            return Sequential(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids, args.Length > 2 && args[2] == "weak");
        if (args.Length > 0 && args[0] == "lookahead")
            return Lookahead(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids,
                args.Length > 2 ? args[2].Split(',').Select(int.Parse).ToArray() : new[] { 1 });
        if (args.Length > 1 && args[0] == "dump")
            return Dump(args[1], args.Length > 2 ? args[2].Split(',') : EnemyAiBenchmarkFixtures.Ids,
                args.Length > 3 ? args[3].Split(',').Select(int.Parse).ToArray() : new[] { 1, 7 },
                args.Length > 4 ? args[4] != "l0" : EnemyTurnPlannerStyle.Default.Lookahead);
        var ids = args.Length > 0 ? args[0].Split(',') : EnemyAiBenchmarkFixtures.Ids;
        var seeds = args.Length > 1 ? args[1].Split(',').Select(int.Parse).ToArray() : new[] { 1 };
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var planner = new EnemyDecisionSettings(decisionModel: EnemyDecisionModel.TurnPlanner);
        int failures = 0;
        Console.WriteLine("fixture seed | utility ms | plan ms  sims  replySims evals beamSteps actions maxStepMs steps fail flip | turn: actions rejects fallbacks cacheHits ms | decisions");
        foreach (var id in ids)
            foreach (var seed in seeds)
            {
                var input = EnemyAiBenchmarkFixtures.Build(id, seed);
                var mainFacts = input.Characters.Single(c => c.OwnershipRole == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main);
                input = input.WithWorld(input.State, input.Characters, input.Main.With(currentHealth: mainFacts.Health.CurrentHealth,
                    persistentMaxHealth: mainFacts.Health.MaxHealth, persistentAttack: mainFacts.Stats.AttackPower),
                    input.SoulCounts, input.Souls, input.EnvironmentBindings);
                try
                {
                    // ① 지금 AI 결정 하나
                    var watch = Stopwatch.StartNew();
                    using (var session = new EnemyDecisionSession(input, rules, EnemyDecisionSettings.Default))
                        while (!session.IsReady) { session.Step(); if (session.IsWaiting) System.Threading.Thread.Sleep(1); }
                    double utilityMs = watch.Elapsed.TotalMilliseconds;

                    // ② 계획 하나(예외 거름 없이)
                    EnemyTurnPlan plan = null;
                    var diagnostics = new EnemyDecisionDiagnostics();
                    watch.Restart();
                    double maxStep = 0; int steps = 0; long last = watch.ElapsedTicks;
                    foreach (var _ in EnemyTurnPlanner.Plan(input, rules, diagnostics, value => plan = value))
                    {
                        steps++;
                        long now = watch.ElapsedTicks;
                        maxStep = Math.Max(maxStep, (now - last) * 1000.0 / Stopwatch.Frequency);
                        last = now;
                    }
                    double planMs = watch.Elapsed.TotalMilliseconds;
                    var stats = plan.Stats;

                    // ③ 적 턴 전체를 게임처럼: 행동마다 새 세션, 같은 옆 캐시
                    var cache = new EnemyDecisionSideCache();
                    var board = input;
                    int actions = 0, rejects = 0, fallbacks = 0, hits = 0;
                    var done = new List<string>();
                    watch.Restart();
                    for (int guard = 0; guard < 64; guard++)
                    {
                        var turnDiag = new EnemyDecisionDiagnostics();
                        using var session = new EnemyDecisionSession(board, rules, planner, turnDiag, cache);
                        CombatCommandTransition transition = null;
                        while (true)
                        {
                            while (!session.IsReady) { session.Step(); if (session.IsWaiting) System.Threading.Thread.Sleep(1); }
                            transition = CombatCommandCalculator.AdvancePrepared(board, session.Decision, rules);
                            if (transition.Accepted || session.Decision.Kind == EnemyDecisionKind.EndEnemyPhase) break;
                            rejects++;
                            session.RejectSelected();
                        }
                        fallbacks += turnDiag.CapHits;
                        hits += turnDiag.CacheHits;
                        var decision = session.Decision;
                        done.Add(decision.Kind == EnemyDecisionKind.EndEnemyPhase ? "End"
                            : decision.Kind + "#" + decision.ActorId.Value);
                        if (!transition.Accepted) { done.Add("REJECTED-END"); break; }
                        if (decision.Kind == EnemyDecisionKind.EndEnemyPhase || transition.Outcome.IsStageEnded) break;
                        actions++;
                        board = CombatCommandComposition.Project(transition);
                    }
                    double turnMs = watch.Elapsed.TotalMilliseconds;
                    if (stats.Failures > 0) Console.WriteLine("   last failure: " + stats.LastFailure);
                    Console.WriteLine($"{id,-4} {seed,3} | {utilityMs,8:F1} | {planMs,8:F1} {stats.Simulations,5} {stats.ReplySimulations,8} {stats.Evaluations,5} {stats.BeamSteps,4} {plan.Count,6} {maxStep,8:F2} {steps,6} {stats.Failures,4} {(stats.Flipped ? "Y" : "-")} | {actions,3} {rejects,3} {fallbacks,3} {hits,3} {turnMs,8:F1} | {string.Join(" ", done)}");
                }
                catch (Exception e)
                {
                    failures++;
                    Console.WriteLine($"{id,-4} {seed,3} | ERROR {e.GetType().Name}: {e.Message}");
                    Console.WriteLine(e.StackTrace?.Split('\n').Take(40).Aggregate((a, b) => a + "\n" + b));
                }
            }
        return failures;
    }

    static int Profile(string id)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var input = EnemyAiBenchmarkFixtures.Build(id, 1);
        var root = new PlannerState(PlannerBoard.Fair(input), ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.StageOutcome.InProgress);
        var evaluator = new PlannerEvaluator(root.Input);
        var simulator = new PlannerSimulator(rules);
        var enemies = PlannerEvaluator.Living(root.Input, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy).ToArray();
        evaluator.Value(root); // 데우기
        foreach (var u0 in enemies) foreach (var p0 in PlannerCandidates.Paths(root.Input, u0).Take(2)) { var b0 = simulator.ApplyEnemy(root, EnemyDecision.Move(u0.EntityId, p0, default)); if (b0 != null) { evaluator.Value(b0); evaluator.QuickMoveScore(root.Input, u0, p0.Endpoint); } }
        var w = Stopwatch.StartNew();
        int n = 0;
        for (int r = 0; r < 20; r++) foreach (var u in enemies) { PlannerCandidates.Aims(root.Input, u, null).ToArray(); n++; }
        Console.WriteLine($"GetAttackRange per unit: {w.Elapsed.TotalMilliseconds / n:F3} ms");
        var paths = enemies.SelectMany(u => PlannerCandidates.Paths(root.Input, u).Take(5).Select(p => (u, p))).ToArray();
        w.Restart();
        foreach (var (u, p) in paths) simulator.ApplyEnemy(root, EnemyDecision.Move(u.EntityId, p, default));
        Console.WriteLine($"ApplyEnemy move: {w.Elapsed.TotalMilliseconds / paths.Length:F3} ms ({paths.Length})");
        var boards = paths.Select(x => simulator.ApplyEnemy(root, EnemyDecision.Move(x.u.EntityId, x.p, default))).Where(b => b != null).ToArray();
        w.Restart();
        foreach (var b in boards) evaluator.Value(b);
        Console.WriteLine($"Value (warm reach): {w.Elapsed.TotalMilliseconds / boards.Length:F3} ms");
        w.Restart();
        foreach (var b in boards) evaluator.Risk(b.Input, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy);
        Console.WriteLine($"  Risk: {w.Elapsed.TotalMilliseconds / boards.Length:F3} ms");
        w.Restart();
        foreach (var b in boards) evaluator.Opportunity(b.Input, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy);
        Console.WriteLine($"  Opportunity: {w.Elapsed.TotalMilliseconds / boards.Length:F3} ms");
        w.Restart();
        foreach (var b in boards) evaluator.GoalTerm(b.Input);
        Console.WriteLine($"  G1: {w.Elapsed.TotalMilliseconds / boards.Length:F3} ms");
        var fresh = new PlannerEvaluator(root.Input);
        w.Restart();
        foreach (var u in enemies) fresh.Reach(u, u.Anchor.Value);
        Console.WriteLine($"Reach (cold) per unit: {w.Elapsed.TotalMilliseconds / enemies.Length:F3} ms");
        w.Restart();
        int q = 0;
        foreach (var (u, p) in paths) { evaluator.QuickMoveScore(root.Input, u, p.Endpoint); q++; }
        Console.WriteLine($"QuickMoveScore: {w.Elapsed.TotalMilliseconds / q:F3} ms");
        return 0;
    }

    /// <summary>지금 AI와 계획 AI로 적 턴 전체를 돌려 행동마다 계산 시간과 세션 단계 수를 잰다(대기 시간 추정용).</summary>
    static int Turns(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        Console.WriteLine("fixture model | units actions | total ms  max-action ms  first-action ms | total steps  first-action steps");
        foreach (var id in ids)
            foreach (var model in new[] { EnemyDecisionModel.Utility, EnemyDecisionModel.TurnPlanner })
            {
                var input = EnemyAiBenchmarkFixtures.Build(id, 1);
                var main = input.Characters.Single(c => c.OwnershipRole == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main);
                input = input.WithWorld(input.State, input.Characters, input.Main.With(currentHealth: main.Health.CurrentHealth,
                    persistentMaxHealth: main.Health.MaxHealth, persistentAttack: main.Stats.AttackPower), input.SoulCounts, input.Souls, input.EnvironmentBindings);
                int units = input.Characters.Count(c => c.Owner == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy && PlannerEvaluator.OnField(c));
                var settings = new EnemyDecisionSettings(decisionModel: model);
                var cache = new EnemyDecisionSideCache();
                var board = input;
                var times = new List<double>(); var steps = new List<int>();
                for (int guard = 0; guard < 80; guard++)
                {
                    var diag = new EnemyDecisionDiagnostics();
                    var watch = Stopwatch.StartNew();
                    int count = 0;
                    using var session = new EnemyDecisionSession(board, rules, settings, diag, cache);
                    CombatCommandTransition transition;
                    while (true)
                    {
                        while (!session.IsReady) { session.Step(); count++; if (session.IsWaiting) System.Threading.Thread.Sleep(1); }
                        transition = CombatCommandCalculator.AdvancePrepared(board, session.Decision, rules);
                        if (transition.Accepted || session.Decision.Kind == EnemyDecisionKind.EndEnemyPhase) break;
                        session.RejectSelected();
                    }
                    times.Add(watch.Elapsed.TotalMilliseconds); steps.Add(count);
                    if (session.Decision.Kind == EnemyDecisionKind.EndEnemyPhase || !transition.Accepted || transition.Outcome.IsStageEnded) break;
                    board = CombatCommandComposition.Project(transition);
                }
                Console.WriteLine($"{id,-4} {model,-11} | {units,3} {times.Count - 1,4} | {times.Sum(),9:F1} {times.Max(),10:F1} {times[0],10:F1} | {steps.Sum(),8} {steps[0],8}");
            }
        return 0;
    }

    /// <summary>
    /// 결정 목록을 파일에 남긴다(별도 스레드·병렬 설계 §7). 판·씨앗마다 (1) 계획 하나(<see cref="EnemyTurnPlanner.Plan"/>)의 수치와 행동,
    /// (2) 게임처럼 행동마다 새 세션으로 돈 적 턴 전체의 행동. 시간은 넣지 않는다(두 실행의 파일을 그대로 비교하려고).
    /// lookahead는 (1)의 깊은 V 종류다. 게임 길(2)은 조정 에셋의 값을 쓰는데 여기서는 기본값(<see cref="EnemyTurnPlannerStyle.Default"/>)이므로, 그것과 다르면 (2)를 적지 않는다.
    /// </summary>
    static int Dump(string path, string[] ids, int[] seeds, bool lookahead)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var planner = new EnemyDecisionSettings(decisionModel: EnemyDecisionModel.TurnPlanner);
        var lines = new List<string>();
        foreach (var id in ids)
            foreach (var seed in seeds)
            {
                var input = Consistent(id, seed);
                EnemyTurnPlan plan = null;
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, new EnemyDecisionDiagnostics(), value => plan = value,
                    EnemyTurnPlannerStyle.Default.WithLookahead(lookahead))) { }
                var s = plan.Stats;
                lines.Add($"plan {id} {seed} sims={s.Simulations} reply={s.ReplySimulations} evals={s.Evaluations} beam={s.BeamSteps} " +
                    $"fail={s.Failures} reranked={s.Reranked} flipped={s.Flipped}" + (lookahead ? $" lookaheadReplies={s.LookaheadReplies}" : ""));
                lines.AddRange(plan.Decisions.Select(d => "  " + Text(d)));
                if (lookahead != EnemyTurnPlannerStyle.Default.Lookahead)
                {
                    Console.WriteLine($"{id} {seed} done (plan only)");
                    continue;
                }

                var cache = new EnemyDecisionSideCache();
                var board = input;
                int rejects = 0, fallbacks = 0, hits = 0;
                var done = new List<string>();
                for (int guard = 0; guard < 64; guard++)
                {
                    var diagnostics = new EnemyDecisionDiagnostics();
                    using var session = new EnemyDecisionSession(board, rules, planner, diagnostics, cache);
                    CombatCommandTransition transition;
                    while (true)
                    {
                        while (!session.IsReady) { session.Step(); if (session.IsWaiting) System.Threading.Thread.Sleep(1); }
                        transition = CombatCommandCalculator.AdvancePrepared(board, session.Decision, rules);
                        if (transition.Accepted || session.Decision.Kind == EnemyDecisionKind.EndEnemyPhase) break;
                        rejects++;
                        session.RejectSelected();
                    }
                    fallbacks += diagnostics.CapHits;
                    hits += diagnostics.CacheHits;
                    done.Add(Text(session.Decision));
                    if (!transition.Accepted) { done.Add("REJECTED-END"); break; }
                    if (session.Decision.Kind == EnemyDecisionKind.EndEnemyPhase || transition.Outcome.IsStageEnded) break;
                    board = CombatCommandComposition.Project(transition);
                }
                lines.Add($"turn {id} {seed} rejects={rejects} fallbacks={fallbacks} cacheHits={hits}");
                lines.AddRange(done.Select(d => "  " + d));
                Console.WriteLine($"{id} {seed} done");
            }
        System.IO.File.WriteAllLines(path, lines);
        Console.WriteLine($"wrote {lines.Count} lines -> {path}");
        return 0;
    }

    /// <summary>
    /// 예상 피해 기억 확인(기억 설계 DM4). 기억한 답이 있어도 늘 다시 계산해 비교한다(다시 계산한 값을 쓰므로 결정은 기억 없을 때와 같다).
    /// 판·씨앗·깊은 V 종류(l0·l1)마다 물은 수·적중·막힘·어긋남을 적는다. 어긋남이 하나라도 있으면 실패(0이 아닌 종료 값)다.
    /// </summary>
    static int MemoCheck(string[] ids, int[] seeds)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        PlannerDamageMemo.Check = true;
        long mismatches = 0, calls = 0, hits = 0, blocked = 0;
        Console.WriteLine("fixture seed look | calls  hits  hit%  blocked mismatches | plan ms");
        foreach (var lookahead in new[] { false, true })
            foreach (var id in ids)
                foreach (var seed in seeds)
                {
                    var input = Consistent(id, seed);
                    EnemyTurnPlan plan = null;
                    var watch = Stopwatch.StartNew();
                    foreach (var _ in EnemyTurnPlanner.Plan(input, rules, new EnemyDecisionDiagnostics(), value => plan = value,
                        EnemyTurnPlannerStyle.Default.WithLookahead(lookahead))) { }
                    var s = plan.Stats;
                    calls += s.DamageCalls; hits += s.DamageHits; blocked += s.DamageBlocked; mismatches += s.DamageMismatches;
                    double rate = s.DamageCalls == 0 ? 0 : 100.0 * s.DamageHits / s.DamageCalls;
                    Console.WriteLine($"{id,-4} {seed,4} {(lookahead ? "l1" : "l0"),4} | {s.DamageCalls,8} {s.DamageHits,8} {rate,5:F1} " +
                        $"{s.DamageBlocked,7} {s.DamageMismatches,10} | {watch.Elapsed.TotalMilliseconds,8:F0}");
                    if (s.DamageMismatches > 0) Console.WriteLine("   last mismatch: " + s.LastDamageMismatch);
                }
        PlannerDamageMemo.Check = false;
        Console.WriteLine($"total calls={calls} hits={hits} ({(calls == 0 ? 0 : 100.0 * hits / calls):F1}%) blocked={blocked} mismatches={mismatches}");
        return mismatches == 0 ? 0 : 1;
    }

    /// <summary>
    /// 계획 도중 세션을 버리면 일 스레드가 멈추나(별도 스레드·병렬 설계 BP2). 계산을 시작해 0.3초 뒤 버리고,
    /// 그 뒤 2초 동안 이 프로세스가 쓴 CPU 시간을 잰다(계산이 계속 돌면 약 2초, 멈췄으면 거의 0).
    /// </summary>
    static int Cancel(string id)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var planner = new EnemyDecisionSettings(decisionModel: EnemyDecisionModel.TurnPlanner);
        var session = new EnemyDecisionSession(Consistent(id, 1), rules, planner, new EnemyDecisionDiagnostics(), new EnemyDecisionSideCache());
        session.Step();
        Console.WriteLine($"after first step: ready={session.IsReady} waiting={session.IsWaiting}");
        System.Threading.Thread.Sleep(300);
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var busy = process.TotalProcessorTime;
        System.Threading.Thread.Sleep(1000);
        process.Refresh();
        Console.WriteLine($"cpu while running (1 s): {(process.TotalProcessorTime - busy).TotalMilliseconds:F0} ms");
        session.Dispose();
        process.Refresh();
        var before = process.TotalProcessorTime;
        Console.WriteLine($"after dispose: waiting={session.IsWaiting}");
        var slices = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            System.Threading.Thread.Sleep(100);
            process.Refresh();
            slices.Add($"{(process.TotalProcessorTime - before).TotalMilliseconds:F0}");
            before = process.TotalProcessorTime;
        }
        Console.WriteLine("cpu ms per 100 ms after dispose: " + string.Join(" ", slices));
        return 0;
    }

    /// <summary>
    /// 계획 AI가 고른 공격 중 아무것도 다치지 않은 공격(헛공격)을 찾고, 그 공격 전후 판의 빠른 V 항목을 비교한다.
    /// 판의 항목: 적 자산, 플레이어 자산, 위험, 기회, 걸음 벌점, G1. 공격자의 방향·행동 단계도 적는다.
    /// </summary>
    static int Air(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var planner = new EnemyDecisionSettings(decisionModel: EnemyDecisionModel.TurnPlanner);
        var Enemy = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy;
        var Player = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Player;
        int attacks = 0, air = 0, splashOnly = 0;
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            var evaluator = new PlannerEvaluator(PlannerBoard.Fair(input));
            var cache = new EnemyDecisionSideCache();
            var board = input;
            for (int guard = 0; guard < 64; guard++)
            {
                using var session = new EnemyDecisionSession(board, rules, planner, new EnemyDecisionDiagnostics(), cache);
                while (!session.IsReady) { session.Step(); if (session.IsWaiting) System.Threading.Thread.Sleep(1); }
                var decision = session.Decision;
                var transition = CombatCommandCalculator.AdvancePrepared(board, decision, rules);
                if (!transition.Accepted || decision.Kind == EnemyDecisionKind.EndEnemyPhase) break;
                var after = CombatCommandComposition.Project(transition);
                if (decision.Kind == EnemyDecisionKind.Attack)
                {
                    attacks++;
                    bool hurt = board.Characters.Any(c => after.Characters.FirstOrDefault(a => a.EntityId == c.EntityId)?.Health.CurrentHealth != c.Health.CurrentHealth)
                        || board.State.Structures.Any(s => after.State.Structures.FirstOrDefault(a => a.EntityId == s.EntityId)?.Health.CurrentHealth != s.Health.CurrentHealth);
                    var aimCell = decision.Aim.Cell;
                    bool emptyAim = aimCell.HasValue &&
                        !board.Characters.Any(c => PlannerEvaluator.OnField(c) && c.OccupiedCells.Contains(aimCell.Value)) &&
                        !board.State.Structures.Any(s => !s.IsDestroyed && s.OccupiedCells.Contains(aimCell.Value));
                    if (emptyAim && hurt)
                    {
                        splashOnly++;
                        var shooter = board.Characters.Single(c => c.EntityId == decision.ActorId);
                        var hits = board.Characters.Where(c => after.Characters.FirstOrDefault(a => a.EntityId == c.EntityId)?.Health.CurrentHealth != c.Health.CurrentHealth)
                            .Select(c => $"{c.CharacterKey}@{c.Anchor}");
                        Console.WriteLine($"{id} EMPTY-AIM {shooter.CharacterKey} at {shooter.Anchor} aim {aimCell} hits {string.Join(" ", hits)}");
                    }
                    if (!hurt)
                    {
                        air++;
                        var actor = board.Characters.Single(c => c.EntityId == decision.ActorId);
                        var actorAfter = after.Characters.Single(c => c.EntityId == decision.ActorId);
                        string Terms(CombatCommandInput b)
                        {
                            var (gain, penalty) = evaluator.Opportunity(b, Enemy);
                            return $"assetsE={evaluator.Assets(b, Enemy):F0} assetsP={evaluator.Assets(b, Player):F0} risk={evaluator.Risk(b, Enemy):F0} " +
                                $"gain={gain:F0} penalty={penalty:F0} g1={evaluator.GoalTerm(b):F0} V={evaluator.Value(new PlannerState(b, transition.Outcome.Outcome)):F0}";
                        }
                        Console.WriteLine($"{id} AIR {actor.CharacterKey} #{actor.EntityId.Value} at {actor.Anchor} aim {Text(decision)} " +
                            $"rot {actor.Rotation}->{actorAfter.Rotation} use {actor.ActionUse}->{actorAfter.ActionUse}");
                        Console.WriteLine("    before: " + Terms(board));
                        Console.WriteLine("    after : " + Terms(after));
                        Console.WriteLine("    events: " + string.Join(", ", transition.Events.Select(e => e.GetType().Name).GroupBy(n => n).Select(g => g.Key + "x" + g.Count())));
                    }
                }
                if (transition.Outcome.IsStageEnded) break;
                board = after;
            }
        }
        Console.WriteLine($"attacks={attacks} air={air} emptyAimButHit={splashOnly}");
        return 0;
    }

    /// <summary>
    /// 깊은 V 두 종류(옛 = l0, 한 턴 더 보기 = l1)를 같은 실행에서 비교한다(한 턴 더 보기 설계 §10 LA-c).
    /// 판마다 데우기 한 번 뒤 l0·l1·l0·l1 차례로 계획하고, 종류마다 짧은 쪽 시간과 할당(이 스레드), 2단 평가의 엔진 수,
    /// 둬 본 플레이어 응수 수, 다시 매김이 빠른 V 1등을 뒤집었나, 두 계획의 행동이 같은지를 적는다.
    /// </summary>
    static int Lookahead(string[] ids, int[] seeds)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        Console.WriteLine("fixture seed | l0 ms   MB   reply flip | l1 ms   MB   reply replies flip | same plan | l1 decisions");
        double sum0 = 0, sum1 = 0, mb0 = 0, mb1 = 0;
        int same = 0, total = 0;
        foreach (var id in ids)
            foreach (var seed in seeds)
            {
                var input = Consistent(id, seed);
                (EnemyTurnPlan plan, double ms, double mb) Run(bool lookahead)
                {
                    EnemyTurnPlan made = null;
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    var watch = Stopwatch.StartNew();
                    foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => made = value,
                        EnemyTurnPlannerStyle.Default.WithLookahead(lookahead))) { }
                    return (made, watch.Elapsed.TotalMilliseconds, (GC.GetAllocatedBytesForCurrentThread() - before) / 1048576.0);
                }
                Run(false);
                var a0 = Run(false);
                var a1 = Run(true);
                var b0 = Run(false);
                var b1 = Run(true);
                var r0 = a0.ms <= b0.ms ? a0 : b0;
                var r1 = a1.ms <= b1.ms ? a1 : b1;
                bool samePlan = r0.plan.Decisions.Select(Text).SequenceEqual(r1.plan.Decisions.Select(Text));
                total++;
                if (samePlan) same++;
                sum0 += r0.ms; sum1 += r1.ms; mb0 += r0.mb; mb1 += r1.mb;
                var s0 = r0.plan.Stats;
                var s1 = r1.plan.Stats;
                Console.WriteLine($"{id,-4} {seed,3} | {r0.ms,6:F0} {r0.mb,6:F0} {s0.ReplySimulations,6} {(s0.Flipped ? "Y" : "-")} | " +
                    $"{r1.ms,6:F0} {r1.mb,6:F0} {s1.ReplySimulations,6} {s1.LookaheadReplies,6} {(s1.Flipped ? "Y" : "-")} | " +
                    $"{(samePlan ? "same" : "DIFF"),4} | {string.Join(" ", r1.plan.Decisions.Select(Text))}");
            }
        Console.WriteLine($"sum ms l0={sum0:F0} l1={sum1:F0} (l1/l0={sum1 / sum0:F2}), MB l0={mb0:F0} l1={mb1:F0} (l1/l0={mb1 / mb0:F2}), same plan {same}/{total}");
        return 0;
    }

    /// <summary>
    /// 성향 값 연결 확인(Core 이식 설계 §11). 기본값이 아닌 값(겁 2, 기회 0, 메인 배수 1, G1 0, 빔 2)으로 (가) 계획을 직접 부르고,
    /// (나) 그 값을 결정 설정에 실어 게임 길(세션)로 첫 결정을 받아, 둘의 첫 행동이 같은지와 기본값 계획과 수치가 다른지를 본다.
    /// </summary>
    static int Style(string id)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var input = Consistent(id, 1);
        var custom = new EnemyTurnPlannerStyle(fear: 2.0, opportunity: 0.0, mainWeight: 1.0, goalStepPercent: 0.0, beam: 2);
        EnemyTurnPlan basePlan = null, direct = null;
        foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => basePlan = value)) { }
        foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => direct = value, custom)) { }
        var settings = new EnemyDecisionSettings(decisionModel: EnemyDecisionModel.TurnPlanner, plannerStyle: custom);
        EnemyDecision viaSession;
        using (var session = new EnemyDecisionSession(input, rules, settings, new EnemyDecisionDiagnostics(), new EnemyDecisionSideCache()))
        {
            while (!session.IsReady) { session.Step(); if (session.IsWaiting) System.Threading.Thread.Sleep(1); }
            viaSession = session.Decision;
        }
        Console.WriteLine($"default plan: sims={basePlan.Stats.Simulations} evals={basePlan.Stats.Evaluations} beam={basePlan.Stats.BeamSteps} first={Text(basePlan.Decisions[0])}");
        Console.WriteLine($"custom  plan: sims={direct.Stats.Simulations} evals={direct.Stats.Evaluations} beam={direct.Stats.BeamSteps} first={Text(direct.Decisions[0])}");
        Console.WriteLine($"session first (custom settings): {Text(viaSession)}");
        Console.WriteLine($"custom differs from default: {direct.Stats.Simulations != basePlan.Stats.Simulations || direct.Stats.Evaluations != basePlan.Stats.Evaluations}");
        Console.WriteLine($"session = direct custom: {Text(viaSession) == Text(direct.Decisions[0])}");
        return 0;
    }

    /// <summary>
    /// 판 값 고치기·행동 규칙 확인(포트 설계 LLMTest/docs/2026-11-19-research-planner-value-fix-port-design.md §4 확인 2·3).
    /// 값 넷: off(끔), i(행동 규칙 0.25만), vb(걸음 벌점 대상 고정 + 기회 모으기), ivb(셋 다 = 기본 프리셋에 넣을 값).
    /// (1) 벤치마크 판·씨앗마다 값마다 계획을 세우고 적 턴 전체를 게임처럼 진행해, 끈 값과 같은지·행동 수·
    ///     이동도 공격도 안 한 적 유닛 수(제자리)·적 턴 뒤 플레이어 메인 HP·거절·대체·후보 예외·
    ///     턴 시작 계획 하나가 엔진으로 돌린 행동 수와 걸린 시간(ms, 한 번 잰 값)을 적는다.
    ///     거절·대체·후보 예외가 하나라도 있거나, 어떤 값이 모든 판에서 끈 값과 같으면(값이 계획기에 닿지 않음) 실패.
    /// (2) 막판 판(<see cref="EndgameBoard"/>)에서 값마다 HP 1인 플레이어 유닛을 잡는지 적는다.
    ///     끈 값이 잡거나 대상 고정을 켠 값이 못 잡으면 실패.
    /// show면 끈 값과 다른 행동 목록을 모두 적는다(눈으로 볼 때).
    /// </summary>
    static int ValueFix(string[] ids, int[] seeds, bool show)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var styles = new (string name, EnemyTurnPlannerStyle style)[]
        {
            ("off", EnemyTurnPlannerStyle.Default),
            ("i", new EnemyTurnPlannerStyle(mustAct: 0.25)),
            ("vb", new EnemyTurnPlannerStyle(stepAnchor: true, pooledChance: true)),
            ("ivb", new EnemyTurnPlannerStyle(mustAct: 0.25, stepAnchor: true, pooledChance: true)),
        };
        int problems = 0, boards = 0;
        var differ = styles.ToDictionary(s => s.name, _ => 0);
        Console.WriteLine("fixture seed value | actions idle mainHP ended | rejects fallbacks fail | plan: sims ms | vs off");
        foreach (var id in ids)
            foreach (var seed in seeds)
            {
                var input = Consistent(id, seed);
                boards++;
                string[] offDone = null;
                foreach (var (name, style) in styles)
                {
                    var (plan, _) = PlanOnce(input, rules, style);
                    var (done, rejects, fallbacks, board, ended) = PlayTurn(input, rules, style);
                    int idle = ended ? 0 : PlannerEvaluator.Living(board, Owner.Enemy).Count(u => u.ActionUse == UnitActionUse.Available);
                    var main = board.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
                    int mainHp = PlannerEvaluator.OnField(main) ? main.Health.CurrentHealth : 0;
                    var text = done.ToArray();
                    offDone ??= text;
                    bool same = text.SequenceEqual(offDone);
                    if (!same) differ[name]++;
                    if (rejects > 0 || fallbacks > 0 || plan.Stats.Failures > 0 || done.Contains("REJECTED-END")) problems++;
                    Console.WriteLine($"{id,-4} {seed,3} {name,-3} | {done.Count - 1,2} {idle,2} {mainHp,3} {(ended ? "ended" : "-")} | {rejects} {fallbacks} {plan.Stats.Failures} | {plan.Stats.Simulations + plan.Stats.ReplySimulations,6} {plan.Stats.ElapsedMilliseconds,7:F0} | {(name == "off" ? "" : same ? "same" : "DIFF")}");
                    if (show && !same) Console.WriteLine("         " + string.Join("  ", text));
                    if (show && name == "off") Console.WriteLine("         " + string.Join("  ", text));
                }
            }
        Console.WriteLine($"boards {boards}; turns that differ from off: " + string.Join(", ", styles.Skip(1).Select(s => $"{s.name} {differ[s.name]}")) +
            $"; problems (rejects/fallbacks/failures) {problems}");
        if (boards >= 11)
            foreach (var (name, _) in styles.Skip(1))
                if (differ[name] == 0) { Console.WriteLine($"{name} changed no turn: it may not reach the planner"); problems++; }

        Console.WriteLine();
        Console.WriteLine("endgame board: player main #1 far away (0,0), player unit #2 HP 1 at (13,13), enemies #11 (13,15) #12 (15,14)");
        var endgame = EndgameBoard();
        var endStyles = styles.Concat(new (string, EnemyTurnPlannerStyle)[]
        {
            ("v", new EnemyTurnPlannerStyle(stepAnchor: true)),
            ("b", new EnemyTurnPlannerStyle(pooledChance: true)),
        });
        foreach (var (name, style) in endStyles)
        {
            var (plan, _) = PlanOnce(endgame, rules, style);
            var (done, rejects, fallbacks, board, _) = PlayTurn(endgame, rules, style);
            var weak = board.Characters.SingleOrDefault(c => c.EntityId.Value == 2);
            bool killed = weak == null || !PlannerEvaluator.OnField(weak);
            Console.WriteLine($"{name,-4} | killed #2: {(killed ? "yes" : "no ")} | rejects {rejects} fallbacks {fallbacks} fail {plan.Stats.Failures} | {string.Join("  ", done)}");
            if (rejects > 0 || fallbacks > 0 || plan.Stats.Failures > 0) problems++;
            if (name == "off" && killed) { Console.WriteLine("  off already kills: this board does not show the fault"); problems++; }
            if (style.StepAnchor && !killed) { Console.WriteLine("  step anchor on still declines the kill"); problems++; }
        }
        return problems;
    }

    static (EnemyTurnPlan plan, EnemyDecisionDiagnostics diagnostics) PlanOnce(CombatCommandInput input,
        IReadOnlyList<IMovementTileRule> rules, EnemyTurnPlannerStyle style)
    {
        EnemyTurnPlan plan = null;
        var diagnostics = new EnemyDecisionDiagnostics();
        foreach (var _ in EnemyTurnPlanner.Plan(input, rules, diagnostics, value => plan = value, style)) { }
        return (plan, diagnostics);
    }

    /// <summary>적 턴 전체를 게임처럼(행동마다 새 세션, 같은 옆 캐시) 진행한다. 돌려주는 판은 마지막으로 받아들여진 행동 뒤의 판이다.</summary>
    static (List<string> done, int rejects, int fallbacks, CombatCommandInput board, bool ended) PlayTurn(CombatCommandInput input,
        IReadOnlyList<IMovementTileRule> rules, EnemyTurnPlannerStyle style)
    {
        var settings = new EnemyDecisionSettings(decisionModel: EnemyDecisionModel.TurnPlanner, plannerStyle: style);
        var cache = new EnemyDecisionSideCache();
        var board = input;
        int rejects = 0, fallbacks = 0;
        bool ended = false;
        var done = new List<string>();
        for (int guard = 0; guard < 64; guard++)
        {
            var diagnostics = new EnemyDecisionDiagnostics();
            using var session = new EnemyDecisionSession(board, rules, settings, diagnostics, cache);
            CombatCommandTransition transition;
            while (true)
            {
                while (!session.IsReady) { session.Step(); if (session.IsWaiting) System.Threading.Thread.Sleep(1); }
                transition = CombatCommandCalculator.AdvancePrepared(board, session.Decision, rules);
                if (transition.Accepted || session.Decision.Kind == EnemyDecisionKind.EndEnemyPhase) break;
                rejects++;
                session.RejectSelected();
            }
            fallbacks += diagnostics.CapHits;
            done.Add(Text(session.Decision));
            if (!transition.Accepted) { done.Add("REJECTED-END"); break; }
            if (session.Decision.Kind == EnemyDecisionKind.EndEnemyPhase) break;
            board = CombatCommandComposition.Project(transition);
            if (transition.Outcome.IsStageEnded) { ended = true; done.Add("End(stage ended)"); break; }
        }
        return (done, rejects, fallbacks, board, ended);
    }

    /// <summary>
    /// 막판 판: 벤치마크 일반 판(EnemyAiBenchmarkFixtures.Build의 N 판)과 같은 값(HP 100, 공격 10, 이동 3, 사거리 4, 16×16, 적 진영 차례)에
    /// 플레이어 메인(멀리 (0,0))과 HP 1인 플레이어 유닛((13,13)), 그 옆의 적 둘만 둔다.
    /// 끈 값은 HP 1 유닛을 잡으면 남은 상대(메인)가 멀어 걸음 벌점이 두 유닛 몫으로 늘어 잡지 않는다(시뮬레이터 knobs 설계 §12.8).
    /// </summary>
    static CombatCommandInput EndgameBoard()
    {
        const int size = 16;
        var template = CombatCommandTestData.Input(empty: true);
        var characters = new[]
        {
            CombatCommandTestData.Character(1, EntityKey.Unit_MainCharacter, new CellCoord(0, 0), Owner.Player, 10, main: true),
            CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Cowboy, new CellCoord(13, 13), Owner.Player, 10), hp: 1),
            CombatCommandTestData.With(CombatCommandTestData.Character(11, EntityKey.Unit_Demon, new CellCoord(13, 15), Owner.Enemy, 10), appointRange: 4),
            CombatCommandTestData.With(CombatCommandTestData.Character(12, EntityKey.Unit_Demon, new CellCoord(15, 14), Owner.Enemy, 10), appointRange: 4),
        };
        var single = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        var profiles = characters.Select(c => c.CharacterKey).Distinct()
            .Select(key => new CombatAttackProfile(CombatCommandTestData.Spec(key, 10, CategoryKey.Main), single)).ToArray();
        var tiles = new List<MovementTileFact>();
        var definitions = new List<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>();
        var environment = new MovementEnvironmentFacts(tiles);
        var state = new CombatCommandState(size, size, CombatFlowPhase.Automatic,
            Array.Empty<StructureCombatFacts>(), new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                controllerTurnSequence: 2, completedPlayerTurnCount: 1, completedControllerTurnCount: 1),
            environment, EnvironmentProgressFacts.Initial(environment, 1u), itemRandomState: 1u);
        state = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks.StageGimmickComposition.Initialize(state, definitions);
        var owned = characters.Where(c => c.OwnedId.HasValue)
            .Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
        var input = new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, EntityId.Empty, default,
            state, characters, profiles, template.Main, owned, owned.ToDictionary(o => o.Id, _ => 0),
            template.Souls, template.Artifacts,
            battle: new CombatBattleFacts(8, 1, 2, 2, Array.Empty<CharacterInstanceId>(), 0, Array.Empty<CellCoord>(), definitions),
            processingKind: CombatProcessingKind.Advance, battleInstanceId: 1,
            environmentBindings: new EnvironmentSemanticBindings(tiles.OfType<StaticTerrainFact>().Select(t => t.Id).ToArray(),
                Array.Empty<long>(), Array.Empty<EntityId>()),
            expectedBattleInstanceId: 1, expectedControllerTurnSequence: 2);
        var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
        return input.WithWorld(input.State, input.Characters, input.Main.With(currentHealth: main.Health.CurrentHealth,
            persistentMaxHealth: main.Health.MaxHealth, persistentAttack: main.Stats.AttackPower), input.SoulCounts, input.Souls, input.EnvironmentBindings);
    }

    /// <summary>
    /// 차례대로 둬 보기 확인(한 턴 더 보기 설계 §10 LA-c). 시작 판에서 바로 턴을 넘긴 위험 판에서 플레이어 유닛마다
    /// (가) 같은 판에서 따로 고른 응수(옛 깊은 V의 꼴)와 (나) 앞 유닛의 응수가 반영된 판에서 고른 응수(한 턴 더 보기의 꼴)를 나란히 적는다.
    /// 응수마다 다친 적 유닛(ID:잃은 HP, 죽으면 †)을 적는다. (가)는 여럿이 같은 적을 노릴 수 있고, (나)는 앞 유닛이 잡은 적을 다시 노리지 않는다.
    /// weak면 첫 유닛이 (가)에서 가장 많이 깎은 적의 HP를 그 피해만큼으로 낮춘 판에서 본다(한 방에 죽게 — 벤치마크 판은 한 방에 죽는 적이 없다).
    /// </summary>
    static int Sequential(string[] ids, bool weak)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var Enemy = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy;
        var Player = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Player;
        var InProgress = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.StageOutcome.InProgress;
        var CommandKind = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatProcessingKind.Command;
        var EndTurn = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatCommandKind.EndPlayerTurn;
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            var fair = PlannerBoard.Fair(input);
            var evaluator = new PlannerEvaluator(fair);
            var simulator = new PlannerSimulator(rules);
            var risk = PlannerReply.TurnToPlayer(new PlannerState(fair, InProgress), simulator, PlannerBoard.Seed(input));
            if (risk == null || risk.IsEnded)
            {
                Console.WriteLine($"{id}: risk board {(risk == null ? "rejected" : "ended " + risk.Outcome)}");
                continue;
            }
            var start = PlannerBoard.Rebuild(risk.Input, CommandKind, EndTurn);
            PlannerState Pick(CombatCommandInput board, CharacterCombatFacts unit)
            {
                PlannerState best = null;
                foreach (var _ in PlannerReply.BestReplySteps(board, unit, Player, evaluator, simulator,
                    evaluator.Assets(board, Player), evaluator.Assets(board, Enemy), (had, chosen) => best = chosen)) { }
                return best;
            }
            if (weak)
            {
                var first = PlannerEvaluator.Living(start, Player).Select(u => Pick(start, u)).FirstOrDefault(b => b != null);
                var victim = first == null ? null : PlannerEvaluator.Living(start, Enemy)
                    .Select(e => (e, lost: e.Health.CurrentHealth - (first.Input.Characters.SingleOrDefault(c => c.EntityId == e.EntityId)?.Health.CurrentHealth ?? 0)))
                    .Where(x => x.lost > 0).OrderByDescending(x => x.lost).Select(x => ((CharacterCombatFacts e, int lost)?)x).FirstOrDefault();
                if (victim.HasValue)
                {
                    var (e, lost) = victim.Value;
                    var health = new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(lost, e.Health.MaxHealth);
                    var characters = start.Characters.Select(c => c.EntityId == e.EntityId ? c.WithHealth(health) : c).ToArray();
                    start = start.WithWorld(start.State, characters, start.Main, start.SoulCounts, start.Souls, start.EnvironmentBindings);
                    Console.WriteLine($"{id}: enemy {e.EntityId.Value} HP {e.Health.CurrentHealth} -> {lost}");
                }
            }
            string Hurt(CombatCommandInput before, CombatCommandInput after)
            {
                var parts = new List<string>();
                foreach (var e in PlannerEvaluator.Living(before, Enemy))
                {
                    var later = after.Characters.SingleOrDefault(c => c.EntityId == e.EntityId);
                    int left = later == null || !PlannerEvaluator.OnField(later) ? 0 : later.Health.CurrentHealth;
                    if (left < e.Health.CurrentHealth) parts.Add($"{e.EntityId.Value}:{e.Health.CurrentHealth - left}{(left == 0 ? "†" : "")}");
                }
                return parts.Count == 0 ? "(none)" : string.Join(",", parts);
            }
            var apart = new List<string>();
            foreach (var unit in PlannerEvaluator.Living(start, Player).ToArray())
            {
                var best = Pick(start, unit);
                apart.Add($"{unit.EntityId.Value}->{(best == null ? "stay" : Hurt(start, best.Input))}");
            }
            var together = new List<string>();
            var board = start;
            foreach (var pid in PlannerEvaluator.Living(start, Player).Select(c => c.EntityId).ToArray())
            {
                var unit = board.Characters.SingleOrDefault(c => c.EntityId == pid);
                if (unit == null || !PlannerEvaluator.OnField(unit)) { together.Add($"{pid.Value}->gone"); continue; }
                var best = Pick(board, unit);
                if (best == null) { together.Add($"{pid.Value}->stay"); continue; }
                together.Add($"{pid.Value}->{Hurt(board, best.Input)}");
                if (best.IsEnded) { together.Add("ended " + best.Outcome); break; }
                board = PlannerBoard.Rebuild(best.Input, CommandKind, EndTurn);
            }
            Console.WriteLine($"{id}: apart    {string.Join("  ", apart)}");
            Console.WriteLine($"{id}: together {string.Join("  ", together)}");
        }
        return 0;
    }

    /// <summary>계획 하나가 새로 만드는 메모리(이 스레드에서 할당한 바이트)와 시간. GC 부담을 보려고 잰다(별도 스레드·병렬 설계 §8).</summary>
    static int Alloc(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        Console.WriteLine("fixture | plan ms | allocated MB | MB/s");
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            long before = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { })) { }
            double ms = watch.Elapsed.TotalMilliseconds;
            double mb = (GC.GetAllocatedBytesForCurrentThread() - before) / 1048576.0;
            Console.WriteLine($"{id,-4} | {ms,8:F0} | {mb,10:F0} | {mb / (ms / 1000),6:F0}");
        }
        return 0;
    }

    /// <summary>계획의 기본 동작 하나가 할당하는 바이트(평균). 계획 하나의 호출 수를 곱해 어디서 많이 만드는지 본다.</summary>
    static int AllocParts(string id)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var Enemy = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy;
        var input = Consistent(id, 1);
        var fair = PlannerBoard.Fair(input);
        var root = new PlannerState(fair, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.StageOutcome.InProgress);
        var evaluator = new PlannerEvaluator(fair);
        var simulator = new PlannerSimulator(rules);
        var enemies = PlannerEvaluator.Living(fair, Enemy).ToArray();
        var paths = enemies.SelectMany(u => PlannerCandidates.Paths(fair, u).Take(5).Select(p => (u, p))).ToArray();
        var boards = paths.Select(x => simulator.ApplyEnemy(root, EnemyDecision.Move(x.u.EntityId, x.p, default))).Where(b => b != null).ToArray();
        foreach (var b in boards) evaluator.Value(b);   // 도달 표 데우기
        void Measure(string name, int count, Action work)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            work();
            Console.WriteLine($"{name,-34} {(GC.GetAllocatedBytesForCurrentThread() - before) / 1024.0 / count,9:F1} KB/call {watch.Elapsed.TotalMilliseconds / count,8:F3} ms/call");
        }
        Measure("ApplyEnemy(move)+Project", paths.Length, () => { foreach (var (u, p) in paths) simulator.ApplyEnemy(root, EnemyDecision.Move(u.EntityId, p, default)); });
        Measure("Value (warm reach)", boards.Length, () => { foreach (var b in boards) evaluator.Value(b); });
        Measure("  Risk", boards.Length, () => { foreach (var b in boards) evaluator.Risk(b.Input, Enemy); });
        Measure("  Opportunity", boards.Length, () => { foreach (var b in boards) evaluator.Opportunity(b.Input, Enemy); });
        Measure("  GoalTerm", boards.Length, () => { foreach (var b in boards) evaluator.GoalTerm(b.Input); });
        Measure("  Assets x2", boards.Length, () => { foreach (var b in boards) { evaluator.Assets(b.Input, Enemy); evaluator.Assets(b.Input, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Player); } });
        Measure("Signature", boards.Length, () => { foreach (var b in boards) PlannerBoard.Signature(b.Input); });
        Measure("Aims (GetAttackRange) per unit", enemies.Length, () => { foreach (var u in enemies) PlannerCandidates.Aims(fair, u, null).ToArray(); });
        Measure("Paths per unit", enemies.Length, () => { foreach (var u in enemies) PlannerCandidates.Paths(fair, u).ToArray(); });
        Measure("TargetsNear per path", paths.Length, () => { foreach (var (u, p) in paths) PlannerCandidates.TargetsNear(fair, u, p.Endpoint); });
        Measure("QuickMoveScore per path", paths.Length, () => { foreach (var (u, p) in paths) evaluator.QuickMoveScore(fair, u, p.Endpoint); });
        var plannerFresh = new PlannerEvaluator(fair);
        Measure("Reach (cold) per unit", enemies.Length, () => { foreach (var u in enemies) plannerFresh.Reach(u, u.Anchor.Value); });
        Measure("Fair", 5, () => { for (int i = 0; i < 5; i++) PlannerBoard.Fair(input); });
        var Player = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Player;
        var board0 = boards[0].Input;
        var players = PlannerEvaluator.Living(board0, Player).ToArray();
        var enemyTargets = PlannerEvaluator.Targets(board0, Player);
        Measure("Living(player)", 100, () => { for (int i = 0; i < 100; i++) PlannerEvaluator.Living(board0, Player).ToArray(); });
        Measure("Targets(player side)", 100, () => { for (int i = 0; i < 100; i++) PlannerEvaluator.Targets(board0, Player); });
        Measure("CanStrike per player unit", players.Length * 20, () => { for (int r = 0; r < 20; r++) foreach (var u in players) PlannerEvaluator.CanStrike(board0, u, false); });
        Measure("Reach (warm) per player unit", players.Length * 20, () => { for (int r = 0; r < 20; r++) foreach (var u in players) evaluator.Reach(u, u.Anchor.Value); });
        Measure("BestStrike per player unit", players.Length * 5, () => { for (int r = 0; r < 5; r++) foreach (var u in players) evaluator.BestStrike(board0, u, enemyTargets, false); });
        var pairs = players.SelectMany(u => enemyTargets.Select(t => (u, t))).Take(40).ToArray();
        Measure("Estimate per pair", pairs.Length, () => { foreach (var (u, t) in pairs) evaluator.Estimate(board0, u, t, 100); });
        Measure("  RequiredProfile per pair", pairs.Length, () => { foreach (var (u, t) in pairs) CombatCommandCalculator.RequiredProfile(board0, u); });
        Measure("  RequiredProfile+CommonAttackDamage", pairs.Length, () => { foreach (var (u, t) in pairs) CombatAttackCalculator.CommonAttackDamage(board0, u, CombatCommandCalculator.RequiredProfile(board0, u), ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatDamageKind.OrdinaryAttack); });
        int inReach = players.Sum(u => enemyTargets.Count(t => t.Cells.Any(c => evaluator.Reach(u, u.Anchor.Value).TryGetValue(c, out var v) && v > 0)));
        Console.WriteLine($"player units {players.Length}, enemy targets {enemyTargets.Count}, (unit,target) pairs in reach {inReach}");
        return 0;
    }

    /// <summary>
    /// 계획 하나가 할당한 메모리를 형식별로 나눈다. 런타임의 할당 표본(약 100KB마다 그때 만든 객체의 형식 하나)을 모은다.
    /// 누가 만들었는지(Core·계획 AI)는 모르고 무엇을 만들었는지만 안다.
    /// </summary>
    static int AllocByType(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var total = new Dictionary<string, long>();
        long allocatedTotal = 0;
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { }, EnemyTurnPlannerStyle.Default.WithLookahead(false))) { } // 데우기(JIT 할당 빼기). l0(옛 깊은 V)으로 잰다
            Dictionary<string, long> bytes;
            long allocated;
            using (var sampler = new AllocationSampler())
            {
                System.Threading.Thread.Sleep(500);
                long before = GC.GetAllocatedBytesForCurrentThread();
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { }, EnemyTurnPlannerStyle.Default.WithLookahead(false))) { }
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                System.Threading.Thread.Sleep(2000);
                bytes = sampler.Snapshot();
            }
            long sampled = bytes.Values.Sum();
            Console.WriteLine($"{id,-4} allocated {allocated / 1048576.0,8:F0} MB, sampled {sampled / 1048576.0,8:F0} MB ({bytes.Values.Count} kinds)");
            foreach (var category in Categorize(bytes).OrderByDescending(p => p.Value))
                Console.WriteLine($"     {category.Key,-28} {100.0 * category.Value / sampled,5:F1}%");
            foreach (var pair in bytes) total[pair.Key] = total.TryGetValue(pair.Key, out var v) ? v + pair.Value : pair.Value;
            allocatedTotal += allocated;
        }
        long all = total.Values.Sum();
        Console.WriteLine($"ALL  allocated {allocatedTotal / 1048576.0,8:F0} MB, sampled {all / 1048576.0,8:F0} MB");
        foreach (var category in Categorize(total).OrderByDescending(p => p.Value))
            Console.WriteLine($"     {category.Key,-28} {100.0 * category.Value / all,5:F1}%");
        Console.WriteLine("top types (all boards):");
        foreach (var pair in total.OrderByDescending(p => p.Value).Take(40))
            Console.WriteLine($"  {100.0 * pair.Value / all,5:F1}%  {Category(pair.Key),-28} {pair.Key}");
        return 0;
    }

    /// <summary>
    /// 계획 하나(l1)의 할당을 "어느 Core 함수의 람다·반복기가 만들었나"로 나눈다(람다 걷기 측정, 2026-11-18).
    /// 할당 표본의 형식 핸들(TypeID)을 Core·계획 AI 어셈블리의 형식과 맞춰, 람다 묶음 객체(<c>&lt;&gt;c__DisplayClass</c>)와
    /// 반복기(<c>&lt;함수&gt;d__N</c>)를 둘러싼 형식·함수 이름으로 바꾼다. 대리자(<c>Func</c>)·LINQ 반복기 자체는 자리를 알 수 없어 형식 이름으로 남긴다.
    /// </summary>
    static int AllocBySite(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var handles = new Dictionary<long, Type>();
        foreach (var t in CoreTypes.Values)
            try { handles[t.TypeHandle.Value.ToInt64()] = t; } catch { }
        var total = new Dictionary<string, long>();
        long all = 0;
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { })) { }
            Dictionary<(string, long), long> bytes;
            using (var sampler = new AllocationSampler(byHandle: true))
            {
                System.Threading.Thread.Sleep(500);
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { })) { }
                System.Threading.Thread.Sleep(2000);
                bytes = sampler.SnapshotByHandle();
            }
            foreach (var pair in bytes)
            {
                string site = Site(pair.Key.Item1, handles.TryGetValue(pair.Key.Item2, out var type) ? type : null);
                total[site] = total.TryGetValue(site, out var v) ? v + pair.Value : pair.Value;
                all += pair.Value;
            }
        }
        Console.WriteLine($"sampled {all / 1048576.0:F0} MB over {string.Join(",", ids)} (l1)");
        foreach (var pair in total.OrderByDescending(p => p.Value).Take(80))
            Console.WriteLine($"  {100.0 * pair.Value / all,5:F1}%  {pair.Key}");
        return 0;
    }

    /// <summary>
    /// 계획 하나(l1)에서 같은 정산이 몇 번 되풀이되나(속도 방향 C, 병렬 설계 §21). 정산마다 두 열쇠로 센다.
    /// 좁은 열쇠 = 판 상태 객체·캐릭터 목록 객체가 같고 행동이 같음(확실히 같은 정산). 넓은 열쇠 = 판 서명(위치·체력·행동 사용)이 같고 행동이 같음
    /// (서명에 없는 상태가 다르면 다른 정산일 수 있어 위쪽 한계).
    /// </summary>
    static int SettleRepeat(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        Console.WriteLine("fixture | settlements | exact repeats | signature repeats");
        long all = 0, allExact = 0, allWide = 0;
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            var exact = new HashSet<(object, object, string)>();
            var wide = new HashSet<string>();
            long total = 0, exactHits = 0, wideHits = 0;
            PlannerSimulator.Observe = (board, decision) =>
            {
                string action = decision != null ? Text(decision)
                    : board.Kind + "#" + board.ActorId.Value + (board.Path != null ? " path " + string.Join(">", board.Path.Cells.Select(c => c.X + "," + c.Y)) : "")
                      + " aim " + board.Aim.Kind + (board.Aim.Cell.HasValue ? "@" + board.Aim.Cell.Value.X + "," + board.Aim.Cell.Value.Y : "") + "/" + board.Aim.Direction;
                total++;
                if (!exact.Add((board.State, board.Characters, action))) exactHits++;
                if (!wide.Add(PlannerBoard.Signature(board) + "||" + action)) wideHits++;
            };
            try { foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { })) { } }
            finally { PlannerSimulator.Observe = null; }
            Console.WriteLine($"{id,-4} | {total,8} | {exactHits,7} ({100.0 * exactHits / Math.Max(1, total),5:F1}%) | {wideHits,7} ({100.0 * wideHits / Math.Max(1, total),5:F1}%)");
            all += total; allExact += exactHits; allWide += wideHits;
        }
        Console.WriteLine($"ALL  | {all,8} | {allExact,7} ({100.0 * allExact / Math.Max(1, all),5:F1}%) | {allWide,7} ({100.0 * allWide / Math.Max(1, all),5:F1}%)");
        return 0;
    }

    /// <summary>람다 묶음·반복기 형식이면 "둘러싼 형식.함수 [종류]", 아니면 형식 이름.</summary>
    static string Site(string name, Type type)
    {
        if (type == null || type.DeclaringType == null) return name;
        var outer = type.DeclaringType;
        while (outer.DeclaringType != null && outer.Name.StartsWith("<")) outer = outer.DeclaringType;
        string owner = outer.FullName.Substring(outer.FullName.LastIndexOf('.') + 1);
        if (type.Name.StartsWith("<>c__DisplayClass"))
        {
            var lambdas = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
                .Select(m => m.Name).Where(n => n.StartsWith("<")).Select(n => n.Substring(1, n.IndexOf('>') - 1)).Distinct().ToArray();
            return $"{owner}.{string.Join("/", lambdas)} [람다 묶음]";
        }
        if (type.Name.StartsWith("<") && type.Name.Contains(">d__"))
            return $"{owner}.{type.Name.Substring(1, type.Name.IndexOf('>') - 1)} [반복기]";
        return $"{owner}+{type.Name}";
    }

    /// <summary>계획 하나(l1)의 시간·할당. 판마다 한 번 데운 뒤 rounds번 잰 평균(람다 걷기 측정, 2026-11-18).</summary>
    static int Bench(string[] ids, int rounds)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        double allMs = 0, allMb = 0;
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { })) { }
            double ms = 0, mb = 0;
            for (int r = 0; r < rounds; r++)
            {
                GC.Collect();
                long before = GC.GetAllocatedBytesForCurrentThread();
                var watch = Stopwatch.StartNew();
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { })) { }
                ms += watch.Elapsed.TotalMilliseconds;
                mb += (GC.GetAllocatedBytesForCurrentThread() - before) / 1048576.0;
            }
            ms /= rounds; mb /= rounds;
            allMs += ms; allMb += mb;
            Console.WriteLine($"{id,-4} | {ms,8:F0} ms | {mb,8:F0} MB");
        }
        Console.WriteLine($"ALL  | {allMs,8:F0} ms | {allMb,8:F0} MB");
        return 0;
    }

    static readonly Dictionary<string, Type> CoreTypes = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.GetName().Name.StartsWith("ProjectL"))
        .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); } })
        .GroupBy(t => t.FullName).ToDictionary(g => g.Key, g => g.First());

    static Dictionary<string, long> Categorize(Dictionary<string, long> bytes)
    {
        var result = new Dictionary<string, long>();
        foreach (var pair in bytes)
        {
            string key = Category(pair.Key);
            result[key] = result.TryGetValue(key, out var v) ? v + pair.Value : pair.Value;
        }
        return result;
    }

    /// <summary>할당 표본 형식 이름 하나를 묶음 이름으로. 배열·컬렉션은 담는 형식을 본다.</summary>
    static string Category(string typeName)
    {
        string name = typeName ?? "";
        bool collection = name.EndsWith("[]") || name.Contains("`");
        string inner = name;
        if (collection)
        {
            int open = name.IndexOf('[');
            inner = open >= 0 && name.Length > open + 1 && name[open + 1] != ']' ? name.Substring(open + 1) : name.Replace("[]", "");
        }
        bool IsEvent(string n) => CoreTypes.Where(p => n.Contains(p.Key)).Any(p => typeof(CombatEvent).IsAssignableFrom(p.Value));
        bool eventish = inner.Contains("CombatHealthExecution") || inner.Contains("CombatHealthClaim") || inner.Contains("CombatDamageOrigin") ||
            inner.Contains("Evidence") || inner.Contains("HealthStep") ||
            inner.Contains(typeof(CombatEvent).FullName) && !inner.Contains(typeof(CombatEvent).FullName + "Kind") || IsEvent(inner);
        // 람다·LINQ·반복기는 담는 형식과 상관없이 따로 센다(무엇을 셌는지가 아니라 어떻게 셌는지의 비용).
        if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^<\w+Steps>d__")) return "Core 단계 반복기(yield)";
        if (name.StartsWith("System.Func`") || name.StartsWith("System.Action`") || name.StartsWith("System.Predicate`") ||
            name.Contains("Iterator") || name.Contains("Enumerator") || name.Contains("DisplayClass") || name.StartsWith("<") ||
            name.Contains("+<") || name.Contains("Linq") || name.Contains("Grouping") || name.Contains("Lookup"))
            return eventish ? "사건 관련 (LINQ·람다)" : "LINQ·람다·반복기";
        if (eventish) return "사건·증거 (객체·목록)";
        if (inner.Contains("CharacterCombatFacts") || inner.Contains("CharacterStateChange") || inner.Contains("CharacterHealth") ||
            inner.Contains("CharacterStats") || inner.Contains("CharacterTraitEffects"))
            return "캐릭터 사실 (객체·배열·사전)";
        if (inner.Contains(".EnemyPlanner.")) return "계획 AI 형식";
        if (inner.Contains("ProjectL.")) return collection ? "Core 형식 (배열·집합·사전)" : "Core 형식 (객체)";
        if (collection) return "시스템 배열·컬렉션";
        return "시스템 기타";
    }

    /// <summary>
    /// 정산 한 번에 사건이 몇 개 나오는지, 정산·결과 검증·다음 판 만들기에 시간·할당이 얼마나 드는지 잰다.
    /// 판마다 적 유닛의 제자리 공격(유닛당 3개)·이동(유닛당 5개)을 돌린다. 결과 검증은 같은 값으로 결과 묶음을 다시 만들어 잰다.
    /// </summary>
    static int Settle(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var Enemy = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy;
        const int Repeat = 20;
        var kinds = new Dictionary<CombatEventKind, long>();
        Console.WriteLine("fixture kind   n | events avg max | settle ms KB | rebuild ms KB | project ms KB");
        foreach (var id in ids)
        {
            var fair = PlannerBoard.Fair(Consistent(id, 1));
            var decisions = new List<(string kind, EnemyDecision decision)>();
            foreach (var unit in PlannerEvaluator.Living(fair, Enemy).ToArray())
            {
                foreach (var (aim, source) in PlannerCandidates.Aims(fair, unit, null).Take(3))
                    decisions.Add(("attack", EnemyDecision.Attack(unit.EntityId, aim, source)));
                foreach (var path in PlannerCandidates.Paths(fair, unit).Take(5))
                    decisions.Add(("move", EnemyDecision.Move(unit.EntityId, path, default)));
            }
            foreach (var group in decisions.GroupBy(d => d.kind))
            {
                var accepted = new List<(EnemyDecision decision, CombatCommandTransition transition)>();
                foreach (var (_, decision) in group)
                {
                    var t = CombatCommandCalculator.AdvancePrepared(fair, decision, rules); // 데우기 겸 수락 거름
                    if (t.Accepted) accepted.Add((decision, t));
                }
                if (accepted.Count == 0) continue;
                foreach (var (_, t) in accepted) foreach (var e in t.Events) kinds[e.Kind] = kinds.TryGetValue(e.Kind, out var c) ? c + 1 : 1;
                (double ms, double kb) Measure(Action<(EnemyDecision decision, CombatCommandTransition transition)> work)
                {
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    var watch = Stopwatch.StartNew();
                    for (int r = 0; r < Repeat; r++) foreach (var item in accepted) work(item);
                    int n = Repeat * accepted.Count;
                    return (watch.Elapsed.TotalMilliseconds / n, (GC.GetAllocatedBytesForCurrentThread() - before) / 1024.0 / n);
                }
                var settle = Measure(x => CombatCommandCalculator.AdvancePrepared(fair, x.decision, rules));
                var rebuild = Measure(x => CombatCommandComposition.WithEvents(x.transition, x.transition.Events));
                var project = Measure(x => CombatCommandComposition.Project(x.transition));
                double avg = accepted.Average(x => x.transition.Events.Count);
                int max = accepted.Max(x => x.transition.Events.Count);
                Console.WriteLine($"{id,-4} {group.Key,-6} {accepted.Count,3} | {avg,6:F1} {max,3} | {settle.ms,6:F3} {settle.kb,6:F0} | {rebuild.ms,6:F3} {rebuild.kb,5:F1} | {project.ms,6:F3} {project.kb,5:F1}");
            }
        }
        Console.WriteLine("event kinds (all):");
        long sum = kinds.Values.Sum();
        foreach (var pair in kinds.OrderByDescending(p => p.Value))
            Console.WriteLine($"  {pair.Key,-22} {pair.Value,6} ({100.0 * pair.Value / sum:F1}%)");
        return 0;
    }

    static string Text(EnemyDecision d)
    {
        if (d.Kind == EnemyDecisionKind.EndEnemyPhase) return "End";
        string text = d.Kind + "#" + d.ActorId.Value;
        if (d.Path != null) text += " path " + string.Join(">", d.Path.Cells.Select(c => c.X + "," + c.Y));
        if (d.Kind == EnemyDecisionKind.Attack)
            text += " aim " + d.Aim.Kind + (d.Aim.Cell.HasValue ? "@" + d.Aim.Cell.Value.X + "," + d.Aim.Cell.Value.Y : "") + "/" + d.Aim.Direction;
        if (!d.TauntSourceId.IsEmpty) text += " taunt " + d.TauntSourceId.Value;
        return text;
    }

    static CombatCommandInput Consistent(string id, int seed)
    {
        var input = EnemyAiBenchmarkFixtures.Build(id, seed);
        var main = input.Characters.Single(c => c.OwnershipRole == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main);
        return input.WithWorld(input.State, input.Characters, input.Main.With(currentHealth: main.Health.CurrentHealth,
            persistentMaxHealth: main.Health.MaxHealth, persistentAttack: main.Stats.AttackPower), input.SoulCounts, input.Souls, input.EnvironmentBindings);
    }

    /// <summary>
    /// 계산이 어디에 쓰이는지: (1) 빔과 2단 평가의 시간·엔진 수, (2) 시작 판에서 적 유닛 후보의 종류별 수,
    /// (3) 위험 판에서 플레이어 유닛 응수의 종류별 수(제자리 공격 / 이동을 돌린 수 / 이동 뒤 공격).
    /// </summary>
    static int Breakdown(string[] ids)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var Enemy = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy;
        var Player = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Player;
        Console.WriteLine("fixture | beam ms sims evals | deep ms sims | root enemy: inPlace nearDest(moveSims) moveAttack moveOnly | risk-board player: inPlace nearDest moveAttack");
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            var fair = PlannerBoard.Fair(input);
            var root = new PlannerState(fair, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.StageOutcome.InProgress);
            var evaluator = new PlannerEvaluator(fair);
            var simulator = new PlannerSimulator(rules);
            var beam = new PlannerBeam(root, evaluator, simulator);
            var watch = Stopwatch.StartNew();
            foreach (var _ in beam.Run()) { }
            double beamMs = watch.Elapsed.TotalMilliseconds;
            int beamSims = simulator.Simulations, beamEvals = evaluator.Evaluations;
            var top = beam.Pool.OrderByDescending(n => n.Value).Take(EnemyTurnPlannerStyle.Default.Rerank).ToList();
            watch.Restart();
            foreach (var node in top)
                foreach (var _ in PlannerReply.DeepSteps(node.State, evaluator, simulator, PlannerBoard.Seed(input), (v, r) => { })) { }
            double deepMs = watch.Elapsed.TotalMilliseconds;
            int deepSims = simulator.Simulations - beamSims;

            // 시작 판의 적 후보 종류
            int inPlace = 0, near = 0, moveAttack = 0, moveOnly = 0;
            var counter = new PlannerSimulator(rules);
            foreach (var unit in PlannerEvaluator.Living(fair, Enemy).ToArray())
            {
                var options = new List<PlannerOption>();
                int before = counter.Simulations;
                foreach (var _ in PlannerCandidates.EnemySteps(root, unit, counter, options)) { }
                near += counter.Simulations - before;
                inPlace += options.Count(o => o.HasAttack && o.Path == null);
                moveAttack += options.Count(o => o.HasAttack && o.Path != null);
                moveOnly += options.Count(o => !o.HasAttack);
            }

            // 위험 판(시작 판에서 바로 턴을 넘긴 판)의 플레이어 응수 종류
            int pInPlace = 0, pNear = 0, pMoveAttack = 0;
            var risk = PlannerReply.TurnToPlayer(root, new PlannerSimulator(rules), PlannerBoard.Seed(input));
            if (risk != null && !risk.IsEnded)
            {
                var command = PlannerBoard.Rebuild(risk.Input, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatProcessingKind.Command,
                    ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatCommandKind.EndPlayerTurn);
                var probe = new PlannerSimulator(rules);
                foreach (var unit in PlannerEvaluator.Living(command, Player).ToArray())
                {
                    pInPlace += PlannerCandidates.Aims(command, unit, null).Count();
                    foreach (var path in PlannerCandidates.Paths(command, unit))
                    {
                        if (!PlannerCandidates.TargetsNear(command, unit, path.Endpoint)) continue;
                        pNear++;
                        var moved = probe.ApplyCommand(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.CombatCommandComposition.Intent(
                            command.WithActor(unit.EntityId), ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatCommandKind.Move,
                            cell: path.Endpoint, path: path));
                        if (moved == null || moved.IsEnded) continue;
                        var mover = moved.Input.Characters.SingleOrDefault(c => c.EntityId == unit.EntityId);
                        if (mover != null && PlannerEvaluator.OnField(mover)) pMoveAttack += PlannerCandidates.Aims(moved.Input, mover, null).Count();
                    }
                }
            }
            Console.WriteLine($"{id,-4} | {beamMs,7:F0} {beamSims,5} {beamEvals,5} | {deepMs,6:F0} {deepSims,5} | {inPlace,4} {near,5} {moveAttack,5} {moveOnly,5} | {pInPlace,4} {pNear,5} {pMoveAttack,5}");
        }
        return 0;
    }
}

/// <summary>런타임 할당 표본(GCAllocationTick)을 형식 이름별 바이트로 모은다. 측정 동안만 켠다.</summary>
sealed class AllocationSampler : System.Diagnostics.Tracing.EventListener
{
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _bytes = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<(string, long), long> _byHandle = new();
    readonly bool _keepHandle;
    bool _on;

    public AllocationSampler(bool byHandle = false) { _on = true; _keepHandle = byHandle; }

    protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource source)
    {
        if (source.Name == "Microsoft-Windows-DotNETRuntime")
            EnableEvents(source, System.Diagnostics.Tracing.EventLevel.Verbose, (System.Diagnostics.Tracing.EventKeywords)0x1);
    }

    protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs e)
    {
        if (!_on || e.EventName == null || !e.EventName.StartsWith("GCAllocationTick")) return;
        int nameAt = e.PayloadNames.IndexOf("TypeName");
        int amountAt = e.PayloadNames.IndexOf("AllocationAmount64");
        if (amountAt < 0) amountAt = e.PayloadNames.IndexOf("AllocationAmount");
        if (nameAt < 0 || amountAt < 0) return;
        string name = e.Payload[nameAt] as string ?? "?";
        long amount = Convert.ToInt64(e.Payload[amountAt]);
        _bytes.AddOrUpdate(name, amount, (_, v) => v + amount);
        if (_keepHandle)
        {
            int handleAt = e.PayloadNames.IndexOf("TypeID");
            long handle = handleAt < 0 ? 0 : e.Payload[handleAt] switch { IntPtr h => h.ToInt64(), UIntPtr h => (long)h.ToUInt64(), ulong h => (long)h, var h => Convert.ToInt64(h) };
            _byHandle.AddOrUpdate((name, handle), amount, (_, v) => v + amount);
        }
    }

    public Dictionary<(string, long), long> SnapshotByHandle()
    {
        _on = false;
        return new Dictionary<(string, long), long>(_byHandle);
    }

    public Dictionary<string, long> Snapshot()
    {
        _on = false;
        return new Dictionary<string, long>(_bytes);
    }
}
