using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactEffectCompilerTests
    {
        [Test]
        public void Compile_SupportedChalicesPreserveRevisionAndSortRulesByOrigin()
        {
            ArtifactEffectData effect = SupportedEffect();
            var firstInput = new[]
            {
                new ArtifactPlacedEffectSnapshot(7, 20, effect),
                new ArtifactPlacedEffectSnapshot(7, 10, effect)
            };
            var reversedInput = new[]
            {
                new ArtifactPlacedEffectSnapshot(7, 10, effect),
                new ArtifactPlacedEffectSnapshot(7, 20, effect)
            };

            CompiledArtifactLoadout first = ArtifactEffectCompiler.Compile(
                new ArtifactLoadoutSnapshot(5, firstInput));
            CompiledArtifactLoadout reversed = ArtifactEffectCompiler.Compile(
                new ArtifactLoadoutSnapshot(5, reversedInput));

            Assert.That(first.Revision, Is.EqualTo(5));
            Assert.That(first.Rules.Length, Is.EqualTo(2));
            Assert.That(first.Rules.Span[0].Origin, Is.EqualTo(Origin(10)));
            Assert.That(first.Rules.Span[1].Origin, Is.EqualTo(Origin(20)));
            Assert.That(reversed.Rules.Span[0].Origin, Is.EqualTo(first.Rules.Span[0].Origin));
            Assert.That(reversed.Rules.Span[1].Origin, Is.EqualTo(first.Rules.Span[1].Origin));
            Assert.That(first.Rules.Span[0].Rule, Is.TypeOf<HealingChaliceRule>());
            Assert.That(((HealingChaliceRule)first.Rules.Span[0].Rule).HealAmount, Is.EqualTo(3));

            firstInput[0] = new ArtifactPlacedEffectSnapshot(99, 99, InvalidEffect("trigger"));
            Assert.That(first.Rules.Span[0].Origin, Is.EqualTo(Origin(10)));
            Assert.That(((HealingChaliceRule)first.Rules.Span[0].Rule).HealAmount, Is.EqualTo(3));
        }

        [Test]
        public void Compile_RejectsUnsupportedOrContaminatedAuthoringDataWithContext()
        {
            string[] cases =
            {
                "source.category", "target.category", "target.excludeCategory",
                "target.useHpRatioFilter", "target.maxHpRatio", "effect.statId",
                "effect.knockbackDistance", "condition.comparison", "trigger",
                "target.type", "value.amount.ten", "value.amount.fraction",
                "value.amount.negative", "value.amount.nan", "value.amount.infinity",
                "target.type.undefined", "durationTurns"
            };

            foreach (string fieldCase in cases)
            {
                ArtifactEffectData invalid = InvalidEffect(fieldCase);
                ArgumentException error = Assert.Throws<ArgumentException>(() =>
                    ArtifactEffectCompiler.Compile(Snapshot(invalid)));

                StringAssert.Contains("ArtifactId=7", error.Message, fieldCase);
                StringAssert.Contains("InstanceId=2", error.Message, fieldCase);
                StringAssert.Contains("entryId=heal", error.Message, fieldCase);
                StringAssert.Contains($"field={ExpectedField(fieldCase)}", error.Message, fieldCase);
            }
        }

        [Test]
        public void Compile_RejectsEmptyAndDuplicateOriginsButAllowsDifferentInstances()
        {
            ArtifactEffectData emptyId = SupportedEffect();
            emptyId.entryId = " ";
            ArgumentException emptyError = Assert.Throws<ArgumentException>(() =>
                ArtifactEffectCompiler.Compile(Snapshot(emptyId)));
            StringAssert.Contains("ArtifactId=7", emptyError.Message);
            StringAssert.Contains("InstanceId=2", emptyError.Message);
            StringAssert.Contains("entryId=<empty>", emptyError.Message);
            StringAssert.Contains("field=entryId", emptyError.Message);

            ArtifactEffectData effect = SupportedEffect();
            var duplicate = new ArtifactLoadoutSnapshot(1, new[]
            {
                new ArtifactPlacedEffectSnapshot(7, 2, effect),
                new ArtifactPlacedEffectSnapshot(7, 2, effect)
            });
            ArgumentException duplicateError = Assert.Throws<ArgumentException>(() =>
                ArtifactEffectCompiler.Compile(duplicate));
            StringAssert.Contains("ArtifactId=7", duplicateError.Message);
            StringAssert.Contains("InstanceId=2", duplicateError.Message);
            StringAssert.Contains("entryId=heal", duplicateError.Message);
            StringAssert.Contains("field=entryId", duplicateError.Message);
            StringAssert.Contains("duplicate", duplicateError.Message.ToLowerInvariant());

            var differentInstances = new ArtifactLoadoutSnapshot(1, new[]
            {
                new ArtifactPlacedEffectSnapshot(7, 2, effect),
                new ArtifactPlacedEffectSnapshot(7, 3, effect)
            });
            Assert.That(ArtifactEffectCompiler.Compile(differentInstances).Rules.Length, Is.EqualTo(2));
        }

        [Test]
        public void Compile_PreservesCapturedPlacedEffectsAcrossLaterLayoutProposals()
        {
            var definition = new ArtifactDefinition(
                7, 2, new[] { CellCoord.Zero }, new[] { SupportedEffect() });
            IArtifactEditCalculation calculation = new ArtifactEditCalculator();
            ArtifactState initial = calculation.CreateInitialState(4, 4, new[] { definition });
            ArtifactEditProposal first = calculation.Place(initial, 7, definition, CellCoord.Zero, Rotation4.R0);
            Assert.That(first.Result.Succeeded, Is.True);
            ArtifactItemState placed = first.State.PlacedItems.Span[0];
            // 순수 컴파일러 입력은 테스트가 명시하고 실제 Buffer 효과 소비는 서비스 회귀로 확인한다.
            var captured = new ArtifactLoadoutSnapshot(0, new[] {
                new ArtifactPlacedEffectSnapshot(7, placed.InstanceId, definition.Effects[0]) });
            CompiledArtifactLoadout compiled = ArtifactEffectCompiler.Compile(captured);

            ArtifactEditProposal second = calculation.Place(first.State, 7, definition, new CellCoord(1, 0), Rotation4.R0);
            Assert.That(second.Result.Succeeded, Is.True);
            Assert.That(second.State.PlacedItems.Length, Is.EqualTo(2));
            Assert.That(captured.Effects.Length, Is.EqualTo(1));
            Assert.That(compiled.Rules.Length, Is.EqualTo(1));
        }

        private static ArtifactLoadoutSnapshot Snapshot(ArtifactEffectData effect)
        {
            return new ArtifactLoadoutSnapshot(1, new[]
            {
                new ArtifactPlacedEffectSnapshot(7, 2, effect)
            });
        }

        private static EffectOriginKey Origin(int instanceId)
        {
            return new EffectOriginKey(EffectOwnerKind.Artifact, instanceId, 7, "heal");
        }

        private static ArtifactEffectData SupportedEffect()
        {
            return new ArtifactEffectData
            {
                entryId = "heal",
                trigger = ArtifactTriggerType.StageCleared,
                source = new ArtifactSourceData
                {
                    faction = ArtifactFactionFilter.Any,
                    category = CategoryKey.None
                },
                condition = new ArtifactConditionData
                {
                    type = ArtifactConditionType.None,
                    comparison = ArtifactComparisonType.LessThan
                },
                target = new ArtifactTargetData
                {
                    type = ArtifactTargetType.MainCharacter,
                    faction = ArtifactFactionFilter.Ally,
                    category = CategoryKey.None,
                    excludeCategory = false,
                    useHpRatioFilter = false,
                    maxHpRatio = 0f
                },
                location = new ArtifactLocationData { type = ArtifactLocationType.None },
                effect = new ArtifactEffectPayloadData
                {
                    type = ArtifactEffectType.Heal,
                    statId = null,
                    knockbackDistance = 0
                },
                value = new ArtifactValueData
                {
                    type = ArtifactValueType.Flat,
                    basis = ArtifactValueBasis.None,
                    amount = 3f
                },
                durationTurns = 0
            };
        }

        private static ArtifactEffectData InvalidEffect(string fieldCase)
        {
            ArtifactEffectData effect = SupportedEffect();
            switch (fieldCase)
            {
                case "source.category":
                    effect.source.category = CategoryKey.Rider;
                    break;
                case "target.category":
                    effect.target.category = CategoryKey.Heavy;
                    break;
                case "target.excludeCategory":
                    effect.target.excludeCategory = true;
                    break;
                case "target.useHpRatioFilter":
                    effect.target.useHpRatioFilter = true;
                    break;
                case "target.maxHpRatio":
                    effect.target.maxHpRatio = 0.5f;
                    break;
                case "effect.statId":
                    effect.effect.statId = "Attack";
                    break;
                case "effect.knockbackDistance":
                    effect.effect.knockbackDistance = 1;
                    break;
                case "condition.comparison":
                    effect.condition.comparison = ArtifactComparisonType.Equal;
                    break;
                case "trigger":
                    effect.trigger = ArtifactTriggerType.UnitTurnStarted;
                    break;
                case "target.type":
                    effect.target.type = ArtifactTargetType.AllMatchingUnits;
                    break;
                case "value.amount.ten":
                    effect.value.amount = 10f;
                    break;
                case "value.amount.fraction":
                    effect.value.amount = 2.5f;
                    break;
                case "value.amount.negative":
                    effect.value.amount = -3f;
                    break;
                case "value.amount.nan":
                    effect.value.amount = float.NaN;
                    break;
                case "value.amount.infinity":
                    effect.value.amount = float.PositiveInfinity;
                    break;
                case "target.type.undefined":
                    effect.target.type = (ArtifactTargetType)999;
                    break;
                case "durationTurns":
                    effect.durationTurns = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fieldCase), fieldCase, null);
            }

            return effect;
        }

        private static string ExpectedField(string fieldCase)
        {
            if (fieldCase.StartsWith("value.amount", StringComparison.Ordinal)) return "value.amount";
            if (fieldCase == "target.type.undefined") return "target.type";
            return fieldCase;
        }
    }
}
