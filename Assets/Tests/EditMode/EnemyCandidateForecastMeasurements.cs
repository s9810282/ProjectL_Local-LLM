using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyCandidateForecastMeasurements
    {
        [Test, Explicit("Requires an isolated Unity run and PROJECTL_ENEMY_AI_RAW.")]
        public void Capture()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "phase-5")
                Assert.Ignore("Enable the dedicated Phase 5 capture.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(new byte[4096]);
            long allocationProbe = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            File.WriteAllLines(Path.Combine(folder, "phase-5-runtime.txt"), new[] {
                $"frequency={Stopwatch.Frequency}", $"allocationProbeBytes={allocationProbe}",
                $"allocationSupported={allocationProbe >= 4096}", "seed=1", "warmRepeats=10", "renderer=none"
            });
            using var totals = File.CreateText(Path.Combine(folder, "phase-5-totals.csv"));
            using var steps = File.CreateText(Path.Combine(folder, "phase-5-steps.csv"));
            totals.WriteLine("fixture,sample,boardCells,forecastFacts,threatCells,attacks,moves,reachNodes,refineNodes,truncated,routeCopies,fullPreviews");
            steps.WriteLine("fixture,sample,owner,operation,ticks");
            foreach (string fixture in new[] { "N5", "W8", "U8", "A8", "X8", "H8" })
            {
                var input = EnemyAiBenchmarkFixtures.Build(fixture, 1);
                var actor = input.Characters.Single(unit => unit.EntityId == new EntityId(11));
                for (int repeat = 0; repeat <= 10; repeat++)
                {
                    var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                    var rows = new List<(string owner, string operation, long ticks)>();
                    long start = Stopwatch.GetTimestamp();
                    using var forecastBuilder = new StageHazardForecast.Builder(input);
                    Add("forecast", "construct", start);
                    Drain("forecast", () => forecastBuilder.IsComplete, forecastBuilder.Step);
                    start = Stopwatch.GetTimestamp();
                    var forecast = forecastBuilder.Build();
                    Add("forecast", "read", start);
                    start = Stopwatch.GetTimestamp();
                    forecastBuilder.Dispose();
                    Add("forecast", "dispose", start);

                    EnemyThreatField field = null;
                    using (var cursor = context.ThreatSteps(value => field = value).GetEnumerator())
                    {
                        bool more;
                        do
                        {
                            start = Stopwatch.GetTimestamp();
                            more = cursor.MoveNext();
                            Add("threat", "step", start);
                        } while (more);
                    }
                    Assert.That(field, Is.Not.Null);

                    start = Stopwatch.GetTimestamp();
                    using var candidates = new EnemyActionCandidates(context, actor, forecast);
                    Add("candidates", "construct", start);
                    Drain("candidates", () => candidates.IsComplete, candidates.Step);
                    start = Stopwatch.GetTimestamp();
                    int attacks = candidates.Attacks.Count;
                    int moves = candidates.Moves.Count;
                    Add("candidates", "read", start);
                    foreach (var candidate in candidates.Moves)
                    {
                        start = Stopwatch.GetTimestamp();
                        _ = forecast.Evaluate(actor, candidate.Path.Cells, candidate.Path.Endpoint);
                        Add("forecast", "lookup", start);
                        start = Stopwatch.GetTimestamp();
                        _ = field.Evaluate(actor, candidate.Path.Endpoint);
                        Add("threat", "lookup", start);
                    }

                    Assert.That(context.PreviewCount, Is.Zero);
                    int boardCells = checked(input.State.BoardWidth * input.State.BoardHeight);
                    Assert.That(field.CoveredCellCount, Is.LessThanOrEqualTo(boardCells));
                    Assert.That(attacks, Is.LessThanOrEqualTo(context.Settings.AttackCandidateLimit));
                    Assert.That(moves, Is.LessThanOrEqualTo(context.Settings.MoveCandidateLimit));
                    Assert.That(candidates.RefinementExpandedNodes, Is.LessThanOrEqualTo(context.Settings.AlternateRouteNodeLimit));
                    totals.WriteLine(FormattableString.Invariant(
                        $"{fixture},{repeat},{boardCells},{forecast.Facts.Count},{field.CoveredCellCount},{attacks},{moves},{candidates.MandatoryExpandedNodes},{candidates.RefinementExpandedNodes},{candidates.RefinementTruncated},{candidates.RouteReconstructionCount},{context.PreviewCount}"));
                    start = Stopwatch.GetTimestamp();
                    candidates.Dispose();
                    Add("candidates", "dispose", start);
                    foreach (var row in rows)
                        steps.WriteLine(FormattableString.Invariant($"{fixture},{repeat},{row.owner},{row.operation},{row.ticks}"));

                    void Add(string owner, string operation, long before)
                    {
                        long ticks = Stopwatch.GetTimestamp() - before;
                        rows.Add((owner, operation, ticks));
                    }

                    void Drain(string owner, Func<bool> complete, Action step)
                    {
                        for (int i = 0; i < 500_000 && !complete(); i++)
                        {
                            long before = Stopwatch.GetTimestamp();
                            step();
                            Add(owner, "step", before);
                        }
                        Assert.That(complete(), Is.True, owner);
                    }
                }
            }
        }
    }
}
