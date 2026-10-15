using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class PiercingFalloffTraitTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(99, 4)]
        public void MigratedCap_PreservesZeroOneAndMultipleTargets(int cap, int expectedBudget)
        {
            var spec = new PiercingFalloffTraitSpec(30, cap);
            var definition = (PiercingFalloffTraitDefinition)new PiercingFalloffTraitDefinitionFactory().BuildDefinition(spec);

            Assert.That(spec.MaxTargetsPerRay, Is.EqualTo(cap));
            Assert.That(definition.MaxTargetsPerRay, Is.EqualTo(cap));
            Assert.That(new PiercingFalloffTraitRule().CalculateTargetBudget(definition, 10), Is.EqualTo(expectedBudget));
        }

        [Test]
        public void StaticContracts_RejectNegativeTargetCap()
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PiercingFalloffTraitSpec(30, -1)).ParamName, Is.EqualTo("maxTargetsPerRay"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PiercingFalloffTraitDefinition(30, -1)).ParamName, Is.EqualTo("maxTargetsPerRay"));
        }

        [TestCase(1)]
        [TestCase(100)]
        public void StaticContracts_AcceptBoundaryReduction(int reduction)
        {
            var spec = new PiercingFalloffTraitSpec(reduction, 99);
            var definition = new PiercingFalloffTraitDefinition(reduction, int.MaxValue);
            var status = new PiercingFalloffTraitStatusSnapshot(reduction);

            Assert.That(spec.TraitKey, Is.EqualTo(TraitKey.PiercingFalloff));
            Assert.That(spec.ReductionPercentPerTarget, Is.EqualTo(reduction));
            Assert.That(definition.TraitKey, Is.EqualTo(TraitKey.PiercingFalloff));
            Assert.That(definition.ReductionPercentPerTarget, Is.EqualTo(reduction));
            Assert.That(status.Key, Is.EqualTo(TraitKey.PiercingFalloff));
            Assert.That(status.ReductionPercentPerTarget, Is.EqualTo(reduction));
        }

        [TestCase(0)]
        [TestCase(101)]
        public void StaticContracts_RejectOutOfRangeReduction(int reduction)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PiercingFalloffTraitSpec(reduction, 99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PiercingFalloffTraitDefinition(reduction, int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PiercingFalloffTraitStatusSnapshot(reduction));
        }

        [Test]
        public void AttackProfile_RejectsPierceWithIncompatibleTargeting_AtConstruction()
        {
            // 결합 조건을 공격 계산 때가 아니라 프로필 생성(전투 준비 경계)에서 거절한다.
            var pierce = CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 21, CategoryKey.Cannon, new PiercingFalloffTraitSpec(30, 99));

            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(pierce,
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)));
            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(pierce,
                new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.All, TargetingSplash.None, 0)));
            Assert.DoesNotThrow(() => new CombatAttackProfile(pierce,
                new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0)));
            // 관통 특성이 없으면 지정 방식 제한이 없다.
            Assert.DoesNotThrow(() => new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 21),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)));
        }

        [Test]
        public void ReductionThirty_UsesLinearHalfUpDamageAndExpectedBudgets()
        {
            var definition = new PiercingFalloffTraitDefinition(30, int.MaxValue);
            var rule = new PiercingFalloffTraitRule();

            Assert.That(ScaleSequence(rule, definition, 10, 4), Is.EqualTo(new[] { 10, 7, 4, 1 }));
            Assert.That(rule.CalculateTargetBudget(definition, 10), Is.EqualTo(4));
            Assert.That(ScaleSequence(rule, definition, 5, 4), Is.EqualTo(new[] { 5, 4, 2, 1 }));
            Assert.That(rule.CalculateTargetBudget(definition, 5), Is.EqualTo(4));
            Assert.That(ScaleSequence(rule, definition, 1, 2), Is.EqualTo(new[] { 1, 1 }));
            Assert.That(rule.CalculateTargetBudget(definition, 1), Is.EqualTo(2));
            Assert.That(rule.CalculateTargetBudget(definition, 0), Is.Zero);
        }

        [Test]
        public void ReductionFifty_StopsAfterTwoDamageEncounters()
        {
            var definition = new PiercingFalloffTraitDefinition(50, int.MaxValue);
            var rule = new PiercingFalloffTraitRule();

            Assert.That(ScaleSequence(rule, definition, 10, 2), Is.EqualTo(new[] { 10, 5 }));
            Assert.That(rule.CalculateTargetBudget(definition, 10), Is.EqualTo(2));
        }

        [Test]
        public void Rule_RejectsNegativeInputsAndHandlesLargeEncounterIndex()
        {
            var definition = new PiercingFalloffTraitDefinition(30, int.MaxValue);
            var rule = new PiercingFalloffTraitRule();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                rule.EvaluateRemainingPercent(definition, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                rule.CalculateTargetBudget(definition, -1));
            Assert.That(
                rule.EvaluateRemainingPercent(definition, int.MaxValue),
                Is.Zero);
        }

        [Test]
        public void RuntimeAndEffectSnapshots_RemainStatelessAcrossEvaluations()
        {
            var state = new PiercingFalloffRuntimeState();
            var clone = (PiercingFalloffRuntimeState)state.Clone();
            var definition = new PiercingFalloffTraitDefinition(30, int.MaxValue);
            var rule = new PiercingFalloffTraitRule();
            var firstSnapshot = new PiercingFalloffEffectStateSnapshot();
            var secondSnapshot = new PiercingFalloffEffectStateSnapshot();

            int first = rule.EvaluateRemainingPercent(definition, 2);
            int second = rule.EvaluateRemainingPercent(definition, 2);
            state.CopyFinalValuesFrom(clone);

            Assert.That((int)TraitKey.PiercingFalloff, Is.EqualTo(30));
            Assert.That(state.TraitKey, Is.EqualTo(TraitKey.PiercingFalloff));
            Assert.That(clone, Is.Not.SameAs(state));
            Assert.That(clone.TraitKey, Is.EqualTo(TraitKey.PiercingFalloff));
            Assert.That(first, Is.EqualTo(40));
            Assert.That(second, Is.EqualTo(40));
            Assert.That(firstSnapshot.ValueEquals(secondSnapshot), Is.True);
            Assert.That(
                firstSnapshot.ValueEquals(StatelessEffectStateSnapshot.Instance),
                Is.False);
            Assert.Throws<ArgumentException>(() =>
                state.CopyFinalValuesFrom(new FocusRuntimeState()));
        }

        [Test]
        public void DefaultRegistry_BuildsCatalogRuntimeCodecStatusAndOwnerNeutralEvaluation()
        {
            TraitRegistry registry = TraitRegistry.CreateDefault();
            UnitSpec spec = CreateSpec(new PiercingFalloffTraitSpec(30, 99));
            var catalog = new UnitCatalog(new[] { spec }, registry);
            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_Akimbo);
            TraitRegistration entry = registry.GetRequiredRegistration(TraitKey.PiercingFalloff);
            var stateSet = new UnitTraitStateSet(new[]
            {
                entry.TraitRuntimeStateFactory.BuildRuntimeState(definition.TraitDefinitions.Span[0]),
            });
            var provider = new TraitEffectProvider(registry);
            TraitRuntimeState state = stateSet.GetRequired<PiercingFalloffRuntimeState>(
                TraitKey.PiercingFalloff);
            IEffectStateSnapshot captured = TraitEffectStateCodec.Capture(state);
            TraitRuntimeState restored = TraitEffectStateCodec.Restore(captured);
            TraitStatusSnapshot[] statuses = provider.CreateStatuses(definition, stateSet);
            var binding = new EffectRuleBinding(
                new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.PiercingFalloff, "main"),
                entry.EffectRuleFactory(definition.TraitDefinitions.Span[0]), captured);
            EffectPierceFalloffEvaluation evaluation = new CombatEffectEngine().EvaluatePierceFalloff(
                new[] { binding }, new PierceFalloffEffectContext(10, 1));

            Assert.That(definition.TraitDefinitions.Length, Is.EqualTo(1));
            Assert.That(
                ((PiercingFalloffTraitDefinition)definition.TraitDefinitions.Span[0])
                    .ReductionPercentPerTarget,
                Is.EqualTo(30));
            Assert.That(restored, Is.TypeOf<PiercingFalloffRuntimeState>());
            Assert.That(restored, Is.Not.SameAs(state));
            Assert.That(statuses.Length, Is.EqualTo(1));
            Assert.That(
                ((PiercingFalloffTraitStatusSnapshot)statuses[0])
                    .ReductionPercentPerTarget,
                Is.EqualTo(30));
            Assert.That(evaluation.RemainingPercent, Is.EqualTo(70));
            Assert.That(evaluation.HasContribution, Is.True);
            Assert.That(evaluation.TargetBudget, Is.EqualTo(4));
            Assert.That(evaluation.Expectations.Count, Is.EqualTo(1));
        }

        private static UnitSpec CreateSpec(TraitSpec trait)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                8,
                2,
                30,
                35,
                new[] { trait });
        }

        private static int[] ScaleSequence(
            PiercingFalloffTraitRule rule,
            PiercingFalloffTraitDefinition definition,
            int damage,
            int count)
        {
            var result = new int[count];
            for (int encounterIndex = 0; encounterIndex < count; encounterIndex++)
            {
                result[encounterIndex] = CategoryMatchupDamageCalculator.Apply(
                    damage,
                    rule.EvaluateRemainingPercent(definition, encounterIndex),
                    100);
            }

            return result;
        }
    }
}
