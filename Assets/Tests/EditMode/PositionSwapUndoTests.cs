using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PositionSwapUndoTests
    {
        [Test]
        public void SecondSwapUndo_PreservesFirstGrant_AndOnlyGrantingSideStartExpiresRemainder()
        {
            var input = TraitCombatFixtures.Equip(PositionSwapMovementTests.Route(UnitActionUse.Available),
                EntityKey.Unit_Akimbo, new PositionSwapTraitSpec(10));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new PositionSwapTraitSpec(10));
            var first = MovementUndoTestData.Run(input);
            var second = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(first),
                CombatCommandKind.Move, actor: new EntityId(20), path: input.Path));
            Assert.That(second.Accepted, Is.True, second.Reason);
            var actor = TraitCombatFixtures.Final(second, 1);
            Assert.That(actor.TraitEffects.ActiveShieldTotal, Is.EqualTo(20));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(second));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(TraitCombatFixtures.Final(undo, 1).SameValues(TraitCombatFixtures.Final(first, 1)), Is.True);
            var current = MovementUndoTestData.After(second);
            var projected = current.Characters.ToDictionary(c => c.EntityId);
            projected[actor.EntityId] = actor.WithStatusEffects(ShieldStatusRules.Absorb(actor.StatusEffects, 7, out var damage));
            Assert.That(damage, Is.Zero);
            TraitTurnLifecycle.BeginControllerTurn(current, projected, Owner.Enemy);
            Assert.That(projected[actor.EntityId].TraitEffects.ActiveShieldTotal, Is.EqualTo(13));
            TraitTurnLifecycle.BeginControllerTurn(current, projected, Owner.Player);
            Assert.That(projected[actor.EntityId].TraitEffects.ActiveShieldTotal, Is.Zero);
        }

        [Test]
        public void ExistingSwapOrigin_AddsToUnspentBalanceAndUndoRestoresIt()
        {
            var input = TraitCombatFixtures.Equip(PositionSwapMovementTests.Route(), EntityKey.Unit_Akimbo,
                new PositionSwapTraitSpec(10));
            var origin = TraitEffectOrigin.Create(input.ActorId, TraitKey.PositionSwap);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 20
                ? c.WithStatusEffects(ShieldStatusRules.Restore(c.StatusEffects, origin, new ShieldStatus(origin, 3, true, 0, Owner.Player))) : c).ToArray());
            var result = MovementUndoTestData.Run(input);
            Assert.That(TraitCombatFixtures.Final(result, 20).TraitEffects.ActiveShieldTotal, Is.EqualTo(13));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(result));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(TraitCombatFixtures.Final(undo, 20).TraitEffects.ActiveShieldTotal, Is.EqualTo(3));
        }

        [Test]
        public void RankedSwap_RestoresBothOriginsAndRejectsConsumedBalance()
        {
            var input = TraitCombatFixtures.Equip(PositionSwapMovementTests.Route(), EntityKey.Unit_Akimbo,
                new PositionSwapTraitSpec(10));
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.FinalState.MovementUndo.Checkpoints.Single().Effects.OfType<MovementShieldReceipt>().Count(), Is.EqualTo(2));
            var undoInput = MovementUndoTestData.Undo(move);
            var passenger = undoInput.Characters.Single(c => c.EntityId.Value == 20);
            var remaining = ShieldStatusRules.Absorb(passenger.StatusEffects, 7, out var damage);
            Assert.That(damage, Is.Zero);
            var stale = CombatCommandTestData.With(undoInput, characters: undoInput.Characters.Select(c =>
                c.EntityId == passenger.EntityId ? c.WithStatusEffects(remaining) : c).ToArray());
            Assert.That(MovementUndoTestData.Run(stale).Accepted, Is.False);
            var undo = MovementUndoTestData.Run(undoInput);
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            foreach (int id in new[] { 1, 20 })
                Assert.That(TraitCombatFixtures.Final(undo, id).SameValues(input.Characters.Single(c => c.EntityId.Value == id)), Is.True);
        }

        [Test]
        public void Undo_RestoresBothWithoutEntryEffects_AndPreservesPriorPassengerAction()
        {
            var input = PositionSwapMovementTests.Route();
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, PositionSwapMovementTests.Spec(),
                new AccelerationTraitSpec(3), new GroundCompactionTraitSpec(1, 1), new StationaryFireTraitSpec(20));
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.FinalState.MovementUndo.Checkpoints.Single().Participants, Has.Count.EqualTo(2));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.UndoMovement.Participants, Has.Count.EqualTo(2));
            foreach (int id in new[] { 1, 20 })
                Assert.That(TraitCombatFixtures.Final(undo, id).SameValues(input.Characters.Single(c => c.EntityId.Value == id)), Is.True);
            Assert.That(undo.Events.OfType<CombatCharacterEvent>().Select(e => e.Kind), Is.EqualTo(new[] { CombatEventKind.Returned, CombatEventKind.Returned }));
            Assert.That(undo.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t =>
                t.TraitActivationKind == TraitActivationKind.TrailCreated || t.TraitKey == TraitKey.PositionSwap), Is.False);
            Assert.That(undo.FinalState.Environment.Tiles.All(t => !t.Active), Is.True);
            Assert.That(undo.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(move.FinalState.EnvironmentProgress.NextSourceId));
            Assert.That(undo.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [TestCase(1, false)]
        [TestCase(20, false)]
        [TestCase(20, true)]
        public void EitherParticipantLaterMoveOrAttack_Commits(int actor, bool move)
        {
            var input = WithEnemy(PositionSwapMovementTests.Route(UnitActionUse.Available));
            var swap = MovementUndoTestData.Run(input);
            var current = MovementUndoTestData.After(swap);
            var command = MovementUndoTestData.Command(current, move ? CombatCommandKind.Move : CombatCommandKind.Attack,
                actor: new EntityId(actor), path: move ? new MovementPath(new[] { new CellCoord(1, 1), new CellCoord(1, 2) }) : null,
                aim: AttackAim.ForCell(new CellCoord(2, 2)));
            var result = MovementUndoTestData.Run(command);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.MovementUndo.TryGet(new EntityId(1), out _), Is.False);
            Assert.That(result.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.ParticipantAction && u.ConsumerId.Value == actor), Is.True);
            if (move)
            {
                var undoConsumer = MovementUndoTestData.Run(MovementUndoTestData.Undo(result));
                Assert.That(undoConsumer.Accepted, Is.True, undoConsumer.Reason);
                Assert.That(undoConsumer.FinalState.MovementUndo.TryGet(new EntityId(1), out _), Is.False);
            }
        }

        [Test]
        public void ReactionAttack_Commits_WhileQueriesAndRejectedActionsDoNot()
        {
            var swap = MovementUndoTestData.Run(WithEnemy(PositionSwapMovementTests.Route()));
            var current = MovementUndoTestData.After(swap);
            var passenger = current.Characters.Single(c => c.EntityId.Value == 20);
            var rejected = MovementUndoTestData.Run(MovementUndoTestData.Command(current, CombatCommandKind.Move,
                actor: passenger.EntityId, path: new MovementPath(new[] { new CellCoord(1, 1), new CellCoord(1, 2) })));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(MovementUndoCalculator.Evaluate(MovementUndoTestData.Undo(swap)).CanUndo, Is.True);
            var reaction = CombatAttackCalculator.Reaction(current, Array.Empty<IMovementTileRule>(), passenger, AttackAim.ForCell(new CellCoord(2, 2)));
            Assert.That(reaction, Is.Not.Null);
            var result = MovementUndoPolicy.Reconcile(reaction);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void OldPassengerCheckpoint_IsNeverResurrected()
        {
            var input = PositionSwapMovementTests.Route(UnitActionUse.Available);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 20
                ? c.WithPlacement(c.Area, null, new CellCoord(3, 2), c.Rotation, new[] { new CellCoord(3, 2) }) : c).ToArray());
            var earlier = MovementUndoTestData.Run(MovementUndoTestData.Command(input, CombatCommandKind.Move, actor: new EntityId(20),
                path: new MovementPath(new[] { new CellCoord(3, 2), new CellCoord(3, 1) })));
            Assert.That(earlier.FinalState.MovementUndo.TryGet(new EntityId(20), out _), Is.True);
            var swap = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(earlier), CombatCommandKind.Move,
                actor: new EntityId(1), path: input.Path));
            Assert.That(swap.FinalState.MovementUndo.Checkpoints.Select(c => c.ActorId.Value), Is.EqualTo(new[] { 1 }));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(swap));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void UnrelatedAction_PreservesExchange()
        {
            var input = PositionSwapMovementTests.Route();
            var swap = MovementUndoTestData.Run(input);
            var unrelated = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(swap), CombatCommandKind.Move,
                actor: new EntityId(10), path: new MovementPath(new[] { new CellCoord(7, 7), new CellCoord(6, 7) })));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(unrelated), CombatCommandKind.UndoMove,
                swap.Input.Number, actor: new EntityId(1)));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(TraitCombatFixtures.Final(undo, 10).Anchor, Is.EqualTo(new CellCoord(6, 7)));
            Assert.That(undo.FinalState.MovementUndo.TryGet(new EntityId(10), out _), Is.True);
        }

        private static CombatCommandInput WithEnemy(CombatCommandInput input) => CombatCommandTestData.With(input,
            characters: input.Characters.Append(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy)).ToArray());
    }
}
