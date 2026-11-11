using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyDecisionBudgetTests
    {
        [Test]
        public void PreviewCapIsEightPerActorAndSixteenPerDecision()
        {
            var input = EnemyAiBenchmarkFixtures.Build("N5", 1);
            var settings = new EnemyDecisionSettings(actorPreviewLimit: 99, decisionPreviewLimit: 100);
            var context = new EnemyDecisionContext(input, settings);
            for (int i = 0; i < 8; i++) Assert.That(context.TryChargePreview(new EntityId(1)), Is.True);
            Assert.That(context.TryChargePreview(new EntityId(1)), Is.False);
            for (int i = 0; i < 8; i++) Assert.That(context.TryChargePreview(new EntityId(2)), Is.True);
            Assert.That(context.TryChargePreview(new EntityId(3)), Is.False);
            Assert.That(context.PreviewCount, Is.EqualTo(16));
        }

        [Test]
        public void FollowupConsumesTheSameBudgetAsItsMove()
        {
            var input = EnemyUtilityDecisionTests.Approach();
            var rules = EnemyUtilityDecisionTests.Rules;
            using var session = new EnemyDecisionSession(input, rules,
                new EnemyDecisionSettings(actorPreviewLimit: 2, decisionPreviewLimit: 2));
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(2));
            Assert.That(session.Diagnostics.FollowupPreviewCalls, Is.EqualTo(1));
            Assert.That(session.Diagnostics.RefinedMoves, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void RejectedActionDoesNotResetBudgetAndMandatoryTauntStillRuns()
        {
            var first = EnemyUtilityDecisionTests.RailInput();
            var next = CombatCommandTestData.Character(7, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy, 10);
            var input = CombatCommandTestData.With(first, characters: first.Characters.Append(next).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new TauntTraitSpec(8, 10));
            using var session = new EnemyDecisionSession(input, EnemyUtilityDecisionTests.Rules,
                new EnemyDecisionSettings(actorPreviewLimit: 1, decisionPreviewLimit: 1));
            EnemyUtilityDecisionTests.Drain(session);
            var firstAttempt = session.Decision;
            Assert.That(firstAttempt.ActorId, Is.EqualTo(input.ActorId));
            Assert.That(firstAttempt.TauntSourceId, Is.EqualTo(input.Characters[1].EntityId));
            Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(1));
            session.RejectSelected();
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(1));
            Assert.That(session.Diagnostics.RejectedAttempts, Is.EqualTo(1));
            Assert.That(session.Decision.ActorId, Is.EqualTo(next.EntityId));
            Assert.That(session.Decision.TauntSourceId, Is.EqualTo(input.Characters[1].EntityId));
        }

        [Test]
        public void RejectedMoveSkipsTheActorInsteadOfReversingIntoItsAttack()
        {
            var input = EnemyUtilityDecisionTests.RailInput();
            using var session = new EnemyDecisionSession(input, EnemyUtilityDecisionTests.Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            int calls = session.Diagnostics.PreviewCalls;
            session.RejectSelected();
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));
            Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(calls));
        }

        [Test]
        public void FailedPreviewCreationIsStillCharged()
        {
            var input = CombatCommandTestData.Input();
            var context = new EnemyDecisionContext(input, new EnemyDecisionSettings(actorPreviewLimit: 1, decisionPreviewLimit: 1));
            var attack = EnemyDecision.Attack(input.ActorId, input.Aim, default);
            using var work = CombatActionAssessment.Begin(input, attack, new IMovementTileRule[] { null }, context);
            Assert.Throws<ArgumentException>(() =>
            {
                while (!work.IsComplete) work.Step();
            });
            Assert.That(context.PreviewCount, Is.EqualTo(1));
            Assert.That(context.CanChargePreview(input.ActorId), Is.False);
        }

        [Test]
        public void StartedAssessmentIsChargedWhenAbandoned()
        {
            var input = EnemyUtilityDecisionTests.Approach();
            var context = new EnemyDecisionContext(input, new EnemyDecisionSettings(actorPreviewLimit: 1, decisionPreviewLimit: 1));
            var move = EnemyDecision.Move(input.ActorId, new MovementPath(new[] { new CellCoord(0, 0), new CellCoord(1, 0) }), default);
            using (var work = CombatActionAssessment.Begin(input, move, EnemyUtilityDecisionTests.Rules, context))
            {
                while (!work.IsComplete && context.PreviewCount == 0) work.Step();
                Assert.That(work.IsComplete, Is.False);
            }
            Assert.That(context.PreviewCount, Is.EqualTo(1));
            Assert.That(context.CanChargePreview(input.ActorId), Is.False);
        }
    }
}
