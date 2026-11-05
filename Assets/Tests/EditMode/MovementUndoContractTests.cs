using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class MovementUndoContractTests
    {
        [TestCase(0, 1, 1)]
        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 0)]
        public void ReadyQueryRequiresEnclosingBattleTurnAndActor(long battle, long turn, int actor)
        {
            var undo = new MovementUndoAvailability(MovementUndoFailureReason.None, 1, CellCoord.Zero);
            Assert.Throws<ArgumentException>(() => new CombatActionAvailability(battle, turn, actor == 0 ? default : new EntityId(actor),
                UnitActionUse.Moved, 4, MovementFailureReason.ActionAlreadyUsed, AttackFailureReason.None, undo));
        }

        [Test]
        public void ExpectedMoveCommandNumberRejectsNegative()
        {
            var s = CombatCommandTestData.Input(empty: true);
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatCommandInput(s.Number, s.Kind, s.ActorId, s.Aim,
                s.State, s.Characters, s.Profiles, s.Main, s.Owned, s.SoulCounts, s.Souls, s.Artifacts, expectedMoveCommandNumber: -1));
            Assert.That(s.ExpectedMoveCommandNumber, Is.Zero);
        }

        [Test]
        public void OriginCaptureIncludesHiddenInactiveSourcesAndBindingChanges()
        {
            var cell = new CellCoord(1, 1);
            var source = new DestinationTileModifierFact(1, cell, false, Array.Empty<Owner>(), 2, 0);
            var input = MovementTestData.SafeRoute(cell, new CellCoord(2, 1));
            var tiles = new MovementTileFact[] { source };
            var environment = new MovementEnvironmentFacts(tiles);
            var state = input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment));
            var bindings = new EnvironmentSemanticBindings(new long[] { 1 }, Array.Empty<long>(), Array.Empty<EntityId>());
            var origin = MovementUndoOriginFacts.Capture(cell, state, bindings, input.Souls);
            tiles[0] = new DestinationTileModifierFact(1, cell, false, Array.Empty<Owner>(), 3, 0);
            Assert.That(origin.Tiles.Single().SameValues(source), Is.True);
            Assert.That(origin.TerrainSourceIds, Is.EqualTo(new long[] { 1 }));
            var remoteEnvironment = new MovementEnvironmentFacts(new MovementTileFact[] {
                source, new DestinationTileModifierFact(2, new CellCoord(5, 5), true, new[] { Owner.Player }, 4, 0) });
            var remote = state.With(environment: remoteEnvironment, environmentProgress: EnvironmentProgressFacts.Initial(remoteEnvironment));
            Assert.That(origin.SameValues(MovementUndoOriginFacts.Capture(cell, remote, bindings, input.Souls)), Is.True);
            Assert.That(origin.SameValues(MovementUndoOriginFacts.Capture(cell, state, EnvironmentSemanticBindings.Empty, input.Souls)), Is.False);
            var changed = state.With(environment: new MovementEnvironmentFacts(tiles));
            Assert.That(origin.SameValues(MovementUndoOriginFacts.Capture(cell, changed, bindings, input.Souls)), Is.False);
        }

        [TestCase(0, 1, 0)]
        [TestCase(1, 0, 0)]
        [TestCase(1, 1, -1)]
        public void ResultRejectsMissingIdentityAndNegativeDistance(int actor, int command, int distance)
        {
            Assert.Throws<ArgumentException>(() => new MovementUndoResult(actor == 0 ? EntityId.Empty : new EntityId(actor),
                command, CellCoord.Zero, CellCoord.Zero, Rotation4.R0, distance));
        }

        [Test]
        public void StateCopiesPreserveUndoAndGameplayEqualityExcludesOnlyMetadata()
        {
            var input = CombatCommandTestData.Input(empty: true);
            Assert.That(input.State.MovementUndo, Is.SameAs(MovementUndoState.Empty));
            var state = input.State.With(movementUndo: new MovementUndoState(new[] { Checkpoint() }));
            Assert.That(state.With().MovementUndo.SameValues(state.MovementUndo), Is.True);
            Assert.That(state.SameValues(input.State), Is.False);
            Assert.That(state.SameGameplayValues(input.State), Is.True);
            Assert.That(state.SameGameplayValues(state.With(itemRandomState: 123)), Is.False);
        }

        [Test]
        public void AvailabilityRevisionDefaultsValidatesAndPreservesAllFields()
        {
            var undo = new MovementUndoAvailability(MovementUndoFailureReason.None, 1, new CellCoord(1, 1));
            var value = new CombatActionAvailability(1, 2, new EntityId(3), UnitActionUse.Moved, 4,
                MovementFailureReason.ActionAlreadyUsed, AttackFailureReason.None, undo);
            Assert.That(value.SourceRevision, Is.Zero);
            var copy = value.WithSourceRevision(19);
            Assert.That(copy.SourceRevision, Is.EqualTo(19));
            Assert.That(copy.MovementUndo, Is.SameAs(undo));
            Assert.That(copy.BattleInstanceId, Is.EqualTo(1));
            Assert.That(copy.ControllerTurnSequence, Is.EqualTo(2));
            Assert.That(copy.ActorId, Is.EqualTo(new EntityId(3)));
            Assert.That(copy.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(copy.MovementAllowance, Is.EqualTo(4));
            Assert.That(copy.CanMove, Is.False);
            Assert.That(copy.CanAttack, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => value.WithSourceRevision(-1));
        }

        [Test]
        public void UndoResultRejectsOnRejectedTransition()
        {
            var input = CombatCommandTestData.Input(empty: true).WithIntent(CombatCommandKind.UndoMove, default, default, null, null);
            var rejected = CombatCommandCalculator.Reject(input, "NoCheckpoint");
            var undo = new MovementUndoResult(input.ActorId, 1, new CellCoord(2, 1), new CellCoord(1, 1), Rotation4.R0, 1);
            Assert.Throws<ArgumentException>(() => new CombatCommandTransition(input, false, rejected.Reason,
                rejected.Characters, rejected.Field, rejected.FinalState, rejected.Outcome, undoMovement: undo));
            Assert.Throws<ArgumentException>(() => new CombatResult(1, CombatProcessingKind.Command, 1, false,
                "NoCheckpoint", commandKind: CombatCommandKind.UndoMove, actorId: input.ActorId, undoMovement: undo));
        }

        [TestCase("missing-result")]
        [TestCase("missing-event")]
        [TestCase("extra-event")]
        [TestCase("wrong-distance")]
        [TestCase("wrong-settlement")]
        [TestCase("wrong-command")]
        [TestCase("wrong-destination")]
        public void UndoResultRejectsMalformedEventAndSettlement(string fault)
        {
            var checkpoint = Checkpoint();
            var before = checkpoint.ExpectedActorAfterMove;
            var after = before.WithPlacement(before.Area, null, checkpoint.Anchor, checkpoint.Rotation, checkpoint.OccupiedCells)
                .With(actionUse: UnitActionUse.Available, stageRecord: before.StageRecord.UndoMovement(1));
            var result = fault == "missing-result" ? null : new MovementUndoResult(before.EntityId, 1, before.Anchor.Value,
                fault == "wrong-destination" ? new CellCoord(6, 6) : checkpoint.Anchor, checkpoint.Rotation, fault == "wrong-distance" ? 0 : 1);
            var returned = new CombatCharacterEvent(CombatEventKind.Returned, before, after);
            CombatEvent[] events = fault == "missing-event" ? Array.Empty<CombatEvent>() :
                fault == "extra-event" ? new CombatEvent[] { returned, returned } : new CombatEvent[] { returned };
            var settlement = new UnitActionUseSettlementSnapshot(before.EntityId, UnitActionUse.Moved,
                fault == "wrong-settlement" ? UnitActionUse.Attacked : UnitActionUse.Available);
            Assert.Throws<ArgumentException>(() => new CombatResult(1, CombatProcessingKind.Command, 2, true, "",
                commandKind: fault == "wrong-command" ? CombatCommandKind.Move : CombatCommandKind.UndoMove,
                actorId: before.EntityId, events: events, actionUseSettlement: settlement, undoMovement: result));
        }

        [Test]
        public void ValidUndoResultRetainsExactFacts()
        {
            var checkpoint = Checkpoint();
            var before = checkpoint.ExpectedActorAfterMove;
            var after = before.WithPlacement(before.Area, null, checkpoint.Anchor, checkpoint.Rotation, checkpoint.OccupiedCells)
                .With(actionUse: UnitActionUse.Available, stageRecord: before.StageRecord.UndoMovement(1));
            var result = new MovementUndoResult(before.EntityId, 1, before.Anchor.Value, checkpoint.Anchor, checkpoint.Rotation, 1);
            var published = new CombatResult(1, CombatProcessingKind.Command, 2, true, "", commandKind: CombatCommandKind.UndoMove,
                actorId: before.EntityId, events: new[] { new CombatCharacterEvent(CombatEventKind.Returned, before, after) },
                actionUseSettlement: new UnitActionUseSettlementSnapshot(before.EntityId, UnitActionUse.Moved, UnitActionUse.Available), undoMovement: result);
            Assert.That(published.UndoMovement, Is.SameAs(result));
            Assert.That(published.Events.Single(), Is.TypeOf<CombatCharacterEvent>());
        }

        [Test]
        public void CheckpointCopiesPlacementAndStateRejectsDuplicateActors()
        {
            var cells = new[] { new CellCoord(1, 1) };
            var checkpoint = Checkpoint(cells: cells);
            cells[0] = new CellCoord(6, 6);
            Assert.That(checkpoint.OccupiedCells.Single(), Is.EqualTo(new CellCoord(1, 1)));
            var entries = new[] { checkpoint };
            var state = new MovementUndoState(entries);
            entries[0] = Checkpoint(command: 3);
            Assert.That(state.TryGet(checkpoint.ActorId, out var saved), Is.True);
            Assert.That(saved.MoveCommandNumber, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => new MovementUndoState(new[] { checkpoint, checkpoint }));
            Assert.That(state.SameValues(new MovementUndoState(new[] { Checkpoint() })), Is.True);
            Assert.That(state.SameValues(new MovementUndoState(entries)), Is.False);
        }

        [TestCase(0, 1, 1, 1, 1)]
        [TestCase(1, 0, 1, 1, 1)]
        [TestCase(1, 1, 0, 1, 1)]
        [TestCase(1, 1, 1, 2, 1)]
        [TestCase(1, 1, 1, 1, -1)]
        public void CheckpointRejectsInvalidIdentityOrDistance(long battle, long turn, int command, int actor, int distance)
        {
            Assert.That(() => Checkpoint(battle, turn, command, actor, distance), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void EvidenceCopiesAndDeduplicatesScopeWithoutGlobalInvalidation()
        {
            var ids = new[] { new EntityId(2), new EntityId(2) };
            var cells = new[] { new CellCoord(5, 5), new CellCoord(5, 5) };
            var evidence = new CombatChangeEvidence(CombatChangeEvidenceKind.Effect, ids, cells);
            ids[0] = new EntityId(1); cells[0] = new CellCoord(1, 1);
            Assert.That(evidence.AffectedEntityIds, Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(evidence.ChangedSourceCells, Is.EqualTo(new[] { new CellCoord(5, 5) }));
            var state = new MovementUndoState(new[] { Checkpoint() });
            Assert.That(state.WithoutAffected(evidence), Is.SameAs(state));
            Assert.That(state.WithoutAffected(new CombatChangeEvidence(CombatChangeEvidenceKind.Effect,
                new[] { new EntityId(1) }, Array.Empty<CellCoord>())).Checkpoints, Is.Empty);
            Assert.That(state.WithoutAffected(new CombatChangeEvidence(CombatChangeEvidenceKind.Effect,
                Array.Empty<EntityId>(), new[] { new CellCoord(1, 1) })).Checkpoints, Is.Empty);
            Assert.That(state.WithoutAffected(new CombatChangeEvidence(CombatChangeEvidenceKind.Lifecycle,
                Array.Empty<EntityId>(), Array.Empty<CellCoord>())).Checkpoints, Is.Empty);
        }

        [Test]
        public void AvailabilityRequiresTokenAndDestinationOnlyForSuccess()
        {
            Assert.That(MovementUndoAvailability.Unavailable.CanUndo, Is.False);
            Assert.Throws<ArgumentException>(() => new MovementUndoAvailability(MovementUndoFailureReason.None));
            var available = new MovementUndoAvailability(MovementUndoFailureReason.None, 12, new CellCoord(1, 1));
            Assert.That(available.CanUndo, Is.True);
            Assert.That(available.MoveCommandNumber, Is.EqualTo(12));
            Assert.Throws<ArgumentException>(() => new MovementUndoAvailability(MovementUndoFailureReason.NoCheckpoint, 12, new CellCoord(1, 1)));
        }

        internal static MovementUndoCheckpoint Checkpoint(long battle = 1, long turn = 1, int command = 1,
            int actor = 1, int distance = 1, CellCoord[] cells = null)
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1));
            var expected = input.Characters.Single(c => c.EntityId == input.ActorId)
                .WithPlacement(CharacterRuntimeArea.Field, null, new CellCoord(2, 1), Rotation4.R0, new[] { new CellCoord(2, 1) })
                .With(actionUse: UnitActionUse.Moved, stageRecord: CharacterStageRecord.Empty.Add(distanceMoved: 1));
            return new MovementUndoCheckpoint(battle, turn, command, new EntityId(actor), new CellCoord(1, 1), Rotation4.R0,
                cells ?? new[] { new CellCoord(1, 1) }, distance,
                MovementUndoOriginFacts.Capture(new CellCoord(1, 1), input.State, input.EnvironmentBindings, input.Souls), expected);
        }

        [Test]
        public void UndoMovementSubtractsOnlyCancelledDistance()
        {
            var before = CharacterStageRecord.Empty.Add(2, 7, 13, 1);
            var after = before.UndoMovement(3);
            Assert.That(after.SameValues(CharacterStageRecord.Empty.Add(2, 7, 10, 1)), Is.True);
            Assert.That(before.DistanceMoved, Is.EqualTo(13));
            Assert.That(after.IsAtLeast(before), Is.False);
        }

        [Test]
        public void UndoMovementZeroPreservesIdentity()
        {
            var before = CharacterStageRecord.Empty.Add(distanceMoved: 10);
            Assert.That(before.UndoMovement(0), Is.SameAs(before));
        }

        [TestCase(-1)]
        [TestCase(11)]
        public void UndoMovementRejectsNegativeAndUnderflow(int distance)
        {
            var before = CharacterStageRecord.Empty.Add(distanceMoved: 10);
            Assert.Throws<ArgumentOutOfRangeException>(() => before.UndoMovement(distance));
        }

        [Test]
        public void UndoMovementDoesNotPermitNegativeAdd()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterStageRecord.Empty.Add(distanceMoved: -1));
        }
    }
}
