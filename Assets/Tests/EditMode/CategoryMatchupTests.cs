using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Task 10a: 16개 방향성 상성표, 순수 조회, 반올림만 검증한다.
    /// 피해 파이프라인 통합은 Task 10b이며 여기서 다루지 않는다.
    /// </summary>
    public sealed class CategoryMatchupTests
    {
        // 표를 행/열 뒤집지 않고 그대로 만든다. 비대칭 항목으로 확인한다.
        [TestCase(CategoryKey.Heavy, CategoryKey.Rider, 15)]
        [TestCase(CategoryKey.Rider, CategoryKey.Heavy, 7)]
        [TestCase(CategoryKey.Cannon, CategoryKey.Heavy, 11)]
        [TestCase(CategoryKey.Heavy, CategoryKey.Cannon, 9)]
        [TestCase(CategoryKey.Rider, CategoryKey.Cannon, 12)]
        [TestCase(CategoryKey.Cannon, CategoryKey.Rider, 9)]
        [TestCase(CategoryKey.Base, CategoryKey.Heavy, 10)]
        public void GetMultiplier_PreservesDirectionalAsymmetry(
            CategoryKey attacker, CategoryKey defender, int expectedNumerator)
        {
            var resolver = new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault());

            CategoryMatchupMultiplier multiplier = resolver.GetMultiplier(attacker, defender);

            Assert.That(multiplier.Numerator, Is.EqualTo(expectedNumerator));
            Assert.That(multiplier.Denominator, Is.EqualTo(10));
        }

        // 누락/중복/미등록/음수/0 분모는 bootstrap 오류다.
        [Test]
        public void Spec_RejectsInvalidEntries()
        {
            var valid = new CategoryMatchupEntry(CategoryKey.Base, CategoryKey.Base, 10, 10);

            // 16개가 모두 있어야 한다.
            Assert.Throws<ArgumentException>(() => new CategoryMatchupSpec(new[] { valid }));

            // 음수 분자와 0 분모는 항목 자체가 거부된다.
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CategoryMatchupEntry(CategoryKey.Base, CategoryKey.Base, -1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CategoryMatchupEntry(CategoryKey.Base, CategoryKey.Base, 10, 0));

            // None은 유효한 Category가 아니다.
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CategoryMatchupEntry(CategoryKey.None, CategoryKey.Base, 10, 10));
        }

        [Test]
        public void GetMultiplier_RejectsUnregisteredCategory()
        {
            var resolver = new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                resolver.GetMultiplier(CategoryKey.None, CategoryKey.Base));
        }

        // [사용자 확정] 비음수 피해의 half-up 반올림. banker's rounding을 쓰지 않는다.
        // 비-동률
        [TestCase(3, 9, 10, 3)] // 2.7 -> 3
        [TestCase(3, 11, 10, 3)] // 3.3 -> 3
        [TestCase(6, 7, 10, 4)] // 4.2 -> 4
        [TestCase(4, 11, 10, 4)] // 4.4 -> 4
        // 동률은 올림
        [TestCase(5, 9, 10, 5)] // 4.5 -> 5
        [TestCase(5, 15, 10, 8)] // 7.5 -> 8
        [TestCase(9, 5, 10, 5)] // 4.5 -> 5
        // 0 피해는 0이며 최소 피해 1을 여기서 만들지 않는다.
        [TestCase(0, 15, 10, 0)]
        public void Apply_UsesHalfUpRounding(int damage, int numerator, int denominator, int expected)
        {
            int result = CategoryMatchupDamageCalculator.Apply(damage, numerator, denominator);

            Assert.That(result, Is.EqualTo(expected));
        }

        // ToEven(banker's)과 결과가 달라지는 지점을 명시적으로 고정한다.
        [Test]
        public void Apply_DoesNotUseBankersRounding()
        {
            // 4.5와 7.5는 ToEven이면 4와 8이 된다. half-up은 5와 8이다.
            Assert.That(CategoryMatchupDamageCalculator.Apply(5, 9, 10), Is.EqualTo(5));
            Assert.That(CategoryMatchupDamageCalculator.Apply(5, 15, 10), Is.EqualTo(8));
            Assert.That(Math.Round(4.5, MidpointRounding.ToEven), Is.EqualTo(4.0));
        }

        [Test]
        public void Apply_RejectsNegativeDamageAndInvalidRatio()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CategoryMatchupDamageCalculator.Apply(-1, 10, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CategoryMatchupDamageCalculator.Apply(1, -1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CategoryMatchupDamageCalculator.Apply(1, 10, 0));
        }
    }
}
