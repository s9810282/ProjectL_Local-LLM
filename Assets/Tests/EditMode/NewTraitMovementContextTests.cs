using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class NewTraitMovementContextTests
    {
        [TestCase("Slow")]
        [TestCase("RampageMovement")]
        [TestCase("RampageHistory")]
        public void SlowOrRampageChangeRejectsStaleContext(string change)
        {
            var input = RampageTraitTests.Equip(MovementUndoTestData.Route(), kills: 8);
            var expected = MovementContextCalculator.Capture(input, Owner.Player, Array.Empty<IMovementTileRule>());
            var updated = change switch
            {
                "Slow" => PeriodicStatusTurnTests.WithStatus(input, 1, new StatusEffectSet(new AppliedStatus[]
                    { new MovementRangeDecreaseStatus(PeriodicStatusTurnTests.Origin(), 2) })),
                "RampageMovement" => RampageTraitTests.Equip(input, kills: 8, specs: new TraitSpec[] { new RampageTraitSpec(10, 0, 2) }),
                _ => RampageTraitTests.Equip(input, kills: 16)
            };
            Assert.That(MovementContextCalculator.Matches(expected, updated, Owner.Player, Array.Empty<IMovementTileRule>()), Is.False, change);
            var withContext = updated.WithIntent(updated.Kind, updated.Aim, updated.Path.Endpoint, updated.Path, expected);
            // Systems가 Matches를 확인한 뒤 사용하는 정상 거절 결과입니다. 원시 이동 계산은 이 검사를 중복하지 않습니다.
            var rejected = MovementContextCalculator.RejectStale(withContext);
            Assert.That(rejected.Accepted, Is.False, change);
            Assert.That(rejected.Characters.Characters, Is.Empty);
            Assert.That(rejected.FinalState.SameValues(updated.State), Is.True);
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 1).StatusEffects.Entries, Is.Empty);
            Assert.That(RampageTraitTests.Status(input.Characters.Single(c => c.EntityId.Value == 1)).KillAttackBonus, Is.EqualTo(8));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnchangedHistoryMoveCanUndo(bool slowed)
        {
            var input = RampageTraitTests.Equip(MovementUndoTestData.Route(), kills: 8);
            if (slowed) input = PeriodicStatusTurnTests.WithStatus(input, 1, new StatusEffectSet(new AppliedStatus[]
                { new MovementRangeDecreaseStatus(PeriodicStatusTurnTests.Origin(), 2) }));
            var original = input.Characters.Single(c => c.EntityId == input.ActorId);
            var moved = MovementUndoTestData.Run(input);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var availability = CombatCommandCalculator.GetActionAvailability(MovementUndoTestData.After(moved));
            Assert.That(availability.MovementUndo.CanUndo, Is.True, availability.MovementUndo.Failure.ToString());
            var undone = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            Assert.That(undone.Accepted, Is.True, undone.Reason);
            Assert.That(MovementTestData.Actor(undone).SameValues(original), Is.True);
            Assert.That(RampageTraitTests.Status(MovementTestData.Actor(undone)).KillAttackBonus, Is.EqualTo(8));
        }

        [Test]
        public void HistoryChangingMoveDoesNotProduceInvalidUndoCandidate()
        {
            var input = RampageTraitTests.Equip(MovementUndoTestData.Route(), kills: 8);
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
                { new DestinationTileModifierFact(1, input.Path.Endpoint, true, new[] { Owner.Player, Owner.Enemy }, 0, -50) });
            input = CombatCommandTestData.With(input, state: input.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment)));
            var moved = MovementUndoTestData.Run(input);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(MovementTestData.Actor(moved).Health.MaxHealth, Is.EqualTo(50));
            Assert.That(RampageTraitTests.Status(MovementTestData.Actor(moved)).KillAttackBonus, Is.Zero);
            Assert.That(moved.FinalState.MovementUndo.Checkpoints, Is.Empty);
            var after = MovementUndoTestData.After(moved);
            CombatActionAvailability availability = null;
            Assert.DoesNotThrow(() => availability = CombatCommandCalculator.GetActionAvailability(after));
            Assert.That(availability.MovementUndo.CanUndo, Is.False);
            Assert.That(availability.MovementUndo.Failure, Is.EqualTo(MovementUndoFailureReason.NoCheckpoint));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            Assert.That(undo.Accepted, Is.False);
            Assert.That(undo.Characters.Characters, Is.Empty);
            Assert.That(undo.FinalState.SameValues(after.State), Is.True);
        }

        [Test]
        public void TauntCoexistenceAvailabilityNeverThrows()
        {
            var input = MovementUndoTestData.Route();
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(6, 1), Owner.Enemy)).ToArray());
            input = RampageTraitTests.Equip(input, kills: 8, specs: new TraitSpec[] { new RampageTraitSpec(10, 0, 1), new TauntTraitSpec(2) });
            var moved = MovementUndoTestData.Run(input);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(TraitCombatFixtures.Final(moved, 2).StatusEffects.Entries.OfType<TauntedStatus>().Count(), Is.EqualTo(1));
            CombatActionAvailability availability = null;
            Assert.DoesNotThrow(() => availability = CombatCommandCalculator.GetActionAvailability(MovementUndoTestData.After(moved)));
            Assert.That(availability.MovementUndo.CanUndo, Is.True, availability.MovementUndo.Failure.ToString());
            var undone = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            Assert.That(undone.Accepted, Is.True, undone.Reason);
            Assert.That(TraitCombatFixtures.Final(undone, 2).StatusEffects.Entries.OfType<TauntedStatus>(), Is.Empty);
            Assert.That(RampageTraitTests.Status(MovementTestData.Actor(undone)).KillAttackBonus, Is.EqualTo(8));
        }
    }
}
