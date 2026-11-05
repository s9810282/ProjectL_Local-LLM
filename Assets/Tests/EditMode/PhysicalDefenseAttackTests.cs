using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>공격 피해에 실효 방어력 백분율을 적용하고 미리보기와 실제 결과가 일치하는지 검증한다.</summary>
    public sealed class PhysicalDefenseAttackTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [TestCase(0, 20)]
        [TestCase(5, 19)]
        [TestCase(100, 1)]
        [TestCase(150, 1)]
        public void Attack_PercentageDefense_WithMinimumOne_AndPreviewAgrees(int defense, int expected)
        {
            // 행동자 공격 20, 대상 체력 100. 방어 0이면 기존 피해와 같다.
            var baseline = CombatCommandTestData.Input();
            var input = CombatCommandTestData.With(baseline, characters: baseline.Characters
                .Select(c => c.EntityId == new EntityId(2) ? WithDefense(c, defense) : c).ToArray());

            var result = CombatCommandCalculator.Calculate(input, NoRules);
            var preview = CombatCommandCalculator.Preview(input, NoRules);

            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(2));
            Assert.That(hit.DamageDealt, Is.EqualTo(expected));
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(2)).ExpectedDamage, Is.EqualTo(expected));
        }

        private static CharacterCombatFacts WithDefense(CharacterCombatFacts c, int defense)
        {
            var s = c.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod,s.MoveRange,s.AppointRange,s.AttackPower,defense,s.Priority,                s.TraitStatuses.ToArray(),s.Category,s.ArtifactAttackBonus,s.EffectiveAttackPower,s.SoulAttackBonus,                s.SoulMaxHealthBonus,s.PositionAttackBonus);
            return CombatCommandCalculator.Copy(c, c.Health, stats);
        }
    }
}
