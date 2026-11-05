using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Tests.Core
{
    public sealed class UnitLevelGrowthRulesTests
    {
        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void LevelOne_UsesDefinitionValues(CategoryKey category)
        {
            var definition = Definition(category);

            Assert.That((UnitLevelGrowthRules.AttackPower(definition, 1), UnitLevelGrowthRules.MaxHealth(definition, 1)),
                Is.EqualTo((10, 20)));
        }

        // 레벨 성장 설계 §3.1: Rider 공+1·체+1, Heavy 체+3, Cannon 공+2, Base 0.
        [TestCase(CategoryKey.Main, 3, 10, 20)]
        [TestCase(CategoryKey.Rider, 3, 12, 22)]
        [TestCase(CategoryKey.Heavy, 4, 10, 29)]
        [TestCase(CategoryKey.Cannon, 2, 12, 20)]
        public void HigherLevel_AddsCategoryGrowthForEachLevelAboveOne(CategoryKey category, int level,
            int attack, int maxHealth)
        {
            var definition = Definition(category);

            Assert.That((UnitLevelGrowthRules.AttackPower(definition, level), UnitLevelGrowthRules.MaxHealth(definition, level)),
                Is.EqualTo((attack, maxHealth)));
        }

        [Test]
        public void RepeatedCalculation_DependsOnlyOnLevelAndDoesNotAccumulate()
        {
            var rider = Definition(CategoryKey.Rider);

            // 레벨 1→3→5로 나눠 오르든 바로 5든, 몇 번 다시 계산하든 (정의, 레벨)의 값 하나다.
            Assert.That(UnitLevelGrowthRules.AttackPower(rider, 3), Is.EqualTo(12));
            Assert.That(UnitLevelGrowthRules.AttackPower(rider, 5), Is.EqualTo(14));
            Assert.That(UnitLevelGrowthRules.AttackPower(rider, 5), Is.EqualTo(14));
            Assert.That(UnitLevelGrowthRules.MaxHealth(rider, 5), Is.EqualTo(24));
            Assert.That((rider.AttackPower, rider.MaxHealth), Is.EqualTo((10, 20)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void LevelBelowOne_IsRejected(int level)
        {
            var definition = Definition(CategoryKey.Rider);

            Assert.Throws<ArgumentOutOfRangeException>(() => UnitLevelGrowthRules.AttackPower(definition, level));
            Assert.Throws<ArgumentOutOfRangeException>(() => UnitLevelGrowthRules.MaxHealth(definition, level));
        }

        [Test]
        public void Overflow_IsReportedInsteadOfWrapping()
        {
            var definition = Definition(CategoryKey.Main, new CategoryLevelGrowth(int.MaxValue, int.MaxValue));

            Assert.That(UnitLevelGrowthRules.AttackPower(definition, 1), Is.EqualTo(10));
            Assert.Throws<OverflowException>(() => UnitLevelGrowthRules.AttackPower(definition, 2));
            Assert.Throws<OverflowException>(() => UnitLevelGrowthRules.MaxHealth(definition, 3));
            Assert.Throws<ArgumentNullException>(() => UnitLevelGrowthRules.AttackPower(null, 1));
            Assert.Throws<ArgumentNullException>(() => UnitLevelGrowthRules.MaxHealth(null, 1));
        }

        [Test]
        public void CategoryGrowth_DefaultsToNoneAndRejectsNegativeValues()
        {
            Assert.That((default(CategoryLevelGrowth).MaxHealthPerLevel, default(CategoryLevelGrowth).AttackPerLevel),
                Is.EqualTo((0, 0)));
            Assert.That(new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),3).LevelGrowth,
                Is.EqualTo(default(CategoryLevelGrowth)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CategoryLevelGrowth(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CategoryLevelGrowth(0, -1));
        }

        [Test]
        public void CategoryRegistry_CarriesSpecGrowthToDefinition()
        {
            var registry = new CategoryRegistry(UnitSpecTestFactory.CreateGrowthCategoryCatalog());

            Assert.That(registry.GetRequired(CategoryKey.Heavy).LevelGrowth, Is.EqualTo(new CategoryLevelGrowth(3, 0)));
            Assert.That(registry.GetRequired(CategoryKey.Main).LevelGrowth, Is.EqualTo(default(CategoryLevelGrowth)));
        }

        internal static UnitDefinition Definition(CategoryKey category, CategoryLevelGrowth baseGrowth = default)
        {
            var spec = UnitSpecTestFactory.Create(EntityKey.Unit_Akimbo, 1, 1, new[] { CellCoord.Zero }, 20,
                MoveMethod.LiftAndPlace, 2, 10, 1, Array.Empty<TraitSpec>(),
                categoryKey: category);
            return new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault(),
                new CategoryRegistry(UnitSpecTestFactory.CreateGrowthCategoryCatalog(baseGrowth))).GetRequired(spec.Key);
        }
    }
}
