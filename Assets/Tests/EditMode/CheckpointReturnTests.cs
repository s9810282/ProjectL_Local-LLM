using System.Linq;
using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class CheckpointReturnTests
    {
        [Test]
        public void Seal_CommitsWithoutRefund_AndReturnPreservesCommittedDistance()
        {
            var first = CombatCommandCalculator.Calculate(MovementRouteTests.Detour(), System.Array.Empty<IMovementTileRule>());
            var after = MovementTestData.After(first);
            var sealedActor = CombatMovementCalculator.SealPendingMovement(MovementTestData.Actor(first));
            Assert.That(sealedActor.CommittedMoveDistance, Is.EqualTo(4));
            Assert.That(sealedActor.PendingMoveDistance, Is.Zero);
            Assert.That(sealedActor.PendingMoveActionCost, Is.Zero);
            Assert.That(sealedActor.Stats.CurrentActionCost, Is.EqualTo(16));
            Assert.That(sealedActor.AttackRefundEligibleMoveCost, Is.EqualTo(4));
            Assert.That(sealedActor.MovementCheckpointAnchor, Is.EqualTo(first.PathMovement.FinalStop));
            after = CombatCommandTestData.With(after, characters: after.Characters.Select(c =>
                c.EntityId == sealedActor.EntityId ? sealedActor : c).ToArray());
            Assert.That(CheckpointReturnCalculator.Preview(MovementTestData.Return(after)).CanReturn, Is.False);
            var continued = CombatCommandCalculator.Calculate(MovementTestData.Route(after,
                new CellCoord(3, 1), new CellCoord(4, 1), new CellCoord(4, 2)), System.Array.Empty<IMovementTileRule>());
            var beforeReturn = MovementTestData.After(continued);
            var preview = CheckpointReturnCalculator.Preview(MovementTestData.Return(beforeReturn));
            Assert.That(preview.CanReturn, Is.True);
            Assert.That(preview.Return.DistanceRefunded, Is.EqualTo(2));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(beforeReturn), System.Array.Empty<IMovementTileRule>());
            var actor = MovementTestData.Actor(returned);
            Assert.That(actor.CommittedMoveDistance, Is.EqualTo(4));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(16));
            Assert.That(actor.Stats.RemainingMoveRange, Is.EqualTo(16));
            Assert.That(actor.AttackRefundEligibleMoveCost, Is.EqualTo(4));
            Assert.That(actor.Anchor, Is.EqualTo(new CellCoord(3, 1)));
        }

        [TestCase(false, "DestinationOccupied")]
        [TestCase(true, "OutOfBounds")]
        public void InvalidCheckpoint_RejectsWithoutRefund(bool outside, string reason)
        {
            var moved = CombatCommandCalculator.Calculate(MovementRouteTests.Detour(), System.Array.Empty<IMovementTileRule>());
            var input = MovementTestData.After(moved);
            var actor = MovementTestData.Actor(moved);
            if (outside)
                actor = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner,
                    actor.OwnershipRole, actor.Area, actor.Health, actor.Stats, actor.ReserveSlotIndex, actor.Anchor,
                    actor.Rotation, actor.OccupiedCells, new CellCoord(-1, 1), actor.CommittedMoveDistance,
                    actor.PendingMoveActionCost, actor.TemporaryAttackBonus, actor.TemporaryMaxHealthBonus,
                    actor.PendingMoveDistance, actor.AttackRefundEligibleMoveCost, actor.PositionModifier);
            var characters = input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray();
            if (!outside)
                characters = characters.Append(CombatCommandTestData.Character(2, EntityKey.Unit_Demon,
                    actor.MovementCheckpointAnchor, Owner.Enemy)).ToArray();
            input = MovementTestData.Return(CombatCommandTestData.With(input, characters: characters));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(CheckpointReturnCalculator.Preview(input).FailureReason.ToString(), Is.EqualTo(reason));
            Assert.That(result.ReturnMovement, Is.Null);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void Return_WithoutPendingMovement_IsRejected()
        {
            var result = CombatCommandCalculator.Calculate(MovementTestData.Return(MovementRouteTests.Detour()), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("NoPendingMovement"));
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void MultipleDrags_ReturnRefundsSixSteps()
        {
            var first = CombatCommandCalculator.Calculate(MovementRouteTests.Detour(), System.Array.Empty<IMovementTileRule>());
            Assert.That(first.Accepted, Is.True, first.Reason);
            var second = CombatCommandCalculator.Calculate(MovementTestData.Route(MovementTestData.After(first),
                new CellCoord(3, 1), new CellCoord(4, 1), new CellCoord(4, 2)), System.Array.Empty<IMovementTileRule>());
            Assert.That(second.Accepted, Is.True, second.Reason);
            var afterSecond = MovementTestData.Actor(second);
            Assert.That(afterSecond.PendingMoveDistance, Is.EqualTo(6));
            Assert.That(afterSecond.Stats.CurrentActionCost, Is.EqualTo(14));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(MovementTestData.After(second)), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            var actor = MovementTestData.Actor(returned);
            Assert.That(returned.ReturnMovement.DistanceRefunded, Is.EqualTo(6));
            Assert.That(returned.ReturnMovement.ActionCostRefunded, Is.EqualTo(6));
            Assert.That(actor.Anchor, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(actor.Stats.RemainingMoveRange, Is.EqualTo(20));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(20));
            Assert.That(actor.PendingMoveDistance, Is.Zero);
            Assert.That(actor.PendingMoveActionCost, Is.Zero);
            Assert.That(actor.CommittedMoveDistance, Is.Zero);
            Assert.That(actor.AttackRefundEligibleMoveCost, Is.Zero);
            Assert.That(returned.Events.Select(e => e.Kind), Is.EqualTo(new[] { CombatEventKind.Returned }));
            Assert.That(returned.Field.Final, Is.SameAs(second.Field.Final));
        }

        [Test]
        public void PaidLoop_SameAnchorReturnRefundsLedger()
        {
            var input = MovementTestData.SafeRoute(20, 20, 1, new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1));
            var first = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var second = CombatCommandCalculator.Calculate(MovementTestData.Route(MovementTestData.After(first),
                new CellCoord(3, 1), new CellCoord(2, 1), new CellCoord(1, 1)), System.Array.Empty<IMovementTileRule>());
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(MovementTestData.Actor(second).Stats.CurrentActionCost, Is.EqualTo(16));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(MovementTestData.After(second)), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(returned.ReturnMovement.From, Is.EqualTo(returned.ReturnMovement.To));
            Assert.That(returned.ReturnMovement.DistanceRefunded, Is.EqualTo(4));
            Assert.That(returned.ReturnMovement.ActionCostRefunded, Is.EqualTo(4));
            Assert.That(MovementTestData.Actor(returned).Stats.CurrentActionCost, Is.EqualTo(20));
        }

        [Test]
        public void EnemyReturn_IsRejected()
        {
            var input = MovementTestData.SafeRoute(20, 20, 1, new CellCoord(1, 1), new CellCoord(2, 1));
            var enemy = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 1), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: new[] { enemy, input.Characters[1] });
            var result = CombatCommandCalculator.Calculate(MovementTestData.Return(input), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("WrongPhase"));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void ZeroMoveCost_ReturnStillRestoresRange()
        {
            var moved = CombatCommandCalculator.Calculate(MovementRouteTests.Detour(0), System.Array.Empty<IMovementTileRule>());
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(MovementTestData.After(moved)), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(returned.ReturnMovement.DistanceRefunded, Is.EqualTo(4));
            Assert.That(returned.ReturnMovement.ActionCostRefunded, Is.Zero);
            Assert.That(MovementTestData.Actor(returned).Stats.RemainingMoveRange, Is.EqualTo(20));
            Assert.That(MovementTestData.Actor(returned).Stats.CurrentActionCost, Is.EqualTo(20));
        }
    }
}
