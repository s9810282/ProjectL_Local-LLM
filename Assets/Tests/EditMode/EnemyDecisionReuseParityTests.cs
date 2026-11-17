using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyDecisionReuseParityTests
    {
        private const string CaptureMode = "reuse-baseline";
        private const string ReferenceDirectory = "docs/superpowers/reports/2026-11-13-enemy-ai-reuse-evidence";
        private static readonly int[] Partitions = { 1, 2, 31 };

        internal static EnemyDecisionSettings Tuning(bool current) => current
            ? new EnemyDecisionSettings(deathTradeThresholdPercent: 20, threatAversionPercent: 20, nextTurnApproachPercent: 200)
            : new EnemyDecisionSettings(deathTradeThresholdPercent: 50, threatAversionPercent: 50, nextTurnApproachPercent: 50);

        [Test, Explicit("Creates the immutable Phase 0 references.")]
        public void CaptureCurrentBehavior()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != CaptureMode)
                Assert.Ignore("Enable the dedicated reuse baseline capture explicitly.");
            Assert.That(File.Exists(ReferencePath(false)) || File.Exists(ReferencePath(true)), Is.False,
                "기존 동결 기준을 덮어쓰지 않습니다.");

            var defaults = Records(Tuning(false), 1, fullRegression: true).Select(record => (record.id, record.row)).ToArray();
            var current = Records(Tuning(true), 1, fullRegression: true).Select(record => (record.id, record.row)).ToArray();
            Directory.CreateDirectory(ReferenceDirectory);
            WriteNew(ReferencePath(false), defaults);
            WriteNew(ReferencePath(true), current);
            TestContext.WriteLine($"Captured {defaults.Length} default and {current.Length} current-tuning scenarios.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FrozenDecisionsAndResultsMatch(bool current)
        {
            var reference = File.ReadAllLines(ReferencePath(current));
            int scenarioCount = EnemyAiBenchmarkFixtures.Ids.Length * EnemyAiBenchmarkFixtures.Seeds.Length + 1;
            Assert.That(reference.Length, Is.EqualTo(scenarioCount + 2));
            Assert.That(reference[0], Is.EqualTo("{\"schemaVersion\":1,\"records\":["));
            Assert.That(reference[reference.Length - 1], Is.EqualTo("]}"));
            bool fullRegression = EnemyAiBenchmarkFixtures.FullRegressionEnabled;
            foreach (int partition in Partitions)
            {
                foreach (var (id, row, index) in Records(Tuning(current), partition, fullRegression))
                    RequireSame(reference[index + 1], RecordLine(row, index, scenarioCount), $"{id}, partition {partition}");
            }
        }

        [Test]
        public void BaselineRejectsOverwriteAndChangedIntent()
        {
            var input = CombatCommandTestData.Input();
            var original = EnemyDecision.Attack(input.ActorId, AttackAim.ForCell(new CellCoord(1, 1)), default);
            var changed = EnemyDecision.Attack(input.ActorId, AttackAim.ForCell(new CellCoord(1, 2)), default);
            var reference = new[] { ("altered-intent", EnemyActionAssessmentTests.Encode(original)) };
            string path = Path.Combine(Path.GetTempPath(), $"reuse-{Guid.NewGuid():N}.json");
            try
            {
                WriteNew(path, reference);
                string bytes = File.ReadAllText(path);
                var altered = new[] { ("altered-intent", EnemyActionAssessmentTests.Encode(changed)) };
                Assert.Throws<IOException>(() => WriteNew(path, altered));
                Assert.That(File.ReadAllText(path), Is.EqualTo(bytes));
                Assert.Throws<AssertionException>(() => RequireSame(File.ReadAllLines(path)[1], altered[0].Item2, altered[0].Item1));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void IrrelevantRemoteTraitDoesNotIncreaseCandidateHarm()
        {
            var input = WithAlly(Knockback(), 20, new CellCoord(0, 0), EntityKey.Unit_Demon);
            var remote = TraitCombatFixtures.Equip(input, EntityKey.Unit_MainCharacter, new HunterTraitSpec(2, 3));
            var baseline = Assess(input, allowPrecise: false);
            var candidate = Assess(remote, allowPrecise: false);
            Assert.That(candidate.Gaps, Is.EqualTo(baseline.Gaps | CombatAssessmentGap.RandomOutcome));
            Assert.That(candidate.PossibleAlliedHpLossPermille, Is.EqualTo(baseline.PossibleAlliedHpLossPermille).And.Zero);
            Assert.That(EnemyUtilityScorer.AttackBenefit(candidate), Is.EqualTo(EnemyUtilityScorer.AttackBenefit(baseline)));
        }

        [Test]
        public void UnrelatedAllyDoesNotInflateHarmBound()
        {
            var input = WithAlly(Knockback(), 20, new CellCoord(7, 4), EntityKey.Unit_Demon);
            var inReach = CliffFallTests.WithEnvironment(input, Mine(new CellCoord(6, 3)));
            var remote = WithAlly(inReach, 21, new CellCoord(0, 0), EntityKey.Unit_Demon);
            var local = Assess(inReach);
            var withRemote = Assess(remote);
            Assert.That(local.HasUnresolvedAlliedLoss, Is.True);
            Assert.That(local.PossibleAlliedHpLossPermille, Is.EqualTo(1000));
            Assert.That(withRemote.PossibleAlliedHpLossPermille, Is.EqualTo(local.PossibleAlliedHpLossPermille));
            Assert.That(EnemyUtilityScorer.AttackBenefit(withRemote), Is.EqualTo(EnemyUtilityScorer.AttackBenefit(local)));
        }

        [Test]
        public void OutsideReachSourceDoesNotCreateGlobalHarm()
        {
            var input = WithAlly(Knockback(), 20, new CellCoord(7, 4), EntityKey.Unit_Demon);
            var clear = Assess(input);
            var outside = Assess(CliffFallTests.WithEnvironment(input, Mine(new CellCoord(0, 7))));
            Assert.That(EnemyActionAssessmentTests.FactsKey(outside), Is.EqualTo(EnemyActionAssessmentTests.FactsKey(clear)));
            Assert.That(outside.IsPrecise, Is.True);
            Assert.That(outside.HasUnresolvedAlliedLoss, Is.False);
            Assert.That(outside.PossibleAlliedHpLossPermille, Is.Zero);
            Assert.That(EnemyUtilityScorer.AttackBenefit(outside), Is.EqualTo(EnemyUtilityScorer.AttackBenefit(clear)));
        }

        private static CombatCommandInput Knockback() =>
            CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));

        private static CombatCommandInput WithAlly(CombatCommandInput input, int id, CellCoord cell, EntityKey key) =>
            TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(id, key, cell, Owner.Player)).ToArray());

        private static MineSourceFact Mine(CellCoord cell) =>
            new MineSourceFact(71, cell, 1, 10, true, new[] { Owner.Player });

        private static EnemyScoreFacts Assess(CombatCommandInput input, bool allowPrecise = true)
        {
            string before = EnemyActionAssessmentTests.Encode(input);
            var context = new EnemyDecisionContext(input, Tuning(false));
            var action = EnemyDecision.Attack(input.ActorId, input.Aim, default);
            using var assessment = CombatActionAssessment.Begin(input, action, MineEnvironmentTests.Rules, context, allowPrecise);
            while (!assessment.IsComplete) assessment.Step();
            Assert.That(assessment.Admitted, Is.True);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            return assessment.Facts;
        }

        private static string ReferencePath(bool current) =>
            Path.Combine(ReferenceDirectory, current ? "baseline-current-tuning.json" : "baseline-default.json");

        private static void WriteNew(string path, IReadOnlyList<(string id, string row)> records)
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };
            writer.WriteLine("{\"schemaVersion\":1,\"records\":[");
            for (int index = 0; index < records.Count; index++)
                writer.WriteLine(RecordLine(records[index].row, index, records.Count));
            writer.WriteLine("]}");
        }

        private static string RecordLine(string row, int index, int count) => index + 1 < count ? row + "," : row;

        private static void RequireSame(string expected, string actual, string id) =>
            Assert.That(actual, Is.EqualTo(expected), id);

        private static IEnumerable<(string id, string row, int index)> Records(
            EnemyDecisionSettings settings, int partition, bool fullRegression)
        {
            int index = 0;
            foreach (string fixture in EnemyAiBenchmarkFixtures.Ids)
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                if (fullRegression || EnemyAiBenchmarkFixtures.IsRepresentative(fixture, seed))
                {
                    string id = $"{fixture}/{seed}";
                    yield return (id, Trace(id, EnemyAiBenchmarkFixtures.Build(fixture, seed), settings, partition, false), index);
                }
                index++;
            }
            const string rejectionId = "A8/1/stale-actor-rejection";
            yield return (rejectionId, Trace(rejectionId, EnemyAiBenchmarkFixtures.Build("A8", 1), settings, partition, true), index);
        }

        private static string Trace(string id, CombatCommandInput input, EnemyDecisionSettings settings, int partition, bool rejectFirst)
        {
            var rules = EnemyAiBenchmarkFixtures.Rules;
            string inputHash = SnapshotHash(input);
            var current = input;
            var attempts = new List<object>();
            bool testedRejection = false;
            for (int step = 0; step < 128; step++)
            {
                string before = SnapshotHash(current);
                var diagnostics = new EnemyDecisionDiagnostics(captureAlternatives: true);
                using var session = new EnemyDecisionSession(current, rules, settings, diagnostics);
                while (true)
                {
                    EnemyUtilityDecisionTests.Drain(session, partition);
                    var decision = session.Decision;
                    bool stale = rejectFirst && !testedRejection;
                    var executionInput = current;
                    if (stale)
                    {
                        Assert.That(decision.Kind, Is.Not.EqualTo(EnemyDecisionKind.EndEnemyPhase), id);
                        executionInput = TraitCombatFixtures.Characters(current,
                            current.Characters.Where(unit => unit.EntityId != decision.ActorId).ToArray());
                    }

                    var transition = CombatCommandCalculator.AdvancePrepared(executionInput, decision, rules);
                    if (stale)
                    {
                        Assert.That(transition.Accepted, Is.False, id);
                        testedRejection = true;
                    }
                    attempts.Add(new object[] { decision, stale, diagnostics.PreviewCalls, diagnostics.FollowupPreviewCalls,
                        ActorRecords(diagnostics), transition.Accepted, transition.Reason, transition.Outcome,
                        transition.Events, SnapshotHash(transition) });
                    Assert.That(SnapshotHash(current), Is.EqualTo(before), id + " input mutated");
                    if (!transition.Accepted)
                    {
                        Assert.That(decision.Kind, Is.Not.EqualTo(EnemyDecisionKind.EndEnemyPhase), id);
                        Assert.That(diagnostics.RejectedAttempts, Is.LessThan(current.Characters.Count * 2), id);
                        session.RejectSelected(transition.Reason);
                        continue;
                    }

                    current = CombatCommandComposition.Project(transition);
                    if (transition.Outcome.IsStageEnded || current.State.Turn.ControllerPhase != CombatControllerPhase.Enemy)
                    {
                        Assert.That(testedRejection, Is.EqualTo(rejectFirst), id);
                        Assert.That(SnapshotHash(input), Is.EqualTo(inputHash), id + " initial input mutated");
                        return EnemyActionAssessmentTests.Encode(new object[] { id, inputHash, attempts,
                            current.State.ItemRandomState, current.State.EnvironmentProgress.RandomState, SnapshotHash(current) });
                    }
                    break;
                }
            }
            throw new InvalidOperationException(id + " exceeded the finite publication bound.");
        }

        private static object[] ActorRecords(EnemyDecisionDiagnostics diagnostics) =>
            Enumerable.Range(0, diagnostics.ActorRecordCount).Select(index =>
            {
                var actor = diagnostics.GetActorRecord(index);
                return new object[] { actor.ActorId, actor.HoldReason, actor.HoldTier, actor.HoldLethal, Terms(actor.HoldTerms),
                    actor.Candidates.Select(candidate => new object[] { candidate.Action, candidate.Tier,
                        candidate.Lethal, candidate.Precise, Terms(candidate.Terms) }).ToArray() };
            }).ToArray();

        private static long[] Terms(EnemyScoreTerms terms) => new[]
        {
            terms.Attack, terms.Hazard, terms.PlayerRisk, terms.Progress, terms.Followup, terms.NextTurn, terms.Bind, terms.Total
        };

        private static string SnapshotHash(object facts) =>
            EnemyAiBenchmarkFixtures.Hash(EnemyActionAssessmentTests.Encode(facts));
    }
}
