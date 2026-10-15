using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactLastStandRuleTests
    {
        [Test]
        public void CompiledRules_ContributeLiteralBonusesFromUnitCountDifference()
        {
            var cases = new[]
            {
                (Ally: 2, Enemy: 4, Copies: 1, ExpectedBonus: 10),
                (Ally: 2, Enemy: 4, Copies: 2, ExpectedBonus: 20),
                (Ally: 3, Enemy: 3, Copies: 2, ExpectedBonus: 0),
                (Ally: 4, Enemy: 2, Copies: 1, ExpectedBonus: 0),
            };

            foreach (var value in cases)
            {
                ArtifactPlacedEffectSnapshot[] effects = Enumerable.Range(1, value.Copies)
                    .Select(instanceId => new ArtifactPlacedEffectSnapshot(
                        8, instanceId, LastStandEffect()))
                    .ToArray();
                CompiledArtifactLoadout loadout = ArtifactEffectCompiler.Compile(
                    new ArtifactLoadoutSnapshot(3, effects));

                EffectAttackDamageEvaluation result = new CombatEffectEngine().EvaluateAttackDamage(
                    loadout.CaptureBindings(),
                    new AttackDamageEffectContext(7, value.Ally, value.Enemy));

                Assert.That(result.Contributions.Count, Is.EqualTo(value.Copies));
                Assert.That(result.Contributions.Sum(contribution => contribution.FlatBonus),
                    Is.EqualTo(value.ExpectedBonus), value.ToString());
                Assert.That(result.EffectiveDamage, Is.EqualTo(7 + value.ExpectedBonus), value.ToString());
            }
        }

        [Test]
        public void Compile_AcceptsChaliceAndLastStandButRejectsContaminatedMixedLoadout()
        {
            CompiledArtifactLoadout compiled = ArtifactEffectCompiler.Compile(
                new ArtifactLoadoutSnapshot(4, new[]
                {
                    new ArtifactPlacedEffectSnapshot(7, 1, HealingEffect()),
                    new ArtifactPlacedEffectSnapshot(8, 2, LastStandEffect()),
                }));

            Assert.That(compiled.Rules.Span[0].Rule, Is.TypeOf<HealingChaliceRule>());
            Assert.That(compiled.Rules.Span[1].Rule, Is.TypeOf<LastStandRule>());

            ArtifactEffectData unsupported = LastStandEffect();
            unsupported.effect.statId = "Defense";
            ArgumentException error = Assert.Throws<ArgumentException>(() =>
                ArtifactEffectCompiler.Compile(new ArtifactLoadoutSnapshot(4, new[]
                {
                    new ArtifactPlacedEffectSnapshot(7, 1, HealingEffect()),
                    new ArtifactPlacedEffectSnapshot(8, 2, unsupported),
                })));
            StringAssert.Contains("field=effect.statId", error.Message);
        }

        internal static ArtifactEffectData LastStandEffect()
        {
            return new ArtifactEffectData
            {
                entryId = "last-stand",
                trigger = ArtifactTriggerType.StageActive,
                source = new ArtifactSourceData
                {
                    faction = ArtifactFactionFilter.Any,
                    category = CategoryKey.None,
                },
                condition = new ArtifactConditionData
                {
                    type = ArtifactConditionType.CompareUnitCounts,
                    comparison = ArtifactComparisonType.LessThan,
                },
                target = new ArtifactTargetData
                {
                    type = ArtifactTargetType.AllMatchingUnits,
                    faction = ArtifactFactionFilter.Ally,
                    category = CategoryKey.None,
                },
                location = new ArtifactLocationData { type = ArtifactLocationType.None },
                effect = new ArtifactEffectPayloadData
                {
                    type = ArtifactEffectType.ModifyStat,
                    statId = "Attack",
                },
                value = new ArtifactValueData
                {
                    type = ArtifactValueType.UnitCountDifference,
                    basis = ArtifactValueBasis.None,
                    amount = 5,
                },
                durationTurns = 0,
            };
        }

        private static ArtifactEffectData HealingEffect()
        {
            return new ArtifactEffectData
            {
                entryId = "heal",
                trigger = ArtifactTriggerType.StageCleared,
                source = new ArtifactSourceData
                {
                    faction = ArtifactFactionFilter.Any,
                    category = CategoryKey.None,
                },
                condition = new ArtifactConditionData
                {
                    type = ArtifactConditionType.None,
                    comparison = ArtifactComparisonType.LessThan,
                },
                target = new ArtifactTargetData
                {
                    type = ArtifactTargetType.MainCharacter,
                    faction = ArtifactFactionFilter.Ally,
                    category = CategoryKey.None,
                },
                location = new ArtifactLocationData { type = ArtifactLocationType.None },
                effect = new ArtifactEffectPayloadData { type = ArtifactEffectType.Heal },
                value = new ArtifactValueData
                {
                    type = ArtifactValueType.Flat,
                    basis = ArtifactValueBasis.None,
                    amount = 3,
                },
                durationTurns = 0,
            };
        }
    }
}
