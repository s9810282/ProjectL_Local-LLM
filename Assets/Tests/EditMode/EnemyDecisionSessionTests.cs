using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyDecisionSessionTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;

        [Test]
        public void DecisionFactories_RejectInconsistentPayloadAndCompareCompleteIntent()
        {
            var actor = new EntityId(1);
            var from = CellCoord.Zero;
            var to = from + CellCoord.Up + CellCoord.Right;
            var left = new MovementPath(new[] { from, CellCoord.Up, to });
            var right = new MovementPath(new[] { from, CellCoord.Right, to });
            Assert.Throws<ArgumentException>(() => EnemyDecision.Attack(default, AttackAim.ForAll(), default));
            Assert.Throws<ArgumentException>(() => EnemyDecision.Attack(actor, default, default));
            Assert.Throws<ArgumentNullException>(() => EnemyDecision.Move(actor, null, default));
            Assert.Throws<ArgumentException>(() => EnemyDecision.Move(actor, new MovementPath(new[] { from }), default));
            Assert.Throws<ArgumentException>(() => EnemyDecision.Move(actor, new MovementPath(new[] { from, to }), default));
            Assert.Throws<ArgumentException>(() => EnemyDecision.Move(actor,
                new MovementPath(new[] { from, CellCoord.Up, from }), default));
            Assert.That(EnemyDecision.Move(actor, left, default), Is.EqualTo(
                EnemyDecision.Move(actor, new MovementPath(left.Cells), default)));
            Assert.That(EnemyDecision.Move(actor, left, default), Is.Not.EqualTo(EnemyDecision.Move(actor, right, default)));
            Assert.That(EnemyDecision.Move(actor, left, default), Is.Not.EqualTo(EnemyDecision.Move(actor, left, new EntityId(3))));
            Assert.That(EnemyDecision.EndEnemyPhase().ActorId.IsEmpty, Is.True);
            Assert.That(EnemyDecision.EndEnemyPhase().Path, Is.Null);
        }

        [Test]
        public void Settings_DefaultsAndRelationshipsAreStable()
        {
            var settings = EnemyDecisionSettings.Default;
            Assert.That(settings, Is.SameAs(EnemyDecisionSettings.Default));
            Assert.That(new[] { settings.AttackCandidateLimit, settings.MoveCandidateLimit, settings.RefinedMoveLimit,
                settings.FollowupAimLimit, settings.ActorPreviewLimit, settings.DecisionPreviewLimit,
                settings.AlternateRouteNodeLimit }, Is.EqualTo(new[] { 4, 6, 2, 1, 8, 16, 256 }), "R-4: 이동 후보 기본 K = 6");
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(attackCandidateLimit: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(moveCandidateLimit: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(followupAimLimit: 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(decisionPreviewLimit: 7));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(alternateRouteNodeLimit: 0));
        }

        [Test]
        public void DecisionBeforeReadyThrows_AndReadyStepIsIdempotent()
        {
            using var session = Create(EnemyAiBenchmarkFixtures.Build("N5", 1));
            Assert.Throws<InvalidOperationException>(() => { _ = session.Decision; });
            Assert.Throws<InvalidOperationException>(() => session.RejectSelected());
            Assert.That(session.WorkVersion, Is.Zero);
            Assert.That(session.Diagnostics.PreviewCalls, Is.Zero);
            Drain(session, 1);
            var intent = session.Decision;
            long version = session.WorkVersion;
            session.Step();
            Assert.That(session.Decision, Is.SameAs(intent));
            Assert.That(session.WorkVersion, Is.EqualTo(version));
        }

        [Test]
        public void CancellingStartedPreviewReleasesItWithoutMutatingTheSource()
        {
            var input = EnemyAiBenchmarkFixtures.Build("N5", 7);
            string before = EnemyAiBenchmarkFixtures.Describe(input, true);
            using var session = Create(input);
            while (session.Diagnostics.PreviewCalls == 0 && !session.IsReady && session.WorkVersion < 100_000)
                session.Step();
            Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(1));
            Assert.That(session.IsReady, Is.False);
            for (int i = 0; i < 10; i++)
            {
                session.Step();
                Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(1));
                Assert.That(session.IsReady, Is.False);
            }
            session.Dispose();
            session.Dispose();
            Assert.Throws<ObjectDisposedException>(() => session.Step());
            Assert.Throws<ObjectDisposedException>(() => session.RejectSelected());
            Assert.That(EnemyAiBenchmarkFixtures.Describe(input, true), Is.EqualTo(before));
            using var replacement = Create(input);
            Drain(replacement, 31);
            Assert.That(replacement.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Attack));
        }

        [TestCase("N5", EnemyDecisionKind.Attack)]
        [TestCase("W8", EnemyDecisionKind.Move)]
        [TestCase("U8", EnemyDecisionKind.EndEnemyPhase)]
        [TestCase("A8", EnemyDecisionKind.Attack)]
        public void PreparedActionMatchesExistingSettlementAcrossPartitions(string fixture, EnemyDecisionKind kind)
        {
            var input = EnemyAiBenchmarkFixtures.Build(fixture, 1);
            Assert.That(CombatCommandCalculator.RequiresEnemyDecision(input), Is.True);
            string before = EnemyAiBenchmarkFixtures.Describe(input, true);
            using var first = Create(input);
            Drain(first, 1);
            Assert.That(first.Decision.Kind, Is.EqualTo(kind));
            foreach (int width in new[] { 2, 31 })
            {
                using var sliced = Create(input);
                Drain(sliced, width);
                Assert.That(sliced.Decision, Is.EqualTo(first.Decision));
                Assert.That(sliced.WorkVersion, Is.EqualTo(first.WorkVersion));
                Assert.That(sliced.Diagnostics.PreviewCalls, Is.EqualTo(first.Diagnostics.PreviewCalls));
            }
            var prepared = CombatCommandCalculator.AdvancePrepared(input, first.Decision, Rules);
            var legacy = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(prepared.Accepted, Is.True, prepared.Reason);
            Assert.That(EnemyAiBaselineTests.Trace(prepared), Is.EqualTo(EnemyAiBaselineTests.Trace(legacy)));
            Assert.That(EnemyAiBenchmarkFixtures.Describe(CombatCommandComposition.Project(prepared), true),
                Is.EqualTo(EnemyAiBenchmarkFixtures.Describe(CombatCommandComposition.Project(legacy), true)));
            Assert.That(EnemyAiBenchmarkFixtures.Describe(input, true), Is.EqualTo(before));
        }

        [Test]
        public void PendingRoarPrecedesSuppliedOrdinaryIntent()
        {
            var source = BossRoarTests.Input(5, withTargets: false);
            var input = new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, source.ActorId, default,
                source.State, source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls,
                source.Artifacts, battle: source.Battle, processingKind: CombatProcessingKind.Advance,
                battleInstanceId: source.BattleInstanceId, expectedBattleInstanceId: source.ExpectedBattleInstanceId,
                expectedControllerTurnSequence: source.ExpectedControllerTurnSequence);
            Assert.That(CombatCommandCalculator.RequiresEnemyDecision(input), Is.False);
            var result = CombatCommandCalculator.AdvancePrepared(input, EnemyDecision.EndEnemyPhase(), Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.False);
            Assert.That(EnemyAiBaselineTests.Trace(result),
                Is.EqualTo(EnemyAiBaselineTests.Trace(CombatCommandCalculator.Advance(input, Rules))));
        }

        [Test]
        public void RejectedAttackDoesNotTryAnotherAimAndRetainsPreviewCount()
        {
            var input = EnemyMovementPathTests.Input(targetCell: new CellCoord(4, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, appointRange: 3) : c).ToArray());
            input = HighGroundMovementTests.TerrainInput(input, new StaticTerrainFact(11, new CellCoord(1, 1),
                StaticTerrainKind.Sandstorm, true, new[] { Owner.Player }));
            var diagnostics = new EnemyDecisionDiagnostics(captureAlternatives: true);
            using var session = EnemyDecisionSession.BeginImmediateCompatibility(input, Rules, EnemyDecisionSettings.Default, diagnostics);
            Drain(session, 2);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Attack));
            int calls = session.Diagnostics.PreviewCalls;
            var attempted = session.Decision;
            var rejected = CombatCommandCalculator.AdvancePrepared(input, attempted, Rules);
            Assert.That(rejected.Accepted, Is.False);
            session.RejectSelected(rejected.Reason);
            Assert.That(diagnostics.LastRejectedDecision, Is.EqualTo(attempted));
            Assert.That(diagnostics.LastRejectionReason, Is.EqualTo(rejected.Reason));
            Drain(session, 31);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));
            Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(calls));
            Assert.That(session.Diagnostics.RejectedAttempts, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => session.RejectSelected());
        }

        [Test]
        public void PreparedRejectedMoveDoesNotChooseFallbackOrPublish()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(0, 1));
            var path = new MovementPath(new[] { new CellCoord(0, 1), new CellCoord(-1, 1) });
            var intent = EnemyDecision.Move(input.ActorId, path, default);
            var result = CombatCommandCalculator.AdvancePrepared(input, intent, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RejectedMoveAdvancesActor()
        {
            var input = EnemyAiBenchmarkFixtures.Build("W8", 1);
            using var session = Create(input);
            Drain(session, 31);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            var actor = session.Decision.ActorId;
            int calls = session.Diagnostics.PreviewCalls;
            session.RejectSelected();
            Drain(session, 31);
            Assert.That(session.Decision.ActorId, Is.Not.EqualTo(actor));
            Assert.That(session.Diagnostics.PreviewCalls, Is.GreaterThanOrEqualTo(calls));
            Assert.That(session.Diagnostics.RejectedAttempts, Is.EqualTo(1));
        }

        [TestCaseSource(typeof(EnemyAiBaselineTests), nameof(EnemyAiBaselineTests.Fixtures))]
        public void CapturedBaselineTraceMatches(string fixture)
        {
            string root = "docs/superpowers/reports/2026-11-07-enemy-utility-ai";
            string[] frozen = File.ReadAllLines(Path.Combine(root, "baseline-traces.tsv"));
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                var current = EnemyAiBenchmarkFixtures.Build(fixture, seed);
                string hash = EnemyAiBenchmarkFixtures.Hash(EnemyAiBenchmarkFixtures.Describe(current, true));
                string[] expected = frozen.Where(line => line.StartsWith($"{fixture}\t{seed}\t{hash}\t", StringComparison.Ordinal)).ToArray();
                Assert.That(expected, Is.Not.Empty, $"{fixture}/{seed}/{hash}");
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.That(current.State.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy), expected[i]);
                    var diagnostics = new EnemyDecisionDiagnostics();
                    var result = CombatCommandCalculator.Advance(current, Rules, diagnostics);
                    Assert.That(result.Accepted, Is.True, result.Reason);
                    Assert.That($"{fixture}\t{seed}\t{hash}\t{i + 1}\t{EnemyAiBaselineTests.Trace(result, diagnostics.SelectedAim)}",
                        Is.EqualTo(expected[i]));
                    current = CombatCommandComposition.Project(result);
                    if (result.Outcome.IsStageEnded)
                    {
                        Assert.That(i + 1, Is.EqualTo(expected.Length));
                        break;
                    }
                    if (i == expected.Length - 1)
                        Assert.That(current.State.Turn.ControllerPhase, Is.Not.EqualTo(CombatControllerPhase.Enemy));
                }
            }
        }

        internal static EnemyDecisionSession Create(CombatCommandInput input) =>
            EnemyDecisionSession.BeginImmediateCompatibility(input, Rules, EnemyDecisionSettings.Default);

        internal static void Drain(EnemyDecisionSession session, int width)
        {
            while (!session.IsReady && session.WorkVersion < 2_000_000)
                for (int i = 0; i < width && !session.IsReady; i++)
                {
                    long previous = session.WorkVersion;
                    session.Step();
                    Assert.That(session.WorkVersion, Is.EqualTo(previous + 1));
                }
            Assert.That(session.IsReady, Is.True, "선택이 유한한 작업량에서 끝나지 않았습니다.");
        }
    }
}
