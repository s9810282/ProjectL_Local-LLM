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
        public void IncomingDamage_CapsCombinedContributionsAtIncomingDamage()
        {
            var bindings = new[]
            {
                Binding(Origin(EffectOwnerKind.Trait, 1, 1, "a"), new FixedIncomingRule(3)),
                Binding(Origin(EffectOwnerKind.Trait, 2, 1, "b"), new FixedIncomingRule(3)),
                Binding(Origin(EffectOwnerKind.Trait, 3, 1, "c"), new FixedIncomingRule(3))
            };
            var result = new CombatEffectEngine().EvaluateIncomingDamage(bindings, new IncomingDamageEffectContext(6));
            Assert.That(result.EffectiveReduction, Is.EqualTo(6));
            Assert.That(result.FinalDamage, Is.Zero);
            Assert.That(System.Linq.Enumerable.Select(result.AllocatedReductions, a => a.FlatReduction),
                Is.EqualTo(new[] { 3, 3, 0 }));
        }

        private sealed class FixedIncomingRule : IEffectIncomingDamageRule
        {
            private readonly int _reduction;
            internal FixedIncomingRule(int reduction) => _reduction = reduction;
            public EffectIncomingDamageContribution EvaluateIncomingDamage(in EffectRuleInput input,
                in IncomingDamageEffectContext context) => new EffectIncomingDamageContribution(input.Origin, _reduction);
        }

        [Test]
        public void Damage_SumsOnlyMatchingCapabilitiesAndPreservesExpectations()
        {
            EffectOriginKey trait = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            EffectOriginKey category = Origin(EffectOwnerKind.Category, 2, 2, "main");
            EffectOriginKey artifact = Origin(EffectOwnerKind.Artifact, 0, 3, "entry");
            var bindings = new[]
            {
                Binding(artifact, new ThrowingIncomingOnlyRule()),
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
                new MoveSucceededEffectContext(2,true));
            CombatEffectPlan attack = engine.PlanAttackSucceeded(
                bindings,
                new AttackSucceededEffectContext());
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

            Assert.Throws<ArgumentException>(() => engine.PlanAttackSucceeded(
                duplicateBindings,
                default));
            Assert.That(rule.Calls, Is.Zero);
            Assert.That(duplicateBindings[0].Origin, Is.EqualTo(second));
            Assert.That(duplicateBindings[1].Origin, Is.EqualTo(first));

            Assert.Throws<ArgumentNullException>(() => engine.PlanAttackSucceeded(null, default));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.PlanAttackSucceeded(
                new[] { firstBinding }, new AttackSucceededEffectContext((ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Rotation4)255)));
            Assert.That(rule.Calls, Is.Zero);
        }

        [Test]
        public void Evaluation_RejectsArithmeticOverflow()
        {
            var engine = new CombatEffectEngine();
            EffectOriginKey first = Origin(EffectOwnerKind.Trait, 7, 1, "first");
            EffectOriginKey second = Origin(EffectOwnerKind.Category, 2, 2, "second");

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

        private sealed class ThrowingIncomingOnlyRule : IEffectIncomingDamageRule
        {
            public EffectIncomingDamageContribution EvaluateIncomingDamage(
                in EffectRuleInput input,
                in IncomingDamageEffectContext context)
            {
                throw new AssertionException("Damage evaluation invoked the incoming-damage capability.");
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

        private sealed class CountingRule : IEffectAttackSucceededRule
        {
            public int Calls { get; private set; }

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
