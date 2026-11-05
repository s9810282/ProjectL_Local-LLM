using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CombatItemContractTests
    {
        [Test]
        public void Snapshot_CopiesInputCounts()
        {
            var counts = new Dictionary<int, int> { [1] = 2 };
            var snapshot = new ItemInventorySnapshot(counts);
            counts[1] = 99;
            Assert.That(snapshot.GetCount(1), Is.EqualTo(2));
            Assert.That(snapshot.GetCount(2), Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<int, int>)snapshot.Counts)[1] = 7);
        }

        [Test]
        public void Snapshot_EqualityIgnoresEnumerationOrderAndZeroEntries()
        {
            var first = new ItemInventorySnapshot(new Dictionary<int, int> { [2] = 3, [1] = 2, [3] = 0 });
            var same = new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 2, [2] = 3 });
            var changed = new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 1, [2] = 3 });
            Assert.That(first.SameValues(same), Is.True);
            Assert.That(first.SameValues(changed), Is.False);
            Assert.That(first.SameValues(null), Is.False);
        }

        [TestCase(0, 1)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void Snapshot_RejectsInvalidCountDomains(int key, int quantity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ItemInventorySnapshot(new Dictionary<int, int> { [key] = quantity }));
        }

        [Test]
        public void Snapshot_RejectsNullAndInvalidLookupButAcceptsMaximumCount()
        {
            Assert.Throws<ArgumentNullException>(() => new ItemInventorySnapshot(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemInventorySnapshot.Empty.GetCount(0));
            var full = new ItemInventorySnapshot(new Dictionary<int, int> { [1] = int.MaxValue });
            Assert.That(full.GetCount(1), Is.EqualTo(int.MaxValue));
            Assert.Throws<ArgumentNullException>(() => new ItemInventoryChange(null, full));
            Assert.Throws<ArgumentNullException>(() => new ItemInventoryChange(full, null));
        }

        [Test]
        public void State_CopiesEffectsAndRetainsFirstDuplicateWithoutRefreshingExpiry()
        {
            var original = new ItemTemporaryEffect(3, ItemStatKind.Priority, 10, 1);
            var effects = new List<ItemTemporaryEffect> { original,
                new ItemTemporaryEffect(3, ItemStatKind.Priority, 20, 2),
                new ItemTemporaryEffect(4, ItemStatKind.PhysicalDefense, 5, 1) };
            var state = new CharacterItemState(1, effects);
            effects.Clear();
            Assert.That(state.Effects.Count, Is.EqualTo(2));
            Assert.That(state.Effects[0].Magnitude, Is.EqualTo(10));
            Assert.That(state.Effects[0].PreparationIndex, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<ItemTemporaryEffect>)state.Effects).Clear());
            var reordered = new CharacterItemState(1, new[] { state.Effects[1], original });
            Assert.That(state.SameValues(reordered), Is.True);
            Assert.That(state.SameValues(new CharacterItemState(null, reordered.Effects)), Is.False);
            Assert.That(state.SameValues(new CharacterItemState(1, new[] {
                new ItemTemporaryEffect(3, ItemStatKind.Priority, 10, 2), state.Effects[1] })), Is.False);
        }

        [Test]
        public void State_EmptyIsImmutableAndRejectsMalformedEntries()
        {
            Assert.That(CharacterItemState.Empty.EquippedItemKey, Is.Null);
            Assert.That(CharacterItemState.Empty.Effects, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterItemState(0, Array.Empty<ItemTemporaryEffect>()));
            Assert.Throws<ArgumentNullException>(() => new CharacterItemState(null, null));
            Assert.Throws<ArgumentException>(() => new CharacterItemState(null, new ItemTemporaryEffect[] { null }));
        }

        [TestCase(0, ItemStatKind.Priority, 10, 0)]
        [TestCase(3, (ItemStatKind)99, 10, 0)]
        [TestCase(3, ItemStatKind.Priority, 0, 0)]
        [TestCase(3, ItemStatKind.Priority, 10, -1)]
        public void TemporaryEffect_RejectsInvalidDomains(int key, ItemStatKind stat, int magnitude, int round)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemTemporaryEffect(key, stat, magnitude, round));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(101)]
        public void PercentageSpecs_RejectOutOfRangeValues(int invalid)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WhiskeyItemSpec(invalid, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WhiskeyItemSpec(50, invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoketItemSpec(invalid, 50));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoketItemSpec(20, invalid));
        }

        [Test]
        public void Definitions_AcceptPlayableSpecsAndRejectInvalidIdentityOrPenalty()
        {
            var whiskey = new WhiskeyItemSpec(50, 10);
            var roket = new RoketItemSpec(20, 50);
            Assert.That(new ItemDefinition(1, whiskey).Effect, Is.SameAs(whiskey));
            Assert.That(whiskey.TriggerHealthPercent, Is.EqualTo(50));
            Assert.That(whiskey.HealPercent, Is.EqualTo(10));
            Assert.That(roket.TriggerDamagePercent, Is.EqualTo(20));
            Assert.That(roket.ReductionPercent, Is.EqualTo(50));
            Assert.DoesNotThrow(() => new WhiskeyItemSpec(100, 100));
            Assert.DoesNotThrow(() => new RoketItemSpec(100, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDefinition(0, whiskey));
            Assert.Throws<ArgumentNullException>(() => new ItemDefinition(1, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RopeItemSpec(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LanternItemSpec(-1));
        }

        [Test]
        public void DamageContext_AllowsUncreditedZeroDamageButRejectsMalformedFacts()
        {
            var before = Character(1, 60);
            var hit = new ItemDamageContext(CombatDamageKind.EnvironmentDamage, null, before, 15, 0);
            Assert.DoesNotThrow(() => new ItemDamageContext(CombatDamageKind.OrdinaryAttack, Character(2), before, 0, 0));
            Assert.Throws<ArgumentNullException>(() => new ItemDamageContext(CombatDamageKind.EnvironmentDamage, null, null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDamageContext((CombatDamageKind)99, null, before, 0, 0));
            // 0은 정의하지 않은 값이라 분류를 정하지 않은 default 우회도 거부한다.
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDamageContext(default, null, before, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDamageContext(CombatDamageKind.EnvironmentDamage, null, before, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDamageContext(CombatDamageKind.EnvironmentDamage, null, before, 1, -1));
            Assert.DoesNotThrow(() => new ItemAfterDamageContext(hit, Character(1, 45), 15));
            Assert.Throws<ArgumentException>(() => new ItemAfterDamageContext(hit, Character(2, 45), 15));
            Assert.Throws<ArgumentException>(() => new ItemAfterDamageContext(hit, Character(1, 45), 14));
            Assert.Throws<ArgumentNullException>(() => new ItemAfterDamageContext(null, before, 0));
        }

        [Test]
        public void AttackContext_FreezesCandidatesAndFinalFacts()
        {
            var candidate = new ItemAttackCandidate(new EntityId(2), new CellCoord(3, 0));
            var candidates = new List<ItemAttackCandidate> { candidate };
            var finals = new List<CharacterCombatFacts> { Character(1), Character(2) };
            var context = new ItemAttackContext(finals[0], new CellCoord(0, 0), candidates, finals, 0, 123);
            candidates.Clear();
            finals.Clear();
            Assert.That(context.DirectHits.Count, Is.EqualTo(1));
            Assert.That(context.FinalCharacters.Count, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IList<ItemAttackCandidate>)context.DirectHits).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterCombatFacts>)context.FinalCharacters).Clear());
            Assert.Throws<ArgumentException>(() => new ItemAttackCandidate(EntityId.Empty, default));
            Assert.Throws<ArgumentException>(() => new ItemAttackContext(Character(1), default,
                new ItemAttackCandidate[] { null }, Array.Empty<CharacterCombatFacts>(), 0, 0));
            Assert.Throws<ArgumentException>(() => new ItemAttackContext(Character(1), default,
                Array.Empty<ItemAttackCandidate>(), new CharacterCombatFacts[] { null }, 0, 0));
        }

        [Test]
        public void Decision_NoActivationPreservesOptionalRandomStateAndCannotConsume()
        {
            var damage = ItemRuleDecision.NoActivation();
            var attack = ItemRuleDecision.NoActivation(123);
            Assert.That(damage.Activated, Is.False);
            Assert.That(damage.Consume, Is.False);
            Assert.That(damage.FinalRandomState, Is.Null);
            Assert.That(attack.FinalRandomState, Is.EqualTo(123));
            Assert.Throws<ArgumentException>(() => new ItemRuleDecision(false, true));
            Assert.Throws<ArgumentException>(() => new ItemRuleDecision(false, false, remainingDamage: 0));
            Assert.Throws<ArgumentException>(() => new ItemRuleDecision(true, false));
        }

        [Test]
        public void Decision_FactoriesRepresentExactlyOneOperationIncludingZeroHeal()
        {
            var target = new EntityId(1);
            var mitigation = ItemRuleDecision.Mitigation(target, 0);
            var heal = ItemRuleDecision.Heal(target, 0);
            var effect = new ItemTemporaryEffect(3, ItemStatKind.Priority, 10, 0);
            var debuff = ItemRuleDecision.ApplyEffect(target, effect, 123);
            Assert.That(mitigation.RemainingDamage, Is.Zero);
            Assert.That(mitigation.Consume, Is.True);
            Assert.That(heal.Activated && heal.Consume, Is.True);
            Assert.That(heal.FinalRandomState, Is.Null);
            Assert.That(debuff.TemporaryEffect, Is.SameAs(effect));
            Assert.That(debuff.FinalRandomState, Is.EqualTo(123));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemRuleDecision.Mitigation(target, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemRuleDecision.Heal(target, -1));
            Assert.Throws<ArgumentException>(() => ItemRuleDecision.Heal(EntityId.Empty, 1));
            Assert.Throws<ArgumentException>(() => new ItemRuleDecision(true, true, target, 1, 2));
            Assert.Throws<ArgumentException>(() => new ItemRuleDecision(true, true, target, 1, finalRandomState: 123));
            Assert.Throws<ArgumentNullException>(() => ItemRuleDecision.ApplyEffect(target, null));
        }

        [Test]
        public void GrantResult_RejectsSuccessWithoutChangeAndFailureWithChange()
        {
            var change = new ItemInventoryChange(ItemInventorySnapshot.Empty, ItemInventorySnapshot.Empty);
            Assert.DoesNotThrow(() => new ItemGrantResult(true, string.Empty, change));
            Assert.DoesNotThrow(() => new ItemGrantResult(false, "RunEnded", null));
            Assert.Throws<ArgumentException>(() => new ItemGrantResult(true, string.Empty, null));
            Assert.Throws<ArgumentException>(() => new ItemGrantResult(false, "RunEnded", change));
            Assert.Throws<ArgumentException>(() => new ItemGrantResult(false, string.Empty, null));
        }

        [Test]
        public void ItemEvent_DistinguishesActivationDiscardAndEquipmentTransfer()
        {
            var id = new EntityId(1);
            var activated = new CombatItemEvent(1, id, id, ItemEventStage.AfterDamage,
                ItemEventReason.Activated, 10, 1, null, CombatDamageKind.EnvironmentDamage);
            var discarded = new CombatItemEvent(1, id, null, ItemEventStage.Lifecycle,
                ItemEventReason.Death, 0, 1, null);
            var equipped = new CombatItemEvent(2, id, null, ItemEventStage.Equipment,
                ItemEventReason.Replaced, 0, 1, 2);
            Assert.That(activated.Kind, Is.EqualTo(CombatEventKind.ItemActivated));
            Assert.That(activated.Activated && activated.Consumed, Is.True);
            Assert.That(discarded.Kind, Is.EqualTo(CombatEventKind.ItemDiscarded));
            Assert.That(discarded.Activated || discarded.Consumed, Is.False);
            Assert.That(equipped.Kind, Is.EqualTo(CombatEventKind.ItemEquipmentChanged));
            Assert.That(equipped.OldItemKey, Is.EqualTo(1));
            Assert.That(equipped.NewItemKey, Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() => new CombatItemEvent(1, id, id, ItemEventStage.Lifecycle,
                ItemEventReason.Activated, 10, 1, null));
            Assert.Throws<ArgumentException>(() => new CombatItemEvent(1, id, null, ItemEventStage.Lifecycle,
                ItemEventReason.Death, 0, 1, 1));
        }

        [Test]
        public void AttackContext_RejectsAmbiguousFinalIdentityAndMissingInputs()
        {
            var holder = Character(1);
            var emptyHits = Array.Empty<ItemAttackCandidate>();
            var final = new[] { holder };
            Assert.Throws<ArgumentNullException>(() => new ItemAttackContext(null, default, emptyHits, final, 0, 0));
            Assert.Throws<ArgumentNullException>(() => new ItemAttackContext(holder, default, null, final, 0, 0));
            Assert.Throws<ArgumentNullException>(() => new ItemAttackContext(holder, default, emptyHits, null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemAttackContext(holder, default, emptyHits, final, -1, 0));
            Assert.Throws<ArgumentException>(() => new ItemAttackContext(holder, default, emptyHits,
                new[] { holder, Character(1, 45) }, 0, 0));
        }

        [Test]
        public void AfterDamage_RejectsHealingOrLossBeyondTheIncomingDamage()
        {
            var hit = new ItemDamageContext(CombatDamageKind.EnvironmentDamage, null, Character(1, 60), 15, 0);
            Assert.Throws<ArgumentNullException>(() => new ItemAfterDamageContext(hit, null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemAfterDamageContext(hit, Character(1, 70), -10));
            Assert.Throws<ArgumentException>(() => new ItemAfterDamageContext(hit, Character(1, 40), 20));
        }

        [Test]
        public void ItemEvent_DuplicateConsumesWithoutEffectAndKeepsCause()
        {
            var id = new EntityId(1);
            var duplicate = new CombatItemEvent(4, id, new EntityId(2), ItemEventStage.AfterDamage,
                ItemEventReason.DuplicateEffect, 0, 4, null, CombatDamageKind.OrdinaryAttack);
            Assert.That(duplicate.Consumed, Is.True);
            Assert.That(duplicate.AppliedMagnitude, Is.Zero);
            Assert.That(duplicate.DamageKind, Is.EqualTo(CombatDamageKind.OrdinaryAttack));
            Assert.Throws<ArgumentException>(() => new CombatItemEvent(4, id, new EntityId(2), ItemEventStage.AfterDamage,
                ItemEventReason.DuplicateEffect, 5, 4, null, CombatDamageKind.OrdinaryAttack));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatItemEvent(4, id, new EntityId(2), ItemEventStage.AfterDamage,
                ItemEventReason.DuplicateEffect, 0, 4, null, default(CombatDamageKind)));
            Assert.Throws<ArgumentException>(() => new CombatItemEvent(4, id, new EntityId(2), ItemEventStage.AfterDamage,
                ItemEventReason.Activated, 5, 4, null));
            Assert.DoesNotThrow(() => new CombatItemEvent(3, id, new EntityId(2), ItemEventStage.AttackCompleted,
                ItemEventReason.Activated, 10, 3, null));
        }

        [Test]
        public void ItemEvent_RejectsInvalidIdentifiersAndMismatchedEquipment()
        {
            var id = new EntityId(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatItemEvent(0, id, null, ItemEventStage.Equipment,
                ItemEventReason.Equipped, 0, null, 1));
            Assert.Throws<ArgumentException>(() => new CombatItemEvent(1, EntityId.Empty, null, ItemEventStage.Equipment,
                ItemEventReason.Equipped, 0, null, 1));
            Assert.Throws<ArgumentException>(() => new CombatItemEvent(1, id, null, ItemEventStage.Equipment,
                ItemEventReason.Equipped, 0, 1, 1));
            Assert.Throws<ArgumentException>(() => new CombatItemEvent(1, id, null, ItemEventStage.Equipment,
                ItemEventReason.Replaced, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatItemEvent(1, id, null, (ItemEventStage)99,
                ItemEventReason.Equipped, 0, null, 1));
        }

        private static CharacterCombatFacts Character(int id, int hp = 100)
        {
            var cell = new CellCoord(id, 0);
            return new CharacterCombatFacts(new EntityId(id), null, EntityKey.Unit_Akimbo,
                Owner.Enemy, null, CharacterRuntimeArea.Field, new HealthStateSnapshot(hp, 100),
                default, null, cell, Rotation4.R0, new[] { cell }, UnitActionUse.Available);
        }
    }
}
