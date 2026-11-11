using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine.Scripting;
using UnityEngine.TestTools;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class EnemyAdvancePreparationMeasurements
    {
        [UnityTest, Explicit("Requires an isolated prepared-service capture.")]
        public IEnumerator CaptureYieldedOpenEncounters()
        {
            string capture = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE");
            bool acceptance = capture == "phase-7-service";
            if (capture != "prepared-service" && !acceptance)
                Assert.Ignore("Enable the prepared Core-service diagnostic explicitly.");
            var fixtures = acceptance ? new[] { "N5", "N6", "N8", "G16" } : new[] { "N5", "N6", "N8" };
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            using var frames = File.CreateText(Path.Combine(folder, "prepared-frames.csv"));
            using var sides = File.CreateText(Path.Combine(folder, "prepared-sides.csv"));
            using var decisions = File.CreateText(Path.Combine(folder, "prepared-decisions.csv"));
            frames.WriteLine("fixture,seed,repeat,frame,decision,operation,totalTicks,preparationTicks,executionTicks,largestAtomTicks,workBefore,workAfter,captures,gc0,gc1,gc2,bytes,disposition,previewCalls,generatedAttacks,generatedMoves,capHits,gcAffected");
            sides.WriteLine("fixture,seed,repeat,decisions,frames,preparationTicks,executionTicks,wallTicks,bytes");
            decisions.WriteLine("fixture,seed,repeat,decision,frames,preparationTicks,executionTicks,latencyTicks,actor,kind");
            long calibrationStart = GC.GetAllocatedBytesForCurrentThread();
            var calibration = new byte[4096];
            long calibrationBytes = GC.GetAllocatedBytesForCurrentThread() - calibrationStart;
            GC.KeepAlive(calibration);
            bool hasBytes = calibrationBytes >= 4096;
            // 증분 GC 조각 길이(틱). 꺼져 있으면 0이고, 그때는 완료된 수집만 GC로 센다.
            long gcSliceTicks = GarbageCollector.isIncremental
                ? (long)GarbageCollector.incrementalTimeSliceNanoseconds * Stopwatch.Frequency / 1_000_000_000 : 0;
            File.WriteAllText(Path.Combine(folder, "prepared-runtime.txt"),
                $"frequency={Stopwatch.Frequency}\nunity={UnityEngine.Application.unityVersion}\n" +
                $"cpu={UnityEngine.SystemInfo.processorType}\nos={UnityEngine.SystemInfo.operatingSystem}\n" +
                $"allocationCalibration={calibrationBytes}\nmode=EditMode EditorApplication.update yields; not gameplay rendering\n" +
                $"frameBudgetMs=1\nfixtures={string.Join(",", fixtures)}\nseeds=1,7,19,41,97\nwarmupSidesPerSeed=1\nmeasuredSidesPerSeed=4\n" +
                $"incrementalGc={GarbageCollector.isIncremental}\ngcSliceNs={GarbageCollector.incrementalTimeSliceNanoseconds}\n" +
                "gcAffected=collection count changed, or a preparation atom >= 90% of the GC slice, or a preparation update >= budget + 90% of the slice, or an execution >= 90% of the slice\n");
            var updates = new List<UpdateRow>(4096);
            var measuredDecisions = new Dictionary<string, int>();
            var measuredCollections = new Dictionary<string, long>();

            foreach (string fixture in fixtures)
            foreach (int seed in new[] { 1, 7, 19, 41, 97 })
            for (int repeat = -1; repeat < 4; repeat++)
            {
                using var battle = EnemyAiPreparationTestData.Create(fixture, seed);
                string band = Band(EnemyAiPreparationTestData.EnemyCount(fixture));
                var b = battle.Buffers;
                var publication = battle.Publication();
                var builder = new SystemBuilderHelper();
                builder.AddService<IBufferQuery>(b);
                int frame = 0;
                var movementRules = CombatMovementRules.Validated(new IMovementTileRule[] { new ThornMovementTileRule(),
                    new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule() });
                var service = new CombatAdvanceService(publication.Assembler, publication.Publisher,
                    movementRules, () => frame, Stopwatch.GetTimestamp, Stopwatch.Frequency, usePreparation: true);
                service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
                var rows = new List<string>(256);
                var decisionRows = new List<string>(32);
                var results = new List<CombatResult>(32);
                var probe = new ExecutionTimer();
                int decision = 1;
                int decisionFrames = 0;
                long preparationTicks = 0, executionTicks = 0, byteTotal = 0;
                long decisionPreparation = 0, decisionExecution = 0;
                long sideStart = Stopwatch.GetTimestamp(), decisionStart = sideStart;
                CoreProfiling.Install(probe);
                try
                {
                    while (b.CombatStateReadSink.Phase == CombatFlowPhase.Automatic &&
                        b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase == CombatControllerPhase.Enemy)
                    {
                        Assert.That(frame, Is.LessThan(20000), fixture);
                        Assert.That(decision, Is.LessThan(128), fixture);
                        int sequence = b.CombatStateReadSink.LatestResultSequence;
                        long version = service.PreparationWorkVersion, captures = service.InputCaptureCount;
                        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
                        long bytesBefore = hasBytes ? GC.GetAllocatedBytesForCurrentThread() : 0;
                        probe.Ticks = 0;
                        long start = Stopwatch.GetTimestamp();
                        service.Tick(0);
                        var state = service.ReadPreparationState();
                        string operation;
                        string disposition = "";
                        var diagnostics = service.PreparationDiagnostics;
                        if (state == AdvancePreparationState.Required || state == AdvancePreparationState.Preparing)
                        {
                            operation = "prepare";
                            service.PrepareAdvanceSlice();
                        }
                        else if (state == AdvancePreparationState.Ready)
                        {
                            operation = "execute";
                            disposition = service.AdvancePreparedCombat().ToString();
                        }
                        else if (state == AdvancePreparationState.NotRequired)
                        {
                            operation = "immediate";
                            service.AdvanceCombat();
                        }
                        else throw new InvalidOperationException("A new yielded frame was unexpectedly deferred.");
                        var after = service.ReadPreparationState();
                        var repeated = service.ReadPreparationState();
                        long end = Stopwatch.GetTimestamp();
                        long bytes = hasBytes ? GC.GetAllocatedBytesForCurrentThread() - bytesBefore : -1;
                        long execution = probe.Ticks;
                        long preparation = end - start - execution;
                        preparationTicks += preparation;
                        executionTicks += execution;
                        decisionPreparation += preparation;
                        decisionExecution += execution;
                        if (hasBytes) byteTotal += bytes;
                        decisionFrames++;
                        long largestAtom = operation == "prepare" ? service.LastPreparationLargestAtomTicks : 0;
                        int collections = GC.CollectionCount(0) - gc0;
                        bool gcAffected = GcAffected(operation, preparation, execution, largestAtom, collections, gcSliceTicks);
                        if (repeat >= 0)
                        {
                            measuredCollections[band] = (measuredCollections.TryGetValue(band, out long known) ? known : 0) + collections;
                            if (operation == "prepare" || operation == "execute")
                                updates.Add(new UpdateRow(band, operation == "execute", preparation, execution, gcAffected));
                        }
                        rows.Add(FormattableString.Invariant($"{fixture},{seed},{repeat},{frame},{decision},{operation},{end - start},{preparation},{execution},{largestAtom},{version},{service.PreparationWorkVersion},{service.InputCaptureCount - captures},{collections},{GC.CollectionCount(1) - gc1},{GC.CollectionCount(2) - gc2},{bytes},{disposition},{diagnostics?.PreviewCalls ?? 0},{diagnostics?.GeneratedAttacks ?? 0},{diagnostics?.GeneratedMoves ?? 0},{diagnostics?.CapHits ?? 0},{(gcAffected ? 1 : 0)}"));
                        Assert.That(repeated, Is.EqualTo(after));
                        if (b.CombatStateReadSink.LatestResultSequence != sequence)
                        {
                            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
                            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                            Assert.That(result.Accepted, Is.True);
                            results.Add(result);
                            decisionRows.Add(FormattableString.Invariant($"{fixture},{seed},{repeat},{decision},{decisionFrames},{decisionPreparation},{decisionExecution},{end - decisionStart},{result.ActorId.Value},{result.CommandKind}"));
                            decision++;
                            decisionFrames = 0;
                            decisionPreparation = decisionExecution = 0;
                            decisionStart = end;
                        }
                        else Assert.That(operation == "prepare" || disposition == nameof(PreparedAdvanceResult.RetryPreparation) ||
                            disposition == nameof(PreparedAdvanceResult.Stale) || disposition == nameof(PreparedAdvanceResult.Cancelled), Is.True);
                        frame++;
                        yield return null;
                    }
                }
                finally
                {
                    CoreProfiling.Install(null);
                    service.CancelPreparation();
                }
                long sideTicks = Stopwatch.GetTimestamp() - sideStart;
                using var reference = EnemyAiPreparationTestData.Create(fixture, seed);
                var referencePublication = reference.Publication();
                foreach (var result in results)
                {
                    var expectedPublished = acceptance
                        ? referencePublication.Publisher.Publish(CombatCommandCalculator.AdvanceUtility(referencePublication.EnemyStep(), movementRules))
                        : reference.Core.CombatAdvance.AdvanceCombat();
                    Assert.That(expectedPublished, Is.True);
                    reference.Buffers.CombatResultReadSink.TryGetLatest(out var expected);
                    EnemyAdvancePreparationTests.AssertValues(result, expected, "result");
                }
                EnemyAdvancePreparationTests.AssertBattleValues(b, reference.Buffers);
                foreach (string row in rows) frames.WriteLine(row);
                foreach (string row in decisionRows) decisions.WriteLine(row);
                sides.WriteLine(FormattableString.Invariant($"{fixture},{seed},{repeat},{results.Count},{frame},{preparationTicks},{executionTicks},{sideTicks},{(hasBytes ? byteTotal : -1)}"));
                frames.Flush(); sides.Flush(); decisions.Flush();
                if (repeat >= 0) measuredDecisions[band] = (measuredDecisions.TryGetValue(band, out int count) ? count : 0) + results.Count;
            }

            string summary = ServiceSummary(updates, measuredDecisions, measuredCollections);
            File.WriteAllText(Path.Combine(folder, "prepared-summary.tsv"), summary);
            TestContext.Out.WriteLine(summary);
        }

        /// <summary>
        /// 준비 서비스 순서(첫 단위 = 입력 조립 + 세션 생성, 이후 Step, 다음 프레임 실행)로 같은 입력의 적 진영을 네 번 돌려,
        /// 단위마다 네 번 중 최솟값을 구조적 비용으로 남긴다. 최솟값 위로 벌어진 차이는 실행 환경(선점·코어 배치·GC)이다.
        /// 프레임 예산이 지켜지는지는 이 최솟값으로 판단한다: 가장 큰 구조적 Step이 0.1ms 이하(임계값 6, 사용자 결정 2026-11-10).
        /// </summary>
        [Test, Explicit("Requires an isolated structural-atom capture.")]
        public void CaptureStructuralAtoms()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "phase-7-atoms")
                Assert.Ignore("Enable the structural-atom capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            var rules = CombatMovementRules.Validated(new IMovementTileRule[] { new ThornMovementTileRule(),
                new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule() });
            var atoms = new List<StructuralAtom>(1 << 20);
            foreach (string fixture in new[] { "N5", "N6", "N8", "G16" })
            foreach (int seed in new[] { 1, 7, 19, 41, 97 })
            {
                AtomTrace reference = null;
                for (int repeat = -1; repeat < 4; repeat++)
                {
                    var trace = TraceSide(fixture, seed, rules);
                    if (repeat < 0) continue;
                    if (reference == null) reference = trace;
                    else reference.Merge(trace, FormattableString.Invariant($"{fixture} seed {seed} repeat {repeat}"));
                }
                reference.AppendTo(atoms, fixture, seed);
            }
            File.WriteAllText(Path.Combine(folder, "structural-runtime.txt"),
                $"frequency={Stopwatch.Frequency}\nunity={UnityEngine.Application.unityVersion}\ncpu={UnityEngine.SystemInfo.processorType}\n" +
                $"incrementalGc={GarbageCollector.isIncremental}\ngcSliceNs={GarbageCollector.incrementalTimeSliceNanoseconds}\n" +
                "fixtures=N5,N6,N8,G16\nseeds=1,7,19,41,97\nwarmupSidesPerSeed=1\nmeasuredSidesPerSeed=4\nstructural=minimum over the measured repeats\n");
            var steps = atoms.FindAll(a => a.Kind == "Step");
            steps.Sort((x, y) => y.Min.CompareTo(x.Min));
            var rows = new StringBuilder("fixture,seed,decision,index,kind,phaseBefore,phaseAfter,minMs,maxMs\n");
            for (int i = 0; i < Math.Min(200, steps.Count); i++)
            {
                var a = steps[i];
                rows.AppendLine(FormattableString.Invariant($"{a.Fixture},{a.Seed},{a.Decision},{a.Index},{a.Kind},{a.Before},{a.After},{Ms(a.Min):F4},{Ms(a.Max):F4}"));
            }
            File.WriteAllText(Path.Combine(folder, "structural-steps.csv"), rows.ToString());
            string summary = StructuralSummary(atoms);
            File.WriteAllText(Path.Combine(folder, "structural-summary.txt"), summary);
            TestContext.Out.WriteLine(summary);
            // 임계값 6 세분(사용자 결정 2026-11-10): 준비 Step의 구조적 비용 상한. 업데이트 p99·최댓값은 GC와 실행 환경에 흔들려 참고만 한다.
            long limit = (long)(StructuralStepLimitMs * Stopwatch.Frequency / 1000);
            if (steps.Count > 0)
                Assert.That(steps[0].Min, Is.LessThanOrEqualTo(limit), FormattableString.Invariant(
                    $"Structural step {Ms(steps[0].Min):F4} ms exceeds {StructuralStepLimitMs} ms: {steps[0].Fixture} seed {steps[0].Seed} decision {steps[0].Decision} {steps[0].Before} -> {steps[0].After}"));
        }

        private const double StructuralStepLimitMs = 0.1;

        // 서비스와 같은 순서로 적 진영 하나를 돌리며 단위마다 시간을 잰다. 프레임 양보는 하지 않는다(구조적 비용만 본다).
        private static AtomTrace TraceSide(string fixture, int seed, IReadOnlyList<IMovementTileRule> rules)
        {
            var trace = new AtomTrace();
            using var battle = EnemyAiPreparationTestData.Create(fixture, seed);
            var b = battle.Buffers;
            var publication = battle.Publication();
            var cache = new EnemyDecisionSideCache();
            int decision = 1;
            while (b.CombatStateReadSink.Phase == CombatFlowPhase.Automatic &&
                b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase == CombatControllerPhase.Enemy)
            {
                Assert.That(decision, Is.LessThan(128), fixture);
                var turn = b.CombatStateReadSink.Current.CommandState.Turn;
                var request = new CombatActionRequest(0, CombatCommandKind.CompleteEnemyTurn, default, default,
                    processingKind: CombatProcessingKind.Advance, expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                    expectedControllerTurnSequence: turn.ControllerTurnSequence);
                long start = Stopwatch.GetTimestamp();
                var input = publication.Assembler.AssembleDecision(request, out _);
                Assert.That(input, Is.Not.Null, fixture);
                if (!CombatCommandCalculator.RequiresEnemyDecision(input))
                {
                    Assert.That(publication.Publisher.Publish(CombatCommandCalculator.AdvanceUtility(input, rules)), Is.True, fixture);
                    trace.Add("Immediate", decision, "-", "-", Stopwatch.GetTimestamp() - start);
                    decision++;
                    continue;
                }
                // 서비스의 첫 단위와 같다: 입력 조립 + 세션 생성.
                using var session = new EnemyDecisionSession(input, rules, EnemyDecisionSettings.Default, cache);
                trace.Add("Capture", decision, "-", session.WorkPhase, Stopwatch.GetTimestamp() - start);
                while (true)
                {
                    while (!session.IsReady)
                    {
                        string before = session.WorkPhase;
                        long stepStart = Stopwatch.GetTimestamp();
                        session.Step();
                        trace.Add("Step", decision, before, session.WorkPhase, Stopwatch.GetTimestamp() - stepStart);
                    }
                    var chosen = session.Decision;
                    long executeStart = Stopwatch.GetTimestamp();
                    var transition = CombatCommandCalculator.AdvancePrepared(input, chosen, rules);
                    bool done = transition.Accepted || chosen.Kind == EnemyDecisionKind.EndEnemyPhase;
                    if (done) Assert.That(publication.Publisher.Publish(transition), Is.True, fixture);
                    else session.RejectSelected();
                    trace.Add(done ? "Execute" : "Reject", decision, "-", "-", Stopwatch.GetTimestamp() - executeStart);
                    if (done) break;
                }
                decision++;
            }
            return trace;
        }

        // GC 정지가 든 업데이트다: 완료된 수집(수집 횟수 변화), 또는 증분 GC 조각 길이의 90% 이상인 준비 단위,
        // 예산 + 조각 길이의 90% 이상인 준비 업데이트(조각이 단위 밖에서 돈 경우), 조각 길이의 90% 이상인 실행.
        // 단위의 구조적 비용은 CaptureStructuralAtoms가 따로 잰다. 증분 GC가 꺼져 있으면 수집 횟수만 본다.
        private static bool GcAffected(string operation, long preparation, long execution, long largestAtom, int collections, long sliceTicks)
        {
            if (collections > 0) return true;
            if (sliceTicks <= 0) return false;
            long threshold = sliceTicks * 9 / 10;
            return operation == "execute"
                ? execution >= threshold
                : largestAtom >= threshold || preparation >= Stopwatch.Frequency / 1000 + threshold;
        }

        private static string Band(int enemies) => enemies <= 5 ? "<=5" : enemies <= 8 ? "6-8" : enemies <= 10 ? "9-10" : ">10";

        // 측정 반복의 준비·실행 업데이트를 적 수 구간별로 묶는다. 준비 값은 기존 p7 집계와 같이 실행 구간을 뺀 업데이트 시간이다.
        private static string ServiceSummary(List<UpdateRow> updates, Dictionary<string, int> decisions, Dictionary<string, long> collections)
        {
            var text = new StringBuilder("band\tupdates\tp95Ms\tp99Ms\tmaxMs\tover4Ms\tgcAffected\tover4MsGcAffected\tgcFreeP99Ms\tgcFreeMaxMs\t" +
                "executeFrames\texecuteP50Ms\texecuteP95Ms\texecuteP99Ms\texecuteMaxMs\texecuteGcFreeMaxMs\tdecisions\tcollections\tcollectionsPer100Decisions\n");
            foreach (string band in new[] { "<=5", "6-8", "9-10", ">10", "all" })
            {
                var rows = band == "all" ? updates : updates.FindAll(r => r.Band == band);
                if (rows.Count == 0) continue;
                var preparation = Sorted(rows, r => true, r => r.Preparation);
                var free = Sorted(rows, r => !r.GcAffected, r => r.Preparation);
                var execute = Sorted(rows, r => r.Execute, r => r.Execution);
                var executeFree = Sorted(rows, r => r.Execute && !r.GcAffected, r => r.Execution);
                long fourMs = Stopwatch.Frequency * 4 / 1000;
                int over = rows.FindAll(r => r.Preparation > fourMs).Count;
                int overGc = rows.FindAll(r => r.Preparation > fourMs && r.GcAffected).Count;
                int affected = rows.FindAll(r => r.GcAffected).Count;
                int decisionCount = Sum(decisions, band);
                long collectionCount = Sum(collections, band);
                text.Append(FormattableString.Invariant(
                    $"{band}\t{rows.Count}\t{Ms(Rank(preparation, 0.95)):F3}\t{Ms(Rank(preparation, 0.99)):F3}\t{Ms(Rank(preparation, 1)):F2}\t{over}\t{affected}\t{overGc}\t"));
                text.Append(FormattableString.Invariant(
                    $"{Ms(Rank(free, 0.99)):F3}\t{Ms(Rank(free, 1)):F2}\t{execute.Count}\t{Ms(Rank(execute, 0.5)):F3}\t{Ms(Rank(execute, 0.95)):F3}\t"));
                text.AppendLine(FormattableString.Invariant(
                    $"{Ms(Rank(execute, 0.99)):F3}\t{Ms(Rank(execute, 1)):F2}\t{Ms(Rank(executeFree, 1)):F2}\t{decisionCount}\t{collectionCount}\t{(decisionCount == 0 ? 0 : collectionCount * 100.0 / decisionCount):F2}"));
            }
            return text.ToString();
        }

        private static string StructuralSummary(List<StructuralAtom> atoms)
        {
            var steps = atoms.FindAll(a => a.Kind == "Step");
            var mins = Sorted(steps, a => true, a => a.Min);
            long Threshold(double ms) => (long)(ms * Stopwatch.Frequency / 1000);
            var text = new StringBuilder();
            text.AppendLine(FormattableString.Invariant($"steps={steps.Count} decisions={atoms.FindAll(a => a.Kind == "Capture" || a.Kind == "Immediate").Count}"));
            text.AppendLine(FormattableString.Invariant(
                $"step structural (min over repeats): p99={Ms(Rank(mins, 0.99)):F4} p999={Ms(Rank(mins, 0.999)):F4} max={Ms(Rank(mins, 1)):F4} ms"));
            foreach (double ms in new[] { 0.05, 0.1, 0.2, 0.5 })
                text.AppendLine(FormattableString.Invariant(
                    $"steps with structural cost > {ms} ms: {steps.FindAll(a => a.Min > Threshold(ms)).Count}; with some repeat > {ms} ms: {steps.FindAll(a => a.Max > Threshold(ms)).Count}"));
            if (steps.Count > 0)
            {
                var top = steps[0];
                foreach (var a in steps) if (a.Min > top.Min) top = a;
                text.AppendLine(FormattableString.Invariant($"largest structural step: {Ms(top.Min):F4} ms {top.Fixture} seed {top.Seed} decision {top.Decision} {top.Before} -> {top.After}"));
            }
            text.AppendLine("by phase (after the step): steps, sum of structural ms, max structural ms, structural > 0.05 ms");
            var phases = new SortedDictionary<string, (int count, long sum, long max, int over)>(StringComparer.Ordinal);
            foreach (var a in steps)
            {
                string phase = a.After.IndexOf('/') < 0 ? a.After : a.After.Substring(0, a.After.IndexOf('/')) + "/*";
                phases.TryGetValue(phase, out var value);
                phases[phase] = (value.count + 1, value.sum + a.Min, Math.Max(value.max, a.Min), value.over + (a.Min > Threshold(0.05) ? 1 : 0));
            }
            foreach (var pair in phases)
                text.AppendLine(FormattableString.Invariant($"  {pair.Key}\t{pair.Value.count}\t{Ms(pair.Value.sum):F1}\t{Ms(pair.Value.max):F4}\t{pair.Value.over}"));
            var capture = Sorted(atoms, a => a.Kind == "Capture", a => a.Min);
            text.AppendLine(FormattableString.Invariant(
                $"first atom (input assembly + session construction) structural: p50={Ms(Rank(capture, 0.5)):F4} p99={Ms(Rank(capture, 0.99)):F4} max={Ms(Rank(capture, 1)):F4} ms"));
            foreach (string fixture in new[] { "N5", "N6", "N8", "G16", null })
            {
                var execute = Sorted(atoms, a => (a.Kind == "Execute" || a.Kind == "Reject") && (fixture == null || a.Fixture == fixture), a => a.Min);
                text.AppendLine(FormattableString.Invariant(
                    $"execute frame structural ({fixture ?? "all"}): n={execute.Count} p50={Ms(Rank(execute, 0.5)):F4} p95={Ms(Rank(execute, 0.95)):F4} max={Ms(Rank(execute, 1)):F4} ms"));
            }
            return text.ToString();
        }

        private static List<long> Sorted<T>(List<T> rows, Predicate<T> keep, Func<T, long> value)
        {
            var result = new List<long>(rows.Count);
            foreach (var row in rows) if (keep(row)) result.Add(value(row));
            result.Sort();
            return result;
        }

        // 가까운 순위 백분위수(기존 p7 집계와 같은 방식). 비어 있으면 0이다.
        private static long Rank(List<long> sorted, double percentile) =>
            sorted.Count == 0 ? 0 : sorted[Math.Max(0, Math.Min(sorted.Count - 1, (int)Math.Ceiling(sorted.Count * percentile) - 1))];

        private static int Sum(Dictionary<string, int> values, string band)
        {
            if (band != "all") return values.TryGetValue(band, out int one) ? one : 0;
            int total = 0;
            foreach (var value in values.Values) total += value;
            return total;
        }

        private static long Sum(Dictionary<string, long> values, string band)
        {
            if (band != "all") return values.TryGetValue(band, out long one) ? one : 0;
            long total = 0;
            foreach (var value in values.Values) total += value;
            return total;
        }

        private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        private readonly struct UpdateRow
        {
            internal string Band { get; }
            internal bool Execute { get; }
            internal long Preparation { get; }
            internal long Execution { get; }
            internal bool GcAffected { get; }

            internal UpdateRow(string band, bool execute, long preparation, long execution, bool gcAffected)
            {
                Band = band;
                Execute = execute;
                Preparation = preparation;
                Execution = execution;
                GcAffected = gcAffected;
            }
        }

        private readonly struct StructuralAtom
        {
            internal string Fixture { get; }
            internal int Seed { get; }
            internal int Decision { get; }
            internal int Index { get; }
            internal string Kind { get; }
            internal string Before { get; }
            internal string After { get; }
            internal long Min { get; }
            internal long Max { get; }

            internal StructuralAtom(string fixture, int seed, int decision, int index, string kind, string before, string after, long min, long max)
            {
                Fixture = fixture;
                Seed = seed;
                Decision = decision;
                Index = index;
                Kind = kind;
                Before = before;
                After = after;
                Min = min;
                Max = max;
            }
        }

        // 적 진영 한 번의 단위 순서와 시간이다. 같은 입력의 반복은 같은 단위 순서를 내야 하고, 시간은 단위마다 최솟값·최댓값을 남긴다.
        private sealed class AtomTrace
        {
            private readonly List<string> _kinds = new List<string>(1 << 16);
            private readonly List<int> _decisions = new List<int>(1 << 16);
            private readonly List<string> _before = new List<string>(1 << 16);
            private readonly List<string> _after = new List<string>(1 << 16);
            private readonly List<long> _min = new List<long>(1 << 16);
            private readonly List<long> _max = new List<long>(1 << 16);

            internal void Add(string kind, int decision, string before, string after, long ticks)
            {
                _kinds.Add(kind);
                _decisions.Add(decision);
                _before.Add(before);
                _after.Add(after);
                _min.Add(ticks);
                _max.Add(ticks);
            }

            internal void Merge(AtomTrace other, string context)
            {
                Assert.That(other._kinds.Count, Is.EqualTo(_kinds.Count), $"Atom count differs between repeats of the same input: {context}");
                for (int i = 0; i < _kinds.Count; i++)
                {
                    if (other._kinds[i] != _kinds[i] || other._decisions[i] != _decisions[i] ||
                        other._before[i] != _before[i] || other._after[i] != _after[i])
                        Assert.Fail($"Atom {i} differs between repeats of the same input: {context}");
                    _min[i] = Math.Min(_min[i], other._min[i]);
                    _max[i] = Math.Max(_max[i], other._max[i]);
                }
            }

            internal void AppendTo(List<StructuralAtom> atoms, string fixture, int seed)
            {
                for (int i = 0; i < _kinds.Count; i++)
                    atoms.Add(new StructuralAtom(fixture, seed, _decisions[i], i, _kinds[i], _before[i], _after[i], _min[i], _max[i]));
            }
        }

        private sealed class ExecutionTimer : ICoreProfiler
        {
            private int _depth;
            private long _start;
            internal long Ticks;
            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.EnemyActionExecution && _depth++ == 0) _start = Stopwatch.GetTimestamp();
            }
            public void End(CoreProfileSection section)
            {
                if (section == CoreProfileSection.EnemyActionExecution && --_depth == 0) Ticks += Stopwatch.GetTimestamp() - _start;
            }
        }
    }
}
