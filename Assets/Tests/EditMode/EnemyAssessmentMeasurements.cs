using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyAssessmentMeasurements
    {
        [Test, Explicit("Dedicated Phase 4 Core measurement")]
        public void CaptureAssessmentAndForecastCosts()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "phase4-assessment")
                Assert.Ignore("Enable the dedicated capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            long beforeProbe = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(new byte[4096]);
            long probe = GC.GetAllocatedBytesForCurrentThread() - beforeProbe;
            bool allocations = probe >= 4096;
            File.WriteAllLines(Path.Combine(folder, "assessment-runtime.txt"), new[] {
                $"frequency={Stopwatch.Frequency}", $"runtime={Environment.Version}",
                $"allocationProbeBytes={probe}", $"allocationSupported={allocations}", "warmSamples=10", "sample0=first observation"
            });
            using var raw = new StreamWriter(new GZipStream(File.Create(Path.Combine(folder, "assessment-steps.csv.gz")), CompressionLevel.Fastest));
            using var totals = File.CreateText(Path.Combine(folder, "assessment-totals.csv"));
            using var whole = File.CreateText(Path.Combine(folder, "assessment-whole.csv"));
            raw.WriteLine("fixture,sample,operation,workVersion,phase,ticks,gcDelta,bytes");
            totals.WriteLine("fixture,sample,admitted,precise,gaps,previews,steps,totalTicks");
            whole.WriteLine("fixture,sample,ticks,gcDelta,bytes");
            foreach (var vector in AttackPreviewParityTests.Vectors().Concat(EnemyActionAssessmentTests.RouteCases()))
            {
                // 두 칸 도약을 넣은 거절 fixture는 EnemyDecision 자체가 생성될 수 없다.
                if (vector.Id == "invalid") continue;
                var input = vector.Input;
                var candidate = input.Kind == CombatCommandKind.Move
                    ? EnemyDecision.Move(input.ActorId, input.Path, default)
                    : EnemyDecision.Attack(input.ActorId, input.Aim, default);
                string expected = null;
                for (int sample = 0; sample <= 10; sample++)
                {
                    var rows = new List<Row>();
                    var stamp = Stamp.Read(allocations);
                    var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                    var work = CombatActionAssessment.Begin(input, candidate, vector.Rules, context);
                    rows.Add(stamp.Since(allocations, "construct", 0, "Begin"));
                    while (!work.IsComplete)
                    {
                        string phase = work.Phase;
                        long version = work.WorkVersion;
                        stamp = Stamp.Read(allocations);
                        work.Step();
                        rows.Add(stamp.Since(allocations, "step", version, phase));
                        Assert.That(work.WorkVersion, Is.LessThan(1000000), vector.Id);
                    }
                    stamp = Stamp.Read(allocations);
                    bool admitted = work.Admitted;
                    var facts = work.Facts;
                    var moved = work.MovedFacts;
                    rows.Add(stamp.Since(allocations, "read", work.WorkVersion, "Result"));
                    long steps = work.WorkVersion;
                    stamp = Stamp.Read(allocations);
                    work.Dispose();
                    rows.Add(stamp.Since(allocations, "dispose", steps, "Dispose"));
                    long total = rows.Sum(row => row.Ticks);
                    string value = admitted + "/" + EnemyActionAssessmentTests.FactsKey(facts) + "/" +
                        (moved == null ? "" : moved.Characters.First(c => c.EntityId == candidate.ActorId).Anchor.ToString());
                    if (expected == null) expected = value;
                    else Assert.That(value, Is.EqualTo(expected), vector.Id);
                    totals.WriteLine($"{vector.Id},{sample},{admitted},{facts.IsPrecise},{(int)facts.Gaps},{context.PreviewCount},{steps},{total}");
                    foreach (var row in rows) row.Write(raw, vector.Id, sample);
                }
                for (int sample = 1; sample <= 10; sample++)
                {
                    var stamp = Stamp.Read(allocations);
                    var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                    bool admitted = CombatActionAssessment.TryAssess(input, candidate, vector.Rules, context, out var facts, out var moved);
                    var row = stamp.Since(allocations, "whole", 0, "");
                    string value = admitted + "/" + EnemyActionAssessmentTests.FactsKey(facts) + "/" +
                        (moved == null ? "" : moved.Characters.First(c => c.EntityId == candidate.ActorId).Anchor.ToString());
                    Assert.That(value, Is.EqualTo(expected), vector.Id);
                    whole.WriteLine($"{vector.Id},{sample},{row.Ticks},{row.Gc},{row.Bytes}");
                }
            }
            using var forecastRows = File.CreateText(Path.Combine(folder, "forecast-costs.csv"));
            forecastRows.WriteLine("seed,sample,operation,workVersion,ticks,gcDelta,bytes");
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                var input = EnemyAiBenchmarkFixtures.Build("R8", seed);
                string expected = null;
                for (int sample = 0; sample <= 20; sample++)
                {
                    var rows = new List<Row>();
                    var stamp = Stamp.Read(allocations);
                    var builder = new StageHazardForecast.Builder(input);
                    rows.Add(stamp.Since(allocations, "construct", 0, ""));
                    while (!builder.IsComplete)
                    {
                        stamp = Stamp.Read(allocations);
                        builder.Step();
                        rows.Add(stamp.Since(allocations, "step", builder.WorkVersion, ""));
                    }
                    stamp = Stamp.Read(allocations);
                    var forecast = builder.Build();
                    rows.Add(stamp.Since(allocations, "build", builder.WorkVersion, ""));
                    string value = EnemyHazardForecastTests.EncodeFacts(forecast);
                    if (expected == null) expected = value;
                    else Assert.That(value, Is.EqualTo(expected));
                    foreach (var actor in input.Characters.Where(c => c.Owner == Owner.Enemy))
                    {
                        for (int x = 0; x < input.State.BoardWidth; x++)
                        {
                            stamp = Stamp.Read(allocations);
                            var exposure = forecast.Evaluate(actor, Array.Empty<CellCoord>(), new CellCoord(x, actor.Anchor.Value.Y));
                            rows.Add(stamp.Since(allocations, "lookup", actor.EntityId.Value, ""));
                            Assert.That(exposure.KnownHpLoss, Is.LessThanOrEqualTo(actor.Health.CurrentHealth));
                        }
                    }
                    stamp = Stamp.Read(allocations);
                    builder.Dispose();
                    rows.Add(stamp.Since(allocations, "dispose", 0, ""));
                    foreach (var row in rows)
                        forecastRows.WriteLine($"{seed},{sample},{row.Operation},{row.Version},{row.Ticks},{row.Gc},{row.Bytes}");
                }
            }
        }

        private readonly struct Stamp
        {
            private readonly long _ticks, _bytes;
            private readonly int _gc;
            private Stamp(long ticks, long bytes, int gc) { _ticks = ticks; _bytes = bytes; _gc = gc; }
            internal static Stamp Read(bool allocations) => new Stamp(Stopwatch.GetTimestamp(),
                allocations ? GC.GetAllocatedBytesForCurrentThread() : -1, Collections());
            internal Row Since(bool allocations, string operation, long version, string phase)
            {
                long end = Stopwatch.GetTimestamp();
                return new Row(operation, version, phase, end - _ticks, Collections() - _gc,
                    allocations ? GC.GetAllocatedBytesForCurrentThread() - _bytes : -1);
            }
            private static int Collections() => GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        }

        private readonly struct Row
        {
            internal readonly string Operation, Phase;
            internal readonly long Version, Ticks, Bytes;
            internal readonly int Gc;
            internal Row(string operation, long version, string phase, long ticks, int gc, long bytes)
            { Operation = operation; Version = version; Phase = phase; Ticks = ticks; Gc = gc; Bytes = bytes; }
            internal void Write(TextWriter writer, string fixture, int sample) =>
                writer.WriteLine($"{fixture},{sample},{Operation},{Version},{Phase},{Ticks},{Gc},{Bytes}");
        }
    }
}
