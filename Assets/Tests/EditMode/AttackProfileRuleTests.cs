using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 옛 지정 방식 필드에 기대던 검사를 새 기준으로 옮긴 규칙을 확인한다.
    /// 공격 모양은 지정 방식이 소유하며, 가시밭과의 결합은 CombatAttackProfile이 검사한다.
    /// </summary>
    public sealed class AttackProfileRuleTests
    {
        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void Category_UsesExplicitTargetingGeometry(CategoryKey category)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single,
                TargetingSplash.Radial, 70);
            var profile = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, category), method);
            Assert.That(profile.Method, Is.SameAs(method));
        }

        /// <summary>선인장 캐논 형태(정사각형·칸 선택·정사각형 주변 스플래시)는 가시밭과 함께 쓸 수 있다.</summary>
        [Test]
        public void ThornField_AllowsSingleChoiceMethod()
        {
            var cactusMethod = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);

            Assert.DoesNotThrow(() => new CombatAttackProfile(ThornCannon(), cactusMethod));
        }

        [TestCase(TargetingShape.Fan, TargetingChoiceMode.Direction)]
        [TestCase(TargetingShape.RadialSquare, TargetingChoiceMode.All)]
        public void ThornField_RejectsMethodWithoutSelectedCell(TargetingShape shape, TargetingChoiceMode choiceMode)
        {
            var method = new TargetingMethodSpec(shape, choiceMode, TargetingSplash.None, 0);

            var exception = Assert.Throws<ArgumentException>(() => new CombatAttackProfile(ThornCannon(), method));
            StringAssert.Contains("가시밭", exception.Message);
        }

        private static UnitSpec ThornCannon() =>
            CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Cannon, new ThornFieldTraitSpec(5, 5));
    }
}
