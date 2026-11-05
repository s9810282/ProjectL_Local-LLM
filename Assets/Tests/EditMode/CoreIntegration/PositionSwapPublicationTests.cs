using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PositionSwapPublicationTests
    {
        [Test]
        public void ExchangeAndUndo_PublishBothOnce_WithImmutableOldSnapshots()
        {
            using var battle = Create();
            var actor = battle.Actor;
            var passenger = battle.Character(EntityKey.Unit_DynamiteHammer);
            var old = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == passenger.EntityId);
            Swap(battle);
            Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(2, 0)));
            Assert.That(battle.Character(EntityKey.Unit_DynamiteHammer).Anchor, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(battle.Actor.TraitEffects.ActiveShieldTotal, Is.EqualTo(10));
            Assert.That(battle.Character(EntityKey.Unit_DynamiteHammer).TraitEffects.ActiveShieldTotal, Is.EqualTo(10));
            var undo = MovementEffectPublicationTests.Undo(battle);
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.UndoMovement.Participants, Has.Count.EqualTo(2));
            Assert.That(undo.UndoMovement.DistanceReverted, Is.EqualTo(3));
            battle.Buffers.BattleWriteSink.Apply(undo);
            Assert.That(battle.Actor.SameValues(actor), Is.True);
            Assert.That(battle.Character(EntityKey.Unit_DynamiteHammer).SameValues(passenger), Is.True);
            Assert.That(old.ToFacts().SameValues(passenger), Is.True);
            var once = battle.CaptureStores();
            battle.Buffers.BattleWriteSink.Apply(undo);
            Assert.That(battle.CaptureStores(), Is.EqualTo(once));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LateFailureOrStalePassenger_DoesNotHalfRestore(bool stalePassenger)
        {
            using var battle = Create();
            Swap(battle);
            var undo = MovementEffectPublicationTests.Undo(battle);
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            if (stalePassenger)
            {
                var passenger = battle.Character(EntityKey.Unit_DynamiteHammer);
                battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] {
                    new CharacterStateChange(passenger, passenger.WithHealth(new HealthStateSnapshot(passenger.Health.CurrentHealth - 1, passenger.Health.MaxHealth))) },
                    Array.Empty<SoulCountChange>()));
            }
            else
            {
                var input = undo.Input;
                undo = CombatCommandCalculator.Calculate(input.WithPersistentFacts(input.Owned, input.ItemInventory,
                    input.GoldBalance.Value + 1, input.FameBalance), Array.Empty<IMovementTileRule>());
                Assert.That(undo.Accepted, Is.True, undo.Reason);
            }
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(undo));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(2, 0)));
            Assert.That(battle.Character(EntityKey.Unit_DynamiteHammer).Anchor, Is.EqualTo(new CellCoord(0, 1)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ForgedShieldOnThirdUnit_RejectsWholeMove(bool exchange)
        {
            using var battle = Create();
            var path = exchange ? new[] { new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 0) }
                : new[] { new CellCoord(0, 1), new CellCoord(1, 1) };
            var command = battle.PreviewMove(battle.Actor, path);
            int number = battle.Buffers.CombatViewWriteSink.Submit(command);
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move,
                command.EntityId, default, path: command.Path, movementContext: command.MovementContext,
                expectedBattleInstanceId: command.ExpectedBattleInstanceId, expectedControllerTurnSequence: command.ExpectedControllerTurnSequence));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var outsider = battle.Main;
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, input.ActorId.Value, (int)TraitKey.PositionSwap, "main");
            var altered = outsider.WithStatusEffects(new StatusEffectSet(outsider.StatusEffects.Entries
                .Append(new ShieldStatus(origin, 10, true, 0, Owner.Player)).ToArray()));
            var changes = new CharacterChanges(move.Characters.Characters.Append(new CharacterStateChange(outsider, altered)).ToArray(),
                move.Characters.Souls, move.Characters.Main, move.Characters.Created, move.Characters.Owned);
            var forged = new CombatCommandTransition(input, true, "", changes, move.Field, move.FinalState, move.Outcome,
                events: move.Events.Append(new CombatTraitStateEvent(outsider, altered)).ToArray(),
                pathMovement: move.PathMovement, actionUseSettlement: move.ActionUseSettlement);
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(MovementUndoPolicy.Reconcile(forged)));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(move);
        }

        [Test]
        public void PassengerMovement_CommitsExchange_AndAdvancesQueryRevision()
        {
            using var battle = Create();
            Swap(battle);
            var revision = battle.Buffers.CombatStateReadSink.QueryRevision;
            var passenger = battle.Character(EntityKey.Unit_DynamiteHammer);
            battle.Move(passenger, new CellCoord(1, 1));
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision + 1));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.TryGet(battle.Actor.EntityId, out _), Is.False);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(battle.Character(EntityKey.Unit_DynamiteHammer).StageRecord.DistanceMoved, Is.EqualTo(1));
        }

        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config,
            new HuntersMarkBattleSource(RankedCombatTestData.Spec(TraitKey.PositionSwap, 2)));

        private static void Swap(ConfiguredMovementBattle battle) => battle.Submit(battle.PreviewMove(battle.Actor,
            new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 0)));
    }
}
