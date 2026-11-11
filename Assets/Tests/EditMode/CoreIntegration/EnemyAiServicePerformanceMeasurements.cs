using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class EnemyAiServicePerformanceMeasurements
    {
        private const int SideLimit = 128;

        [Test, Explicit("Requires an isolated Unity run and PROJECTL_ENEMY_AI_RAW.")]
        public void CaptureOpenEncounters()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "service")
                Assert.Ignore("Enable the dedicated CoreIntegration capture explicitly.");
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty);
            Directory.CreateDirectory(folder);
            using var decisions = File.CreateText(Path.Combine(folder, "service-decisions.csv"));
            using var sides = File.CreateText(Path.Combine(folder, "service-sides.csv"));
            decisions.WriteLine("fixture,seed,repeat,decision,ticks,actor,outcome");
            sides.WriteLine("fixture,seed,repeat,decisionCount,advanceTicks");

            var elapsed = new long[SideLimit];
            var actors = new int[SideLimit];
            var outcomes = new CombatCommandKind[SideLimit];
            foreach (string fixture in new[] { "N5", "N6", "N8" })
            {
                foreach (int seed in new[] { 1, 7, 19, 41, 97 })
                {
                    for (int repeat = 0; repeat < 4; repeat++)
                    {
                        using var battle = EnemyAiPreparationTestData.Create(fixture, seed);
                        var buffers = battle.Buffers;
                        int count = 0;
                        while (buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase == CombatControllerPhase.Enemy &&
                               count < SideLimit)
                        {
                            int sequence = buffers.CombatStateReadSink.LatestResultSequence;
                            long start = Stopwatch.GetTimestamp();
                            bool published = battle.Core.CombatAdvance.AdvanceCombat();
                            elapsed[count] = Stopwatch.GetTimestamp() - start;
                            Assert.That(published, Is.True);
                            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                            Assert.That(result.Accepted, Is.True, result.Reason);
                            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
                            actors[count] = result.ActorId.Value;
                            outcomes[count++] = result.CommandKind;
                            if (buffers.CombatStateReadSink.Phase == CombatFlowPhase.Finished) break;
                        }
                        Assert.That(count, Is.InRange(1, SideLimit - 1), $"{fixture}/{seed}: enemy phase did not finish.");
                        long total = 0;
                        for (int i = 0; i < count; i++)
                        {
                            total += elapsed[i];
                            decisions.WriteLine(FormattableString.Invariant($"{fixture},{seed},{repeat},{i + 1},{elapsed[i]},{actors[i]},{outcomes[i]}"));
                        }
                        sides.WriteLine(FormattableString.Invariant($"{fixture},{seed},{repeat},{count},{total}"));
                    }
                }
            }
            File.WriteAllText(Path.Combine(folder, "service-runtime.txt"),
                Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture) + "\n");
        }
    }
}
