using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RetiredTraitAuthoringTests
    {
        [Test]
        public void RetiredDiscount_HasNoExecutableAuthoringOrPureDataType()
        {
            var assembly = typeof(UnitTraitAuthoring).Assembly;
            var prefix = typeof(UnitTraitAuthoring).Namespace + ".";
            Assert.That(assembly.GetType(prefix + "MoveAttackDiscountTraitAuthoring"), Is.Null);
            Assert.That(assembly.GetType(prefix + "MoveAttackDiscountTraitData"), Is.Null);
            Assert.That(TypeCache.GetTypesDerivedFrom<UnitTraitAuthoring>().Any(t => t.Name == "MoveAttackDiscountTraitAuthoring"), Is.False);
        }

        [TestCase(20)]
        [TestCase(35)]
        public void ConfiguredIntimidation_ExportsAttackReductionWithoutPriorityReduction(int percent)
        {
            using var fixture = new CoreAuthoringFixture();
            var authoring = new IntimidationTraitAuthoring();
            CoreAuthoringFixture.Set(authoring, "attackReductionPercent", percent);
            var asset = fixture.CreateUnit(EntityKey.Unit_Shielder, CategoryKey.Heavy, authoring);
            var data = (UnitData)asset.GetData();
            var intimidation = data.Traits.OfType<IntimidationTraitData>().Single();
            intimidation.Validate();
            Assert.That(intimidation.AttackReductionPercent, Is.EqualTo(percent));
            Assert.That(typeof(IntimidationTraitData).GetProperty("PriorityReduction"), Is.Null);
            Assert.That(typeof(IntimidationTraitAuthoring).GetField("priorityReduction", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), Is.Null);
            var spec = TraitSpecAdapter.Read(intimidation);
            Assert.That(spec.GetType().GetProperty("AttackReductionPercent").GetValue(spec), Is.EqualTo(percent));
            Assert.That(spec.GetType().GetProperty("PriorityReduction"), Is.Null);
        }
        [Test]
        public void ConfiguredPullUnit_PreservesTraitsAndCategoryInOwnedProjection()
        {
            using var fixture = new CoreAuthoringFixture();
            var authoring = new PullTraitAuthoring();
            CoreAuthoringFixture.Set(authoring, "extraRangeCells", 3);
            var source = fixture.CreateUnit(EntityKey.Unit_Cowboy, CategoryKey.Rider, authoring);
            var cowboy = UnitSpecAdapter.ReadUnit((UnitData)source.GetData());
            // 테스트가 지정한 정의가 보유 능력치로 그대로 이어지는지 확인합니다.
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), cowboy.Key, CharacterOwnershipRole.Sub);
            var profile = new CombatAttackProfile(cowboy,
                new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var stats = OwnedCharacterStatsCalculator.Calculate(new[] { profile },
                UnitSpecAdapter.ReadCategories(ConfiguredMovementBattle.Config), new[] { owned },
                null, new Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 }).Single().Stats;
            Assert.That(cowboy.Traits.ToArray().Select(trait => trait.TraitKey), Does.Contain(TraitKey.Pull));
            Assert.That(stats.TraitKeys.ToArray(), Is.EqualTo(cowboy.Traits.ToArray().Select(trait => trait.TraitKey).ToArray()));
            Assert.That(stats.CategoryKey, Is.EqualTo(cowboy.CategoryKey));
        }

        [Test]
        public void ConfiguredExposureUnit_ConvertsMarkAlongsideOtherTraits()
        {
            using var fixture = new CoreAuthoringFixture();
            var markAuthoring = new HuntersMarkTraitAuthoring();
            CoreAuthoringFixture.Set(markAuthoring, "bonusDamagePercent", 25);
            CoreAuthoringFixture.Set(markAuthoring, "durationAffectedSideTurnEnds", 2);
            var unit = fixture.CreateUnit(EntityKey.Unit_ShotGun, CategoryKey.Heavy,
                new RegenerationTraitAuthoring(), markAuthoring);
            var data = (UnitData)unit.GetData();
            data.Validate();
            var spec = UnitSpecAdapter.ReadUnit(data);
            Assert.That((spec.FootprintWidth, spec.FootprintHeight), Is.EqualTo((1, 1)));
            var mark = data.Traits.OfType<HuntersMarkTraitData>().Single();
            Assert.That((mark.BonusDamagePercent, mark.DurationAffectedSideTurnEnds), Is.EqualTo((25, 2)));
            Assert.That(spec.Traits.ToArray().Select(trait => trait.TraitKey),
                Is.EqualTo(new[] { TraitKey.Regeneration, TraitKey.HuntersMark }));
            Assert.DoesNotThrow(() => TraitSpecAdapter.Read(mark));
        }

        [Test]
        public void RetiredTraitNumber_IsReservedAndSurvivorsKeepTheirValues()
        {
            Assert.That(Enum.IsDefined(typeof(TraitKey), 120), Is.False);
            Assert.That(((int)TraitKey.Focus, (int)TraitKey.DistanceDamageScale, (int)TraitKey.Intimidation,
                (int)TraitKey.DoubleShot, (int)TraitKey.ChainExplosion), Is.EqualTo((10, 60, 110, 160, 180)));
        }
    }
}
