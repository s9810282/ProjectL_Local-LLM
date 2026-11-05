using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Task 10b: 확정된 대상별 피해 단계 순서와 상성 통합을 검증한다.
    /// 공격자 가산 뒤 Category 상성 → PhysicalDefense를 검증한다. 피격 경감과 최소 피해는 Attack이 조립한다.
    /// </summary>
    public sealed class CategoryDamagePipelineTests
    {
        private static TargetDamagePipeline CreatePipeline()
        {
            return new TargetDamagePipeline(
                new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()));
        }

        // 하나의 다중 대상 공격에서 피격자 Category마다 독립적인 배율이 적용된다.
        [TestCase(CategoryKey.Heavy, 7, 3)]   // Rider->Heavy 0.7: 2.8 -> 3
        [TestCase(CategoryKey.Cannon, 12, 5)] // 4 * 1.2 = 4.8 -> 5
        [TestCase(CategoryKey.Main, 10, 4)]   // Rider->Base 1.0: 4
        public void Calculate_AppliesIndependentMultiplierPerDefender(
            CategoryKey defender, int expectedNumerator, int expectedDamage)
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(
                damageBeforeMatchup: 4,
                attackerCategory: CategoryKey.Rider,
                defenderCategory: defender);

            Assert.That(breakdown.MatchupNumerator, Is.EqualTo(expectedNumerator));
            Assert.That(breakdown.DamageAfterMatchup, Is.EqualTo(expectedDamage));
            Assert.That(breakdown.FinalDamage, Is.EqualTo(expectedDamage));
        }

        // 공격자/피격자 방향성이 보존된다. Rider->Heavy와 Heavy->Rider는 다르다.
        [Test]
        public void Calculate_PreservesAttackerDefenderDirectionality()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown riderIntoHeavy = pipeline.Calculate(
                10, CategoryKey.Rider, CategoryKey.Heavy);
            TargetDamageBreakdown heavyIntoRider = pipeline.Calculate(
                10, CategoryKey.Heavy, CategoryKey.Rider);

            Assert.That(riderIntoHeavy.DamageAfterMatchup, Is.EqualTo(7));
            Assert.That(heavyIntoRider.DamageAfterMatchup, Is.EqualTo(15));
        }

        // half-up 반올림. banker's rounding이면 4.5는 4가 된다.
        [TestCase(CategoryKey.Cannon, CategoryKey.Rider, 5, 5)] // 9/10: 4.5 -> 5
        [TestCase(CategoryKey.Heavy, CategoryKey.Rider, 5, 8)]  // 15/10: 7.5 -> 8
        [TestCase(CategoryKey.Cannon, CategoryKey.Heavy, 4, 4)] // 11/10: 4.4 -> 4
        public void Calculate_UsesHalfUpRounding(
            CategoryKey attacker, CategoryKey defender, int damage, int expected)
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(damage, attacker, defender);

            Assert.That(breakdown.DamageAfterMatchup, Is.EqualTo(expected));
        }

        // 상성 5 * 0.7을 반올림한 4에서 방어 50%를 적용하면 2다. 방어를 먼저 적용하면 1이 된다.
        [Test]
        public void Calculate_AppliesDefenseAfterMatchup()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(
                damageBeforeMatchup: 5,
                attackerCategory: CategoryKey.Rider,
                defenderCategory: CategoryKey.Heavy,
                physicalDefense: 50);

            Assert.That(breakdown.DamageAfterMatchup, Is.EqualTo(4));
            Assert.That(breakdown.DamageAfterPhysicalDefense, Is.EqualTo(2));
            Assert.That(breakdown.IncomingReduction, Is.Zero);
            Assert.That(breakdown.FinalDamage, Is.EqualTo(2));
        }

        // Category가 없는 대상은 상성을 적용하지 않고 통과시킨다. 가짜 Base를 만들지 않는다.
        [Test]
        public void Calculate_SkipsMatchupForCategorylessTarget()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(
                4, CategoryKey.Rider, defenderCategory: null);

            Assert.That(breakdown.MatchupNumerator, Is.EqualTo(breakdown.MatchupDenominator));
            Assert.That(breakdown.FinalDamage, Is.EqualTo(4));
        }

        [Test]
        public void Calculate_RejectsNegativeInput()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                pipeline.Calculate(-1, CategoryKey.Main, CategoryKey.Main));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                pipeline.Calculate(1, CategoryKey.Main, CategoryKey.Main, physicalDefense: -1));
        }

        // 방어력은 백분율이며 소수점을 버린다. 중간 계산에서는 최소 피해를 보정하지 않는다.
        [TestCase(100, 5, 95)]
        [TestCase(20, 5, 19)]
        [TestCase(10, 5, 9)]
        [TestCase(5, 0, 5)]
        [TestCase(1, 5, 0)]
        [TestCase(5, 100, 0)]
        [TestCase(5, int.MaxValue, 0)]
        [TestCase(int.MaxValue, 5, 2040109464)]
        [TestCase(0, 5, 0)]
        public void Calculate_PhysicalDefenseReducesPercentageWithoutMinimum(int damage, int defense, int afterDefense)
        {
            TargetDamageBreakdown breakdown = CreatePipeline().Calculate(
                damage, CategoryKey.Main, CategoryKey.Main, physicalDefense: defense);

            Assert.That(breakdown.DamageAfterPhysicalDefense, Is.EqualTo(afterDefense));
            Assert.That(breakdown.FinalDamage, Is.EqualTo(afterDefense));
            Assert.That(breakdown.IncomingReduction, Is.Zero);
            Assert.That(breakdown.DamageAfterReceivedPercentages, Is.EqualTo((decimal)afterDefense));
        }
    }
}
