using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyUtilityDecisionTests
    {
        internal static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;

        [TestCase("N5", "Attack/")]
        [TestCase("N8", "UtilityFollowup/Attack/")]
        public void RepeatedWorkPhaseReusesDiagnosticLabel(string fixture, string prefix)
        {
            using var session = new EnemyDecisionSession(EnemyAiBenchmarkFixtures.Build(fixture, 1), Rules, EnemyDecisionSettings.Default);
            string previous = session.WorkPhase;
            while (!session.IsReady && session.WorkVersion < 100000)
            {
                session.Step();
                string current = session.WorkPhase;
                if (current.StartsWith(prefix, StringComparison.Ordinal) && current == previous)
                {
                    Assert.That(current, Is.SameAs(previous));
                    return;
                }
                previous = current;
            }
            Assert.Fail("No consecutive observations of the same phase.");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void DueRailEscapeBeatsAvailableAttackAcrossSlices(int slice)
        {
            var input = RailInput();
            var actor = input.Characters[0];
            var forecast = EnemyActionCandidateTests.MakeForecast(input);
            Assert.That(forecast.Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value).KnownHpLoss,
                Is.EqualTo(actor.Health.CurrentHealth));
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(session, slice);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(session.Decision.Path.Endpoint.X, Is.Not.EqualTo(1));
            var actual = CombatCommandCalculator.AdvancePrepared(input, session.Decision, Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
        }

        [Test]
        public void IncompleteTerminalProjectionKeepsDueRailExposure()
        {
            var input = RailInput(targetIsMain: true);
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(session.Decision.Path.Endpoint.X, Is.Not.EqualTo(1));
        }

        [Test]
        public void ActualAcceptedMainKillSettlesBeforeLaterGimmick()
        {
            var input = RailInput(targetIsMain: true, dueRail: false);
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Attack));
            var actual = CombatCommandCalculator.AdvancePrepared(input, session.Decision, Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(actual.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
        }

        [Test]
        public void MandatoryTauntBeatsSaferEscapeAndHold()
        {
            var input = TraitCombatFixtures.Equip(RailInput(), EntityKey.Unit_Akimbo, new TauntTraitSpec(8, 10));
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Attack));
            Assert.That(session.Decision.TauntSourceId, Is.EqualTo(input.Characters[1].EntityId));
        }

        [Test]
        public void MovedActorWithNoUsefulAttackHoldsAndEndsExactlyOnce()
        {
            var input = EnemyMovementPathTests.Input(targetCell: new CellCoord(4, 1));
            var actor = input.Characters[0].With(actionUse: UnitActionUse.Moved);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], input.Characters[2] });
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));
            var result = CombatCommandCalculator.AdvanceUtility(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Moved), Is.False);
            Assert.That(input.Characters[0].ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [Test]
        public void SafeApproachUsesActualNewFactsAndThenAttacks()
        {
            var input = Approach();
            var move = CombatCommandCalculator.AdvanceUtility(input, Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.Events.Any(e => e.Kind == CombatEventKind.Moved), Is.True);
            var next = CombatCommandComposition.Project(move);
            Assert.That(next.Characters.Single(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            using var session = new EnemyDecisionSession(next, Rules, EnemyDecisionSettings.Default);
            Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Attack));
            Assert.That(session.Decision.ActorId, Is.EqualTo(input.ActorId));
        }

        [Test]
        public void LowerEntityIdActsBeforeAnotherPreferableActor()
        {
            var input = EnemyMovementPathTests.Input(targetCell: new CellCoord(2, 1));
            var second = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 4), Owner.Enemy, 10);
            input = CombatCommandTestData.With(input, characters: new[] { second }.Concat(input.Characters).ToArray());
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(session);
            Assert.That(session.Decision.ActorId, Is.EqualTo(input.ActorId));
        }

        [TestCase("N5")]
        [TestCase("X8")]
        [TestCase("H8")]
        public void UnobservedRngCursorsDoNotChangeChoice(string fixture)
        {
            var source = EnemyAiBenchmarkFixtures.Build(fixture, 1);
            var progress = source.State.EnvironmentProgress;
            var changed = CombatCommandTestData.With(source, state: source.State.With(itemRandomState: 9751,
                environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, 9187)));
            string before = EnemyAiBenchmarkFixtures.Describe(changed, true);
            using var a = new EnemyDecisionSession(source, Rules, EnemyDecisionSettings.Default);
            using var b = new EnemyDecisionSession(changed, Rules, EnemyDecisionSettings.Default);
            Drain(a, 2);
            Drain(b, 31);
            Assert.That(b.Decision, Is.EqualTo(a.Decision));
            Assert.That(b.Diagnostics.PreviewCalls, Is.EqualTo(a.Diagnostics.PreviewCalls));
            Assert.That(EnemyAiBenchmarkFixtures.Describe(changed, true), Is.EqualTo(before));
        }

        internal static CombatCommandInput Approach()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(0, 0), targetCell: new CellCoord(3, 0));
            var actor = input.Characters[0];
            return CombatCommandTestData.With(input, characters: new[] { actor.With(stats: actor.Stats.With(moveRange: 2)),
                input.Characters[1], input.Characters[2] });
        }

        internal static CombatCommandInput RailInput(bool targetIsMain = false, bool dueRail = true)
        {
            var targetCell = new CellCoord(2, 1);
            var source = EnemyMovementPathTests.Input(targetCell: targetCell);
            var characters = targetIsMain
                ? new[] { source.Characters[0], CombatCommandTestData.With(source.Characters[2], position: targetCell)
                    .With(health: new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(1, 100)) }
                : source.Characters;
            var specs = new StageGimmickSpec[] { new RailwayGimmickSpec(dueRail ? 1 : 4, 5) };
            var rails = Enumerable.Range(0, 5).Select(y => new CellCoord(1, y)).ToArray();
            var battle = new CombatBattleFacts(10, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0, rails, specs);
            return new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim,
                StageGimmickComposition.Initialize(source.State, specs), characters, source.Profiles, source.Main,
                source.Owned, source.SoulCounts, source.Souls, source.Artifacts, battle: battle, processingKind: CombatProcessingKind.Advance);
        }

        internal static void Drain(EnemyDecisionSession session, int slice = 31)
        {
            for (int i = 0; !session.IsReady && i < 500_000; i += slice)
                for (int step = 0; step < slice && !session.IsReady; step++) session.Step();
            Assert.That(session.IsReady, Is.True);
        }
    }
}
