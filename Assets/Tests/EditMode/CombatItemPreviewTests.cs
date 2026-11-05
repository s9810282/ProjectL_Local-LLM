using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CombatItemPreviewTests
    {
        [Test]
        public void DeferredRope_RepeatedPreviewDoesNotActivateOrAdvanceRandomState()
        {
            var seed = CombatCommandTestData.Input();
            var chars = seed.Characters.Where(c => c.EntityId.Value != 4).Select(c => c.EntityId.Value == 1
                ? CombatItemDirectDamageTests.Equip(CombatCommandTestData.With(c, position: new CellCoord(1, 1)), 100, 3)
                : c.EntityId.Value == 3 ? CombatCommandTestData.With(c, position: new CellCoord(3, 4)) : c).ToArray();
            var input = CombatItemDirectDamageTests.Items(CombatCommandTestData.With(seed, chars),
                new[] { new ItemDefinition(3, new RopeItemSpec(10)) });
            var inventory = input.ItemInventory;
            var state = input.State;
            for (int count = 0; count < 3; count++)
            {
                var preview = CombatCommandCalculator.Preview(input, CombatItemDirectDamageTests.NoRules, out var proposed);
                Assert.That(preview.CanAttack, Is.True);
                Assert.That(proposed.Events.OfType<CombatItemEvent>(), Is.Empty);
                Assert.That(proposed.FinalState.ItemRandomState, Is.EqualTo(state.ItemRandomState));
                Assert.That(input.State, Is.SameAs(state));
                Assert.That(input.State.ItemRandomState, Is.EqualTo(0u));
                Assert.That(input.ItemInventory.SameValues(inventory), Is.True);
                Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).Items.EquippedItemKey, Is.EqualTo(3));
                Assert.That(input.Characters.All(c => c.Items.Effects.Count == 0), Is.True);
            }
        }

        [TestCase(1)]
        [TestCase(3)]
        public void AiProbes_PreserveItemFactsInventoryAndRandom(int key)
        {
            var seed = ThornHeartTestData.Input(heart: false, hp: 60, piercing: true);
            var input = CombatItemDirectDamageTests.Items(ThornHeartTestData.Replace(seed, 1,
                c => CombatItemDirectDamageTests.Equip(c, 60, key)),
                CombatItemDirectDamageTests.Definitions.Append(new ItemDefinition(3, new RopeItemSpec(10))).ToArray());
            var policy = new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi.EnemyAiPolicy(
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat.HostilityPolicy(),
                CombatItemDirectDamageTests.NoRules);
            var range = CombatCommandCalculator.GetAttackRange(input);
            var stock = input.ItemInventory;
            var state = input.State;
            for (int n = 0; n < 3; n++) Assert.That(policy.TrySelectAttackAim(input, range, out _), Is.True);
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 1).Items.EquippedItemKey, Is.EqualTo(key));
            Assert.That(input.Characters.All(c => c.Items.Effects.Count == 0), Is.True);
            Assert.That(input.ItemInventory.SameValues(stock), Is.True);
            Assert.That(input.State.SameValues(state), Is.True);
        }

        [TestCase(1, 40)]
        [TestCase(2, 45)]
        public void PreviewHealth_EqualsExecutionWithoutSpendingOriginalSlot(int key, int expected)
        {
            var input = CombatItemDirectDamageTests.Input(30, 60, key);
            var preview = CombatCommandCalculator.Preview(input, CombatItemDirectDamageTests.NoRules);
            var live = CombatCommandCalculator.Calculate(input, CombatItemDirectDamageTests.NoRules);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId.Value == 2).TargetHealthAfter, Is.EqualTo(expected));
            Assert.That(CombatItemDirectDamageTests.Final(live, 2).Health.CurrentHealth, Is.EqualTo(expected));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Items.EquippedItemKey, Is.EqualTo(key));
        }

        [Test]
        public void ChangedWhiskeyPayload_UsesSameApplicationAndSeparateRecoveryEvents()
        {
            var normal = CombatItemDirectDamageTests.Input(15, 60, 1);
            var variant = CombatItemDirectDamageTests.Items(normal, new[] { new ItemDefinition(1, new WhiskeyItemSpec(50, 20)) });
            foreach (var pair in new[] { (Input: normal, Health: 55, Heal: 10), (Input: variant, Health: 65, Heal: 20) })
            {
                var result = CombatCommandCalculator.Calculate(pair.Input, CombatItemDirectDamageTests.NoRules);
                Assert.That(CombatItemDirectDamageTests.Final(result, 2).Health.CurrentHealth, Is.EqualTo(pair.Health));
                Assert.That(CombatItemDirectDamageTests.Final(result, 2).Items.EquippedItemKey, Is.Null);
                var health = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId.Value == 2 &&
                    e.Expected.Health.CurrentHealth != e.Final.Health.CurrentHealth).ToArray();
                Assert.That(health.Select(e => e.Final.Health.CurrentHealth), Is.EqualTo(new[] { 45, pair.Health }));
                Assert.That(result.Events.OfType<CombatItemEvent>().Single().AppliedMagnitude, Is.EqualTo(pair.Heal));
            }
        }
    }
}
