using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class MovementEffectReceiptTests
    {


        [Test]
        public void OrdinaryUndo_RestoresExactDistanceOnce_WithParticipantDetails()
        {
            var move = MovementUndoTestData.Run(MovementUndoTestData.Route(7));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.Characters.Characters.Single().Final.StageRecord.DistanceMoved, Is.EqualTo(7));
            Assert.That(undo.FinalState.MovementUndo.Checkpoints, Is.Empty);
            var checkpoint = move.FinalState.MovementUndo.Checkpoints.Single();
            Assert.That(checkpoint.Participants.Single().EntityId, Is.EqualTo(move.Input.ActorId));
            Assert.That(checkpoint.Effects, Is.Empty);
            Assert.That(undo.UndoMovement.Participants.Single().SameValues(checkpoint.Participants[0].Restoration), Is.True);
            var repeated = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(undo),
                CombatCommandKind.UndoMove, move.Input.Number));
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.Characters.Characters, Is.Empty);
        }

        [Test]
        public void Copies_DoNotAliasReceiptCollections()
        {
            var ordinary = MovementUndoContractTests.Checkpoint();
            var people = ordinary.Participants.ToArray();
            var gain = new MovementAccelerationReceipt(ordinary.Movement, Origin(ordinary.ActorId, TraitKey.Acceleration), 3, 10);
            var effects = new MovementEffectReceipt[] { gain };
            var checkpoint = new MovementUndoCheckpoint(ordinary.Movement, people, effects);
            people[0] = null;
            effects[0] = null;
            Assert.That(checkpoint.Participants.Single().SameValues(ordinary.Participants.Single()), Is.True);
            Assert.That(((MovementAccelerationReceipt)checkpoint.Effects.Single()).ExpectedHeldAmount, Is.EqualTo(10));
            Assert.That(ordinary.Effects, Is.Empty);
            Assert.That(checkpoint.SameValues(new MovementUndoCheckpoint(ordinary.Movement, ordinary.Participants, new[] { gain })), Is.True);
            Assert.That(checkpoint.SameValues(ordinary), Is.False);
            var details = ordinary.Participants.Select(p => p.Restoration).ToArray();
            var result = new MovementUndoResult(ordinary.ActorId, ordinary.MoveCommandNumber, details, Array.Empty<MovementEffectReceipt>());
            details[0] = null;
            Assert.That(result.Participants.Single().To, Is.EqualTo(new CellCoord(1, 1)));
        }

        [TestCase("battle")]
        [TestCase("turn")]
        [TestCase("move")]
        public void ForeignBattleTurnOrMove_Rejects(string part)
        {
            var checkpoint = MovementUndoContractTests.Checkpoint();
            var foreign = new MovementEffectId(part == "battle" ? 2 : 1, part == "turn" ? 2 : 1,
                checkpoint.ActorId, part == "move" ? 2 : 1);
            var effect = new MovementAccelerationReceipt(foreign, Origin(checkpoint.ActorId, TraitKey.Acceleration), 3, 10);
            Assert.Throws<ArgumentException>(() => new MovementUndoCheckpoint(checkpoint.Movement, checkpoint.Participants, new[] { effect }));
            Assert.That(foreign, Is.Not.EqualTo(checkpoint.Movement));
        }

        [Test]
        public void ParticipantReceipt_DoesNotRefundPassengerActionOrDistance()
        {
            var checkpoint = PairedCheckpoint();
            var passengers = checkpoint.Participants;
            var actor = passengers[0].ExpectedAfterMove;
            var passenger = passengers[1].ExpectedAfterMove;
            var restoredActor = actor.WithPlacement(actor.Area, null, checkpoint.Anchor, checkpoint.Rotation, checkpoint.OccupiedCells)
                .With(actionUse: UnitActionUse.Available, stageRecord: actor.StageRecord.UndoMovement(1));
            var restoredPassenger = passenger.WithPlacement(passenger.Area, null, new CellCoord(2, 1), passenger.Rotation, new[] { new CellCoord(2, 1) });
            var result = new MovementUndoResult(checkpoint.ActorId, 1, passengers.Select(p => p.Restoration).ToArray(), Array.Empty<MovementEffectReceipt>());
            var events = new[] { new CombatCharacterEvent(CombatEventKind.Returned, actor, restoredActor),
                new CombatCharacterEvent(CombatEventKind.Returned, passenger, restoredPassenger) };
            var settlement = new UnitActionUseSettlementSnapshot(actor.EntityId, UnitActionUse.Moved, UnitActionUse.Available);
            Assert.DoesNotThrow(() => new CombatResult(1, CombatProcessingKind.Command, 2, true, "",
                commandKind: CombatCommandKind.UndoMove, actorId: actor.EntityId, events: events, actionUseSettlement: settlement, undoMovement: result));
            Assert.That(restoredPassenger.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(restoredPassenger.StageRecord.DistanceMoved, Is.EqualTo(9));
            events[1] = new CombatCharacterEvent(CombatEventKind.Returned, passenger, restoredPassenger.With(actionUse: UnitActionUse.Available));
            Assert.Throws<ArgumentException>(() => new CombatResult(1, CombatProcessingKind.Command, 2, true, "",
                commandKind: CombatCommandKind.UndoMove, actorId: actor.EntityId, events: events, actionUseSettlement: settlement, undoMovement: result));
            Assert.Throws<ArgumentException>(() => new MovementParticipantRestoration(passenger.EntityId,
                new CellCoord(1, 1), new CellCoord(2, 1), Rotation4.R0, false, 1));
        }

        [Test]
        public void MalformedOwnershipAndDuplicateParticipantsOrSources_Reject()
        {
            var checkpoint = MovementUndoContractTests.Checkpoint();
            var participant = checkpoint.Participants.Single();
            Assert.Throws<ArgumentException>(() => new MovementUndoCheckpoint(checkpoint.Movement,
                new[] { participant, participant }, Array.Empty<MovementEffectReceipt>()));
            Assert.Throws<ArgumentException>(() => new MovementAccelerationReceipt(checkpoint.Movement, Origin(new EntityId(2), TraitKey.Acceleration), 3, 10));
            Assert.Throws<ArgumentException>(() => new MovementAccelerationReceipt(checkpoint.Movement, Origin(checkpoint.ActorId, TraitKey.Hunter), 3, 10));
            var trail = new MovementTrailReceipt(checkpoint.Movement, new TraitTrailFact(3, new CellCoord(1, 1),
                Origin(checkpoint.ActorId, TraitKey.GroundCompaction), Owner.Player, 1, 0, 0, 1, true, new[] { Owner.Player }));
            Assert.Throws<ArgumentException>(() => new MovementUndoCheckpoint(checkpoint.Movement, checkpoint.Participants, new[] { trail, trail }));
            Assert.Throws<ArgumentException>(() => new MovementUndoCheckpoint(default, checkpoint.Participants, Array.Empty<MovementEffectReceipt>()));
        }

        [Test]
        public void UnsupportedInverse_RemainsDisabled_AndPassengerChangesInvalidate()
        {
            var move = MovementUndoTestData.Run(MovementUndoTestData.Route());
            var checkpoint = move.FinalState.MovementUndo.Checkpoints.Single();
            var gain = new MovementAccelerationReceipt(checkpoint.Movement, Origin(checkpoint.ActorId, TraitKey.Acceleration), 3, 10);
            var pending = new MovementUndoCheckpoint(checkpoint.Movement, checkpoint.Participants, new[] { gain });
            var request = MovementUndoTestData.Undo(move);
            request = CombatCommandTestData.With(request, state: request.State.With(movementUndo: new MovementUndoState(new[] { pending })));
            Assert.That(MovementUndoCalculator.Evaluate(request).CanUndo, Is.False);
            Assert.That(MovementUndoCalculator.Calculate(request).Accepted, Is.False);
            var pair = PairedCheckpoint();
            var state = new MovementUndoState(new[] { pair });
            Assert.That(state.WithoutAffected(new CombatChangeEvidence(CombatChangeEvidenceKind.Action,
                new[] { pair.Participants[1].EntityId }, Array.Empty<CellCoord>())).Checkpoints, Is.Empty);
            Assert.That(state.Checkpoints, Has.Count.EqualTo(1));
        }

        private static MovementUndoCheckpoint PairedCheckpoint()
        {
            var actor = MovementUndoContractTests.Checkpoint();
            var input = MovementUndoTestData.Route();
            var passenger = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(1, 1), Owner.Player)
                .With(actionUse: UnitActionUse.Attacked, stageRecord: CharacterStageRecord.Empty.Add(distanceMoved: 9));
            var receipt = new MovementUndoParticipant(passenger, MovementUndoOriginFacts.Capture(new CellCoord(2, 1),
                input.State, input.EnvironmentBindings, input.Souls), Rotation4.R0);
            return new MovementUndoCheckpoint(actor.Movement, new[] { actor.Participants[0], receipt }, Array.Empty<MovementEffectReceipt>());
        }

        private static EffectOriginKey Origin(EntityId actor, TraitKey key) => new(EffectOwnerKind.Trait, actor.Value, (int)key, "main");
    }
}
