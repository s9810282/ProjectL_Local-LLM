using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactSoulSchemaTests
    {
        [TestCase(1, 1, 0)]
        [TestCase(1, 1, 1)]
        [TestCase(1, 1, 5)]
        public void Compile_AcceptsFixedGrowthAndDataDrivenHealing(
            int attackBonus,
            int maxHealthBonus,
            int healAmount)
        {
            ArtifactEffectData effect = SoulEffect();
            effect.effect.soulGrowth = new SoulGrowthAmounts(
                attackBonus,
                maxHealthBonus,
                healAmount);

            CompiledArtifactLoadout compiled = ArtifactEffectCompiler.Compile(Snapshot(effect));

            Assert.That(compiled.Rules.Length, Is.EqualTo(1));
            var rule = (SoulAltarRule)compiled.Rules.Span[0].Rule;
            Assert.That(rule.Amounts.AttackBonus, Is.EqualTo(attackBonus));
            Assert.That(rule.Amounts.MaxHealthBonus, Is.EqualTo(maxHealthBonus));
            Assert.That(rule.Amounts.HealAmount, Is.EqualTo(healAmount));
            Assert.That(rule.DurationUnitTurns, Is.EqualTo(5));
        }

        [TestCase(2, 1)]
        [TestCase(1, 0)]
        public void SoulAltar_RejectsGrowthOtherThanOnePerSoul(int attack, int health)
        {
            ArtifactEffectData effect = SoulEffect();
            effect.effect.soulGrowth = new SoulGrowthAmounts(attack, health, 0);
            Assert.Throws<ArgumentException>(() => ArtifactEffectCompiler.Compile(Snapshot(effect)));
        }

        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        public void SoulSpawnsOnlyForNightEnemyUnitDeath(
            bool isNight,
            bool isEnemyUnitDeath,
            bool expected)
        {
            var rule = new SoulAltarRule(new SoulGrowthAmounts(5, 5, 5), 5);

            Assert.That(rule.CanSpawn(isNight, isEnemyUnitDeath), Is.EqualTo(expected));
        }

        [TestCase(-1, 0, 0, "attackBonus")]
        [TestCase(0, -1, 0, "maxHealthBonus")]
        [TestCase(0, 0, -1, "healAmount")]
        public void Amounts_RejectNegativeConstructorInputs(
            int attackBonus,
            int maxHealthBonus,
            int healAmount,
            string parameter)
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SoulGrowthAmounts(attackBonus, maxHealthBonus, healAmount));

            Assert.That(error.ParamName, Is.EqualTo(parameter));
        }

        [TestCase("attackBonus")]
        [TestCase("maxHealthBonus")]
        [TestCase("healAmount")]
        public void Compile_RejectsNegativeSoulAmountsWithContext(string field)
        {
            ArtifactEffectData effect = SoulEffect();
            switch (field)
            {
                case "attackBonus": effect.effect.soulGrowth.attackBonus = -1; break;
                case "maxHealthBonus": effect.effect.soulGrowth.maxHealthBonus = -1; break;
                case "healAmount": effect.effect.soulGrowth.healAmount = -1; break;
                default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }

            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                ArtifactEffectCompiler.Compile(Snapshot(effect)));

            StringAssert.Contains("ArtifactId=11", error.Message);
            StringAssert.Contains("InstanceId=4", error.Message);
            StringAssert.Contains("entryId=soul", error.Message);
            StringAssert.Contains($"field=effect.soulGrowth.{field}", error.Message);
        }

        [TestCase("trigger")]
        [TestCase("source.faction")]
        [TestCase("source.category")]
        [TestCase("condition.type")]
        [TestCase("condition.comparison")]
        [TestCase("target.type")]
        [TestCase("target.faction")]
        [TestCase("target.category")]
        [TestCase("target.excludeCategory")]
        [TestCase("target.useHpRatioFilter")]
        [TestCase("target.maxHpRatio")]
        [TestCase("location.type")]
        [TestCase("effect.type")]
        [TestCase("effect.statId")]
        [TestCase("effect.knockbackDistance")]
        [TestCase("value.type")]
        [TestCase("value.basis")]
        [TestCase("value.amount")]
        [TestCase("durationTurns")]
        public void Compile_RejectsContaminatedSoulSchema(string field)
        {
            ArtifactEffectData effect = SoulEffect();
            Contaminate(ref effect, field);

            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                ArtifactEffectCompiler.Compile(Snapshot(effect)));

            StringAssert.Contains($"field={field}", error.Message);
        }

        private static ArtifactLoadoutSnapshot Snapshot(ArtifactEffectData effect)
        {
            return new ArtifactLoadoutSnapshot(3, new[]
            {
                new ArtifactPlacedEffectSnapshot(11, 4, effect),
            });
        }

        internal static ArtifactEffectData SoulEffect()
        {
            return new ArtifactEffectData
            {
                entryId = "soul",
                trigger = ArtifactTriggerType.UnitDied,
                source = new ArtifactSourceData
                {
                    faction = ArtifactFactionFilter.Enemy,
                    category = CategoryKey.None,
                },
                condition = new ArtifactConditionData
                {
                    type = ArtifactConditionType.IsNight,
                    comparison = ArtifactComparisonType.LessThan,
                },
                target = new ArtifactTargetData
                {
                    type = ArtifactTargetType.Collector,
                    faction = ArtifactFactionFilter.Ally,
                    category = CategoryKey.None,
                },
                location = new ArtifactLocationData
                {
                    type = ArtifactLocationType.DeathPosition,
                },
                effect = new ArtifactEffectPayloadData
                {
                    type = ArtifactEffectType.SpawnSoul,
                    soulGrowth = new SoulGrowthAmounts(1, 1, 5),
                },
                value = new ArtifactValueData
                {
                    type = ArtifactValueType.Flat,
                    basis = ArtifactValueBasis.None,
                    amount = 0,
                },
                durationTurns = 5,
            };
        }

        private static void Contaminate(ref ArtifactEffectData effect, string field)
        {
            switch (field)
            {
                case "trigger": effect.trigger = ArtifactTriggerType.AttackEnded; break;
                case "source.faction": effect.source.faction = ArtifactFactionFilter.Any; break;
                case "source.category": effect.source.category = CategoryKey.Rider; break;
                case "condition.type": effect.condition.type = ArtifactConditionType.IsDay; break;
                case "condition.comparison": effect.condition.comparison = ArtifactComparisonType.Equal; break;
                case "target.type": effect.target.type = ArtifactTargetType.LastAttacker; break;
                case "target.faction": effect.target.faction = ArtifactFactionFilter.Enemy; break;
                case "target.category": effect.target.category = CategoryKey.Heavy; break;
                case "target.excludeCategory": effect.target.excludeCategory = true; break;
                case "target.useHpRatioFilter": effect.target.useHpRatioFilter = true; break;
                case "target.maxHpRatio": effect.target.maxHpRatio = 0.5f; break;
                case "location.type": effect.location.type = ArtifactLocationType.None; break;
                case "effect.type": effect.effect.type = ArtifactEffectType.Damage; break;
                case "effect.statId": effect.effect.statId = "Attack"; break;
                case "effect.knockbackDistance": effect.effect.knockbackDistance = 1; break;
                case "value.type": effect.value.type = ArtifactValueType.Percent; break;
                case "value.basis": effect.value.basis = ArtifactValueBasis.MaxHp; break;
                case "value.amount": effect.value.amount = 1; break;
                case "durationTurns": effect.durationTurns = 4; break;
                default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }
        }
    }
}
