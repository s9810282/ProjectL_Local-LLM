using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MovementEffectPublicationTests
    {
        [Test]
        public void UsageWithoutDamage_AdvancesQueryRevision_AndHandledReplayDoesNothing()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var attack = Attack(battle);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.MovementEffectUses, Has.Count.EqualTo(1));
            Assert.That(attack.Characters.Characters.All(c => c.Expected.Health.Equals(c.Final.Health)), Is.True);
            var revision = battle.Buffers.CombatStateReadSink.QueryRevision;
            battle.Buffers.BattleWriteSink.Apply(attack);
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision + 1));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
            var after = battle.CaptureStores();
            battle.Buffers.BattleWriteSink.Apply(attack);
            Assert.That(battle.CaptureStores(), Is.EqualTo(after));
        }

        [Test]
        public void LatePrepareFailure_PreservesAllStoresResultAndRevision()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var valid = Attack(battle);
            var input = valid.Input;
            var stale = input.WithPersistentFacts(input.Owned, input.ItemInventory, input.GoldBalance.Value + 1, input.FameBalance);
            var invalid = CombatCommandCalculator.Calculate(stale, Array.Empty<IMovementTileRule>());
            Assert.That(invalid.MovementEffectUses, Has.Count.EqualTo(1));
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(invalid));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(valid);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
        }

        [TestCase("consumer")]
        [TestCase("move")]
        [TestCase("unsupported-effect")]
        public void ForgedUnrelatedUse_InstallsNothing(string defect)
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var valid = Attack(battle);
            var use = valid.MovementEffectUses.Single();
            var movement = use.Movement.Value;
            var forged = new MovementEffectUse(defect == "move" ? new MovementEffectId(movement.BattleInstanceId,
                    movement.ControllerTurnSequence, movement.ActorId, movement.MoveCommandNumber + 1) : movement,
                defect == "unsupported-effect" ? new EffectOriginKey(EffectOwnerKind.Trait, movement.ActorId.Value, (int)TraitKey.Acceleration, "main") : (EffectOriginKey?)null,
                defect == "consumer" ? battle.Main.EntityId : use.ConsumerId,
                defect == "unsupported-effect" ? MovementEffectUseKind.AccelerationDamage : MovementEffectUseKind.ParticipantAction, use.Execution);
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(valid.WithMovementEffectUses(new[] { forged })));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        [Test]
        public void StaleParticipant_InstallsNothing()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var pending = Attack(battle);
            var actor = battle.Actor;
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor,
                actor.WithPlacement(actor.Area, null, new CellCoord(1, 2), actor.Rotation, new[] { new CellCoord(1, 2) })) }, Array.Empty<SoulCountChange>()));
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(pending));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
        }

        [Test]
        public void ForgedReceiptOnOrdinaryMove_InstallsNothing()
        {
            using var battle = Battle();
            var command = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1));
            int number = battle.Buffers.CombatViewWriteSink.Submit(command);
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move,
                command.EntityId, default, path: command.Path, movementContext: command.MovementContext,
                expectedBattleInstanceId: command.ExpectedBattleInstanceId, expectedControllerTurnSequence: command.ExpectedControllerTurnSequence));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var checkpoint = move.FinalState.MovementUndo.Checkpoints.Single();
            var receipt = new MovementAccelerationReceipt(checkpoint.Movement,
                new EffectOriginKey(EffectOwnerKind.Trait, checkpoint.ActorId.Value, (int)TraitKey.Acceleration, "main"), 3, 10);
            var forged = new MovementUndoCheckpoint(checkpoint.Movement, checkpoint.Participants, new[] { receipt });
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(move.WithFinalState(
                move.FinalState.With(movementUndo: new MovementUndoState(new[] { forged })))));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(move);
        }

        [Test]
        public void TrailUndo_AtomicallyDeactivatesOnlyCreatedSources_AndReplayDoesNothing()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TraitTrailPublicationTests.TrailSource());
            var before = battle.Actor;
            // 흔적은 지나간 (1,1)에만 두 종류 생긴다(출발·도착 칸 제외).
            battle.Submit(battle.PreviewMove(before, before.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2)));
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(state.MovementUndo.Checkpoints.Single().Effects, Has.Count.EqualTo(2));
            var undo = Undo(battle);
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            battle.Buffers.BattleWriteSink.Apply(undo);
            Assert.That(battle.Actor.SameValues(before), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Environment.Tiles.OfType<TraitTrailFact>().All(t => !t.Active), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.NextSourceId, Is.EqualTo(state.EnvironmentProgress.NextSourceId));
            var once = battle.CaptureStores();
            battle.Buffers.BattleWriteSink.Apply(undo);
            Assert.That(battle.CaptureStores(), Is.EqualTo(once));
        }

        [Test]
        public void TrailInverse_LatePrepareFailure_InstallsNeitherCharacterNorEnvironment()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TraitTrailPublicationTests.TrailSource());
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var valid = Undo(battle);
            var input = valid.Input;
            var invalid = CombatCommandCalculator.Calculate(input.WithPersistentFacts(input.Owned, input.ItemInventory,
                input.GoldBalance.Value + 1, input.FameBalance), Array.Empty<IMovementTileRule>());
            Assert.That(invalid.Accepted, Is.True, invalid.Reason);
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(invalid));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        [Test]
        public void StationaryAndAcceleration_UndoPublishesOneReturnAndPreservesOtherCharacters()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TraitMovementAttackPublicationTests.MovementAttackSource());
            var before = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var undo = Undo(battle);
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            battle.Buffers.BattleWriteSink.Apply(undo);
            Assert.That(battle.Buffers.CharacterReadSink.Characters.All(c => before.Any(p => p.SameValues(c.ToFacts()))), Is.True);
            Assert.That(undo.Events.Count(e => e.Kind == CombatEventKind.Returned), Is.EqualTo(1));
            Assert.That(undo.UndoMovement.RemovedEffects, Has.Count.EqualTo(2));
        }

        internal static CombatCommandTransition Undo(ConfiguredMovementBattle battle)
        {
            var buffer = battle.Buffers;
            var checkpoint = buffer.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints.Single(c => c.ActorId == battle.Actor.EntityId);
            int number = buffer.CombatViewWriteSink.Submit(CombatViewCommandData.UndoMove(battle.Actor.EntityId,
                checkpoint.BattleInstanceId, checkpoint.ControllerTurnSequence, checkpoint.MoveCommandNumber));
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.UndoMove,
                battle.Actor.EntityId, default, expectedBattleInstanceId: checkpoint.BattleInstanceId,
                expectedControllerTurnSequence: checkpoint.ControllerTurnSequence, expectedMoveCommandNumber: checkpoint.MoveCommandNumber));
            return CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
        }

        private static ConfiguredMovementBattle Battle()
        {
            var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ConfiguredMovementBattle.EncounterSource(new CellCoord(2, 3)));
            var enemy = battle.Character(EntityKey.Unit_Demon);
            var protectedEnemy = enemy.WithStatusEffects(new StatusEffectSet(enemy.StatusEffects.Entries.Append(
                new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Item, 91, 200, "test-shield"), 1000, true, 0)).ToArray()));
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(enemy, protectedEnemy) }, Array.Empty<SoulCountChange>()));
            return battle;
        }

        private static CombatCommandTransition Attack(ConfiguredMovementBattle battle)
        {
            return CombatCommandCalculator.Calculate(battle.Publication().Attack(battle.Actor, new CellCoord(2, 3)), Array.Empty<IMovementTileRule>());
        }
    }
}
