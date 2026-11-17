using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Tests.Core;

public static class HarnessProgram
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "tests")
            return new NUnitLite.AutoRun(typeof(HarnessProgram).Assembly).Execute(args.Skip(1).ToArray());
        if (args.Length > 0 && args[0] == "turns") return Turns(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "breakdown") return Breakdown(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "profile") return Profile(args.Length > 1 ? args[1] : "N8");
        if (args.Length > 0 && args[0] == "cancel") return Cancel(args.Length > 1 ? args[1] : "G16");
        if (args.Length > 0 && args[0] == "air") return Air(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 0 && args[0] == "allocparts") return AllocParts(args.Length > 1 ? args[1] : "N8");
        if (args.Length > 0 && args[0] == "alloc") return Alloc(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids);
        if (args.Length > 1 && args[0] == "dump")
            return Dump(args[1], args.Length > 2 ? args[2].Split(',') : EnemyAiBenchmarkFixtures.Ids,
                args.Length > 3 ? args[3].Split(',').Select(int.Parse).ToArray() : new[] { 1, 7 });
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
    /// </summary>
    static int Dump(string path, string[] ids, int[] seeds)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var planner = new EnemyDecisionSettings(decisionModel: EnemyDecisionModel.TurnPlanner);
        var lines = new List<string>();
        foreach (var id in ids)
            foreach (var seed in seeds)
            {
                var input = Consistent(id, seed);
                EnemyTurnPlan plan = null;
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, new EnemyDecisionDiagnostics(), value => plan = value)) { }
                var s = plan.Stats;
                lines.Add($"plan {id} {seed} sims={s.Simulations} reply={s.ReplySimulations} evals={s.Evaluations} beam={s.BeamSteps} " +
                    $"fail={s.Failures} reranked={s.Reranked} flipped={s.Flipped}");
                lines.AddRange(plan.Decisions.Select(d => "  " + Text(d)));

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
            var top = beam.Pool.OrderByDescending(n => n.Value).Take(EnemyTurnPlannerConfig.Rerank).ToList();
            watch.Restart();
            foreach (var node in top)
                foreach (var _ in PlannerReply.DeepSteps(node.State, evaluator, simulator, PlannerBoard.Seed(input), v => { })) { }
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
