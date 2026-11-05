using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
namespace ProjectL.Tests.Core
{
    public sealed class CombatItemDamageRuleTests
    {
        private static readonly ItemDefinition Whiskey = new ItemDefinition(1, new WhiskeyItemSpec(50, 10));
        private static readonly ItemDefinition Roket = new ItemDefinition(2, new RoketItemSpec(20, 50));

        [TestCase(100, 60, 10, true, 10)]
        [TestCase(100, 40, 1, true, 10)]
        [TestCase(100, 61, 10, false, 0)]
        [TestCase(100, 40, 0, false, 0)]
        [TestCase(100, 10, 10, false, 0)]
        [TestCase(100, 10, 100, false, 0)]
        [TestCase(100, 2, 1, true, 10)]
        [TestCase(19, 10, 1, true, 1)]
        [TestCase(19, 11, 1, false, 0)]
        [TestCase(9, 4, 1, true, 0)]
        [TestCase(int.MaxValue, 1073741824, 1, true, 214748364)]
        [TestCase(int.MaxValue, 1073741825, 1, false, 0)]
        public void Whiskey_ChecksCurrentSurvivingHealthAndFloorsRecovery(
            int max, int hp, int damage, bool activates, int heal)
        {
            var context = After(Whiskey, max, hp, damage);
            var decision = CombatItemResolver.AfterDamage(Whiskey, context);
            Assert.That(decision.Activated, Is.EqualTo(activates));
            Assert.That(decision.Consume, Is.EqualTo(activates));
            Assert.That(decision.HealAmount, Is.EqualTo(heal));
            Assert.That(decision.TargetId, Is.EqualTo(activates ? context.RecipientAfterDamage.EntityId : (EntityId?)null));
            Assert.That(decision.RemainingDamage, Is.Null);
            Assert.That(decision.FinalRandomState, Is.Null);
        }

        [Test]
        public void Whiskey_ObservesEveryDamageKindWithoutAttackerOrHostilityRequirement()
        {
            foreach (CombatDamageKind kind in Enum.GetValues(typeof(CombatDamageKind)))
            {
                Assert.That(CombatItemResolver.AfterDamage(Whiskey, After(Whiskey, 100, 40, 1, kind)).Activated,
                    Is.True, kind.ToString());
            }
            var friendly = Character(Owner.Player, 100, 100);
            Assert.That(CombatItemResolver.AfterDamage(Whiskey,
                After(Whiskey, 100, 40, 1, attacker: friendly)).Activated, Is.True);
        }

        [Test]
        public void Whiskey_RequiresActualLossAndLatestEquippedSlot()
        {
            var context = After(Whiskey, 100, 40, 1);
            var unchanged = new ItemAfterDamageContext(context.Damage, context.Damage.Recipient, 0);
            Assert.That(CombatItemResolver.AfterDamage(Whiskey, unchanged).Activated, Is.False);
            foreach (int? slot in new int?[] { null, 2 })
            {
                var latest = ItemCharacterChanges.WithItems(context.RecipientAfterDamage,
                    new CharacterItemState(slot, Array.Empty<ItemTemporaryEffect>()));
                Assert.That(CombatItemResolver.AfterDamage(Whiskey,
                    new ItemAfterDamageContext(context.Damage, latest, 1)).Activated, Is.False);
            }
        }

        [TestCase(100, 20, true, 10)]
        [TestCase(100, 19, false, 0)]
        [TestCase(19, 4, true, 2)]
        [TestCase(19, 3, false, 0)]
        [TestCase(100, 0, false, 0)]
        [TestCase(int.MaxValue, 429496730, true, 214748365)]
        [TestCase(int.MaxValue, 429496729, false, 0)]
        [TestCase(int.MaxValue, int.MaxValue, true, 1073741823)]
        public void Roket_ChecksExactThresholdBeforeHealthCapping(int max, int damage, bool activates, int remaining)
        {
            var target = Character(Owner.Player, 1, max, Roket.Key);
            var decision = CombatItemResolver.BeforeDamage(Roket, Hit(target, damage));
            Assert.That(decision.Activated, Is.EqualTo(activates));
            Assert.That(decision.Consume, Is.EqualTo(activates));
            Assert.That(decision.RemainingDamage, Is.EqualTo(activates ? remaining : (int?)null));
            Assert.That(decision.TargetId, Is.EqualTo(activates ? target.EntityId : (EntityId?)null));
            Assert.That(decision.HealAmount, Is.Zero);
            Assert.That(decision.FinalRandomState, Is.Null);
        }

        [Test]
        public void Roket_FloorsRemainingDamage()
        {
            var decision = CombatItemResolver.BeforeDamage(Roket,
                Hit(Character(Owner.Player, 20, 20, Roket.Key), 5));
            Assert.That(decision.RemainingDamage, Is.EqualTo(2));
            Assert.That(decision.Consume, Is.True);
        }

        [TestCase(CombatDamageKind.ExtraAttack)]
        [TestCase(CombatDamageKind.ReactionAttack)]
        [TestCase(CombatDamageKind.ReflectedDamage)]
        [TestCase(CombatDamageKind.EnvironmentDamage)]
        [TestCase(CombatDamageKind.DamageOverTime)]
        public void Roket_RejectsNonOrdinaryCauses(CombatDamageKind kind)
        {
            Assert.That(CombatItemResolver.BeforeDamage(Roket,
                Hit(Character(Owner.Player, 100, 100, Roket.Key), 100, kind)).Activated, Is.False);
        }

        [TestCase(Owner.Player, Owner.Enemy, true)]
        [TestCase(Owner.Enemy, Owner.Player, true)]
        [TestCase(Owner.Player, Owner.Player, false)]
        [TestCase(Owner.Enemy, Owner.Enemy, false)]
        public void Roket_UsesExistingHostilityPolicy(Owner attacker, Owner recipient, bool activates)
        {
            var target = Character(recipient, 100, 100, Roket.Key);
            var context = new ItemDamageContext(CombatDamageKind.OrdinaryAttack,
                Character(attacker, 100, 100), target, 30, 0);
            Assert.That(CombatItemResolver.BeforeDamage(Roket, context).Activated, Is.EqualTo(activates));
        }

        [Test]
        public void Roket_RejectsUncreditedDamageDeadHolderAndAbsentOrDifferentSlot()
        {
            var target = Character(Owner.Player, 100, 100, Roket.Key);
            Assert.That(CombatItemResolver.BeforeDamage(Roket,
                new ItemDamageContext(CombatDamageKind.OrdinaryAttack, null, target, 30, 0)).Activated, Is.False);
            Assert.That(CombatItemResolver.BeforeDamage(Roket,
                Hit(Character(Owner.Player, 0, 100, Roket.Key), 30)).Activated, Is.False);
            foreach (int? slot in new int?[] { null, 1 })
                Assert.That(CombatItemResolver.BeforeDamage(Roket,
                    Hit(Character(Owner.Player, 100, 100, slot), 30)).Activated, Is.False);
        }

        [Test]
        public void ExistingSpecVariants_UseParametersWithoutItemIdBranches()
        {
            var whiskey = new ItemDefinition(801, new WhiskeyItemSpec(75, 100));
            var after = After(whiskey, int.MaxValue, 1500000000, 1);
            Assert.That(CombatItemResolver.AfterDamage(whiskey, after).HealAmount, Is.EqualTo(int.MaxValue));
            var roket = new ItemDefinition(802, new RoketItemSpec(25, 100));
            Assert.That(CombatItemResolver.BeforeDamage(roket,
                Hit(Character(Owner.Player, 100, 100, roket.Key), 25)).RemainingDamage, Is.Zero);
            var variant = new ItemDefinition(803, new RoketItemSpec(25, 1));
            Assert.That(CombatItemResolver.BeforeDamage(variant,
                Hit(Character(Owner.Player, 100, 100, variant.Key), 25)).RemainingDamage, Is.EqualTo(24));
        }

        [Test]
        public void Decisions_DoNotChangeContextHealthEquipmentOrEffects()
        {
            var after = After(Whiskey, 100, 60, 10);
            var items = after.RecipientAfterDamage.Items;
            var first = CombatItemResolver.AfterDamage(Whiskey, after);
            var second = CombatItemResolver.AfterDamage(Whiskey, after);
            Assert.That(first.HealAmount, Is.EqualTo(second.HealAmount));
            Assert.That(after.RecipientAfterDamage.Health.CurrentHealth, Is.EqualTo(50));
            Assert.That(after.RecipientAfterDamage.Items, Is.SameAs(items));
            Assert.That(items.EquippedItemKey, Is.EqualTo(Whiskey.Key));
            var hit = Hit(Character(Owner.Player, 100, 100, Roket.Key), 30);
            CombatItemResolver.BeforeDamage(Roket, hit);
            Assert.That(hit.DamageBeforeItems, Is.EqualTo(30));
            Assert.That(hit.Recipient.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(hit.Recipient.Items.EquippedItemKey, Is.EqualTo(Roket.Key));
        }

        [TestCase(0u)]
        [TestCase(uint.MaxValue)]
        public void MissingCapabilities_DoNotActivateAndAttackCompletionPreservesRandomState(uint seed)
        {
            Assert.That(CombatItemResolver.BeforeDamage(Whiskey,
                Hit(Character(Owner.Player, 100, 100, Whiskey.Key), 30)).Activated, Is.False);
            Assert.That(CombatItemResolver.AfterDamage(Roket, After(Roket, 100, 60, 10)).Activated, Is.False);
            foreach (var definition in new[] { Whiskey, Roket })
            {
                var holder = Character(Owner.Player, 100, 100, definition.Key);
                var context = new ItemAttackContext(holder, CellCoord.Zero,
                    Array.Empty<ItemAttackCandidate>(), new[] { holder }, 0, seed);
                var decision = CombatItemResolver.AfterOrdinaryAttack(definition, context);
                Assert.That(decision.Activated, Is.False);
                Assert.That(decision.Consume, Is.False);
                Assert.That(decision.FinalRandomState, Is.EqualTo(seed));
            }
        }

        [Test]
        public void Registry_IsExplicitAndRejectsUnknownAndNotYetImplementedRules()
        {
            var registry = ItemRuleRegistry.CreateDefault();
            Assert.That(registry.GetRequired(Whiskey.Effect), Is.TypeOf<WhiskeyItemRule>());
            Assert.That(registry.GetRequired(new WhiskeyItemSpec(75, 20)),
                Is.SameAs(registry.GetRequired(Whiskey.Effect)));
            Assert.That(registry.GetRequired(Roket.Effect), Is.TypeOf<RoketItemRule>());
            Assert.Throws<InvalidOperationException>(() => registry.GetRequired(new UnknownSpec()));
            Assert.That(registry.GetRequired(new RopeItemSpec(10)), Is.TypeOf<RopeItemRule>());
            Assert.That(registry.GetRequired(new LanternItemSpec(5)), Is.TypeOf<LanternItemRule>());
            Assert.Throws<InvalidOperationException>(() => CombatItemResolver.BeforeDamage(
                new ItemDefinition(99, new UnknownSpec()), Hit(Character(Owner.Player, 100, 100, 99), 30)));
            Assert.Throws<InvalidOperationException>(() => new WhiskeyItemRule().Evaluate(Roket, After(Roket, 100, 40, 1)));
            Assert.Throws<InvalidOperationException>(() => new RoketItemRule().Evaluate(Whiskey,
                Hit(Character(Owner.Player, 100, 100, Whiskey.Key), 30)));
        }

        [Test]
        public void PublicBoundaries_RejectNullInputs()
        {
            var hit = Hit(Character(Owner.Player, 100, 100, Roket.Key), 30);
            var after = After(Whiskey, 100, 60, 10);
            var attack = new ItemAttackContext(hit.Recipient, CellCoord.Zero,
                Array.Empty<ItemAttackCandidate>(), new[] { hit.Recipient }, 0, 1);
            Assert.Throws<ArgumentNullException>(() => ItemRuleRegistry.CreateDefault().GetRequired(null));
            Assert.Throws<ArgumentNullException>(() => CombatItemResolver.BeforeDamage(null, hit));
            Assert.Throws<ArgumentNullException>(() => CombatItemResolver.BeforeDamage(Roket, null));
            Assert.Throws<ArgumentNullException>(() => CombatItemResolver.AfterDamage(null, after));
            Assert.Throws<ArgumentNullException>(() => CombatItemResolver.AfterDamage(Whiskey, null));
            Assert.Throws<ArgumentNullException>(() => CombatItemResolver.AfterOrdinaryAttack(null, attack));
            Assert.Throws<ArgumentNullException>(() => CombatItemResolver.AfterOrdinaryAttack(Roket, null));
        }

        private static CharacterCombatFacts Character(Owner owner, int hp, int max, int? item = null)
        {
            var source = CombatCommandTestData.Character(owner == Owner.Player ? 1 : 2,
                owner == Owner.Player ? EntityKey.Unit_Akimbo : EntityKey.Unit_Demon, CellCoord.Zero, owner);
            return ItemCharacterChanges.WithItems(CharacterSettlementCalculator.Copy(source,
                new HealthStateSnapshot(hp, max), source.Stats),
                new CharacterItemState(item, Array.Empty<ItemTemporaryEffect>()));
        }

        private static ItemDamageContext Hit(CharacterCombatFacts recipient, int damage,
            CombatDamageKind kind = CombatDamageKind.OrdinaryAttack) => new ItemDamageContext(kind,
                Character(Owner.Enemy, 100, 100), recipient, damage, 0);

        private static ItemAfterDamageContext After(ItemDefinition definition, int max, int hp, int damage,
            CombatDamageKind kind = CombatDamageKind.OrdinaryAttack, CharacterCombatFacts attacker = null)
        {
            var before = Character(Owner.Player, hp, max, definition.Key);
            int loss = Math.Min(hp, damage);
            var after = CharacterSettlementCalculator.Copy(before, new HealthStateSnapshot(hp - loss, max), before.Stats);
            return new ItemAfterDamageContext(new ItemDamageContext(kind, attacker, before, damage, 0), after, loss);
        }

        private sealed class UnknownSpec : ItemEffectSpec { }
    }
}
