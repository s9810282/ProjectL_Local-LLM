using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterCategoryContractTests
    {
        [TestCase(-1)]
        public void CategorySpec_RejectsNegativeMovement(int allowance)
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),allowance)).ParamName,
                Is.EqualTo("movementAllowance"));
        }

        [TestCase(0)]
        [TestCase(5)]
        public void CategorySpec_PreservesExplicitMovement(int allowance)
        {
            var spec = new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),allowance);

            Assert.That(spec.MovementAllowance, Is.EqualTo(allowance));
        }

        private static readonly EntityId UnitA = new EntityId(7);
        private static readonly EntityId UnitB = new EntityId(8);

        [Test]
        public void CategoryRuleKey_UsesStableSerializedValues()
        {
            Assert.That((int)CategoryRuleKey.None, Is.EqualTo(0));
            Assert.That(Enum.IsDefined(typeof(CategoryRuleKey), 1), Is.False);
            Assert.That((int)CategoryRuleKey.DirectionalArmor, Is.EqualTo(2));
            Assert.That(Enum.IsDefined(typeof(CategoryRuleKey), 4), Is.False);
            Assert.That((int)CategoryRuleKey.SafeBombardment, Is.EqualTo(3));
        }

        [Test]
        public void CategoryActivationKind_DoesNotCollideWithTraitActivationKind()
        {
            Assert.That(Enum.IsDefined(typeof(CategoryActivationKind), 10), Is.False);
            Assert.That((int)CategoryActivationKind.DefenseFacingChanged, Is.EqualTo(20));
            Assert.That(Enum.IsDefined(typeof(CategoryActivationKind), 21), Is.False);

            // 숫자가 겹쳐도 서로 다른 enum이므로 변환 없이 구분되어야 한다.
            Assert.That(
                Enum.GetName(typeof(CategoryActivationKind), CategoryActivationKind.DefenseFacingChanged),
                Is.Not.EqualTo(
                    Enum.GetName(typeof(TraitActivationKind), TraitActivationKind.AttackDiscountArmed)));
        }

        [Test]
        public void CategoryRuleSpec_IsNotTraitSpec()
        {
            var categorySpec = new SafeBombardmentCategoryRuleSpec();

            Assert.That(categorySpec, Is.Not.InstanceOf<TraitSpec>());
            Assert.That(typeof(TraitSpec).IsAssignableFrom(typeof(CategoryRuleSpec)), Is.False);
            Assert.That(categorySpec.Key, Is.EqualTo(CategoryRuleKey.SafeBombardment));
        }

        [Test]
        public void CategoryRuleDefinitionAndRuntimeState_DoNotDeriveFromTraitTypes()
        {
            Assert.That(typeof(TraitDefinition).IsAssignableFrom(typeof(CategoryRuleDefinition)), Is.False);
            Assert.That(typeof(TraitRuntimeState).IsAssignableFrom(typeof(CategoryRuntimeState)), Is.False);
        }

        [Test]
        public void CategorySpec_RejectsNoneKey()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new CategorySpec(CategoryKey.None,Array.Empty<CategoryRuleSpec>(),3)).ParamName,
                Is.EqualTo("key"));
        }

        [Test]
        public void CategorySpec_RejectsUnregisteredKey()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new CategorySpec((CategoryKey)9999,Array.Empty<CategoryRuleSpec>(),3)).ParamName,
                Is.EqualTo("key"));
        }

        [Test]
        public void CategorySpec_RejectsDuplicateRuleKey()
        {
            var specs = new CategoryRuleSpec[]
            {
                new SafeBombardmentCategoryRuleSpec(),
                new SafeBombardmentCategoryRuleSpec(),
            };

            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    new CategorySpec(CategoryKey.Rider,specs,4)).ParamName,
                Is.EqualTo("ruleSpecs"));
        }

        [Test]
        public void CategorySpec_RejectsNullRuleSpecEntry()
        {
            var specs = new CategoryRuleSpec[] { null };

            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    new CategorySpec(CategoryKey.Rider,specs,4)).ParamName,
                Is.EqualTo("ruleSpecs"));
        }

        [Test]
        public void CategorySpec_BaseAllowsZeroRules()
        {
            var spec = new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),3);

            Assert.That(spec.Key, Is.EqualTo(CategoryKey.Main));
            Assert.That(spec.RuleSpecs.Length, Is.Zero);
        }

        [Test]
        public void CategorySpec_CopiesRuleListDefensively()
        {
            var specs = new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() };
            var spec = new CategorySpec(CategoryKey.Cannon,specs,2);

            specs[0] = null;

            Assert.That(spec.RuleSpecs.Span[0].Key, Is.EqualTo(CategoryRuleKey.SafeBombardment));
        }

        [Test]
        public void CategoryEffectOrigin_RoundTripsCategoryAndRuleKey()
        {
            EffectOriginKey origin = CategoryEffectOrigin.Create(
                UnitA, CategoryKey.Cannon, CategoryRuleKey.SafeBombardment);

            Assert.That(origin.Kind, Is.EqualTo(EffectOwnerKind.Category));
            Assert.That(origin.OwnerInstanceId, Is.EqualTo(UnitA.Value));
            Assert.That(
                CategoryEffectOrigin.GetRequiredCategoryKey(origin, UnitA),
                Is.EqualTo(CategoryKey.Cannon));
            Assert.That(
                CategoryEffectOrigin.GetRequiredRuleKey(origin, UnitA),
                Is.EqualTo(CategoryRuleKey.SafeBombardment));
        }

        [Test]
        public void CategoryEffectOrigin_RejectsMismatchedUnitId()
        {
            EffectOriginKey origin = CategoryEffectOrigin.Create(
                UnitA, CategoryKey.Cannon, CategoryRuleKey.SafeBombardment);

            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    CategoryEffectOrigin.GetRequiredRuleKey(origin, UnitB)).ParamName,
                Is.EqualTo("origin"));
        }

        [Test]
        public void CategoryEffectOrigin_RejectsForeignOwnerKind()
        {
            EffectOriginKey traitOrigin = new EffectOriginKey(
                EffectOwnerKind.Trait, UnitA.Value, (int)TraitKey.Focus, "main");

            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    CategoryEffectOrigin.GetRequiredRuleKey(traitOrigin, UnitA)).ParamName,
                Is.EqualTo("origin"));
        }

        [Test]
        public void CategoryEffectOrigin_RejectsUnknownRuleKeyName()
        {
            EffectOriginKey unknown = new EffectOriginKey(
                EffectOwnerKind.Category, UnitA.Value, (int)CategoryKey.Rider, "not-a-rule");

            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    CategoryEffectOrigin.GetRequiredRuleKey(unknown, UnitA)).ParamName,
                Is.EqualTo("origin"));
        }

        [Test]
        public void CategoryEffectOrigin_RejectsNoneAndUndefinedInput()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    CategoryEffectOrigin.Create(UnitA, CategoryKey.None, CategoryRuleKey.SafeBombardment))
                    .ParamName,
                Is.EqualTo("categoryKey"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    CategoryEffectOrigin.Create(UnitA, CategoryKey.Rider, CategoryRuleKey.None)).ParamName,
                Is.EqualTo("ruleKey"));
            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    CategoryEffectOrigin.Create(EntityId.Empty, CategoryKey.Rider, CategoryRuleKey.SafeBombardment))
                    .ParamName,
                Is.EqualTo("unitId"));
        }

        [Test]
        public void TraitAndCategoryOrigins_StayDistinctWithSameNumericDefinitionKey()
        {
            // Category Cannon(40)과 Trait ThornField(40)는 숫자가 같다.
            Assert.That((int)CategoryKey.Cannon, Is.EqualTo((int)TraitKey.ThornField));

            EffectOriginKey categoryOrigin = CategoryEffectOrigin.Create(
                UnitA, CategoryKey.Cannon, CategoryRuleKey.SafeBombardment);
            EffectOriginKey traitOrigin = new EffectOriginKey(
                EffectOwnerKind.Trait, UnitA.Value, (int)TraitKey.ThornField, "main");

            Assert.That(categoryOrigin, Is.Not.EqualTo(traitOrigin));
            Assert.That(categoryOrigin.CompareTo(traitOrigin), Is.Not.Zero);
        }
    }
}
