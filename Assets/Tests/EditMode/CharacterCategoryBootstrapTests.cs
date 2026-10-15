using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterCategoryBootstrapTests
    {
        [Test]
        public void DefaultRegistry_ContainsExactlyTheRequiredCategoryRules()
        {
            CategoryRegistry registry = CategoryRegistry.CreateDefault();

            Assert.That(registry.GetRequired(CategoryKey.Base).RuleDefinitions.Length, Is.Zero);
            AssertRuleKeys(registry.GetRequired(CategoryKey.Rider), CategoryRuleKey.ManeuverRefund);
            AssertRuleKeys(registry.GetRequired(CategoryKey.Heavy), CategoryRuleKey.DirectionalArmor);
            AssertRuleKeys(registry.GetRequired(CategoryKey.Cannon), CategoryRuleKey.SafeBombardment);
        }

        [Test]
        public void Registry_RejectsMissingAndDuplicateCategoryKeys()
        {
            CategorySpec[] complete = CreateCompleteSpecs();

            Assert.Throws<ArgumentException>(() => new CategoryRegistry(new[]
            {
                complete[0], complete[1], complete[2],
            }));

            Assert.Throws<ArgumentException>(() => new CategoryRegistry(new[]
            {
                complete[0], complete[1], complete[2], complete[3], complete[3],
            }));
        }

        [TestCase(CategoryKey.Base, CategoryRuleKey.ManeuverRefund)]
        [TestCase(CategoryKey.Rider, CategoryRuleKey.DirectionalArmor)]
        [TestCase(CategoryKey.Heavy, CategoryRuleKey.SafeBombardment)]
        [TestCase(CategoryKey.Cannon, CategoryRuleKey.ManeuverRefund)]
        public void Registry_RejectsMismatchedCategoryRule(CategoryKey category, CategoryRuleKey rule)
        {
            CategorySpec[] specs = CreateCompleteSpecs();
            specs[CategoryIndex(category)] = new CategorySpec(category, new[] { CreateRuleSpec(rule) });

            Assert.Throws<ArgumentException>(() => new CategoryRegistry(specs));
        }

        [Test]
        public void RuleFactories_CreateOnlyTheRegisteredDefinitionRuleAndStateTypes()
        {
            CategoryRuleDefinition riderDefinition = CategoryRuleFactories.CreateDefinition(
                new ManeuverRefundCategoryRuleSpec());
            CategoryRuleDefinition heavyDefinition = CategoryRuleFactories.CreateDefinition(
                new DirectionalArmorCategoryRuleSpec(50, Rotation4.R90));
            CategoryRuleDefinition cannonDefinition = CategoryRuleFactories.CreateDefinition(
                new SafeBombardmentCategoryRuleSpec());

            Assert.That(riderDefinition, Is.TypeOf<ManeuverRefundCategoryRuleDefinition>());
            Assert.That(heavyDefinition, Is.TypeOf<DirectionalArmorCategoryRuleDefinition>());
            Assert.That(cannonDefinition, Is.TypeOf<SafeBombardmentCategoryRuleDefinition>());
            Assert.That(CategoryRuleFactories.CreateRule(riderDefinition), Is.TypeOf<ManeuverRefundCategoryRule>());
            Assert.That(CategoryRuleFactories.CreateRule(heavyDefinition), Is.TypeOf<DirectionalArmorCategoryRule>());
            Assert.That(CategoryRuleFactories.CreateRule(cannonDefinition), Is.TypeOf<SafeBombardmentCategoryRule>());
            Assert.That(CategoryRuleFactories.CreateInitialState(riderDefinition), Is.TypeOf<ManeuverRefundRuntimeState>());
            Assert.That(CategoryRuleFactories.CreateInitialState(heavyDefinition), Is.TypeOf<DirectionalArmorRuntimeState>());
            Assert.That(CategoryRuleFactories.CreateInitialState(cannonDefinition), Is.Null);
        }

        [Test]
        public void RuleFactories_RejectAnUnregisteredInternalRuleInsteadOfReturningNoOp()
        {
            Assert.Throws<InvalidOperationException>(() =>
                CategoryRuleFactories.CreateDefinition(new UnknownCategoryRuleSpec()));
            Assert.Throws<InvalidOperationException>(() =>
                CategoryRuleFactories.CreateRule(new UnknownCategoryRuleDefinition()));
            Assert.Throws<InvalidOperationException>(() =>
                CategoryRuleFactories.CreateInitialState(new UnknownCategoryRuleDefinition()));
        }

        [Test]
        public void UnitCategoryState_CloneAndApplyKeepInstancesIndependent()
        {
            var first = new UnitCategoryState(new CategoryRuntimeState[]
            {
                new ManeuverRefundRuntimeState(false),
            });
            UnitCategoryState second = first.Clone();
            var changes = new CategoryTransitionBatch(new[]
            {
                CategoryStateChange.Change(
                    CategoryRuleKey.ManeuverRefund,
                    new ManeuverRefundRuntimeState(true)),
            });

            first.Apply(changes);

            Assert.That(first.GetRequired<ManeuverRefundRuntimeState>(
                CategoryRuleKey.ManeuverRefund).RefundUsedThisTurn, Is.True);
            Assert.That(second.GetRequired<ManeuverRefundRuntimeState>(
                CategoryRuleKey.ManeuverRefund).RefundUsedThisTurn, Is.False);
        }

        [Test]
        public void SnapshotBuilder_ExposesOnlyTheOwningCategoryState()
        {
            CategoryRegistry registry = CategoryRegistry.CreateDefault();
            var builder = new CategorySnapshotBuilder();

            CategorySnapshot rider = builder.CreateSnapshot(
                registry.GetRequired(CategoryKey.Rider),
                new UnitCategoryState(new CategoryRuntimeState[]
                {
                    new ManeuverRefundRuntimeState(true),
                }));
            CategorySnapshot heavy = builder.CreateSnapshot(
                registry.GetRequired(CategoryKey.Heavy),
                new UnitCategoryState(new CategoryRuntimeState[]
                {
                    new DirectionalArmorRuntimeState(Rotation4.R270, true),
                }));
            CategorySnapshot cannon = builder.CreateSnapshot(
                registry.GetRequired(CategoryKey.Cannon),
                new UnitCategoryState(Array.Empty<CategoryRuntimeState>()));

            Assert.That(rider.Key, Is.EqualTo(CategoryKey.Rider));
            Assert.That(rider.RefundUsedThisTurn, Is.True);
            Assert.That(rider.DefenseFacing, Is.Null);
            Assert.That(heavy.DefenseFacing, Is.EqualTo(Rotation4.R270));
            Assert.That(heavy.MovedThisTurn, Is.True);
            Assert.That(heavy.DamageReductionPercent, Is.EqualTo(50));
            Assert.That(cannon.RefundUsedThisTurn, Is.Null);
            Assert.That(cannon.DefenseFacing, Is.Null);
        }

        private static void AssertRuleKeys(CategoryDefinition definition, params CategoryRuleKey[] expected)
        {
            Assert.That(definition.RuleDefinitions.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
                Assert.That(definition.RuleDefinitions.Span[i].Key, Is.EqualTo(expected[i]));
        }

        private static CategorySpec[] CreateCompleteSpecs()
        {
            return new[]
            {
                new CategorySpec(CategoryKey.Base, Array.Empty<CategoryRuleSpec>()),
                new CategorySpec(CategoryKey.Rider, new CategoryRuleSpec[]
                {
                    new ManeuverRefundCategoryRuleSpec(),
                }),
                new CategorySpec(CategoryKey.Heavy, new CategoryRuleSpec[]
                {
                    new DirectionalArmorCategoryRuleSpec(50, Rotation4.R0),
                }),
                new CategorySpec(CategoryKey.Cannon, new CategoryRuleSpec[]
                {
                    new SafeBombardmentCategoryRuleSpec(),
                }),
            };
        }

        private static int CategoryIndex(CategoryKey category)
        {
            return category switch
            {
                CategoryKey.Base => 0,
                CategoryKey.Rider => 1,
                CategoryKey.Heavy => 2,
                CategoryKey.Cannon => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(category)),
            };
        }

        private static CategoryRuleSpec CreateRuleSpec(CategoryRuleKey key)
        {
            return key switch
            {
                CategoryRuleKey.ManeuverRefund => new ManeuverRefundCategoryRuleSpec(),
                CategoryRuleKey.DirectionalArmor => new DirectionalArmorCategoryRuleSpec(50, Rotation4.R0),
                CategoryRuleKey.SafeBombardment => new SafeBombardmentCategoryRuleSpec(),
                _ => throw new ArgumentOutOfRangeException(nameof(key)),
            };
        }

        private sealed class UnknownCategoryRuleSpec : CategoryRuleSpec
        {
            internal UnknownCategoryRuleSpec() : base(CategoryRuleKey.ManeuverRefund) { }
        }

        private sealed class UnknownCategoryRuleDefinition : CategoryRuleDefinition
        {
            internal UnknownCategoryRuleDefinition() : base(CategoryRuleKey.ManeuverRefund) { }
        }
    }
}
