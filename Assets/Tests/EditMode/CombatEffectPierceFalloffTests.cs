using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class CombatEffectPierceFalloffTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(99, 4)]
        public void RealTrait_TargetBudgetIncludesAuthoredCap(int cap, int expectedBudget)
        {
            var binding = new EffectRuleBinding(TraitEffectOrigin.Create(new EntityId(7), TraitKey.PiercingFalloff),
                new PiercingFalloffEffectRule(new PiercingFalloffTraitDefinition(30, cap), new PiercingFalloffTraitRule()),
                new PiercingFalloffEffectStateSnapshot());
            var result = new CombatEffectEngine().EvaluatePierceFalloff(new[] { binding }, new PierceFalloffEffectContext(10, 0));

            Assert.That(result.HasContribution, Is.True);
            Assert.That(result.RemainingPercent, Is.EqualTo(100));
            Assert.That(result.TargetBudget, Is.EqualTo(expectedBudget));
        }

        [TestCase(0)]
        [TestCase(10)]
        public void EmptyBindings_PreserveFullDamageWithoutTargetBudget(int damage)
        {
            EffectPierceFalloffEvaluation evaluation =
                new CombatEffectEngine().EvaluatePierceFalloff(
                    Array.Empty<EffectRuleBinding>(),
                    new PierceFalloffEffectContext(damage, encounterIndex: 0));

            Assert.That(evaluation.RemainingPercent, Is.EqualTo(100));
            Assert.That(evaluation.HasContribution, Is.False);
            Assert.That(evaluation.TargetBudget, Is.Null);
            Assert.That(evaluation.Contributions, Is.Empty);
            Assert.That(evaluation.Expectations, Is.Empty);
        }

        [Test]
        public void RealTrait_ContributesAtOneHundredPercentAndKeepsExplicitBudget()
        {
            EffectOriginKey origin = TraitEffectOrigin.Create(
                new EntityId(7),
                TraitKey.PiercingFalloff);
            var definition = new PiercingFalloffTraitDefinition(30, int.MaxValue);
            var effectRule = new PiercingFalloffEffectRule(
                definition,
                new PiercingFalloffTraitRule());
            var binding = new EffectRuleBinding(
                origin,
                effectRule,
                new PiercingFalloffEffectStateSnapshot());
            var engine = new CombatEffectEngine();

            EffectPierceFalloffEvaluation damaging = engine.EvaluatePierceFalloff(
                new[] { binding },
                new PierceFalloffEffectContext(10, encounterIndex: 0));
            EffectPierceFalloffEvaluation zeroDamage = engine.EvaluatePierceFalloff(
                new[] { binding },
                new PierceFalloffEffectContext(0, encounterIndex: 0));

            Assert.That(damaging.RemainingPercent, Is.EqualTo(100));
            Assert.That(damaging.HasContribution, Is.True);
            Assert.That(damaging.TargetBudget, Is.EqualTo(4));
            Assert.That(damaging.Expectations.Count, Is.EqualTo(1));
            Assert.That(zeroDamage.HasContribution, Is.True);
            Assert.That(zeroDamage.TargetBudget, Is.Zero);
        }

        [Test]
        public void MultipleRules_FoldPercentageAndBudgetByMinimumInOriginOrder()
        {
            EffectOriginKey later = Origin(EffectOwnerKind.Artifact, 0, 2, "later");
            EffectOriginKey earlier = Origin(EffectOwnerKind.Category, 2, 1, "earlier");
            var engine = new CombatEffectEngine();

            EffectPierceFalloffEvaluation evaluation = engine.EvaluatePierceFalloff(
                new[]
                {
                    Binding(later, new FixedFalloffRule(70, 4)),
                    Binding(earlier, new FixedFalloffRule(50, 2)),
                },
                new PierceFalloffEffectContext(10, encounterIndex: 1));

            Assert.That(evaluation.RemainingPercent, Is.EqualTo(50));
            Assert.That(evaluation.TargetBudget, Is.EqualTo(2));
            Assert.That(evaluation.Contributions[0].Origin, Is.EqualTo(earlier));
            Assert.That(evaluation.Contributions[1].Origin, Is.EqualTo(later));
            Assert.That(evaluation.Expectations.Count, Is.EqualTo(2));
        }

        [Test]
        public void InvalidContextsContributionsAndBindings_AreRejected()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 30, "main");
            EffectOriginKey foreign = Origin(EffectOwnerKind.Artifact, 0, 2, "entry");
            EffectRuleBinding binding = Binding(origin, new FixedFalloffRule(70, 4));
            var engine = new CombatEffectEngine();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PierceFalloffEffectContext(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PierceFalloffEffectContext(1, -1));
            Assert.Throws<ArgumentException>(() =>
                new EffectPierceFalloffContribution(default, 100, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EffectPierceFalloffContribution(origin, 101, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EffectPierceFalloffContribution(origin, 100, -1));
            Assert.Throws<ArgumentException>(() => engine.EvaluatePierceFalloff(
                new[] { binding, binding },
                new PierceFalloffEffectContext(10, 0)));
            Assert.Throws<InvalidOperationException>(() => engine.EvaluatePierceFalloff(
                new[] { Binding(origin, new FixedFalloffRule(70, 4, foreign)) },
                new PierceFalloffEffectContext(10, 0)));
        }

        [Test]
        public void Evaluation_DefensivelyCopiesAndValidatesFoldContracts()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 30, "main");
            var contributions = new[]
            {
                new EffectPierceFalloffContribution(origin, 70, 4),
            };
            var expectations = new[]
            {
                new EffectStateExpectation(origin, StatelessEffectStateSnapshot.Instance),
            };
            var evaluation = new EffectPierceFalloffEvaluation(
                70,
                4,
                contributions,
                expectations);

            contributions[0] = new EffectPierceFalloffContribution(origin, 50, 2);
            expectations[0] = default;

            Assert.That(evaluation.RemainingPercent, Is.EqualTo(70));
            Assert.That(evaluation.TargetBudget, Is.EqualTo(4));
            Assert.That(evaluation.Contributions[0].RemainingPercent, Is.EqualTo(70));
            Assert.That(evaluation.Expectations[0].ExpectedState, Is.Not.Null);
            Assert.Throws<ArgumentException>(() =>
                new EffectPierceFalloffEvaluation(
                    50,
                    4,
                    new[] { new EffectPierceFalloffContribution(origin, 70, 4) },
                    new[]
                    {
                        new EffectStateExpectation(
                            origin,
                            StatelessEffectStateSnapshot.Instance),
                    }));
        }

        [Test]
        public void TraitAdapter_RejectsForeignOriginAndSnapshot()
        {
            var effectRule = new PiercingFalloffEffectRule(
                new PiercingFalloffTraitDefinition(30, int.MaxValue),
                new PiercingFalloffTraitRule());
            EffectOriginKey focusOrigin = TraitEffectOrigin.Create(
                new EntityId(7),
                TraitKey.Focus);
            EffectOriginKey falloffOrigin = TraitEffectOrigin.Create(
                new EntityId(7),
                TraitKey.PiercingFalloff);
            var context = new PierceFalloffEffectContext(10, 0);

            Assert.Throws<ArgumentException>(() =>
                effectRule.EvaluatePierceFalloff(
                    new EffectRuleInput(
                        focusOrigin,
                        new PiercingFalloffEffectStateSnapshot()),
                    context));
            Assert.Throws<ArgumentException>(() =>
                effectRule.EvaluatePierceFalloff(
                    new EffectRuleInput(
                        falloffOrigin,
                        StatelessEffectStateSnapshot.Instance),
                    context));
        }

        private static EffectRuleBinding Binding(
            EffectOriginKey origin,
            ICombatEffectRule rule)
        {
            return new EffectRuleBinding(
                origin,
                rule,
                StatelessEffectStateSnapshot.Instance);
        }

        private static EffectOriginKey Origin(
            EffectOwnerKind kind,
            int ownerInstanceId,
            int definitionKey,
            string ruleKey)
        {
            return new EffectOriginKey(
                kind,
                ownerInstanceId,
                definitionKey,
                ruleKey);
        }

        private sealed class FixedFalloffRule : IEffectPierceFalloffRule
        {
            private readonly int _remainingPercent;
            private readonly int _targetBudget;
            private readonly EffectOriginKey? _originOverride;

            public FixedFalloffRule(
                int remainingPercent,
                int targetBudget,
                EffectOriginKey? originOverride = null)
            {
                _remainingPercent = remainingPercent;
                _targetBudget = targetBudget;
                _originOverride = originOverride;
            }

            public EffectPierceFalloffContribution EvaluatePierceFalloff(
                in EffectRuleInput input,
                in PierceFalloffEffectContext context)
            {
                return new EffectPierceFalloffContribution(
                    _originOverride ?? input.Origin,
                    _remainingPercent,
                    _targetBudget);
            }
        }
    }
}
