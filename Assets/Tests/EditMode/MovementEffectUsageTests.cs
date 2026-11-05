using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class MovementEffectUsageTests
    {
        [Test]
        public void AccelerationUndo_SubtractsOnlyMoveGain()
        {
            var input = TraitCombatFixtures.Equip(MovementUndoTestData.Route(), EntityKey.Unit_Akimbo, new AccelerationTraitSpec(1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.With(stats: AccelerationTraitRule.WithHeld(c.Stats, 7)) : c).ToArray());
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var move = MovementUndoTestData.Run(input);
            Assert.That(AccelerationTraitRule.Held(MovementTestData.Actor(move).Stats), Is.EqualTo(10));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(MovementTestData.Actor(undo).SameValues(before), Is.True);
            Assert.That(undo.UndoMovement.RemovedEffects.Single(), Is.TypeOf<MovementAccelerationReceipt>());
        }

        [Test]
        public void TrailUndo_PreservesOverlapAndNeverRewindsAllocation()
        {
            var input = TraitCombatFixtures.Equip(MovementUndoTestData.Route(), EntityKey.Unit_Akimbo,
                new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1));
            var earlier = TraitTrailTestData.Trail(10, input.Path.Origin, TraitKey.GroundCompaction, source: 9);
            input = TraitTrailTestData.Environment(input, earlier);
            var move = MovementUndoTestData.Run(input);
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.FinalState.Environment.Tiles.Single(t => t.Id == 10).SameValues(earlier), Is.True);
            Assert.That(undo.FinalState.Environment.Tiles.OfType<TraitTrailFact>().Where(t => t.Id != 10).All(t => !t.Active), Is.True);
            Assert.That(undo.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(move.FinalState.EnvironmentProgress.NextSourceId));
            Assert.That(undo.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(move.FinalState.EnvironmentProgress.RandomState));
            Assert.That(move.FinalState.Environment.Tiles.OfType<TraitTrailFact>().All(t => t.Active), Is.True);
        }

        [Test]
        public void ShortDeparture_UsesPreexistingPackedGround_AndConsumerUndoDoesNotReopenProducer()
        {
            var input = ProducerAcrossAlly();
            var producer = MovementUndoTestData.Run(input);
            Assert.That(producer.FinalState.MovementUndo.TryGet(input.ActorId, out _), Is.True, "Newly created ground must not consume its own move.");
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(producer), CombatCommandKind.Move,
                actor: new EntityId(20), path: new MovementPath(new[] { new CellCoord(1, 2), new CellCoord(2, 2) }));
            var consumer = MovementUndoTestData.Run(request);
            Assert.That(consumer.Accepted, Is.True, consumer.Reason);
            Assert.That(consumer.FinalState.MovementUndo.TryGet(input.ActorId, out _), Is.False);
            Assert.That(consumer.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.GroundDeparture), Is.True);
            var undoConsumer = MovementUndoTestData.Run(MovementUndoTestData.Undo(consumer));
            Assert.That(undoConsumer.Accepted, Is.True, undoConsumer.Reason);
            Assert.That(undoConsumer.FinalState.MovementUndo.TryGet(input.ActorId, out _), Is.False);
            Assert.That(undoConsumer.FinalState.Environment.SameValues(producer.FinalState.Environment), Is.True);
        }

        [Test]
        public void DustDamage_CommitsActualRecipientSources_EvenWithFullShield()
        {
            var input = ProducerAcrossAlly(new DustScreenTraitSpec(1, 20, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 20
                ? WeaknessExposureDamageTests.Shield(c, 1000) : c).Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy)).ToArray());
            var move = MovementUndoTestData.Run(input);
            var current = MovementUndoTestData.After(move);
            var enemy = current.Characters.Single(c => c.EntityId.Value == 2);
            var attacked = CombatAttackCalculator.Reaction(current, Array.Empty<IMovementTileRule>(), enemy, AttackAim.ForCell(new CellCoord(1, 2)));
            Assert.That(attacked, Is.Not.Null);
            var committed = MovementUndoPolicy.Reconcile(attacked);
            Assert.That(committed.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.DustDefense), Is.True);
            Assert.That(committed.FinalState.MovementUndo.TryGet(input.ActorId, out _), Is.False);
            Assert.That(TraitCombatFixtures.Final(committed, 20).Health.CurrentHealth,
                Is.EqualTo(current.Characters.Single(c => c.EntityId.Value == 20).Health.CurrentHealth));
        }

        [Test]
        public void CapturedGain_NumericHitCommitsEvenWithFullShield()
        {
            var input = TraitCombatFixtures.Equip(MovementUndoTestData.WithEnemy(MovementUndoTestData.Route()), EntityKey.Unit_Akimbo, new AccelerationTraitSpec(1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? WeaknessExposureDamageTests.Shield(c, 1000) : c).ToArray());
            var move = MovementUndoTestData.Run(input);
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(move), CombatCommandKind.Attack, aim: AttackAim.ForCell(new CellCoord(2, 2)));
            var attack = MovementUndoTestData.Run(request);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.AccelerationDamage), Is.True);
            Assert.That(attack.FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(TraitCombatFixtures.Final(attack, 2).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void DustAttackStart_CommitsRestrictedRangeSource_ButPreviewAndRejectionDoNot()
        {
            var input = ProducerAcrossAlly(new DustScreenTraitSpec(1, 20, 1));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy)).ToArray());
            var move = MovementUndoTestData.Run(input);
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(move), CombatCommandKind.Attack,
                actor: new EntityId(20), aim: AttackAim.ForCell(new CellCoord(3, 2)));
            Assert.That(CombatCommandCalculator.Preview(request, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            Assert.That(request.State.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var rejected = MovementUndoTestData.Run(CombatAttackQueries.WithAim(request, AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var attack = MovementUndoTestData.Run(request);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.DustAttackRange), Is.True);
            Assert.That(attack.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void TiedDustMaxima_AllCommit_ButSuppressedTrailIsNotUsed()
        {
            var input = ProducerAcrossAlly(new DustScreenTraitSpec(1, 20, 1));
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 20
                ? WeaknessExposureDamageTests.Shield(c, 1000) : c).Append(
                CombatCommandTestData.Character(30, EntityKey.Unit_Akimbo, new CellCoord(1, 4), Owner.Player)).Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new DustScreenTraitSpec(1, 20, 1), new LeapTraitSpec());
            input = TraitTrailTestData.Environment(input, TraitTrailTestData.Trail(10, new CellCoord(1, 2), TraitKey.DustScreen, percent: 5, source: 99));
            var first = MovementUndoTestData.Run(input);
            var second = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(first), CombatCommandKind.Move,
                actor: new EntityId(30), path: new MovementPath(new[] { new CellCoord(1, 4), new CellCoord(1, 3), new CellCoord(1, 2), new CellCoord(1, 1) })));
            Assert.That(second.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(2));
            var current = MovementUndoTestData.After(second);
            var reaction = CombatAttackCalculator.Reaction(current, Array.Empty<IMovementTileRule>(),
                current.Characters.Single(c => c.EntityId.Value == 2), AttackAim.ForCell(new CellCoord(1, 2)));
            var result = MovementUndoPolicy.Reconcile(reaction);
            var uses = result.MovementEffectUses.Where(u => u.Kind == MovementEffectUseKind.DustDefense).ToArray();
            Assert.That(uses.Select(u => u.Source.Value.OwnerInstanceId), Is.EquivalentTo(new[] { 1, 30 }));
            Assert.That(uses.All(u => u.SourceId != 10), Is.True);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        internal static CombatCommandInput ProducerAcrossAlly(params TraitSpec[] trails)
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(1, 3));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(1, 2), Owner.Player)).ToArray());
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo,
                (trails.Length == 0 ? new TraitSpec[] { new GroundCompactionTraitSpec(1, 1) } : trails).Append(new LeapTraitSpec()).ToArray());
        }
    }
}
