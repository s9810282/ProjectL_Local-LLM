using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class MainCategoryMigrationTests
    {
        [Test]
        public void Rename_PreservesNumericIdentityAndMatchupMatrix()
        {
            // 변경 전 숫자 순서와 방향별 25개 값을 고정해 이름 변경과 함께 검증한다.
            int[] keys = { 10, 20, 30, 40, 50 };
            int[,] numerators =
            {
                { 10, 10, 10, 10, 10 },
                { 10, 10, 7, 12, 10 },
                { 10, 15, 10, 9, 10 },
                { 10, 9, 11, 10, 10 },
                { 10, 10, 10, 10, 10 },
            };
            var matchup = CategoryMatchupSpec.CreateDefault();

            for (int attacker = 0; attacker < keys.Length; attacker++)
            {
                for (int defender = 0; defender < keys.Length; defender++)
                {
                    Assert.That(matchup.TryGetEntry((CategoryKey)keys[attacker], (CategoryKey)keys[defender], out var entry),
                        Is.True, $"{keys[attacker]}->{keys[defender]}");
                    Assert.That(entry.Numerator, Is.EqualTo(numerators[attacker, defender]));
                    Assert.That(entry.Denominator, Is.EqualTo(10));
                }
            }
            Assert.That(Array.ConvertAll((CategoryKey[])Enum.GetValues(typeof(CategoryKey)), key => (int)key),
                Is.EqualTo(new[] { 0, 10, 20, 30, 40, 50 }));
            Assert.That(Enum.GetName(typeof(CategoryKey), 10), Is.EqualTo("Main"));
            Assert.That(Enum.GetNames(typeof(CategoryKey)), Does.Not.Contain("Base"));
        }
    }
}
