using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyDecisionStepMeasurements
    {
        private const int WarmSamples = 12;
        private const int MaxSteps = 2_000_000;

        [Test, Explicit("Requires an isolated Unity run and PROJECTL_ENEMY_AI_RAW.")]
        public void CaptureDecisionSteps()
        {
            string capture = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE");
            if (capture != "decision-steps" && capture != "phase-6-atoms")
                Assert.Ignore("Enable the dedicated decision capture explicitly.");
            bool utility = capture == "phase-6-atoms";
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            bool cycleSupported = Environment.OSVersion.Platform == PlatformID.Win32NT &&
                QueryThreadCycleTime(GetCurrentThread(), out _);
            IntPtr thread = cycleSupported ? GetCurrentThread() : IntPtr.Zero;
            long beforeProbe = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(new byte[4096]);
            long probe = GC.GetAllocatedBytesForCurrentThread() - beforeProbe;
            File.WriteAllLines(Path.Combine(folder, "decision-step-runtime.txt"), new[] {
                $"frequency={Stopwatch.Frequency}", $"allocationProbeBytes={probe}",
                $"allocationSupported={probe >= 4096}", $"threadCyclesSupported={cycleSupported}",
                $"runtime={Environment.Version}", $"warmSamples={WarmSamples}", "warmup=3", "fixtureSeed=1"
            });
            using var raw = new StreamWriter(new GZipStream(File.Create(Path.Combine(folder, "decision-steps.csv.gz")),
                CompressionLevel.Fastest));
            using var tails = File.CreateText(Path.Combine(folder, "decision-step-tails.csv"));
            using var totals = File.CreateText(Path.Combine(folder, "decision-step-totals.csv"));
            const string header = "fixture,sample,operation,workVersion,phase,ticks,gcDelta,threadCycles,bytes";
            raw.WriteLine(header);
            tails.WriteLine(header);
            totals.WriteLine("fixture,sample,decision,steps,previewCalls,nodes,operationTicks");

            var rows = new List<Sample>();
            foreach (string fixture in EnemyAiBenchmarkFixtures.Ids)
            {
                var input = EnemyAiBenchmarkFixtures.Build(fixture, 1);
                Capture(fixture, input, 0, thread, probe >= 4096, raw, tails, totals, rows, utility);
                for (int warmup = 0; warmup < 3; warmup++)
                {
                    using var session = Create(input, utility);
                    while (!session.IsReady && session.WorkVersion < MaxSteps) session.Step();
                    Assert.That(session.IsReady, Is.True, fixture);
                }
                for (int sample = 1; sample <= WarmSamples; sample++)
                    Capture(fixture, input, sample, thread, probe >= 4096, raw, tails, totals, rows, utility);
            }
        }

        private static void Capture(string fixture, CombatCommandInput input, int sample, IntPtr thread, bool measureBytes,
            StreamWriter raw, StreamWriter tails, StreamWriter totals, List<Sample> rows, bool utility)
        {
            rows.Clear();
            var start = Stamp.Read(thread, measureBytes);
            using var session = Create(input, utility);
            rows.Add(start.Since(thread, measureBytes, "construct", 0, session.WorkPhase));
            while (!session.IsReady && session.WorkVersion < MaxSteps)
            {
                start = Stamp.Read(thread, measureBytes);
                session.Step();
                rows.Add(start.Since(thread, measureBytes, "step", session.WorkVersion, session.WorkPhase));
            }
            Assert.That(session.IsReady, Is.True, fixture);
            var decision = session.Decision.Kind;
            int previews = session.Diagnostics.PreviewCalls, nodes = session.Diagnostics.ExpandedNodes;
            long version = session.WorkVersion;
            start = Stamp.Read(thread, measureBytes);
            session.Dispose();
            rows.Add(start.Since(thread, measureBytes, "dispose", version, "DecisionDispose"));
            long ticks = 0;
            foreach (var row in rows)
            {
                ticks += row.Ticks;
                string line = FormattableString.Invariant(
                    $"{fixture},{sample},{row.Operation},{row.Version},{row.Phase},{row.Ticks},{row.GcDelta},{row.Cycles},{row.Bytes}");
                raw.WriteLine(line);
                if (row.Ticks * 2000 > Stopwatch.Frequency) tails.WriteLine(line);
            }
            totals.WriteLine(FormattableString.Invariant($"{fixture},{sample},{decision},{version},{previews},{nodes},{ticks}"));
        }

        private static EnemyDecisionSession Create(CombatCommandInput input, bool utility) => utility
            ? new EnemyDecisionSession(input, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default)
            : EnemyDecisionSessionTests.Create(input);

        private readonly struct Stamp
        {
            private readonly long _ticks;
            private readonly int _collections;
            private readonly ulong _cycles;
            private readonly long _bytes;

            private Stamp(long ticks, int collections, ulong cycles, long bytes)
            {
                _ticks = ticks;
                _collections = collections;
                _cycles = cycles;
                _bytes = bytes;
            }

            internal static Stamp Read(IntPtr thread, bool measureBytes)
            {
                int collections = Collections();
                ulong cycles = Cycles(thread);
                long bytes = measureBytes ? GC.GetAllocatedBytesForCurrentThread() : -1;
                return new Stamp(Stopwatch.GetTimestamp(), collections, cycles, bytes);
            }

            internal Sample Since(IntPtr thread, bool measureBytes, string operation, long version, string phase)
            {
                long ticks = Stopwatch.GetTimestamp() - _ticks;
                long bytes = measureBytes ? GC.GetAllocatedBytesForCurrentThread() - _bytes : -1;
                ulong cycles = Cycles(thread);
                return new Sample(operation, version, phase, ticks, Collections() - _collections,
                    _cycles > 0 && cycles >= _cycles ? (long)(cycles - _cycles) : -1, bytes);
            }

            private static int Collections()
            {
                int count = 0;
                for (int generation = 0; generation <= GC.MaxGeneration; generation++)
                    count += GC.CollectionCount(generation);
                return count;
            }
            private static ulong Cycles(IntPtr thread) => thread != IntPtr.Zero && QueryThreadCycleTime(thread, out var cycles) ? cycles : 0;
        }

        private readonly struct Sample
        {
            internal string Operation { get; }
            internal long Version { get; }
            internal string Phase { get; }
            internal long Ticks { get; }
            internal int GcDelta { get; }
            internal long Cycles { get; }
            internal long Bytes { get; }

            internal Sample(string operation, long version, string phase, long ticks, int gcDelta, long cycles, long bytes)
            {
                Operation = operation;
                Version = version;
                Phase = phase;
                Ticks = ticks;
                GcDelta = gcDelta;
                Cycles = cycles;
                Bytes = bytes;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryThreadCycleTime(IntPtr threadHandle, out ulong cycleTime);
    }
}
