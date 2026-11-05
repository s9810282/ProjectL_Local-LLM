using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    /// <summary>경험치·레벨업 설계 §3.4: 메인 랜덤 성장은 균등 확률의 결정적 순수 함수이고, 결과는 지속 상태에 더한다.</summary>
    public sealed class MainGrowthRulesTests
    {
        private static readonly LevelProgressionSpec Spec = new(150, 100, 10, 3, 1, 1);
        private static readonly CharacterInstanceId Main = new(1);

        [Test]
        public void Choose_IsDeterministicForSameInputs()
        {
            Assert.That(MainGrowthRules.Choose(37, Main, 2), Is.EqualTo(MainGrowthRules.Choose(37, Main, 2)));
            Assert.That(Enumerable.Range(2, 50).Select(level => MainGrowthRules.Choose(37, Main, level)),
                Is.All.Not.EqualTo(MainGrowthKind.None));
        }

        [Test]
        public void Choose_IsRoughlyUniformAcrossLevelsAndSeeds()
        {
            var counts = Enumerable.Range(0, 30).SelectMany(seed => Enumerable.Range(2, 100)
                    .Select(level => MainGrowthRules.Choose(seed * 7919, Main, level)))
                .GroupBy(kind => kind).ToDictionary(g => g.Key, g => g.Count());

            // 3000번 중 각 선택지 기대값 1000. 결정적 함수이므로 넉넉한 범위로 균등함을 확인한다.
            Assert.That(counts.Keys, Is.EquivalentTo(new[] { MainGrowthKind.MaxHealth, MainGrowthKind.Attack, MainGrowthKind.Defense }));
            Assert.That(counts.Values, Is.All.InRange(850, 1150));
        }

        [Test]
        public void Choose_RejectsInvalidInputs()
        {
            Assert.Throws<ArgumentException>(() => MainGrowthRules.Choose(1, CharacterInstanceId.Empty, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => MainGrowthRules.Choose(1, Main, 1));
        }

        [Test]
        public void Apply_AddsOneGrowth_KeepingCurrentHealth()
        {
            var state = new MainCharacterPersistentState(30, 50, 1, 2);

            var health = MainGrowthRules.Apply(state, MainGrowthKind.MaxHealth, Spec);
            var attack = MainGrowthRules.Apply(state, MainGrowthKind.Attack, Spec);
            var defense = MainGrowthRules.Apply(state, MainGrowthKind.Defense, Spec);

            Assert.That((health.CurrentHealth, health.PersistentMaxHealth, health.PersistentAttack, health.PersistentDefense),
                Is.EqualTo((30, 53, 1, 2)), "최대 체력이 올라도 현재 체력은 유지(X4)");
            Assert.That((attack.CurrentHealth, attack.PersistentMaxHealth, attack.PersistentAttack, attack.PersistentDefense),
                Is.EqualTo((30, 50, 2, 2)));
            Assert.That((defense.CurrentHealth, defense.PersistentMaxHealth, defense.PersistentAttack, defense.PersistentDefense),
                Is.EqualTo((30, 50, 1, 3)));
            Assert.Throws<ArgumentOutOfRangeException>(() => MainGrowthRules.Apply(state, MainGrowthKind.None, Spec));
            Assert.Throws<InvalidOperationException>(() => MainGrowthRules.Apply(state, MainGrowthKind.Attack, default));
        }
    }
}
