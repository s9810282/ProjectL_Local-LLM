using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactThornHeartRuleTests
    {
        [TestCase(0)]
        [TestCase(5)]
        [TestCase(8)]
        public void Compile_AcceptsConfiguredReflectionDamage(int amount)
        {
            ArtifactEffectData effect = HeartEffect();
            effect.value.amount = amount;

            CompiledArtifactLoadout compiled = ArtifactEffectCompiler.Compile(Snapshot(effect));

            Assert.That(compiled.Rules.Length, Is.EqualTo(1));
            var rule = (ThornHeartRule)compiled.Rules.Span[0].Rule;
            Assert.That(rule.ReflectionDamage, Is.EqualTo(amount));
        }

        [TestCase(true, true, 8)]
        [TestCase(false, true, 0)]
        [TestCase(true, false, 0)]
        public void HeartUsesConfiguredAmount(bool night, bool actualDamage, int expected)
        {
            var rule = new ThornHeartRule(8);

            Assert.That(rule.EvaluateDamage(night, actualDamage), Is.EqualTo(expected));
        }

        [TestCase("negative")]
        [TestCase("fraction")]
        [TestCase("nan")]
        [TestCase("infinity")]
        [TestCase("outside-int-range")]
        public void Compile_RejectsInvalidReflectionDamageBeforeConversion(string valueCase)
        {
            ArtifactEffectData effect = HeartEffect();
            switch (valueCase)
            {
                case "negative": effect.value.amount = -1; break;
                case "fraction": effect.value.amount = 2.5f; break;
                case "nan": effect.value.amount = float.NaN; break;
                case "infinity": effect.value.amount = float.PositiveInfinity; break;
                case "outside-int-range": effect.value.amount = 2147483648f; break;
                default: throw new ArgumentOutOfRangeException(nameof(valueCase), valueCase, null);
            }

            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                ArtifactEffectCompiler.Compile(Snapshot(effect)));

            StringAssert.Contains("field=value.amount", error.Message);
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
        [TestCase("effect.soulGrowth")]
        [TestCase("value.type")]
        [TestCase("value.basis")]
        [TestCase("durationTurns")]
        public void Compile_RejectsContaminatedHeartSchema(string field)
        {
            ArtifactEffectData effect = HeartEffect();
            Contaminate(ref effect, field);

            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                ArtifactEffectCompiler.Compile(Snapshot(effect)));

            StringAssert.Contains($"field={field}", error.Message);
        }

        private static ArtifactLoadoutSnapshot Snapshot(ArtifactEffectData effect)
        {
            return new ArtifactLoadoutSnapshot(7, new[]
            {
                new ArtifactPlacedEffectSnapshot(12, 9, effect),
            });
        }

        internal static ArtifactEffectData HeartEffect()
        {
            return new ArtifactEffectData
            {
                entryId = "heart",
                trigger = ArtifactTriggerType.UnitDamaged,
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
                    type = ArtifactTargetType.LastAttacker,
                    faction = ArtifactFactionFilter.Enemy,
                    category = CategoryKey.None,
                },
                location = new ArtifactLocationData { type = ArtifactLocationType.None },
                effect = new ArtifactEffectPayloadData
                {
                    type = ArtifactEffectType.Damage,
                },
                value = new ArtifactValueData
                {
                    type = ArtifactValueType.Flat,
                    basis = ArtifactValueBasis.None,
                    amount = 5,
                },
                durationTurns = 0,
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
                case "target.type": effect.target.type = ArtifactTargetType.AllMatchingUnits; break;
                case "target.faction": effect.target.faction = ArtifactFactionFilter.Ally; break;
                case "target.category": effect.target.category = CategoryKey.Heavy; break;
                case "target.excludeCategory": effect.target.excludeCategory = true; break;
                case "target.useHpRatioFilter": effect.target.useHpRatioFilter = true; break;
                case "target.maxHpRatio": effect.target.maxHpRatio = 0.5f; break;
                case "location.type": effect.location.type = ArtifactLocationType.TargetPosition; break;
                case "effect.type": effect.effect.type = ArtifactEffectType.Heal; break;
                case "effect.statId": effect.effect.statId = "Attack"; break;
                case "effect.knockbackDistance": effect.effect.knockbackDistance = 1; break;
                case "effect.soulGrowth": effect.effect.soulGrowth.attackBonus = 1; break;
                case "value.type": effect.value.type = ArtifactValueType.Percent; break;
                case "value.basis": effect.value.basis = ArtifactValueBasis.MaxHp; break;
                case "durationTurns": effect.durationTurns = 1; break;
                default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }
        }
    }
}
