using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Tests.Core
{
    public sealed class CombatEffectEngineTests
    {
        [Test]
        public void Cost_IgnoresZeroContributionMinimumAndSortsOrigins()
        {
            EffectOriginKey first = Origin(EffectOwnerKind.Artifact, 0, 10, "a");
            EffectOriginKey second = Origin(EffectOwnerKind.Category, 9, 20, "b");
            var bindings = new[]
            {
                Binding(first, new FixedCostRule(1, 1, true)),
                Binding(second, new FixedCostRule(0, 5, true)),
            };
            var engine = new CombatEffectEngine();

            EffectAttackCostEvaluation forward = engine.EvaluateAttackCost(
                bindings,
                new AttackCostEffectContext(5));
            Array.Reverse(bindings);
            EffectAttackCostEvaluation reversed = engine.EvaluateAttackCost(
                bindings,
                new AttackCostEffectContext(5));

            Assert.That(forward.EffectiveCost, Is.EqualTo(4));
            Assert.That(reversed.EffectiveCost, Is.EqualTo(4));
            Assert.That(forward.Contributions[0].Origin, Is.EqualTo(second));
            Assert.That(reversed.Contributions[0].Origin, Is.EqualTo(second));
            Assert.That(forward.PositiveCostContributors, Is.EqualTo(new[] { first }));
            Assert.That(forward.Expectations.Count, Is.EqualTo(2));
        }

        [Test]
        public void Damage_SumsOnlyMatchingCapabilitiesAndPreservesExpectations()
        {
            EffectOriginKey trait = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            EffectOriginKey category = Origin(EffectOwnerKind.Category, 2, 2, "main");
            EffectOriginKey artifact = Origin(EffectOwnerKind.Artifact, 0, 3, "entry");
            var bindings = new[]
            {
                Binding(artifact, new ThrowingCostOnlyRule()),
                Binding(trait, new FixedDamageRule(2)),
                Binding(category, new FixedDamageRule(3)),
            };

            EffectAttackDamageEvaluation evaluation = new CombatEffectEngine().EvaluateAttackDamage(
                bindings,
                new AttackDamageEffectContext(4));

            Assert.That(evaluation.EffectiveDamage, Is.EqualTo(9));
            Assert.That(evaluation.Contributions.Count, Is.EqualTo(2));
            Assert.That(evaluation.Contributions[0].Origin, Is.EqualTo(trait));
            Assert.That(evaluation.Contributions[1].Origin, Is.EqualTo(category));
            Assert.That(evaluation.Expectations.Count, Is.EqualTo(2));
        }

        [Test]
        public void TransitionMethods_InvokeTheirCapabilityAndPreserveReadExpectations()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Artifact, 0, 3, "entry");
            var rule = new TransitionRouteRule();
            var bindings = new[] { Binding(origin, rule) };
            var engine = new CombatEffectEngine();

            CombatEffectPlan move = engine.PlanMoveSucceeded(
                bindings,
                new MoveSucceededEffectContext(2, true, 1));
            CombatEffectPlan attack = engine.PlanAttackSucceeded(
                bindings,
                new AttackSucceededEffectContext(Array.Empty<EffectOriginKey>()));
            CombatEffectPlan turnStarted = engine.PlanTurnStarted(bindings);
            CombatEffectPlan turnEnded = engine.PlanTurnEnded(bindings);

            Assert.That(move.Activations[0].Code, Is.EqualTo(1));
            Assert.That(attack.Activations[0].Code, Is.EqualTo(2));
            Assert.That(turnStarted.Activations[0].Code, Is.EqualTo(3));
            Assert.That(turnEnded.Activations[0].Code, Is.EqualTo(4));
            Assert.That(move.Expectations.Count, Is.EqualTo(1));
            Assert.That(attack.Expectations.Count, Is.EqualTo(1));
            Assert.That(turnStarted.Expectations.Count, Is.EqualTo(1));
            Assert.That(turnEnded.Expectations.Count, Is.EqualTo(1));
            Assert.That(move.Changes, Is.Empty);
        }

        [Test]
        public void Planning_RejectsInvalidStateChangesWithoutMutatingInputs()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            EffectOriginKey foreign = Origin(EffectOwnerKind.Artifact, 0, 2, "entry");
            var expected = new IntStateSnapshot(1);
            var engine = new CombatEffectEngine();

            AssertInvalidTurnStartedResult(engine, origin, expected, new EffectStateChange(
                foreign,
                expected,
                new IntStateSnapshot(2)));
            AssertInvalidTurnStartedResult(engine, origin, expected, new EffectStateChange(
                origin,
                new IntStateSnapshot(9),
                new IntStateSnapshot(2)));
            AssertInvalidTurnStartedResult(engine, origin, expected, new EffectStateChange(
                origin,
                expected,
                new OtherStateSnapshot(2)));

            Assert.That(expected.Value, Is.EqualTo(1));
        }

        [Test]
        public void Planning_RejectsNullResultsAndForeignActivations()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            EffectOriginKey foreign = Origin(EffectOwnerKind.Artifact, 0, 2, "entry");
            var engine = new CombatEffectEngine();

            Assert.Throws<InvalidOperationException>(() => engine.PlanTurnEnded(
                new[] { Binding(origin, new FixedResultRule(null)) }));
            Assert.Throws<InvalidOperationException>(() => engine.PlanTurnEnded(
                new[]
                {
                    Binding(origin, new FixedResultRule(new EffectRuleResult(
                        null,
                        new[]
                        {
                            new EffectActivation(foreign, 1, 1, new EntityId(11)),
                        }))),
                }));
        }

        [Test]
        public void InvalidInputs_AreRejectedBeforeAnyRuleRunsAndWithoutReorderingBindings()
        {
            EffectOriginKey first = Origin(EffectOwnerKind.Category, 2, 1, "first");
            EffectOriginKey second = Origin(EffectOwnerKind.Artifact, 0, 2, "second");
            var rule = new CountingRule();
            EffectRuleBinding firstBinding = Binding(first, rule);
            EffectRuleBinding secondBinding = Binding(second, rule);
            var duplicateBindings = new[] { secondBinding, firstBinding, firstBinding };
            var engine = new CombatEffectEngine();

            Assert.Throws<ArgumentException>(() => engine.EvaluateAttackCost(
                duplicateBindings,
                new AttackCostEffectContext(5)));
            Assert.That(rule.Calls, Is.Zero);
            Assert.That(duplicateBindings[0].Origin, Is.EqualTo(second));
            Assert.That(duplicateBindings[1].Origin, Is.EqualTo(first));

            Assert.Throws<ArgumentNullException>(() => engine.EvaluateAttackCost(null, default));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.EvaluateAttackCost(
                new[] { firstBinding },
                default));
            Assert.Throws<ArgumentException>(() => engine.PlanAttackSucceeded(
                new[] { firstBinding },
                default));
            Assert.That(rule.Calls, Is.Zero);
        }

        [Test]
        public void Evaluation_RejectsArithmeticOverflow()
        {
            var engine = new CombatEffectEngine();
            EffectOriginKey first = Origin(EffectOwnerKind.Trait, 7, 1, "first");
            EffectOriginKey second = Origin(EffectOwnerKind.Category, 2, 2, "second");

            Assert.Throws<OverflowException>(() => engine.EvaluateAttackCost(
                new[]
                {
                    Binding(first, new FixedCostRule(int.MaxValue, 1, false)),
                    Binding(second, new FixedCostRule(1, 1, false)),
                },
                new AttackCostEffectContext(5)));
            Assert.Throws<OverflowException>(() => engine.EvaluateAttackDamage(
                new[]
                {
                    Binding(first, new FixedDamageRule(int.MaxValue)),
                    Binding(second, new FixedDamageRule(1)),
                },
                new AttackDamageEffectContext(5)));
        }

        [Test]
        public void Evaluation_RejectsContributionsFromAnotherOrigin()
        {
            EffectOriginKey bound = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            EffectOriginKey foreign = Origin(EffectOwnerKind.Artifact, 0, 2, "entry");
            var engine = new CombatEffectEngine();

            Assert.Throws<InvalidOperationException>(() => engine.EvaluateAttackCost(
                new[] { Binding(bound, new FixedCostRule(1, 1, true, foreign)) },
                new AttackCostEffectContext(5)));
            Assert.Throws<InvalidOperationException>(() => engine.EvaluateAttackDamage(
                new[] { Binding(bound, new FixedDamageRule(1, foreign)) },
                new AttackDamageEffectContext(5)));
        }

        private static void AssertInvalidTurnStartedResult(
            CombatEffectEngine engine,
            EffectOriginKey origin,
            IntStateSnapshot expected,
            EffectStateChange change)
        {
            var bindings = new[]
            {
                new EffectRuleBinding(
                    origin,
                    new TurnStartedResultRule(new EffectRuleResult(
                        change,
                        Array.Empty<EffectActivation>())),
                    expected),
            };

            Assert.Throws<InvalidOperationException>(() => engine.PlanTurnStarted(bindings));
            Assert.That(bindings[0].Origin, Is.EqualTo(origin));
            Assert.That(bindings[0].ExpectedState, Is.SameAs(expected));
        }

        private static EffectRuleBinding Binding(EffectOriginKey origin, ICombatEffectRule rule)
        {
            return new EffectRuleBinding(origin, rule, StatelessEffectStateSnapshot.Instance);
        }

        private static EffectOriginKey Origin(
            EffectOwnerKind kind,
            int ownerInstanceId,
            int definitionKey,
            string ruleKey)
        {
            return new EffectOriginKey(kind, ownerInstanceId, definitionKey, ruleKey);
        }

        private sealed class FixedCostRule : IEffectAttackCostRule
        {
            private readonly int _reduction;
            private readonly int _minimum;
            private readonly bool _consume;
            private readonly EffectOriginKey? _originOverride;

            public FixedCostRule(
                int reduction,
                int minimum,
                bool consume,
                EffectOriginKey? originOverride = null)
            {
                _reduction = reduction;
                _minimum = minimum;
                _consume = consume;
                _originOverride = originOverride;
            }

            public EffectAttackCostContribution EvaluateAttackCost(
                in EffectRuleInput input,
                in AttackCostEffectContext context)
            {
                return new EffectAttackCostContribution(
                    _originOverride ?? input.Origin,
                    _reduction,
                    _minimum,
                    _consume);
            }
        }

        private sealed class FixedDamageRule : IEffectAttackDamageRule
        {
            private readonly int _bonus;
            private readonly EffectOriginKey? _originOverride;

            public FixedDamageRule(int bonus, EffectOriginKey? originOverride = null)
            {
                _bonus = bonus;
                _originOverride = originOverride;
            }

            public EffectAttackDamageContribution EvaluateAttackDamage(
                in EffectRuleInput input,
                in AttackDamageEffectContext context)
            {
                return new EffectAttackDamageContribution(
                    _originOverride ?? input.Origin,
                    _bonus);
            }
        }

        private sealed class ThrowingCostOnlyRule : IEffectAttackCostRule
        {
            public EffectAttackCostContribution EvaluateAttackCost(
                in EffectRuleInput input,
                in AttackCostEffectContext context)
            {
                throw new AssertionException("Damage evaluation invoked the cost capability.");
            }
        }

        private sealed class TransitionRouteRule :
            IEffectMoveSucceededRule,
            IEffectAttackSucceededRule,
            IEffectTurnStartedRule,
            IEffectTurnEndedRule
        {
            public EffectRuleResult PlanMoveSucceeded(
                in EffectRuleInput input,
                in MoveSucceededEffectContext context)
            {
                return Result(input.Origin, 1);
            }

            public EffectRuleResult PlanAttackSucceeded(
                in EffectRuleInput input,
                in AttackSucceededEffectContext context)
            {
                return Result(input.Origin, 2);
            }

            public EffectRuleResult PlanTurnStarted(in EffectRuleInput input)
            {
                return Result(input.Origin, 3);
            }

            public EffectRuleResult PlanTurnEnded(in EffectRuleInput input)
            {
                return Result(input.Origin, 4);
            }

            private static EffectRuleResult Result(EffectOriginKey origin, int code)
            {
                return new EffectRuleResult(
                    null,
                    new[]
                    {
                        new EffectActivation(origin, code, 1, new EntityId(11)),
                    });
            }
        }

        private sealed class TurnStartedResultRule : IEffectTurnStartedRule
        {
            private readonly EffectRuleResult _result;

            public TurnStartedResultRule(EffectRuleResult result)
            {
                _result = result;
            }

            public EffectRuleResult PlanTurnStarted(in EffectRuleInput input)
            {
                return _result;
            }
        }

        private sealed class FixedResultRule : IEffectTurnEndedRule
        {
            private readonly EffectRuleResult _result;

            public FixedResultRule(EffectRuleResult result)
            {
                _result = result;
            }

            public EffectRuleResult PlanTurnEnded(in EffectRuleInput input)
            {
                return _result;
            }
        }

        private sealed class CountingRule : IEffectAttackCostRule, IEffectAttackSucceededRule
        {
            public int Calls { get; private set; }

            public EffectAttackCostContribution EvaluateAttackCost(
                in EffectRuleInput input,
                in AttackCostEffectContext context)
            {
                Calls++;
                return new EffectAttackCostContribution(input.Origin, 1, 1, false);
            }

            public EffectRuleResult PlanAttackSucceeded(
                in EffectRuleInput input,
                in AttackSucceededEffectContext context)
            {
                Calls++;
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());
            }
        }

        private sealed class IntStateSnapshot : IEffectStateSnapshot
        {
            public int Value { get; }

            public IntStateSnapshot(int value)
            {
                Value = value;
            }

            public bool ValueEquals(IEffectStateSnapshot other)
            {
                return other is IntStateSnapshot state && state.Value == Value;
            }
        }

        private sealed class OtherStateSnapshot : IEffectStateSnapshot
        {
            public int Value { get; }

            public OtherStateSnapshot(int value)
            {
                Value = value;
            }

            public bool ValueEquals(IEffectStateSnapshot other)
            {
                return other is OtherStateSnapshot state && state.Value == Value;
            }
        }
    }
}
