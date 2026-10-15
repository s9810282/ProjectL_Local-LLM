using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Tests.Core
{
    public sealed class CombatEffectContractTests
    {
        [Test]
        public void ArrayBackedContracts_CopyTheirInputs()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            var state = new IntStateSnapshot(1);
            var expectation = new EffectStateExpectation(origin, state);
            var costContributions = new[]
            {
                new EffectAttackCostContribution(origin, 2, 1, true),
            };
            var positiveCostContributors = new[] { origin };
            var costExpectations = new[] { expectation };
            var damageContributions = new[]
            {
                new EffectAttackDamageContribution(origin, 3),
            };
            var damageExpectations = new[] { expectation };
            var activations = new[]
            {
                new EffectActivation(origin, 1, 3, new EntityId(11)),
            };

            var cost = new EffectAttackCostEvaluation(
                3, costContributions, positiveCostContributors, costExpectations);
            var damage = new EffectAttackDamageEvaluation(
                8, damageContributions, damageExpectations);
            var result = new EffectRuleResult(null, activations);

            costContributions[0] = default;
            positiveCostContributors[0] = default;
            costExpectations[0] = default;
            damageContributions[0] = default;
            damageExpectations[0] = default;
            activations[0] = default;

            Assert.That(cost.Contributions[0].Origin, Is.EqualTo(origin));
            Assert.That(cost.PositiveCostContributors[0], Is.EqualTo(origin));
            Assert.That(cost.Expectations[0].Origin, Is.EqualTo(origin));
            Assert.That(damage.Contributions[0].Origin, Is.EqualTo(origin));
            Assert.That(damage.Expectations[0].Origin, Is.EqualTo(origin));
            Assert.That(result.Activations[0].Origin, Is.EqualTo(origin));
        }

        [Test]
        public void AttackSucceededContext_CopiesSortsAndRejectsDuplicateOrigins()
        {
            EffectOriginKey first = Origin(EffectOwnerKind.Category, 4, 2, "first");
            EffectOriginKey second = Origin(EffectOwnerKind.Artifact, 0, 3, "second");
            var contributors = new[] { second, first };

            var context = new AttackSucceededEffectContext(contributors);
            contributors[0] = default;

            Assert.That(context.PositiveCostContributors, Is.EqualTo(new[] { first, second }));
            Assert.Throws<ArgumentException>(() =>
                new AttackSucceededEffectContext(new[] { first, first }));
        }

        [Test]
        public void Plan_CopiesInputAndPreservesExpectationsWithoutChanges()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            var expectations = new[]
            {
                new EffectStateExpectation(origin, StatelessEffectStateSnapshot.Instance),
            };

            var plan = new CombatEffectPlan(
                expectations,
                Array.Empty<EffectStateChange>(),
                Array.Empty<EffectActivation>());
            expectations[0] = default;

            Assert.That(plan.Expectations[0].Origin, Is.EqualTo(origin));
            Assert.That(plan.Changes, Is.Empty);
            Assert.That(plan.Activations, Is.Empty);
        }

        [Test]
        public void Plan_SortsOriginsAndPreservesActivationOrderWithinOrigin()
        {
            EffectOriginKey first = Origin(EffectOwnerKind.Category, 4, 2, "first");
            EffectOriginKey second = Origin(EffectOwnerKind.Artifact, 0, 3, "second");
            var firstState = new IntStateSnapshot(1);
            var secondState = new IntStateSnapshot(2);

            var plan = new CombatEffectPlan(
                new[]
                {
                    new EffectStateExpectation(second, secondState),
                    new EffectStateExpectation(first, firstState),
                },
                new[]
                {
                    new EffectStateChange(second, secondState, new IntStateSnapshot(3)),
                    new EffectStateChange(first, firstState, new IntStateSnapshot(4)),
                },
                new[]
                {
                    new EffectActivation(second, 11, 1, new EntityId(11)),
                    new EffectActivation(first, 21, 1, new EntityId(12)),
                    new EffectActivation(second, 12, 1, new EntityId(13)),
                });

            Assert.That(plan.Expectations[0].Origin, Is.EqualTo(first));
            Assert.That(plan.Changes[0].Origin, Is.EqualTo(first));
            Assert.That(plan.Activations[0].Origin, Is.EqualTo(first));
            Assert.That(plan.Activations[1].Code, Is.EqualTo(11));
            Assert.That(plan.Activations[2].Code, Is.EqualTo(12));
        }

        [Test]
        public void Plan_RejectsDuplicateExpectationAndChangeOrigins()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            var state = new IntStateSnapshot(1);
            var expectation = new EffectStateExpectation(origin, state);
            var change = new EffectStateChange(origin, state, new IntStateSnapshot(2));

            Assert.Throws<ArgumentException>(() => new CombatEffectPlan(
                new[] { expectation, expectation },
                Array.Empty<EffectStateChange>(),
                Array.Empty<EffectActivation>()));
            Assert.Throws<ArgumentException>(() => new CombatEffectPlan(
                new[] { expectation },
                new[] { change, change },
                Array.Empty<EffectActivation>()));
        }

        [Test]
        public void Plan_RejectsChangesAndActivationsWithoutExpectation()
        {
            EffectOriginKey registered = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            EffectOriginKey unregistered = Origin(EffectOwnerKind.Artifact, 0, 2, "entry");
            var state = new IntStateSnapshot(1);
            var expectation = new EffectStateExpectation(registered, state);

            Assert.Throws<ArgumentException>(() => new CombatEffectPlan(
                new[] { expectation },
                new[]
                {
                    new EffectStateChange(unregistered, state, new IntStateSnapshot(2)),
                },
                Array.Empty<EffectActivation>()));
            Assert.Throws<ArgumentException>(() => new CombatEffectPlan(
                new[] { expectation },
                Array.Empty<EffectStateChange>(),
                new[]
                {
                    new EffectActivation(unregistered, 1, 1, new EntityId(11)),
                }));
        }

        [Test]
        public void Plan_RejectsIncompatibleStateTransitions()
        {
            EffectOriginKey origin = Origin(EffectOwnerKind.Trait, 7, 1, "main");
            var expected = new IntStateSnapshot(1);
            var expectation = new EffectStateExpectation(origin, expected);

            Assert.Throws<ArgumentException>(() => new CombatEffectPlan(
                new[] { expectation },
                new[]
                {
                    new EffectStateChange(
                        origin,
                        new IntStateSnapshot(2),
                        new IntStateSnapshot(3)),
                },
                Array.Empty<EffectActivation>()));
            Assert.Throws<ArgumentException>(() => new CombatEffectPlan(
                new[] { expectation },
                new[]
                {
                    new EffectStateChange(origin, expected, new OtherStateSnapshot(1)),
                },
                Array.Empty<EffectActivation>()));
        }

        private static EffectOriginKey Origin(
            EffectOwnerKind kind,
            int ownerInstanceId,
            int definitionKey,
            string ruleKey)
        {
            return new EffectOriginKey(kind, ownerInstanceId, definitionKey, ruleKey);
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
