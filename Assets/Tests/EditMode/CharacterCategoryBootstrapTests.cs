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
        [TestCase(-1)]
        public void CategoryDefinition_RejectsNegativeMovement(int allowance)
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CategoryDefinition(CategoryKey.Main, Array.Empty<CategoryRuleDefinition>(), default, allowance)).ParamName,
                Is.EqualTo("movementAllowance"));
        }

        [TestCase(5)]
        [TestCase(0)]
        public void Registry_UnitDefinitionUsesSuppliedMovement(int allowance)
        {
            CategorySpec[] specs = CreateCompleteSpecs();
            specs[1] = new CategorySpec(CategoryKey.Rider,                Array.Empty<CategoryRuleSpec>(),allowance);
            var registry = new CategoryRegistry(specs);
            var unit = UnitSpecTestFactory.Create(EntityKey.Unit_Akimbo, 1, 1, new[] { CellCoord.Zero },
                10, MoveMethod.LiftAndPlace, 1, 5, 0, Array.Empty<TraitSpec>(), categoryKey: CategoryKey.Rider);
            var catalog = new UnitCatalog(new[] { unit }, TraitRegistry.CreateDefault(), registry);
            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_Akimbo);

            Assert.That(definition.Category.MovementAllowance, Is.EqualTo(allowance));
            Assert.That(definition.MoveRange, Is.EqualTo(allowance));
            Assert.That(registry.GetRequired(CategoryKey.Main).MovementAllowance, Is.EqualTo(3));
            Assert.DoesNotThrow(catalog.ValidateCombatReady);
        }

        [TestCase(CategoryKey.Main, 3)]
        [TestCase(CategoryKey.Rider, 4)]
        [TestCase(CategoryKey.Heavy, 3)]
        [TestCase(CategoryKey.Cannon, 2)]
        [TestCase(CategoryKey.Boss, 0)]
        public void DefaultRegistry_PreservesMovement(CategoryKey key, int allowance)
        {
            var definition = CategoryRegistry.CreateDefault().GetRequired(key);
            Assert.That(definition.MovementAllowance, Is.EqualTo(allowance));
        }

        [Test]
        public void DefaultRegistry_ContainsExactlyTheRequiredCategoryRules()
        {
            CategoryRegistry registry = CategoryRegistry.CreateDefault();

            Assert.That(registry.GetRequired(CategoryKey.Main).RuleDefinitions.Length, Is.Zero);
            AssertRuleKeys(registry.GetRequired(CategoryKey.Rider));
            AssertRuleKeys(registry.GetRequired(CategoryKey.Heavy));
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

        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void Registry_RejectsMismatchedCategoryRule(CategoryKey category)
        {
            CategorySpec[] specs = CreateCompleteSpecs();
            specs[CategoryIndex(category)] = new CategorySpec(category,                new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() },3);

            Assert.Throws<ArgumentException>(() => new CategoryRegistry(specs));
        }

        [Test]
        public void RuleFactories_CreateOnlyTheRegisteredDefinitionRuleAndStateTypes()
        {
            CategoryRuleDefinition cannonDefinition = CategoryRuleFactories.CreateDefinition(
                new SafeBombardmentCategoryRuleSpec());

            Assert.That(cannonDefinition, Is.TypeOf<SafeBombardmentCategoryRuleDefinition>());
            Assert.That(CategoryRuleFactories.CreateRule(cannonDefinition), Is.TypeOf<SafeBombardmentCategoryRule>());
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
                new TestCategoryState(false),
            });
            UnitCategoryState second = first.Clone();
            var changes = new CategoryTransitionBatch(new[]
            {
                CategoryStateChange.Change(
                    CategoryRuleKey.SafeBombardment,
                    new TestCategoryState(true)),
            });

            first.Apply(changes);

            Assert.That(first.GetRequired<TestCategoryState>(
                CategoryRuleKey.SafeBombardment).Value, Is.True);
            Assert.That(second.GetRequired<TestCategoryState>(
                CategoryRuleKey.SafeBombardment).Value, Is.False);
        }

        [Test]
        public void SnapshotBuilder_ExposesOnlyTheOwningCategoryState()
        {
            CategoryRegistry registry = CategoryRegistry.CreateDefault();
            var builder = new CategorySnapshotBuilder();

            CategorySnapshot rider = builder.CreateSnapshot(
                registry.GetRequired(CategoryKey.Rider),
                new UnitCategoryState(Array.Empty<CategoryRuntimeState>()));
            CategorySnapshot heavy = builder.CreateSnapshot(
                registry.GetRequired(CategoryKey.Heavy),
                new UnitCategoryState(Array.Empty<CategoryRuntimeState>()));
            CategorySnapshot cannon = builder.CreateSnapshot(
                registry.GetRequired(CategoryKey.Cannon),
                new UnitCategoryState(Array.Empty<CategoryRuntimeState>()));

            Assert.That(rider.Key, Is.EqualTo(CategoryKey.Rider));
            Assert.That(heavy.Key, Is.EqualTo(CategoryKey.Heavy));
            Assert.That(cannon.Key, Is.EqualTo(CategoryKey.Cannon));
        }

        [Test]
        public void Registry_CannonStillRequiresItsRule()
        {
            var specs = CreateCompleteSpecs();
            specs[3] = new CategorySpec(CategoryKey.Cannon,Array.Empty<CategoryRuleSpec>(),2);
            Assert.Throws<ArgumentException>(() => new CategoryRegistry(specs));
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
                new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Rider,Array.Empty<CategoryRuleSpec>(),4),
                new CategorySpec(CategoryKey.Heavy,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Cannon,new CategoryRuleSpec[]
                {
                    new SafeBombardmentCategoryRuleSpec(),
                },2),
            };
        }

        private static int CategoryIndex(CategoryKey category)
        {
            return category switch
            {
                CategoryKey.Main => 0,
                CategoryKey.Rider => 1,
                CategoryKey.Heavy => 2,
                CategoryKey.Cannon => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(category)),
            };
        }

        private sealed class UnknownCategoryRuleSpec : CategoryRuleSpec
        {
            internal UnknownCategoryRuleSpec() : base(CategoryRuleKey.SafeBombardment) { }
        }

        private sealed class UnknownCategoryRuleDefinition : CategoryRuleDefinition
        {
            internal UnknownCategoryRuleDefinition() : base(CategoryRuleKey.SafeBombardment) { }
        }

        private sealed class TestCategoryState : CategoryRuntimeState
        {
            internal bool Value { get; private set; }
            internal TestCategoryState(bool value) : base(CategoryRuleKey.SafeBombardment) => Value = value;
            public override CategoryRuntimeState Clone() => new TestCategoryState(Value);
            public override IEffectStateSnapshot Capture() => StatelessEffectStateSnapshot.Instance;
            internal override void CopyFinalValuesFrom(CategoryRuntimeState source) =>
                Value = ((TestCategoryState)source).Value;
        }
    }
}
