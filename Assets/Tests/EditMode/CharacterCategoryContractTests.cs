using System;
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
        private static readonly EntityId UnitA = new EntityId(7);
        private static readonly EntityId UnitB = new EntityId(8);

        [Test]
        public void CategoryRuleKey_UsesStableSerializedValues()
        {
            Assert.That((int)CategoryRuleKey.None, Is.EqualTo(0));
            Assert.That((int)CategoryRuleKey.ManeuverRefund, Is.EqualTo(1));
            Assert.That((int)CategoryRuleKey.DirectionalArmor, Is.EqualTo(2));
            Assert.That((int)CategoryRuleKey.SafeBombardment, Is.EqualTo(3));
        }

        [Test]
        public void CategoryActivationKind_DoesNotCollideWithTraitActivationKind()
        {
            Assert.That((int)CategoryActivationKind.RefundApplied, Is.EqualTo(10));
            Assert.That((int)CategoryActivationKind.DefenseFacingChanged, Is.EqualTo(20));
            Assert.That((int)CategoryActivationKind.IncomingDamageReduced, Is.EqualTo(21));

            // 숫자가 겹쳐도 서로 다른 enum이므로 변환 없이 구분되어야 한다.
            Assert.That(
                Enum.GetName(typeof(CategoryActivationKind), CategoryActivationKind.DefenseFacingChanged),
                Is.Not.EqualTo(
                    Enum.GetName(typeof(TraitActivationKind), TraitActivationKind.AttackDiscountArmed)));
        }

        [Test]
        public void CategoryRuleSpec_IsNotTraitSpec()
        {
            var refundSpec = new ManeuverRefundCategoryRuleSpec();

            Assert.That(refundSpec, Is.Not.InstanceOf<TraitSpec>());
            Assert.That(typeof(TraitSpec).IsAssignableFrom(typeof(CategoryRuleSpec)), Is.False);
            Assert.That(refundSpec.Key, Is.EqualTo(CategoryRuleKey.ManeuverRefund));
        }

        [Test]
        public void CategoryRuleDefinitionAndRuntimeState_DoNotDeriveFromTraitTypes()
        {
            Assert.That(typeof(TraitDefinition).IsAssignableFrom(typeof(CategoryRuleDefinition)), Is.False);
            Assert.That(typeof(TraitRuntimeState).IsAssignableFrom(typeof(CategoryRuntimeState)), Is.False);
        }

        [Test]
        public void DirectionalArmorSpec_StoresExplicitConfiguration()
        {
            var spec = new DirectionalArmorCategoryRuleSpec(50, Rotation4.R0);

            Assert.That(spec.Key, Is.EqualTo(CategoryRuleKey.DirectionalArmor));
            Assert.That(spec.DamageReductionPercent, Is.EqualTo(50));
            Assert.That(spec.InitialFacing, Is.EqualTo(Rotation4.R0));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(101)]
        public void DirectionalArmorSpec_RejectsOutOfRangeReduction(int percent)
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new DirectionalArmorCategoryRuleSpec(percent, Rotation4.R0)).ParamName,
                Is.EqualTo("damageReductionPercent"));
        }

        [Test]
        public void CategorySpec_RejectsNoneKey()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new CategorySpec(CategoryKey.None, Array.Empty<CategoryRuleSpec>())).ParamName,
                Is.EqualTo("key"));
        }

        [Test]
        public void CategorySpec_RejectsUnregisteredKey()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new CategorySpec((CategoryKey)9999, Array.Empty<CategoryRuleSpec>())).ParamName,
                Is.EqualTo("key"));
        }

        [Test]
        public void CategorySpec_RejectsDuplicateRuleKey()
        {
            var specs = new CategoryRuleSpec[]
            {
                new ManeuverRefundCategoryRuleSpec(),
                new ManeuverRefundCategoryRuleSpec(),
            };

            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    new CategorySpec(CategoryKey.Rider, specs)).ParamName,
                Is.EqualTo("ruleSpecs"));
        }

        [Test]
        public void CategorySpec_RejectsNullRuleSpecEntry()
        {
            var specs = new CategoryRuleSpec[] { null };

            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    new CategorySpec(CategoryKey.Rider, specs)).ParamName,
                Is.EqualTo("ruleSpecs"));
        }

        [Test]
        public void CategorySpec_BaseAllowsZeroRules()
        {
            var spec = new CategorySpec(CategoryKey.Base, Array.Empty<CategoryRuleSpec>());

            Assert.That(spec.Key, Is.EqualTo(CategoryKey.Base));
            Assert.That(spec.RuleSpecs.Length, Is.Zero);
        }

        [Test]
        public void CategorySpec_CopiesRuleListDefensively()
        {
            var specs = new CategoryRuleSpec[] { new ManeuverRefundCategoryRuleSpec() };
            var spec = new CategorySpec(CategoryKey.Rider, specs);

            specs[0] = new SafeBombardmentCategoryRuleSpec();

            Assert.That(spec.RuleSpecs.Span[0].Key, Is.EqualTo(CategoryRuleKey.ManeuverRefund));
        }

        [Test]
        public void CategoryEffectOrigin_RoundTripsCategoryAndRuleKey()
        {
            EffectOriginKey origin = CategoryEffectOrigin.Create(
                UnitA, CategoryKey.Heavy, CategoryRuleKey.DirectionalArmor);

            Assert.That(origin.Kind, Is.EqualTo(EffectOwnerKind.Category));
            Assert.That(origin.OwnerInstanceId, Is.EqualTo(UnitA.Value));
            Assert.That(
                CategoryEffectOrigin.GetRequiredCategoryKey(origin, UnitA),
                Is.EqualTo(CategoryKey.Heavy));
            Assert.That(
                CategoryEffectOrigin.GetRequiredRuleKey(origin, UnitA),
                Is.EqualTo(CategoryRuleKey.DirectionalArmor));
        }

        [Test]
        public void CategoryEffectOrigin_RejectsMismatchedUnitId()
        {
            EffectOriginKey origin = CategoryEffectOrigin.Create(
                UnitA, CategoryKey.Rider, CategoryRuleKey.ManeuverRefund);

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
                    CategoryEffectOrigin.Create(UnitA, CategoryKey.None, CategoryRuleKey.ManeuverRefund))
                    .ParamName,
                Is.EqualTo("categoryKey"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    CategoryEffectOrigin.Create(UnitA, CategoryKey.Rider, CategoryRuleKey.None)).ParamName,
                Is.EqualTo("ruleKey"));
            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    CategoryEffectOrigin.Create(EntityId.Empty, CategoryKey.Rider, CategoryRuleKey.ManeuverRefund))
                    .ParamName,
                Is.EqualTo("unitId"));
        }

        [Test]
        public void TraitAndCategoryOrigins_StayDistinctWithSameNumericDefinitionKey()
        {
            // Category Rider(20)와 Trait MoveAttackDiscount(20)는 숫자가 같다.
            Assert.That((int)CategoryKey.Rider, Is.EqualTo((int)TraitKey.MoveAttackDiscount));

            EffectOriginKey categoryOrigin = CategoryEffectOrigin.Create(
                UnitA, CategoryKey.Rider, CategoryRuleKey.ManeuverRefund);
            EffectOriginKey traitOrigin = new EffectOriginKey(
                EffectOwnerKind.Trait, UnitA.Value, (int)TraitKey.MoveAttackDiscount, "main");

            Assert.That(categoryOrigin, Is.Not.EqualTo(traitOrigin));
            Assert.That(categoryOrigin.CompareTo(traitOrigin), Is.Not.Zero);
        }
    }
}
