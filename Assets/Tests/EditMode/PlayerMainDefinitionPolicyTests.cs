using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerMainDefinitionPolicyTests
    {
        [TestCase(CategoryKey.Rider, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(CategoryKey.Heavy, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(CategoryKey.Cannon, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(CategoryKey.Boss, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(CategoryKey.Main, UnitOwnerEligibility.EnemyOnly)]
        [TestCase(CategoryKey.Main, UnitOwnerEligibility.Both)]
        public void PlayerMain_RequiresMainCategoryAndPlayerOnly(CategoryKey category, UnitOwnerEligibility eligibility)
        {
            Assert.Throws<InvalidOperationException>(() => PlayerMainDefinitionPolicy.Validate(
                EntityKey.Unit_Akimbo, UnitRole.MainCharacter, category, eligibility));
        }

        [Test]
        public void DistinctTestOnlyMainDefinitions_AreEligible()
        {
            // 운영 역할을 바꾸지 않고, 허용 목록이나 두 종류 제한이 없는 공통 규칙을 검증한다.
            foreach (var key in new[] { EntityKey.Unit_Akimbo, EntityKey.Unit_Cowboy, EntityKey.Unit_Shielder })
            {
                var definition = MainFixture(key, 1);
                Assert.DoesNotThrow(() => Validate(definition));
            }
        }

        [Test]
        public void Demon_MainCategoryDoesNotGrantPlayerMainRole()
        {
            Assert.DoesNotThrow(() => PlayerMainDefinitionPolicy.Validate(
                EntityKey.Unit_Demon, UnitRole.Regular, CategoryKey.Main, UnitOwnerEligibility.EnemyOnly));
            Assert.Throws<InvalidOperationException>(() => PlayerMainDefinitionPolicy.Validate(
                EntityKey.Unit_Demon, UnitRole.MainCharacter, CategoryKey.Main, UnitOwnerEligibility.EnemyOnly));
        }

        [TestCase(UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(UnitRole.MainCharacter, UnitOwnerEligibility.Both)]
        [TestCase(UnitRole.Regular, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(UnitRole.Regular, UnitOwnerEligibility.Both)]
        public void RetiredMain_CannotBeActivePlayerDefinition(UnitRole role, UnitOwnerEligibility eligibility)
        {
            Assert.Throws<InvalidOperationException>(() => PlayerMainDefinitionPolicy.Validate(
                EntityKey.Unit_MainCharacter, role, CategoryKey.Main, eligibility));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        public void ValidMainDefinition_DoesNotForceAttackRangeOne(int attackRange)
        {
            var definition = MainFixture(EntityKey.Unit_Akimbo, attackRange);

            Assert.DoesNotThrow(() => Validate(definition));
            Assert.That(definition.AttackRange, Is.EqualTo(attackRange));
        }

        private static void Validate(UnitSpec definition)
        {
            PlayerMainDefinitionPolicy.Validate(definition.Key, definition.Role,
                definition.CategoryKey, definition.OwnerEligibility);
        }

        private static UnitSpec MainFixture(EntityKey key, int attackRange)
        {
            // 순수 입력 계약만 만든다. 운영 UnitDefinition의 기존 검증 연결은 D1에서 전환한다.
            return new UnitSpec(key, UnitRole.MainCharacter, 1, 1, new[] { new CellCoord(0, 0) },
                10, MoveMethod.LiftAndPlace, attackRange, CategoryKey.Main, 2, 0, 10, Array.Empty<TraitSpec>(),
                UnitOwnerEligibility.PlayerOnly);
        }
    }
}
