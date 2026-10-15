using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Framework
{
    /// <summary>Unity 객체 없이 공격 지정 방식의 데이터 계약을 검증합니다.</summary>
    public sealed class TargetingMethodDataTests
    {
        [TestCase(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)]
        [TestCase(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0)]
        [TestCase(TargetingShape.EightDirectionLine, TargetingChoiceMode.All, TargetingSplash.None, 0)]
        [TestCase(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.Cross, 1)]
        [TestCase(TargetingShape.DiagonalLine, TargetingChoiceMode.Single, TargetingSplash.Diagonal, 100)]
        [TestCase(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 150)]
        [TestCase(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, int.MaxValue)]
        public void Validate_AcceptsSupportedMethodsWithoutPercentUpperLimit(TargetingShape shape,
            TargetingChoiceMode choiceMode, TargetingSplash splash, int percent)
        {
            var data = new TargetingMethodData(1, shape, choiceMode, splash, percent);

            Assert.DoesNotThrow(data.Validate);
            Assert.That((data.MethodKey, data.Shape, data.ChoiceMode, data.Splash, data.SplashPercent),
                Is.EqualTo((1, shape, choiceMode, splash, percent)));
        }

        [TestCase(0, 5, 1, 0, 0)]
        [TestCase(-1, 5, 1, 0, 0)]
        [TestCase(1, 0, 1, 0, 0)]
        [TestCase(1, 99, 1, 0, 0)]
        [TestCase(1, 5, 0, 0, 0)]
        [TestCase(1, 5, 99, 0, 0)]
        [TestCase(1, 5, 1, 99, 1)]
        [TestCase(1, 5, 1, 0, -1)]
        [TestCase(1, 5, 1, 0, 1)]
        [TestCase(1, 5, 1, 1, 0)]
        [TestCase(1, 5, 1, 1, -1)]
        [TestCase(1, 4, 2, 1, 50)]
        [TestCase(1, 3, 3, 3, 50)]
        public void Validate_RejectsInvalidKeyEnumPercentAndChoiceSplashCombination(
            int key, int shape, int choice, int splash, int percent)
        {
            var data = new TargetingMethodData(key, (TargetingShape)shape,
                (TargetingChoiceMode)choice, (TargetingSplash)splash, percent);

            Assert.Throws<InvalidOperationException>(data.Validate);
        }

        [TestCase(1, 0, false)]
        [TestCase(1, -1, false)]
        [TestCase(1, 1, true)]
        [TestCase(0, 0, true)]
        [TestCase(0, -1, false)]
        [TestCase(0, 1, true)]
        public void Unit_RequiresPositiveMethodKeyWhenAttacking(int attackPower, int methodKey, bool valid)
        {
            var unit = new UnitData(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.Both,
                1, 1, 10, 0, 3, 1, 1, 
                CategoryKey.Base, attackPower, 3, 1, 10, 10,
                Array.Empty<UnitTraitData>(), methodKey);

            if (valid)
                Assert.DoesNotThrow(unit.Validate);
            else
                Assert.Throws<InvalidOperationException>(unit.Validate);
        }
    }
}
