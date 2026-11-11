using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>적 전용 사망 특성의 DB 입력 → 순수 데이터 → Core Spec 변환과 DB 유닛 검사입니다.</summary>
    public sealed class EnemyDeathTraitAuthoringTests
    {
        [Test]
        public void Authoring_ReturnsFreshPureDataWithTheDefaultLevel()
        {
            var reconstruction = new ReconstructionTraitAuthoring();
            var first = (ReconstructionTraitData)reconstruction.ToData();
            Assert.That(first.Level, Is.EqualTo(1));
            Assert.That(reconstruction.ToData(), Is.Not.SameAs(first));
            Assert.That(((SelfDestructTraitData)new SelfDestructTraitAuthoring().ToData()).Level, Is.EqualTo(1));
            Assert.That(new FearTraitAuthoring().ToData(), Is.TypeOf<FearTraitData>());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Adapter_CarriesTheLevelIntoTheSpec(int level)
        {
            Assert.That(((ReconstructionTraitSpec)TraitSpecAdapter.Read(new ReconstructionTraitData(level))).Level, Is.EqualTo(level));
            Assert.That(((SelfDestructTraitSpec)TraitSpecAdapter.Read(new SelfDestructTraitData(level))).Level, Is.EqualTo(level));
            Assert.That(TraitSpecAdapter.Read(new FearTraitData()), Is.TypeOf<FearTraitSpec>());
        }

        [TestCase(0)]
        [TestCase(4)]
        public void Data_RejectsALevelOutOfRange(int level)
        {
            Assert.Throws<InvalidOperationException>(() => new ReconstructionTraitData(level).Validate());
            Assert.Throws<InvalidOperationException>(() => new SelfDestructTraitData(level).Validate());
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(new ReconstructionTraitData(level)));
        }

        [Test]
        public void UnitData_AcceptsDeathTraitsOnEnemyOnlyUnits()
        {
            Assert.DoesNotThrow(() => Unit(UnitOwnerEligibility.EnemyOnly,
                new ReconstructionTraitData(2), new SelfDestructTraitData(3), new FearTraitData()).Validate());
        }

        [TestCase(UnitOwnerEligibility.PlayerOnly)]
        [TestCase(UnitOwnerEligibility.Both)]
        public void UnitData_RejectsDeathTraitsOnUnitsThatCanJoinThePlayer(UnitOwnerEligibility eligibility)
        {
            foreach (var trait in new UnitTraitData[] { new ReconstructionTraitData(1), new SelfDestructTraitData(1), new FearTraitData() })
            {
                var error = Assert.Throws<InvalidOperationException>(() => Unit(eligibility, trait).Validate());
                Assert.That(error.Message, Does.Contain("Enemy-only"), trait.TraitKey.ToString());
            }
        }

        private static UnitData Unit(UnitOwnerEligibility eligibility, params UnitTraitData[] traits) =>
            new(EntityKey.Unit_Demon, UnitRole.Regular, eligibility, 1, 1, 10, 0, 1, CategoryKey.Heavy, 0, 1, traits, 0);
    }
}
