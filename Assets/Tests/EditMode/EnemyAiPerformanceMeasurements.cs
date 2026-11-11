using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyAiPerformanceMeasurements
    {
        private const int Warmup = 10;
        private const int Samples = 20;
        private const int SideLimit = 128;
        private delegate CombatCommandTransition AdvanceDecision(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyDecisionDiagnostics diagnostics);
        private const string TraceHeader = "accepted\tactor\tkind\ttargetCell\tresolvedRotation\tpath\ttaunt\tbeforeUse\tafterUse\tfinalController\tevents\taimKind\taimCell\taimDirection";

        [Test, Explicit("Requires an isolated Unity run and PROJECTL_ENEMY_AI_RAW.")]
        public void CaptureCorpus()
        {
            string capture = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE");
            if (capture != "core" && capture != "phase-6-corpus")
                Assert.Ignore("Enable the dedicated Core capture explicitly.");
            bool utility = capture == "phase-6-corpus";
            AdvanceDecision advance = utility ? CombatCommandCalculator.AdvanceUtility : CombatCommandCalculator.Advance;
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            long beforeProbe = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(new byte[4096]);
            long probe = GC.GetAllocatedBytesForCurrentThread() - beforeProbe;

            Directory.CreateDirectory(folder);
            if (utility) CaptureScoreAllocation(folder, probe);
            var priorCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                Capture(folder, probe, utility, advance);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = priorCulture;
            }
        }

        private static void CaptureScoreAllocation(string folder, long probe)
        {
            const int samples = 20000;
            var facts = new EnemyScoreFacts(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects.CombatAssessmentGap.None,
                opponentHpLoss: 250, actorHazardLoss: 100);
            _ = EnemyUtilityScorer.Score(facts);
            long before = GC.GetAllocatedBytesForCurrentThread();
            long checksum = 0;
            for (int i = 0; i < samples; i++) checksum += EnemyUtilityScorer.Score(facts);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(checksum, Is.EqualTo(-150L * samples));
            if (probe >= 4096) Assert.That(bytes, Is.Zero);
            File.WriteAllLines(Path.Combine(folder, "score-allocation.txt"), new[]
            {
                $"samples={samples}", $"probeBytes={probe}", $"allocationSupported={probe >= 4096}",
                $"measuredBytes={(probe >= 4096 ? bytes : -1)}"
            });
        }

        private static void Capture(string folder, long allocationProbe, bool utility, AdvanceDecision advance)
        {
            bool measureBytes = allocationProbe >= 4096;
            var corpus = (from fixture in EnemyAiBenchmarkFixtures.Ids
                          from seed in EnemyAiBenchmarkFixtures.Seeds
                          let input = EnemyAiBenchmarkFixtures.Build(fixture, seed)
                          let manifest = EnemyAiBenchmarkFixtures.Describe(input, true)
                          select (Fixture: fixture, Seed: seed, Input: input, Manifest: manifest,
                              Hash: EnemyAiBenchmarkFixtures.Hash(manifest))).ToArray();
            using (var manifests = File.CreateText(Path.Combine(folder, "inputs.tsv")))
            {
                manifests.WriteLine("fixture\tseed\tsha256\tcanonicalInputUtf8Base64");
                foreach (var item in corpus)
                    manifests.WriteLine($"{item.Fixture}\t{item.Seed}\t{item.Hash}\t{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(item.Manifest))}");
            }
            File.WriteAllLines(Path.Combine(folder, "runtime.txt"), new[]
            {
                $"frequency={Stopwatch.Frequency}", $"allocationProbeBytes={allocationProbe}", $"allocationSupported={measureBytes}", $"gcMaxGeneration={GC.MaxGeneration}",
                $"runtime={Environment.Version}", $"os={Environment.OSVersion}", $"process64Bit={Environment.Is64BitProcess}",
                $"warmup={Warmup}", $"samples={Samples}", $"fullSidesPerFixture={Samples}",
                utility ? "profiler=none; decision Steps captured separately; Unity batch EditMode, no render frames" :
                    "profiler=custom ICoreProfiler; Unity batch EditMode, no render frames"
            });
            using var decisions = File.CreateText(Path.Combine(folder, "decisions.csv"));
            using var atoms = File.CreateText(Path.Combine(folder, "atoms.csv"));
            using var sides = File.CreateText(Path.Combine(folder, "sides.csv"));
            using var traces = File.CreateText(Path.Combine(folder, utility ? "utility-traces.tsv" : "baseline-traces.tsv"));
            using var overhead = File.CreateText(Path.Combine(folder, "overhead.csv"));
            decisions.WriteLine("fixture,seed,decision,inputHash,stage,ticks,bytes,actor,outcome,previewCalls,expandedNodes,workUnits,generatedAttacks,generatedMoves,cacheHits,rejectedAttempts,capHits");
            atoms.WriteLine("fixture,seed,decision,section,ticks,bytes,stage");
            sides.WriteLine("fixture,seed,repeat,decisionCount,advanceTicks,advanceBytes,projectionTicks,projectionBytes");
            traces.WriteLine("fixture\tseed\tinputHash\tdecision\t" + TraceHeader);
            overhead.WriteLine("fixture,seed,repeat,mode,ticks,bytes");

            var sampler = new AtomRecorder(measureBytes);
            var rows = new (Timing Time, CombatCommandTransition Result, EnemyDecisionDiagnostics Diagnostics)[Samples + 1];
            foreach (var item in corpus)
            {
                sampler.Reset();
                CoreProfiling.Install(utility ? null : sampler);
                try
                {
                    sampler.Decision = 0;
                    rows[0] = Decide(item.Input, measureBytes, advance);
                    sampler.Recording = false;
                    for (int i = 0; i < Warmup; i++) advance(item.Input, EnemyAiBenchmarkFixtures.Rules, null);
                    sampler.Recording = true;
                    for (int i = 1; i <= Samples; i++)
                    {
                        sampler.Decision = i;
                        rows[i] = Decide(item.Input, measureBytes, advance);
                    }
                }
                finally
                {
                    CoreProfiling.Install(null);
                }
                Assert.That(sampler.Depth, Is.Zero);
                Assert.That(sampler.Overflowed, Is.False, $"{item.Fixture}/{item.Seed} atom storage exhausted.");
                for (int i = 0; i < rows.Length; i++)
                {
                    var (time, result, diagnostics) = rows[i];
                    Assert.That(result.Accepted, Is.True, result.Reason);
                    string kind = result.Attack.HasValue ? "Attack" : result.PathMovement != null ? "Move" : "Boundary";
                    decisions.WriteLine($"{item.Fixture},{item.Seed},{i},{item.Hash},{(i == 0 ? "first" : "warm")},{time.Ticks},{Bytes(time.Bytes, measureBytes)}," +
                        $"{result.ActionUseSettlement?.ActorId.Value ?? 0},{kind},{diagnostics.PreviewCalls},{diagnostics.ExpandedNodes}," +
                        $"{diagnostics.WorkUnits},{diagnostics.GeneratedAttacks},{diagnostics.GeneratedMoves},{diagnostics.CacheHits}," +
                        $"{diagnostics.RejectedAttempts},{diagnostics.CapHits}");
                }
                for (int i = 0; i < sampler.Count; i++)
                {
                    var row = sampler.Samples[i];
                    atoms.WriteLine($"{item.Fixture},{item.Seed},{row.Decision},{row.Section},{row.Time.Ticks},{Bytes(row.Time.Bytes, measureBytes)},{(row.Decision == 0 ? "first" : "warm")}");
                }
                CaptureSides(sides, traces, item.Fixture, item.Seed, item.Hash, item.Input, measureBytes, advance);
                if (item.Fixture == "N5" && item.Seed == 1)
                    CaptureOverhead(overhead, item.Input, measureBytes, advance);
            }
        }


        [Test, Explicit("Requires an isolated Unity run and PROJECTL_ENEMY_AI_RAW.")]
        public void CapturePreviewWork()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "preview-work")
                Assert.Ignore("Enable the dedicated preview capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            long probeStart = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(new byte[4096]);
            long probe = GC.GetAllocatedBytesForCurrentThread() - probeStart;
            bool bytes = probe >= 4096;
            File.WriteAllLines(Path.Combine(folder, "preview-runtime.txt"), new[]
            {
                $"frequency={Stopwatch.Frequency}", $"allocationProbeBytes={probe}", $"allocationSupported={bytes}",
                "warmup=10", "samples=20", "partitions=1;2;31", "profiler=none; per-Step wall-clock; batch not render frames"
            });
            using var writer = File.CreateText(Path.Combine(folder, "preview-work.csv"));
            using var totals = File.CreateText(Path.Combine(folder, "preview-totals.csv"));
            using var plain = File.CreateText(Path.Combine(folder, "preview-plain.csv"));
            writer.WriteLine("fixture,seed,decision,previewId,parentId,owner,phase,item,workVersion,operation,ticks,bytes,stage,partition,gc0");
            totals.WriteLine("fixture,partition,repeat,stage,constructorTicks,stepTicks,readTicks,disposeTicks,totalTicks,steps,bytes,semanticHash,gc0");
            plain.WriteLine("fixture,repeat,ticks,bytes,gc0");
            var timings = new Timing[200000];
            var phases = new string[timings.Length];
            var versions = new long[timings.Length];
            var stepCollections = new int[timings.Length];
            var expected = AttackPreviewParityTests.ReadReference();
            foreach (var entry in AttackPreviewParityTests.ReadReference(Path.Combine(Directory.GetCurrentDirectory(),
                AttackPreviewParityTests.MovementReferencePath)))
                expected.Add(entry.Key, entry.Value);
            foreach (var vector in AttackPreviewParityTests.Vectors().Concat(AttackPreviewParityTests.MovementVectors()))
            {
                string before = AttackPreviewParityTests.Serialize(vector.Input, new System.Collections.Generic.HashSet<string>());
                var actual = CombatCommandCalculator.Calculate(vector.Input, vector.Rules);
                foreach (int partition in new[] { 1, 2, 31 })
                {
                    for (int repeat = -1; repeat < Samples; repeat++)
                    {
                        if (repeat == 0)
                            for (int i = 0; i < Warmup; i++) CombatCommandCalculator.Preview(vector.Input, vector.Rules);
                        string stage = repeat < 0 ? "first" : "warm";
                        int gcBefore = GC.CollectionCount(0);
                        var overall = Timing.Read(bytes);
                        int gcConstructorStart = GC.CollectionCount(0);
                        var start = Timing.Read(bytes);
                        var work = CombatCommandCalculator.BeginPreview(vector.Input, vector.Rules);
                        var construction = Timing.Since(start, bytes);
                        int constructorCollections = GC.CollectionCount(0) - gcConstructorStart;
                        int count = 0;
                        while (!work.IsComplete)
                        {
                            for (int i = 0; i < partition && !work.IsComplete; i++)
                            {
                                int gcStepStart = GC.CollectionCount(0);
                                start = Timing.Read(bytes);
                                work.Step();
                                timings[count] = Timing.Since(start, bytes);
                                stepCollections[count] = GC.CollectionCount(0) - gcStepStart;
                                phases[count] = work.Phase;
                                versions[count] = work.WorkVersion;
                                count++;
                                if (count >= timings.Length) Assert.Fail("Step storage exhausted.");
                            }
                        }
                        int gcReadStart = GC.CollectionCount(0);
                        start = Timing.Read(bytes);
                        var preview = work.Preview;
                        var transition = work.Transition;
                        var read = Timing.Since(start, bytes);
                        int readCollections = GC.CollectionCount(0) - gcReadStart;
                        int gcDisposeStart = GC.CollectionCount(0);
                        start = Timing.Read(bytes);
                        work.Dispose();
                        var dispose = Timing.Since(start, bytes);
                        int disposeCollections = GC.CollectionCount(0) - gcDisposeStart;
                        var total = Timing.Since(overall, bytes);
                        int collections = GC.CollectionCount(0) - gcBefore;
                        string semantic = AttackPreviewParityTests.Serialize(new object[] { before, preview, transition, actual },
                            new System.Collections.Generic.HashSet<string>());
                        Assert.That(semantic, Is.EqualTo(AttackPreviewParityTests.Decode(expected[vector.Id].Split('|')[0])), vector.Id);
                        long ticks = 0;
                        void Row(string phase, long version, string operation, Timing time, int? gc0 = null)
                        {
                            writer.WriteLine($"{vector.Id},{vector.Input.Battle?.TimelineSeed ?? 0},{repeat},{vector.Id}-{partition}-{repeat},none,CombatAttackCalculation,{phase},{version},{version},{operation},{time.Ticks},{Bytes(time.Bytes, bytes)},{stage},{partition},{gc0}");
                        }
                        Row("Begin", 0, "constructor", construction, constructorCollections);
                        for (int i = 0; i < count; i++)
                        {
                            ticks += timings[i].Ticks;
                            Row(phases[i], versions[i], "Step", timings[i], stepCollections[i]);
                        }
                        Row("Result", count, "read", read, readCollections);
                        Row("Dispose", count, "dispose", dispose, disposeCollections);
                        totals.WriteLine($"{vector.Id},{partition},{repeat},{stage},{construction.Ticks},{ticks},{read.Ticks},{dispose.Ticks},{total.Ticks},{count},{Bytes(total.Bytes, bytes)},{EnemyAiBenchmarkFixtures.Hash(semantic)},{collections}");
                    }
                }
                for (int repeat = 0; repeat < Samples; repeat++)
                {
                    int gcBefore = GC.CollectionCount(0);
                    var start = Timing.Read(bytes);
                    CombatCommandCalculator.Preview(vector.Input, vector.Rules);
                    var elapsed = Timing.Since(start, bytes);
                    int collections = GC.CollectionCount(0) - gcBefore;
                    plain.WriteLine($"{vector.Id},{repeat},{elapsed.Ticks},{Bytes(elapsed.Bytes, bytes)},{collections}");
                }
            }
        }

        [Test, Explicit("Requires an isolated Unity run and PROJECTL_ENEMY_AI_RAW.")]
        public void CaptureSacrificeLeaf()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "sacrifice-leaf")
                Assert.Ignore("Enable the dedicated leaf capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            CaptureOwnedWork(folder, "sacrifice-ordered", CoreProfileSection.AttackSacrificePlanning, "sacrifice", true);
        }

        [Test, Explicit("Requires an isolated Unity run and PROJECTL_ENEMY_AI_RAW.")]
        public void CaptureForcedPathLeaf()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "forced-path")
                Assert.Ignore("Enable the dedicated forced-path capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            CaptureOwnedWork(folder, "pull-tornado", CoreProfileSection.AttackForcedPathStep, "forced-path", true);
        }

        private static void CaptureOwnedWork(string folder, string vectorId, CoreProfileSection section, string prefix, bool resumable)
        {
            var vector = AttackPreviewParityTests.Vectors().Single(v => v.Id == vectorId);
            var sampler = new AtomRecorder(false);
            var reference = AttackPreviewParityTests.Decode(AttackPreviewParityTests.ReadReference()[vector.Id].Split('|')[0]);
            var types = new System.Collections.Generic.HashSet<string>();
            string before = AttackPreviewParityTests.Serialize(vector.Input, types);
            var actual = CombatCommandCalculator.Calculate(vector.Input, vector.Rules);
            for (int i = 0; i < 10; i++) CombatCommandCalculator.Preview(vector.Input, vector.Rules);
            using var writer = File.CreateText(Path.Combine(folder, prefix + "-leaf.csv"));
            writer.WriteLine("fixture,repeat,workVersion,owner,ticks,gc0,gc1,gc2,semanticHash,phase");
            using var totals = File.CreateText(Path.Combine(folder, prefix + "-totals.csv"));
            totals.WriteLine("fixture,repeat,steps,sumTicks,maxStepTicks,plainPreviewTicks");
            var phases = new string[200000];
            int? expectedCalls = null;
            File.WriteAllText(Path.Combine(folder, prefix + "-runtime.txt"), $"frequency={Stopwatch.Frequency}\nwarmup=10\nsamples=100\nallocation=unavailable\nprofiler=exact owner call or MoveNext per scope\n");
            for (int repeat = 0; repeat < 100; repeat++)
            {
                sampler.Reset();
                int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
                using var work = CombatCommandCalculator.BeginPreview(vector.Input, vector.Rules);
                CoreProfiling.Install(sampler);
                try
                {
                    while (!work.IsComplete)
                    {
                        sampler.Decision = checked((int)work.WorkVersion + 1);
                        work.Step();
                        phases[checked((int)work.WorkVersion)] = work.Phase;
                    }
                }
                finally
                {
                    CoreProfiling.Install(null);
                }
                int d0 = GC.CollectionCount(0) - g0, d1 = GC.CollectionCount(1) - g1, d2 = GC.CollectionCount(2) - g2;
                Assert.That(sampler.Depth, Is.Zero);
                Assert.That(sampler.Overflowed, Is.False);
                string semantic = AttackPreviewParityTests.Serialize(new object[] { before, work.Preview, work.Transition, actual }, types);
                Assert.That(semantic, Is.EqualTo(reference));
                string hash = EnemyAiBenchmarkFixtures.Hash(semantic);
                int calls = 0;
                long sum = 0, max = 0;
                for (int i = 0; i < sampler.Count; i++)
                {
                    var row = sampler.Samples[i];
                    if (row.Section != section) continue;
                    calls++;
                    sum += row.Time.Ticks;
                    max = Math.Max(max, row.Time.Ticks);
                    writer.WriteLine($"{vector.Id},{repeat},{row.Decision},{row.Section},{row.Time.Ticks},{d0},{d1},{d2},{hash},{phases[row.Decision]}");
                }
                if (resumable) Assert.That(calls, Is.GreaterThan(1));
                else Assert.That(calls, Is.EqualTo(1));
                expectedCalls ??= calls;
                Assert.That(calls, Is.EqualTo(expectedCalls.Value));
                long started = Stopwatch.GetTimestamp();
                CombatCommandCalculator.Preview(vector.Input, vector.Rules);
                long plain = Stopwatch.GetTimestamp() - started;
                totals.WriteLine($"{vector.Id},{repeat},{calls},{sum},{max},{plain}");
            }
        }

        private static string Bytes(long value, bool available) => available ? value.ToString(CultureInfo.InvariantCulture) : "";

        private static (Timing Time, CombatCommandTransition Result, EnemyDecisionDiagnostics Diagnostics) Decide(CombatCommandInput input, bool measureBytes, AdvanceDecision advance)
        {
            var diagnostics = new EnemyDecisionDiagnostics();
            var start = Timing.Read(measureBytes);
            var result = advance(input, EnemyAiBenchmarkFixtures.Rules, diagnostics);
            return (Timing.Since(start, measureBytes), result, diagnostics);
        }

        private static void CaptureSides(StreamWriter sides, StreamWriter traces, string fixture, int seed, string hash, CombatCommandInput input, bool measureBytes, AdvanceDecision advance)
        {
            var results = new CombatCommandTransition[SideLimit];
            var aims = new AttackAim?[SideLimit];
            // 한 fixture의 전체 진영 20회는 고정 시드 5개에 균등 분배한다.
            for (int repeat = 0; repeat < Samples / EnemyAiBenchmarkFixtures.Seeds.Length; repeat++)
            {
                var current = input;
                int count = 0;
                long ticks = 0, bytes = 0, projectionTicks = 0, projectionBytes = 0;
                while (current.State.Turn.ControllerPhase == CombatControllerPhase.Enemy && count < SideLimit)
                {
                    var diagnostics = new EnemyDecisionDiagnostics();
                    var start = Timing.Read(measureBytes);
                    var result = advance(current, EnemyAiBenchmarkFixtures.Rules, diagnostics);
                    var elapsed = Timing.Since(start, measureBytes);
                    ticks += elapsed.Ticks;
                    bytes += elapsed.Bytes;
                    aims[count] = diagnostics.SelectedAim;
                    results[count++] = result;
                    start = Timing.Read(measureBytes);
                    current = CombatCommandComposition.Project(result);
                    elapsed = Timing.Since(start, measureBytes);
                    projectionTicks += elapsed.Ticks;
                    projectionBytes += elapsed.Bytes;
                    Assert.That(result.Accepted, Is.True, result.Reason);
                    if (result.Outcome.IsStageEnded) break;
                }
                Assert.That(count, Is.LessThan(SideLimit), $"{fixture}/{seed}: enemy phase did not finish.");
                Assert.That(count, Is.GreaterThan(0));
                sides.WriteLine($"{fixture},{seed},{repeat},{count},{ticks},{Bytes(bytes, measureBytes)},{projectionTicks},{Bytes(projectionBytes, measureBytes)}");
                if (repeat != 0) continue;
                for (int i = 0; i < count; i++)
                    traces.WriteLine($"{fixture}\t{seed}\t{hash}\t{i + 1}\t{EnemyAiBaselineTests.Trace(results[i], aims[i])}");
            }
        }

        private static void CaptureOverhead(StreamWriter writer, CombatCommandInput input, bool measureBytes, AdvanceDecision advance)
        {
            var timings = new Timing[3, Samples];
            var modes = new[] { "none", "counters", "alternatives" };
            var diagnostics = new EnemyDecisionDiagnostics[3, Samples];
            for (int mode = 1; mode < modes.Length; mode++)
                for (int repeat = 0; repeat < Samples; repeat++)
                    diagnostics[mode, repeat] = new EnemyDecisionDiagnostics(captureAlternatives: mode == 2);
            for (int mode = 0; mode < modes.Length; mode++)
                for (int warmup = 0; warmup < Warmup; warmup++)
                    advance(input, EnemyAiBenchmarkFixtures.Rules,
                        mode == 0 ? null : new EnemyDecisionDiagnostics(mode == 2));
            for (int repeat = 0; repeat < Samples; repeat++)
            {
                for (int i = 0; i < modes.Length; i++)
                {
                    int mode = (i + repeat) % modes.Length;
                    var start = Timing.Read(measureBytes);
                    advance(input, EnemyAiBenchmarkFixtures.Rules, diagnostics[mode, repeat]);
                    timings[mode, repeat] = Timing.Since(start, measureBytes);
                }
            }
            for (int mode = 0; mode < modes.Length; mode++)
                for (int repeat = 0; repeat < Samples; repeat++)
                    writer.WriteLine($"N5,1,{repeat},{modes[mode]},{timings[mode, repeat].Ticks},{Bytes(timings[mode, repeat].Bytes, measureBytes)}");
        }

        private readonly struct Timing
        {
            internal long Ticks { get; }
            internal long Bytes { get; }

            private Timing(long ticks, long bytes)
            {
                Ticks = ticks;
                Bytes = bytes;
            }

            internal static Timing Read(bool measureBytes) => new Timing(Stopwatch.GetTimestamp(),
                measureBytes ? GC.GetAllocatedBytesForCurrentThread() : 0);
            internal static Timing Since(Timing start, bool measureBytes) =>
                new Timing(Stopwatch.GetTimestamp() - start.Ticks, measureBytes ? GC.GetAllocatedBytesForCurrentThread() - start.Bytes : 0);
        }

        private sealed class AtomRecorder : ICoreProfiler
        {
            private readonly bool _measureBytes;
            private readonly (CoreProfileSection Section, Timing Start)[] _stack = new (CoreProfileSection, Timing)[32];

            internal readonly (int Decision, CoreProfileSection Section, Timing Time)[] Samples =
                new (int, CoreProfileSection, Timing)[200_000];

            internal int Decision { get; set; }
            internal int Count { get; private set; }
            internal int Depth { get; private set; }
            internal bool Overflowed { get; private set; }
            internal bool Recording { get; set; }

            internal AtomRecorder(bool measureBytes)
            {
                _measureBytes = measureBytes;
            }

            internal void Reset()
            {
                Depth = Count = 0;
                Overflowed = false;
                Recording = true;
            }

            public void Begin(CoreProfileSection section)
            {
                if (!Recording) return;
                _stack[Depth++] = (section, Timing.Read(_measureBytes));
            }

            public void End(CoreProfileSection section)
            {
                if (!Recording) return;
                var (begin, start) = _stack[--Depth];
                var elapsed = Timing.Since(start, _measureBytes);
                if (begin != section) throw new InvalidOperationException("프로파일링 구간의 시작과 끝이 일치하지 않습니다.");
                if (Count == Samples.Length)
                {
                    Overflowed = true;
                    return;
                }
                Samples[Count++] = (Decision, section, elapsed);
            }
        }
    }
}
