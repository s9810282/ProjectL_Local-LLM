using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Tests.Core
{
    public sealed class MovementEffectCombinationTests
    {
        [Test]
        public void SwapReconAccelerationTrailsAndPassive_RestoreOnlyOwnedFacts()
        {
            var input = PositionSwapMovementTests.Route();
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new PositionSwapTraitSpec(), new ReconSquadTraitSpec(),
                new AccelerationTraitSpec(3), new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1),
                new StationaryFireTraitSpec(20), new CohesionTraitSpec(2, 5, 1, 2));
            var overlap = TraitTrailTestData.Trail(10, input.Path.Origin, TraitKey.GroundCompaction, source: 90);
            input = TraitTrailTestData.Environment(input, overlap);
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            var checkpoint = move.FinalState.MovementUndo.Checkpoints.Single();
            Assert.That(checkpoint.Participants, Has.Count.EqualTo(2));
            Assert.That(checkpoint.Effects.Any(e => e is MovementShieldReceipt), Is.True);
            Assert.That(checkpoint.Effects.Any(e => e is MovementExposureReceipt), Is.True);
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            foreach (var before in input.Characters)
                Assert.That(TraitCombatFixtures.Final(undo, before.EntityId.Value).SameValues(before), Is.True, before.EntityId.ToString());
            Assert.That(undo.FinalState.Environment.Tiles.Single(t => t.Id == 10).SameValues(overlap), Is.True);
            Assert.That(undo.FinalState.Environment.Tiles.Where(t => t.Id != 10).All(t => !t.Active), Is.True);
            Assert.That(undo.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(move.FinalState.EnvironmentProgress.NextSourceId));
            Assert.That(move.FinalState.Environment.Tiles.All(t => t.Active), Is.True);
        }

        [Test]
        public void RecipientReturn_ClearsGrantWithoutUndoResurrection()
        {
            var move = MovementUndoTestData.Run(ReconSquadMovementTests.Route());
            var input = MovementUndoTestData.After(move);
            var target = input.Characters.Single(c => c.EntityId.Value == 2);
            var returned = CombatPlacementCalculator.Location(target, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var evidence = CombatChangeEvidenceReader.ReadCharacterChanges(new[] { target }, new[] { returned });
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == target.EntityId ? returned : c).ToArray(),
                state: input.State.With(movementUndo: input.State.MovementUndo.WithoutAffected(evidence)));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Command(input, CombatCommandKind.UndoMove, move.Input.Number));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(TraitCombatFixtures.Final(undo, 2).SameValues(returned), Is.True);
            Assert.That(undo.UndoMovement.RemovedEffects.OfType<MovementExposureReceipt>().Any(e => e.RecipientId == target.EntityId), Is.False);
        }

        [Test]
        public void SwapUndo_DoesNotRunReserveResetOnEitherParticipant()
        {
            var input = PositionSwapMovementTests.Route();
            var before = input.Characters.Single(c => c.EntityId.Value == 20);
            before = before.With(stats: before.Stats.With(traitAttackBonus: 4), heldEnvironment: new CharacterHeldEnvironmentState(41, true, 7),
                items: new CharacterItemState(null, new[] { new ItemTemporaryEffect(1, ItemStatKind.PhysicalDefense, 3, 0) }))
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[] {
                    new AttackIncreaseStatus(new EffectOriginKey(EffectOwnerKind.Trait, 90, (int)TraitKey.Vanguard, "main"), 4, 2),
                    new AttackDecreaseStatus(new EffectOriginKey(EffectOwnerKind.Trait, 91, (int)TraitKey.Intimidation, "main"), 20) }));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == before.EntityId ? before : c).ToArray());
            var move = MovementUndoTestData.Run(input);
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(TraitCombatFixtures.Final(undo, 20).SameValues(before), Is.True);
        }
    }
}
