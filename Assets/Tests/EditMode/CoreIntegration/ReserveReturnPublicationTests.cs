using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ReserveReturnPublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ManualReturn_ClearsEffects_RefundsOnlyEquippedItemOnce(bool equipped)
        {
            using var battle = Create();
            AddEffects(battle, equipped);
            var old = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == battle.Actor.EntityId);
            var proposal = Return(battle);
            Assert.That(proposal.Accepted, Is.True, proposal.Reason);
            battle.Buffers.BattleWriteSink.Apply(proposal);
            Assert.That(battle.Actor.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(battle.Actor.StatusEffects.Entries, Is.Empty);
            Assert.That(battle.Actor.Stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>().Single().HeldAttackBonus, Is.Zero);
            Assert.That(battle.Actor.Items.SameValues(CharacterItemState.Empty), Is.True);
            Assert.That(battle.Buffers.ItemInventoryReadSink.Current.GetCount(1), Is.EqualTo(equipped ? 1 : 0));
            Assert.That(old.StatusEffects.Entries, Has.Count.EqualTo(2));
            Assert.That(proposal.Events.OfType<CombatTraitStateEvent>().Last().Final.StatusEffects.SameValues(battle.Actor.StatusEffects), Is.True);
            var once = battle.CaptureStores();
            battle.Buffers.BattleWriteSink.Apply(proposal);
            Assert.That(battle.CaptureStores(), Is.EqualTo(once));
        }

        [Test]
        public void InventoryPrepareFailure_ClearsNothing()
        {
            using var battle = Create();
            AddEffects(battle, equipped: true);
            var valid = Return(battle);
            var input = valid.Input;
            var invalid = CombatCommandCalculator.Calculate(input.WithPersistentFacts(input.Owned,
                new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 1 }), input.GoldBalance, input.FameBalance), Array.Empty<IMovementTileRule>());
            Assert.That(invalid.Accepted, Is.True, invalid.Reason);
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(invalid));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(valid);
            Assert.That(battle.Actor.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(battle.Actor.StatusEffects.Entries, Is.Empty);
        }

        [Test]
        public void MaxHpNormalization_HasNoNumericDamageOrRecoveryCredit()
        {
            using var battle = Create();
            var initial = battle.Actor;
            var boosted = initial.With(health: new HealthStateSnapshot(initial.Health.CurrentHealth + 10, initial.Health.MaxHealth + 10),
                stats: initial.Stats.With(artifactAttackBonus: initial.Stats.ArtifactAttackBonus + 5), temporaryAttackBonus: 5, temporaryMaxHealthBonus: 10);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(initial, boosted) }, Array.Empty<SoulCountChange>()));
            var proposal = Return(battle);
            Assert.That(proposal.Accepted, Is.True, proposal.Reason);
            var normalization = proposal.HealthExecutions.Single().Steps.Single();
            var expected = new HealthStateSnapshot(
                Math.Min(boosted.Health.CurrentHealth, initial.Health.MaxHealth), initial.Health.MaxHealth);
            Assert.That(normalization.RecipientId, Is.EqualTo(initial.EntityId));
            Assert.That(normalization.Source.Kind, Is.EqualTo(CombatHealthOperationKind.Normalization));
            Assert.That(normalization.Before, Is.EqualTo(boosted.Health));
            Assert.That(normalization.After, Is.EqualTo(expected));
            Assert.That(normalization.NormalizationLoss, Is.EqualTo(boosted.Health.CurrentHealth - expected.CurrentHealth));
            Assert.That(normalization.ActualLoss, Is.Zero);
            Assert.That(normalization.ActualRecovery, Is.Zero);
            Assert.That(proposal.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.HealthChanged), Is.False);
            battle.Buffers.BattleWriteSink.Apply(proposal);
            Assert.That(battle.Actor.Health, Is.EqualTo(expected));
            Assert.That(battle.Actor.StageRecord.SameValues(initial.StageRecord), Is.True);
            Assert.That(battle.Actor.TemporaryMaxHealthBonus, Is.Zero);
        }

        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config, new ReturnSource(), enterCombat: false);

        private static CombatCommandTransition Return(ConfiguredMovementBattle battle)
        {
            int number = battle.Buffers.CombatViewWriteSink.Submit(CombatViewCommandData.ReturnUnit(battle.Actor.EntityId));
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.ReturnUnit, battle.Actor.EntityId, default));
            return CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
        }

        private static void AddEffects(ConfiguredMovementBattle battle, bool equipped)
        {
            var actor = battle.Actor;
            var changed = actor.With(stats: actor.Stats.With(traitAttackBonus: 3, traitStatuses: new TraitStatusSnapshot[] { new AccelerationTraitStatusSnapshot(3) }),
                items: new CharacterItemState(equipped ? 1 : null, new[] { new ItemTemporaryEffect(1, ItemStatKind.PhysicalDefense, 3) }))
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[] {
                    new AttackDecreaseStatus(new EffectOriginKey(EffectOwnerKind.Trait, 90, (int)TraitKey.Intimidation, "main"), 20),
                    new WeaknessExposureStatus(new EffectOriginKey(EffectOwnerKind.Trait, 91, (int)TraitKey.ReconSquad, "main"), 10,
                        WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath) }));
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor, changed) }, Array.Empty<SoulCountChange>()));
        }

        private sealed class ReturnSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = new HuntersMarkBattleSource(new AccelerationTraitSpec(1)).Read(database, chapterKey, stageKey);
                return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation, source.Units, source.Structures,
                    source.Categories, source.AttackProfiles, itemDefinitions: new[] { new ItemDefinition(1, new WhiskeyItemSpec(50, 10)) }, clearRewards: source.ClearRewards);
            }
        }
    }
}
