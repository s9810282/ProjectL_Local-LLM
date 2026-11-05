using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterOwnershipLimitTests
    {
        private static CharacterDefinitionFacts Sub(EntityKey key, CategoryKey category) =>
            new CharacterDefinitionFacts(key, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly, category: category);
        private static CharacterDefinitionFacts Main(EntityKey key) =>
            new CharacterDefinitionFacts(key, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly, category: CategoryKey.Main);

        [Test]
        public void Limits_AreOnePerDefinitionAndTwoPerSubCategory()
        {
            Assert.That(CharacterRosterRules.PerDefinitionLimit, Is.EqualTo(1));
            Assert.That(CharacterRosterRules.SubCategoryLimit, Is.EqualTo(2));
        }

        [Test]
        public void DifferentCannons_ShareCategoryCap()
        {
            var cactus = Sub(EntityKey.Unit_CactusCannon, CategoryKey.Cannon);
            var ballista = Sub(EntityKey.Unit_Ballista, CategoryKey.Cannon);
            var third = Sub(EntityKey.Unit_Gravekeeper, CategoryKey.Cannon);
            Assert.That(CharacterRosterRules.ValidateOwnership(new[] { cactus, ballista }), Is.EqualTo(RosterAcquireFailureReason.None));
            var overflow = new[] { cactus, ballista, third };
            Assert.That(CharacterRosterRules.ValidateOwnership(overflow), Is.EqualTo(RosterAcquireFailureReason.OwnershipLimitReached));
            Assert.That(overflow, Is.EqualTo(new[] { cactus, ballista, third }));
        }

        [Test]
        public void DistinctTestOnlyMains_AreLegalButDuplicateDefinitionIsRejected()
        {
            var first = Main(EntityKey.Unit_Akimbo);
            var second = Main(EntityKey.Unit_Cowboy);
            Assert.That(CharacterRosterRules.ValidateOwnership(new[] { first, second }), Is.EqualTo(RosterAcquireFailureReason.None));
            Assert.That(CharacterRosterRules.ValidateOwnership(new[] { first, first }), Is.EqualTo(RosterAcquireFailureReason.OwnershipLimitReached));
        }

        /// <summary>중복 서브 금지 설계 U1: 카테고리 칸이 남아 있어도 같은 서브 정의는 하나만 보유한다.</summary>
        [TestCase(CategoryKey.Cannon)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void SameSubDefinition_IsRejectedEvenWithCategoryRoom(CategoryKey category)
        {
            var cactus = Sub(EntityKey.Unit_CactusCannon, category);
            Assert.That(CharacterRosterRules.ValidateOwnership(new[] { cactus }), Is.EqualTo(RosterAcquireFailureReason.None));
            Assert.That(CharacterRosterRules.ValidateOwnership(new[] { cactus, cactus }), Is.EqualTo(RosterAcquireFailureReason.OwnershipLimitReached));
            Assert.That(CharacterRosterRules.PlanAcquire(cactus, new[] { cactus }).FailureReason,
                Is.EqualTo(RosterAcquireFailureReason.OwnershipLimitReached));
        }

        [Test]
        public void NoFixedTotalOrTwoMainDefinitionLimit()
        {
            // 운영 콘텐츠가 아닌 세 Main과 여섯 Sub 정의로 9개 보유를 검증한다.
            var instances = new[]
            {
                Main(EntityKey.Unit_Akimbo), Main(EntityKey.Unit_Cowboy), Main(EntityKey.Unit_Shielder),
                Sub(EntityKey.Unit_CactusCannon, CategoryKey.Cannon), Sub(EntityKey.Unit_Ballista, CategoryKey.Cannon),
                Sub(EntityKey.Unit_ShotGun, CategoryKey.Heavy), Sub(EntityKey.Unit_DynamiteHammer, CategoryKey.Heavy),
                Sub(EntityKey.Unit_Gravekeeper, CategoryKey.Rider), Sub(EntityKey.Unit_UndeadHeavy, CategoryKey.Rider),
            };
            Assert.That(CharacterRosterRules.ValidateOwnership(instances), Is.EqualTo(RosterAcquireFailureReason.None));
        }

        [TestCase(CategoryKey.None)]
        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Boss)]
        public void UnsupportedSubCategory_RejectsWholeBatch(CategoryKey category)
        {
            var invalid = Sub(EntityKey.Unit_CactusCannon, category);
            Assert.That(CharacterRosterRules.ValidateOwnership(new[] { Main(EntityKey.Unit_Akimbo), invalid }),
                Is.EqualTo(RosterAcquireFailureReason.InvalidDefinition));
        }

        [Test]
        public void EnemyRetiredAndNullDefinitions_AreRejected()
        {
            var enemy = new CharacterDefinitionFacts(EntityKey.Unit_Demon, UnitRole.Regular,
                UnitOwnerEligibility.EnemyOnly, category: CategoryKey.Main);
            foreach (var definition in new[] { enemy, Main(EntityKey.Unit_MainCharacter), null })
                Assert.That(CharacterRosterRules.ValidateOwnership(new[] { definition }), Is.EqualTo(RosterAcquireFailureReason.InvalidDefinition));
            Assert.Throws<ArgumentNullException>(() => CharacterRosterRules.ValidateOwnership(null));
        }

        [Test]
        public void AddedFailureReason_PreservesExistingNumbers()
        {
            Assert.That((int)RosterAcquireFailureReason.None, Is.Zero);
            Assert.That((int)RosterAcquireFailureReason.InvalidRequest, Is.EqualTo(1));
            Assert.That((int)RosterAcquireFailureReason.InvalidDefinition, Is.EqualTo(2));
            Assert.That((int)RosterAcquireFailureReason.MainAlreadyOwned, Is.EqualTo(3));
            Assert.That((int)RosterAcquireFailureReason.InvalidMainState, Is.EqualTo(4));
            Assert.That((int)RosterAcquireFailureReason.IdentityOverflow, Is.EqualTo(5));
            Assert.That((int)RosterAcquireFailureReason.RunEnded, Is.EqualTo(6));
            Assert.That((int)RosterAcquireFailureReason.OwnershipLimitReached, Is.EqualTo(7));
        }
    }
}
