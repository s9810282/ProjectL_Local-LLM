using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyAiReuseMeasurements
    {
        private static readonly string[] Fixtures = { "N8", "A8", "W8", "U8" };
        private static readonly string[] VisibilityFixtures = { "U8", "H8", "X8" };
        private const int Warmups = 3;
        private const int Samples = 12;

        [Test, Explicit("Requires a dedicated reuse measurement run.")]
        public void CaptureReuseWork()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "reuse-work")
                Assert.Ignore("Enable the dedicated reuse work capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            var hook = typeof(EnemyAttackReach).GetField("ReuseWorkProbe", BindingFlags.Static | BindingFlags.NonPublic);
            bool counts = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_REUSE_COUNTS") == "1";
            Assert.That(hook != null, Is.EqualTo(counts), "계측 패치의 사용 여부와 측정 모드가 일치해야 합니다.");
            if (counts)
                CaptureCounts(folder, hook);
            else
                CaptureTiming(folder, Fixtures);
        }

        [Test, Explicit("Requires a dedicated assessment visibility measurement run.")]
        public void CaptureAssessmentVisibilityWork()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "reuse-visibility")
                Assert.Ignore("Enable the dedicated assessment visibility capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            var hook = typeof(EnemyDecisionContext).GetField("AssessmentVisibilityProbe", BindingFlags.Static | BindingFlags.NonPublic);
            bool counts = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_REUSE_COUNTS") == "1";
            Assert.That(hook != null, Is.EqualTo(counts), "계측 패치의 사용 여부와 측정 모드가 일치해야 합니다.");
            if (counts)
                CaptureVisibilityCounts(folder, hook);
            else
                CaptureTiming(folder, VisibilityFixtures);
        }

        private static void CaptureVisibilityCounts(string folder, FieldInfo hook)
        {
            using var writer = File.CreateText(Path.Combine(folder, "assessment-visibility.csv"));
            writer.WriteLine("tuning,fixture,seed,requests,builds,previews,capHits,intentSha256");
            foreach (bool current in new[] { false, true })
            foreach (string fixture in VisibilityFixtures)
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                int requests = 0, builds = 0;
                using var session = new EnemyDecisionSession(EnemyAiBenchmarkFixtures.Build(fixture, seed),
                    EnemyAiBenchmarkFixtures.Rules, EnemyDecisionReuseParityTests.Tuning(current));
                hook.SetValue(null, (Action<bool>)(isBuild =>
                {
                    if (isBuild) builds++;
                    else requests++;
                }));
                try
                {
                    EnemyUtilityDecisionTests.Drain(session);
                }
                finally
                {
                    hook.SetValue(null, null);
                }
                Assert.That(requests, Is.Positive);
                Assert.That(builds, Is.InRange(1, requests));
                writer.WriteLine(FormattableString.Invariant(
                    $"{TuningName(current)},{fixture},{seed},{requests},{builds},{session.Diagnostics.PreviewCalls},{session.Diagnostics.CapHits},{IntentHash(session.Decision)}"));
            }
        }

        private static void CaptureTiming(string folder, IReadOnlyList<string> fixtures)
        {
            long beforeProbe = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(new byte[4096]);
            long calibration = GC.GetAllocatedBytesForCurrentThread() - beforeProbe;
            bool bytesAvailable = calibration >= 4096;
            File.WriteAllLines(Path.Combine(folder, "reuse-runtime.txt"), new[]
            {
                $"frequency={Stopwatch.Frequency}",
                $"runtime={Environment.Version}",
                $"os={Environment.OSVersion}",
                $"allocationCalibration={calibration}",
                $"allocationSupported={bytesAvailable}",
                $"warmups={Warmups}",
                $"samplesPerSeed={Samples}"
            });

            using var writer = File.CreateText(Path.Combine(folder, "reuse-timing.csv"));
            writer.WriteLine("tuning,fixture,seed,sample,preparationTicks,totalTicks,allocationBytes,gc0,previews,capHits,intentSha256");
            foreach (bool current in new[] { false, true })
            foreach (string fixture in fixtures)
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                var settings = EnemyDecisionReuseParityTests.Tuning(current);
                var input = EnemyAiBenchmarkFixtures.Build(fixture, seed);
                for (int repeat = -Warmups - 1; repeat < Samples; repeat++)
                {
                    int gc = GC.CollectionCount(0);
                    long bytes = bytesAvailable ? GC.GetAllocatedBytesForCurrentThread() : 0;
                    long start = Stopwatch.GetTimestamp();
                    using var session = new EnemyDecisionSession(input, EnemyAiBenchmarkFixtures.Rules, settings);
                    EnemyUtilityDecisionTests.Drain(session);
                    var decision = session.Decision;
                    long prepared = Stopwatch.GetTimestamp();
                    var result = CombatCommandCalculator.AdvancePrepared(input, decision, EnemyAiBenchmarkFixtures.Rules);
                    long end = Stopwatch.GetTimestamp();
                    long allocated = bytesAvailable ? GC.GetAllocatedBytesForCurrentThread() - bytes : -1;
                    int collections = GC.CollectionCount(0) - gc;
                    Assert.That(result.Accepted, Is.True, result.Reason);
                    if (repeat >= 0 || repeat == -Warmups - 1)
                        writer.WriteLine(FormattableString.Invariant(
                            $"{TuningName(current)},{fixture},{seed},{repeat},{prepared - start},{end - start},{allocated},{collections},{session.Diagnostics.PreviewCalls},{session.Diagnostics.CapHits},{IntentHash(decision)}"));
                }
            }
        }

        private static void CaptureCounts(string folder, FieldInfo hook)
        {
            using var reach = File.CreateText(Path.Combine(folder, "reuse-reach.csv"));
            using var visibility = File.CreateText(Path.Combine(folder, "reuse-visibility.csv"));
            using var decisions = File.CreateText(Path.Combine(folder, "reuse-count-intents.csv"));
            reach.WriteLine("tuning,fixture,seed,scope,kind,positionAttempts,uniquePositions,aimAttempts,uniqueAimKeys,damageBuilds,damagePositions,selectionBuilds,selectionPositions,deliveries,uniqueDeliveryKeys,deliveryResolves,nullDeliveries,targetMisses,peakMoved,peakProjected,peakPrerequisites");
            visibility.WriteLine("tuning,fixture,seed,context,builds,uniqueKeys,consecutiveRepeats");
            decisions.WriteLine("tuning,fixture,seed,intentSha256");
            foreach (bool current in new[] { false, true })
            {
                foreach (string fixture in Fixtures)
                foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
                {
                    var recorder = new Recorder();
                    using var session = new EnemyDecisionSession(EnemyAiBenchmarkFixtures.Build(fixture, seed),
                        EnemyAiBenchmarkFixtures.Rules, EnemyDecisionReuseParityTests.Tuning(current));
                    WithProbe(hook, recorder, () => EnemyUtilityDecisionTests.Drain(session));
                    string label = $"{TuningName(current)},{fixture},{seed}";
                    recorder.Write(label, reach, visibility);
                    decisions.WriteLine($"{label},{IntentHash(session.Decision)}");
                }
            }

            var capped = new Recorder();
            WithProbe(hook, capped, CountPiercingCap);
            capped.Write("focused,piercing-cap-1,1", reach, visibility);
            var scope = capped.Scopes.Values.Single();
            Assert.That(scope.Deliveries, Is.GreaterThan(scope.DeliveryKeys.Count));
            Assert.That(scope.NullDeliveries, Is.Positive);
            Assert.That(scope.TargetMisses, Is.Positive);
        }

        private static void WithProbe(FieldInfo hook, Recorder recorder, Action capture)
        {
            hook.SetValue(null, (Action<string, object, object, CellCoord, AttackAim, int>)recorder.Record);
            try
            {
                capture();
            }
            finally
            {
                hook.SetValue(null, null);
            }
        }

        private static void CountPiercingCap()
        {
            var input = PiercingShotTestData.Input(cap: 1);
            var actor = input.Characters[0];
            var environment = input.State.Environment;
            var geometry = new EnemyDecisionContext.ObservedGeometry(environment,
                WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, environment, input.EnvironmentBindings,
                    input.State.WallBlocking), AttackElevation.From(environment, input.EnvironmentBindings));
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            foreach (var id in new[] { 9, 2 })
            {
                var reach = new EnemyAttackReach(input, actor, new EntityId(id), environment, geometry, scope);
                foreach (var step in reach.TargetSteps()) { }
                bool hit = reach.From(actor.Anchor.Value);
                Assert.That(hit, Is.EqualTo(id == 9));
            }
        }

        private static string TuningName(bool current) => current ? "20/20/200" : "50/50/50";

        private static string IntentHash(EnemyDecision decision) =>
            EnemyAiBenchmarkFixtures.Hash(EnemyActionAssessmentTests.Encode(decision));

        private sealed class Recorder
        {
            internal readonly Dictionary<object, ReachCounts> Scopes = new Dictionary<object, ReachCounts>();
            private readonly Dictionary<object, ReachCounts> _projected = new Dictionary<object, ReachCounts>();
            private readonly Dictionary<object, VisibilityCounts> _visibility = new Dictionary<object, VisibilityCounts>();

            internal void Record(string operation, object scope, object input, CellCoord cell, AttackAim aim, int value)
            {
                if (operation == "visibility")
                {
                    if (!_visibility.TryGetValue(scope, out var visibility))
                        _visibility.Add(scope, visibility = new VisibilityCounts());
                    visibility.Add(input, value);
                    return;
                }

                if (operation == "position" && !Scopes.ContainsKey(scope))
                    Scopes.Add(scope, new ReachCounts(scope is EnemyAttackReach.Relocations));
                ReachCounts counts;
                if (scope != null)
                    counts = Scopes[scope];
                else if (!_projected.TryGetValue(input, out counts))
                    return;
                var key = (cell, aim);
                switch (operation)
                {
                    case "position":
                        counts.PositionAttempts++;
                        counts.Positions.Add(cell);
                        counts.PeakMoved = Math.Max(counts.PeakMoved, value);
                        break;
                    case "aim":
                        counts.AimAttempts++;
                        counts.Aims.Add(key);
                        break;
                    case "reach-input":
                        _projected[input] = counts;
                        counts.PeakProjected = Math.Max(counts.PeakProjected, value);
                        break;
                    case "prerequisites":
                        counts.PeakPrerequisites = Math.Max(counts.PeakPrerequisites, value);
                        break;
                    case "damage":
                        counts.DamageBuilds++;
                        counts.DamagePositions.Add(cell);
                        break;
                    case "selection":
                        counts.SelectionBuilds++;
                        counts.SelectionPositions.Add(cell);
                        break;
                    case "delivery":
                        counts.Deliveries++;
                        counts.DeliveryKeys.Add(key);
                        break;
                    case "resolve":
                        counts.Resolves++;
                        break;
                    case "null-delivery":
                        counts.NullDeliveries++;
                        break;
                    case "target-miss":
                        counts.TargetMisses++;
                        break;
                }
            }

            internal void Write(string label, StreamWriter reach, StreamWriter visibility)
            {
                int index = 0;
                foreach (var scope in Scopes.Values)
                    reach.WriteLine(FormattableString.Invariant(
                        $"{label},{++index},{(scope.Shared ? "relocations" : "fallback")},{scope.PositionAttempts},{scope.Positions.Count},{scope.AimAttempts},{scope.Aims.Count},{scope.DamageBuilds},{scope.DamagePositions.Count},{scope.SelectionBuilds},{scope.SelectionPositions.Count},{scope.Deliveries},{scope.DeliveryKeys.Count},{scope.Resolves},{scope.NullDeliveries},{scope.TargetMisses},{scope.PeakMoved},{scope.PeakProjected},{scope.PeakPrerequisites}"));
                index = 0;
                foreach (var projection in _visibility.Values)
                    visibility.WriteLine(FormattableString.Invariant(
                        $"{label},{++index},{projection.Builds},{projection.Keys.Count},{projection.ConsecutiveRepeats}"));
            }
        }

        private sealed class ReachCounts
        {
            internal readonly bool Shared;
            internal readonly HashSet<CellCoord> Positions = new HashSet<CellCoord>();
            internal readonly HashSet<CellCoord> DamagePositions = new HashSet<CellCoord>();
            internal readonly HashSet<CellCoord> SelectionPositions = new HashSet<CellCoord>();
            internal readonly HashSet<(CellCoord, AttackAim)> Aims = new HashSet<(CellCoord, AttackAim)>();
            internal readonly HashSet<(CellCoord, AttackAim)> DeliveryKeys = new HashSet<(CellCoord, AttackAim)>();
            internal int PositionAttempts;
            internal int AimAttempts;
            internal int DamageBuilds;
            internal int SelectionBuilds;
            internal int Deliveries;
            internal int Resolves;
            internal int NullDeliveries;
            internal int TargetMisses;
            internal int PeakMoved;
            internal int PeakProjected;
            internal int PeakPrerequisites;

            internal ReachCounts(bool shared) => Shared = shared;
        }

        private sealed class VisibilityCounts
        {
            internal readonly HashSet<(object, int)> Keys = new HashSet<(object, int)>();
            internal int Builds;
            internal int ConsecutiveRepeats;
            private (object, int) _previous;

            internal void Add(object source, int observer)
            {
                Builds++;
                var key = (source, observer);
                if (_previous == key) ConsecutiveRepeats++;
                Keys.Add(key);
                _previous = key;
            }
        }
    }
}
