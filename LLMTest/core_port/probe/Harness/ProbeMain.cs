using System;
using System.Diagnostics;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Tests.Core;

/// <summary>측정 전용: 계획 하나의 할당·시간을 "계획 AI가 부른 Core 함수 안"과 "우리 코드"로 나눈다.</summary>
public static class ProbeMain
{
    static readonly string[] Names =
    {
        "정산", "다음 판 만들기", "공격 범위·선택 조회", "도달 표의 공격 칸·이동 칸", "이동 범위 탐색",
        "예상 피해", "공격 프로필", "입력 복사(행위자·의도·판)", "환경 시점 바꾸기", "작은 규칙 판정",
    };

    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // 예상 피해 기억(DM-a)이 켜져 있으면 damage 모드는 기억에 걸리지 않은 질문만 본다. 옛 되풀이 측정(§17·DM-0)과 같게 재려면 끈다.
        // --no-memo: 시간·할당을 기억 없이 잴 때도(DM-b 전후 비교) 쓴다.
        if (args.Contains("--no-memo") || args.Length > 0 && args[0] == "damage")
        {
            PlannerDamageMemo.Disabled = true;
            args = args.Where(a => a != "--no-memo").ToArray();
        }
        if (args.Length > 0 && args[0] == "damage")
            return Damage(args.Length > 1 ? args[1].Split(',') : EnemyAiBenchmarkFixtures.Ids,
                args.Length > 2 ? args[2].Split(',') : new[] { "l0", "l1" });
        var ids = args.Length > 0 ? args[0].Split(',') : EnemyAiBenchmarkFixtures.Ids;
        var modes = args.Length > 1 ? args[1].Split(',') : new[] { "l0", "l1" };
        var rules = EnemyAiBenchmarkFixtures.Rules;
        var sumBytes = new double[modes.Length, AllocProbe.Slots + 1];
        var sumTicks = new double[modes.Length, AllocProbe.Slots + 1];
        var totalBytes = new double[modes.Length];
        var totalTicks = new double[modes.Length];
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            for (int m = 0; m < modes.Length; m++)
            {
                bool lookahead = modes[m] == "l1";
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { }, EnemyTurnPlannerStyle.Default.WithLookahead(lookahead))) { } // 데우기
                AllocProbe.Reset();
                long before = GC.GetAllocatedBytesForCurrentThread();
                long start = Stopwatch.GetTimestamp();
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => { }, EnemyTurnPlannerStyle.Default.WithLookahead(lookahead))) { }
                long ticks = Stopwatch.GetTimestamp() - start;
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                long coreBytes = AllocProbe.Bytes.Sum(), coreTicks = AllocProbe.Ticks.Sum();
                Console.WriteLine($"{id,-4} {modes[m]} | plan {Ms(ticks),7:F0} ms {bytes / 1048576.0,7:F0} MB | " +
                    $"Core 안 {100.0 * coreBytes / bytes,5:F1}% 할당 {100.0 * coreTicks / ticks,5:F1}% 시간 | " +
                    $"우리 코드 {100.0 * (bytes - coreBytes) / bytes,5:F1}% 할당 {100.0 * (ticks - coreTicks) / ticks,5:F1}% 시간");
                for (int s = 0; s < AllocProbe.Slots; s++)
                {
                    sumBytes[m, s] += AllocProbe.Bytes[s];
                    sumTicks[m, s] += AllocProbe.Ticks[s];
                }
                sumBytes[m, AllocProbe.Slots] += bytes - coreBytes;
                sumTicks[m, AllocProbe.Slots] += ticks - coreTicks;
                totalBytes[m] += bytes;
                totalTicks[m] += ticks;
                for (int s = 0; s < AllocProbe.Slots; s++)
                    Console.WriteLine($"       {Names[s],-22} {100.0 * AllocProbe.Bytes[s] / bytes,5:F1}% 할당 {100.0 * AllocProbe.Ticks[s] / ticks,5:F1}% 시간  {AllocProbe.Calls[s],8}번");
            }
        }
        for (int m = 0; m < modes.Length; m++)
        {
            Console.WriteLine($"ALL {modes[m]} | {Ms((long)totalTicks[m]),8:F0} ms {totalBytes[m] / 1048576.0,8:F0} MB");
            for (int s = 0; s <= AllocProbe.Slots; s++)
            {
                string name = s < AllocProbe.Slots ? Names[s] : "우리 코드(나머지)";
                Console.WriteLine($"       {name,-22} {100.0 * sumBytes[m, s] / totalBytes[m],5:F1}% 할당 {100.0 * sumTicks[m, s] / totalTicks[m],5:F1}% 시간");
            }
        }
        return 0;
    }

    /// <summary>
    /// 예상 피해 질문의 되풀이 비율과 같은 질문의 답이 늘 같은지(어긋남). instrument.py --damage-log로 만든 사본에서만 센다.
    /// 판마다 계획 결과의 지문(평가 수·결정 해시)도 적는다. --move-memo 사본과 지문이 같으면 DM3가 결정을 바꾸지 않은 것이다.
    /// </summary>
    static int Damage(string[] ids, string[] modes)
    {
        var rules = EnemyAiBenchmarkFixtures.Rules;
        int levels = DamageLog.Levels.Length;
        var calls = new long[modes.Length];
        var repeats = new long[modes.Length, levels];
        var mismatches = new long[modes.Length, levels];
        var distinct = new long[modes.Length, levels];
        var blockedRule = new long[modes.Length];
        var blockedKey = new long[modes.Length];
        var boardPart = new long[modes.Length];
        var moveCalls = new long[modes.Length];
        var moveRepeats = new long[modes.Length];
        var diffCases = new long[modes.Length];
        var diffs = new System.Collections.Generic.Dictionary<string, long>[modes.Length];
        for (int m = 0; m < modes.Length; m++) diffs[m] = new System.Collections.Generic.Dictionary<string, long>();
        foreach (var id in ids)
        {
            var input = Consistent(id, 1);
            for (int m = 0; m < modes.Length; m++)
            {
                var style = EnemyTurnPlannerStyle.Default.WithLookahead(modes[m] == "l1");
                DamageLog.Reset();
                EnemyTurnPlan plan = null;
                foreach (var _ in EnemyTurnPlanner.Plan(input, rules, null, value => plan = value, style)) { }
                string print = $"지문 evals={plan.Stats.Evaluations} sims={plan.Stats.Simulations} 결정={Fnv(string.Join(";", plan.Decisions.Select(Text))):x8}";
                if (DamageLog.Calls == 0) { Console.WriteLine($"{id,-4} {modes[m]} | 예상 피해 0번(공격할 수 있는 판이 없음, 또는 --damage-log 사본이 아님) | {print}"); continue; }
                Console.Write($"{id,-4} {modes[m]} | 예상 피해 {DamageLog.Calls,7}번 | 되풀이(어긋남)");
                for (int l = 0; l < levels; l++)
                {
                    var level = DamageLog.Levels[l];
                    Console.Write($" | L{l + 1} {100.0 * level.Repeats / DamageLog.Calls,5:F1}% ({level.Mismatches})");
                    repeats[m, l] += level.Repeats;
                    mismatches[m, l] += level.Mismatches;
                    distinct[m, l] += level.First.Count;
                }
                Console.WriteLine();
                Console.WriteLine($"          막힘 규칙 {DamageLog.BlockedRule} 키 {DamageLog.BlockedKey} | 판 몫 있음 {DamageLog.WithBoardPart} | " +
                    $"L6 서로 다른 열쇠 {DamageLog.Levels[5].First.Count} L8 {DamageLog.Levels[7].First.Count} | 이동 사본 {DamageLog.MoveCalls}번 중 같은 객체 {DamageLog.MoveRepeats} | " +
                    $"대표 찾기 {DamageLog.CanonLookups}번 비교 {DamageLog.CanonCompares}번 | {print}");
                calls[m] += DamageLog.Calls;
                blockedRule[m] += DamageLog.BlockedRule;
                blockedKey[m] += DamageLog.BlockedKey;
                boardPart[m] += DamageLog.WithBoardPart;
                moveCalls[m] += DamageLog.MoveCalls;
                moveRepeats[m] += DamageLog.MoveRepeats;
                diffCases[m] += DamageLog.DiffCases;
                foreach (var pair in DamageLog.Diffs) diffs[m][pair.Key] = (diffs[m].TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            }
        }
        for (int l = 0; l < levels; l++) Console.WriteLine($"L{l + 1}: {DamageLog.Levels[l].Name}");
        for (int m = 0; m < modes.Length; m++)
        {
            Console.Write($"ALL  {modes[m]} | 예상 피해 {calls[m],7}번 | 되풀이(어긋남)");
            for (int l = 0; l < levels; l++)
                Console.Write($" | L{l + 1} {100.0 * repeats[m, l] / Math.Max(1, calls[m]),5:F1}% ({mismatches[m, l]})");
            Console.WriteLine();
            Console.WriteLine($"          막힘 규칙 {blockedRule[m]} 키 {blockedKey[m]} | 판 몫 있음 {boardPart[m]} | " +
                $"L6 서로 다른 열쇠 {distinct[m, 5]} | 이동 사본 {moveCalls[m]}번 중 같은 객체 {moveRepeats[m]}");
            Console.WriteLine($"          L5로는 되풀이인데 L7로는 처음인 질문 {diffCases[m]}번 — 처음 물은 객체와 다른 속성(번):");
            foreach (var pair in diffs[m].OrderByDescending(pair => pair.Value))
                Console.WriteLine($"            {pair.Key,-32} {pair.Value,8} ({100.0 * pair.Value / Math.Max(1, diffCases[m]),5:F1}%)");
        }
        return 0;
    }

    static uint Fnv(string text)
    {
        uint hash = 2166136261;
        foreach (char ch in text) { hash ^= ch; hash *= 16777619; }
        return hash;
    }

    /// <summary>harness Program.cs의 Text와 같은 결정 글.</summary>
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

    static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    static CombatCommandInput Consistent(string id, int seed)
    {
        var input = EnemyAiBenchmarkFixtures.Build(id, seed);
        var main = input.Characters.Single(c => c.OwnershipRole == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main);
        return input.WithWorld(input.State, input.Characters, input.Main.With(currentHealth: main.Health.CurrentHealth,
            persistentMaxHealth: main.Health.MaxHealth, persistentAttack: main.Stats.AttackPower), input.SoulCounts, input.Souls, input.EnvironmentBindings);
    }
}
