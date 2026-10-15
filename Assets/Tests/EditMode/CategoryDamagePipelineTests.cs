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
    /// 순서는 Trait 가산 → Category 상성 → PhysicalDefense → Heavy 피격 경감 → 최종 피해다.
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
        [TestCase(CategoryKey.Base, 10, 4)]   // Rider->Base 1.0: 4
        public void Calculate_AppliesIndependentMultiplierPerDefender(
            CategoryKey defender, int expectedNumerator, int expectedDamage)
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(
                damageBeforeMatchup: 4,
                attackerCategory: CategoryKey.Rider,
                defenderCategory: defender,
                incomingReduction: 0);

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
                10, CategoryKey.Rider, CategoryKey.Heavy, 0);
            TargetDamageBreakdown heavyIntoRider = pipeline.Calculate(
                10, CategoryKey.Heavy, CategoryKey.Rider, 0);

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

            TargetDamageBreakdown breakdown = pipeline.Calculate(damage, attacker, defender, 0);

            Assert.That(breakdown.DamageAfterMatchup, Is.EqualTo(expected));
        }

        // [사용자 확정] Heavy 경감은 상성과 PhysicalDefense 뒤에 적용된다.
        // Rider->Heavy 입력 4: 상성 먼저면 2.8->3, 그 뒤 50% floor 경감 1 => 최종 2.
        // Heavy 먼저였다면 4->2->1이 되므로 순서가 결과를 바꾼다.
        [Test]
        public void Calculate_AppliesIncomingReductionAfterMatchupAndDefense()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(
                damageBeforeMatchup: 4,
                attackerCategory: CategoryKey.Rider,
                defenderCategory: CategoryKey.Heavy,
                incomingReduction: 1); // 3의 50% floor

            Assert.That(breakdown.DamageAfterMatchup, Is.EqualTo(3), "상성이 먼저 적용된다.");
            Assert.That(breakdown.DamageAfterPhysicalDefense, Is.EqualTo(3));
            Assert.That(breakdown.IncomingReduction, Is.EqualTo(1));
            Assert.That(breakdown.FinalDamage, Is.EqualTo(2), "Heavy 먼저 적용한 1이 아니다.");
        }

        // 경감량은 남은 피해를 넘지 않는다.
        [Test]
        public void Calculate_CapsReductionAtRemainingDamage()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(
                2, CategoryKey.Base, CategoryKey.Base, incomingReduction: 99);

            Assert.That(breakdown.IncomingReduction, Is.EqualTo(2));
            Assert.That(breakdown.FinalDamage, Is.Zero);
        }

        // Category가 없는 대상은 상성을 적용하지 않고 통과시킨다. 가짜 Base를 만들지 않는다.
        [Test]
        public void Calculate_SkipsMatchupForCategorylessTarget()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            TargetDamageBreakdown breakdown = pipeline.Calculate(
                4, CategoryKey.Rider, defenderCategory: null, incomingReduction: 0);

            Assert.That(breakdown.MatchupNumerator, Is.EqualTo(breakdown.MatchupDenominator));
            Assert.That(breakdown.FinalDamage, Is.EqualTo(4));
        }

        [Test]
        public void Calculate_RejectsNegativeInput()
        {
            TargetDamagePipeline pipeline = CreatePipeline();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                pipeline.Calculate(-1, CategoryKey.Base, CategoryKey.Base, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                pipeline.Calculate(1, CategoryKey.Base, CategoryKey.Base, -1));
        }
    }
}
