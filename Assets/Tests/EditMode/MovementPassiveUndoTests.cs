using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class MovementPassiveUndoTests
    {
        [Test]
        public void StationaryFlag_RestoresOnlyUnconsumedVersion()
        {
            var input = TraitCombatFixtures.Equip(MovementUndoTestData.Route(), EntityKey.Unit_Akimbo,
                new StationaryFireTraitSpec(100), new AccelerationTraitSpec(1));
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var move = MovementUndoTestData.Run(input);
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(MovementTestData.Actor(undo).SameValues(before), Is.True);
            Assert.That(undo.UndoMovement.RemovedEffects.OfType<MovementStationaryReceipt>().Count(), Is.EqualTo(1));
            var changed = MovementTestData.Actor(move).With(stats: MovementTestData.Actor(move).Stats.With(traitStatuses:
                MovementTestData.Actor(move).Stats.TraitStatuses.ToArray().Select(t => t is StationaryFireTraitStatusSnapshot
                    ? new StationaryFireTraitStatusSnapshot(false) : t).ToArray()));
            var committed = move.FinalState.MovementUndo.WithoutAffected(CombatChangeEvidenceReader.ReadCharacterChanges(
                new[] { MovementTestData.Actor(move) }, new[] { changed }));
            Assert.That(committed.Checkpoints, Is.Empty);
        }

        [Test]
        public void PresenceUndo_PreservesUnrelatedShieldBudgetAndRecipientHealth()
        {
            var input = PresenceRoute();
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var current = MovementUndoTestData.After(move);
            var recipient = current.Characters.Single(c => c.EntityId.Value == 20);
            var changed = recipient.WithStatusEffects(new StatusEffectSet(recipient.StatusEffects.Entries.Select(s =>
                s is ShieldStatus shield && shield.Origin.Value.Kind == EffectOwnerKind.Item
                    ? new ShieldStatus(shield.Origin.Value, 4, shield.Eligible, shield.LastResetControllerTurnCount) : s).ToArray()))
                .WithHealth(new HealthStateSnapshot(99, 100));
            var evidence = CombatChangeEvidenceReader.ReadCharacterChanges(new[] { recipient }, new[] { changed });
            current = CombatCommandTestData.With(current, state: current.State.With(movementUndo: current.State.MovementUndo.WithoutAffected(evidence)),
                characters: current.Characters.Select(c => c.EntityId == changed.EntityId ? changed : c).ToArray());
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Command(current, CombatCommandKind.UndoMove, move.Input.Number));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            var final = TraitCombatFixtures.Final(undo, 20);
            var shared = final.StatusEffects.Entries.OfType<ShieldStatus>().Single(s => s.Origin.Value.Kind == EffectOwnerKind.Trait);
            Assert.That(shared.RemainingBudget, Is.EqualTo(3));
            Assert.That(shared.Eligible, Is.True);
            Assert.That(final.StatusEffects.Entries.OfType<ShieldStatus>().Single(s => s.Origin.Value.Kind == EffectOwnerKind.Item).RemainingBudget, Is.EqualTo(4));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(99));
            Assert.That(MovementTestData.Actor(undo).TraitEffects.CohesionDefenseBonus, Is.EqualTo(2));
        }

        [Test]
        public void ConsumedOrReplacedContribution_CommitsPermanently()
        {
            var move = MovementUndoTestData.Run(PresenceRoute());
            var recipient = TraitCombatFixtures.Final(move, 20);
            var changed = recipient.WithStatusEffects(new StatusEffectSet(recipient.StatusEffects.Entries.Select(s =>
                s is ShieldStatus shield && shield.Origin.Value.Kind == EffectOwnerKind.Trait
                    ? new ShieldStatus(shield.Origin.Value, 2, shield.Eligible, shield.LastResetControllerTurnCount) : s).ToArray()));
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var state = move.FinalState.MovementUndo.WithoutAffected(CombatChangeEvidenceReader.ReadCharacterChanges(new[] { recipient }, new[] { changed }));
            state = state.WithoutAffected(CombatChangeEvidenceReader.ReadCharacterChanges(new[] { changed }, new[] { recipient }));
            Assert.That(state.Checkpoints, Is.Empty);
        }

        [TestCase(6, 1, false)]
        [TestCase(1, 3, true)]
        public void TauntRangeChange_UndoRecomputesTauntedWithoutReceipt(int enemyX, int enemyY, bool tauntedBefore)
        {
            // 도발 보유자가 (1,1) → (4,1)로 가며 적이 반경 2 안으로 들어오거나(6,1) 밖으로 나갑니다(1,3).
            var input = MovementUndoTestData.Route();
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(enemyX, enemyY), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new TauntTraitSpec(2));
            var initial = TraitPresenceReconciler.Calculate(input, input.Characters).Characters;
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => initial.SingleOrDefault(s => s.Final.EntityId == c.EntityId)?.Final ?? c).ToArray());
            Assert.That(Taunted(input.Characters.Single(c => c.EntityId.Value == 2)), Is.EqualTo(tauntedBefore));

            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(Taunted(TraitCombatFixtures.Final(move, 2)), Is.EqualTo(!tauntedBefore));
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));

            // 이동 직후 행동 가능 여부 조회가 되돌리기 후보를 만들다 예외로 멈추던 자리입니다.
            var availability = CombatCommandCalculator.GetActionAvailability(MovementUndoTestData.After(move));
            Assert.That(availability.MovementUndo.CanUndo, Is.True, availability.MovementUndo.Failure.ToString());

            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(move));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.UndoMovement.RemovedEffects, Is.Empty);
            Assert.That(Taunted(TraitCombatFixtures.Final(undo, 2)), Is.EqualTo(tauntedBefore));
            Assert.That(MovementTestData.Actor(undo).Anchor, Is.EqualTo(new CellCoord(1, 1)));
        }

        private static bool Taunted(CharacterCombatFacts character) => character.StatusEffects.Entries.OfType<TauntedStatus>().Any();

        private static CombatCommandInput PresenceRoute()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(1, 2), Owner.Player)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new CohesionTraitSpec(2, 5, 1, 100));
            var initial = TraitPresenceReconciler.Calculate(input, input.Characters).Characters;
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => initial.SingleOrDefault(s => s.Final.EntityId == c.EntityId)?.Final ?? c).ToArray());
            return CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 20
                ? c.WithStatusEffects(new StatusEffectSet(c.StatusEffects.Entries.Select(s => s is ShieldStatus shield
                    ? new ShieldStatus(shield.Origin.Value, 3, shield.Eligible, shield.LastResetControllerTurnCount) : s).Append(
                    new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Item, 99, 1, "independent"), 7, true, 0)).ToArray())) : c).ToArray());
        }
    }
}
