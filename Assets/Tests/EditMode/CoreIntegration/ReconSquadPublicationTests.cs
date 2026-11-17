using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ReconSquadPublicationTests
    {
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void GrantAndUndo_PublishFullSourceFactsOnce_WithUnrelatedRecipientHealthPreserved(int percent)
        {
            using var battle = Create(increasePercent: percent);
            var old = battle.Buffers.CharacterReadSink.Characters.Single(c => c.CharacterKey == EntityKey.Unit_Demon);
            var before = battle.CaptureGameplay();
            var request = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(0, 2));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            battle.Submit(request);
            var exposed = battle.Character(EntityKey.Unit_Demon);
            var row = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == exposed.EntityId)
                .StatusEffectSnapshots.OfType<WeaknessExposureEffectSnapshot>().Single();
            Assert.That(row.EffectiveIncreasePercent, Is.EqualTo(percent));
            Assert.That(row.Sources.Single().RemainingAffectedSideTurnEnds, Is.Null);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] {
                new CharacterStateChange(exposed, exposed.WithHealth(new HealthStateSnapshot(99, 100))) }, Array.Empty<SoulCountChange>()));
            var undo = MovementEffectPublicationTests.Undo(battle);
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.UndoMovement.RemovedEffects.Single(), Is.TypeOf<MovementExposureReceipt>());
            battle.Buffers.BattleWriteSink.Apply(undo);
            var final = battle.Character(EntityKey.Unit_Demon);
            Assert.That(final.StatusEffects.Entries.OfType<WeaknessExposureStatus>(), Is.Empty);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(99));
            Assert.That(old.StatusEffects.Entries.OfType<WeaknessExposureStatus>(), Is.Empty);
            Assert.That(row.Sources, Has.Count.EqualTo(1));
            Assert.That(undo.Events.OfType<CombatTraitStateEvent>().Single().Final.SameValues(final), Is.True);
            var once = battle.CaptureStores();
            battle.Buffers.BattleWriteSink.Apply(undo);
            Assert.That(battle.CaptureStores(), Is.EqualTo(once));
        }

        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void ShieldOnlyAlliedHit_CommitsOriginalMoveAndAdvancesQueryRevision(int percent)
        {
            using var battle = Create(shield: true, increasePercent: percent);
            battle.Move(battle.Actor, new CellCoord(0, 2));
            var attack = Attack(battle);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.ExposureDamage), Is.True);
            var revision = battle.Buffers.CombatStateReadSink.QueryRevision;
            battle.Buffers.BattleWriteSink.Apply(attack);
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision + 1));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LatePreparationFailure_InstallsNeitherInverseNorUseLock(bool undoing)
        {
            using var battle = Create(shield: true);
            battle.Move(battle.Actor, new CellCoord(0, 2));
            var valid = undoing ? MovementEffectPublicationTests.Undo(battle) : Attack(battle);
            var input = valid.Input;
            var invalid = CombatCommandCalculator.Calculate(input.WithPersistentFacts(input.Owned, input.ItemInventory,
                input.GoldBalance.Value + 1, input.FameBalance), Array.Empty<IMovementTileRule>());
            Assert.That(invalid.Accepted, Is.True, invalid.Reason);
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(invalid));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(valid);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void ReplacedContribution_RejectsStaleUndoAndNeverReopens()
        {
            using var battle = Create();
            battle.Move(battle.Actor, new CellCoord(0, 2));
            var pending = MovementEffectPublicationTests.Undo(battle);
            var before = battle.Character(EntityKey.Unit_Demon);
            var source = before.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Single();
            var after = before.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] {
                new WeaknessExposureStatus(source.Origin.Value, 20, source.LifetimeKind, movement: source.Movement) }));
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before, after) }, Array.Empty<SoulCountChange>()));
            var saved = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(pending));
            Assert.That(battle.CaptureStores(), Is.EqualTo(saved));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void BadgeProtectedAlliedHit_CommitsReconBeforeHeldShield()
        {
            using var battle = Create();
            var enemy = battle.Character(EntityKey.Unit_Demon);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(enemy,
                HeldEnvironmentEffects.AcquireBadge(enemy, 99)) }, Array.Empty<SoulCountChange>()));
            battle.Move(battle.Actor, new CellCoord(0, 2));
            var attack = Attack(battle);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.ExposureDamage), Is.True);
            battle.Buffers.BattleWriteSink.Apply(attack);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(battle.Character(EntityKey.Unit_Demon).HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OmittedNumericUseGroup_CannotReopenOriginalCheckpoint(bool freshConstructor)
        {
            using var battle = Create(shield: true);
            battle.Move(battle.Actor, new CellCoord(0, 2));
            var valid = Attack(battle);
            Assert.That(valid.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.ExposureDamage), Is.True);
            var reopened = valid.FinalState.With(movementUndo: valid.Input.State.MovementUndo);
            var forged = freshConstructor ? new CombatCommandTransition(valid.Input, true, "", valid.Characters, valid.Field, reopened,
                valid.Outcome, valid.Attack, valid.ConsumedSoulIds, valid.Completion, valid.Movement, valid.Events, valid.PathMovement,
                valid.EnvironmentBindings, valid.BlockedContacts, valid.InventoryChange, valid.RewardChange, valid.RewardSettlement,
                valid.ActionUseSettlement, valid.UndoMovement) : valid.WithMovementEffectUses(Array.Empty<MovementEffectUse>()).WithFinalState(reopened);
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        private static CombatCommandTransition Attack(ConfiguredMovementBattle battle) => CombatCommandCalculator.Calculate(
            battle.Publication().Attack(battle.Character(EntityKey.Unit_DynamiteHammer), battle.Character(EntityKey.Unit_Demon).Anchor.Value),
            Array.Empty<IMovementTileRule>());

        private static ConfiguredMovementBattle Create(bool shield = false, int increasePercent = 10)
        {
            var trait = new ReconSquadTraitSpec(increasePercent);
            var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HuntersMarkBattleSource(trait));
            if (shield)
            {
                var enemy = battle.Character(EntityKey.Unit_Demon);
                var protectedEnemy = enemy.WithStatusEffects(new StatusEffectSet(enemy.StatusEffects.Entries.Append(
                    new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Item, 91, 200, "test-shield"), 1000, true, 0)).ToArray()));
                battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(enemy, protectedEnemy) }, Array.Empty<SoulCountChange>()));
            }
            return battle;
        }
    }
}
