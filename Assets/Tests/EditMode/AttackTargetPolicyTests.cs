using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Tests.Core
{
    public sealed class AttackTargetPolicyTests
    {
        [Test]
        public void NoContribution_UsesRestrictiveBaseline()
        {
            var result = new CombatEffectEngine().EvaluateAttackTargeting(Array.Empty<EffectRuleBinding>());
            AssertPolicy(result.Policy, false, false, false, false);
            Assert.That(result.Contributions, Is.Empty);
            Assert.That(result.Expectations, Is.Empty);
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void CategoryBindings_GrantOnlyCannonCapabilitiesWithoutChangingState(CategoryKey category)
        {
            var input = CombatCommandTestData.Input();
            var snapshot = category == CategoryKey.Rider ? CategorySnapshot.CreateRider(true)
                : category == CategoryKey.Heavy ? CategorySnapshot.CreateHeavy(Rotation4.R90, true, 40)
                : CategorySnapshot.CreateStateless(category);
            var actor = CombatCommandTestData.With(input.Characters[0], category: snapshot);
            var profile = new CombatAttackProfile(
                CombatCommandTestData.Spec(actor.CharacterKey, 20, category), input.Profiles[0].Method);
            var bindings = CombatCommandEffects.Capture(actor, profile);
            var states = bindings.Select(b => b.ExpectedState).ToArray();
            var engine = new CombatEffectEngine();

            var result = engine.EvaluateAttackTargeting(bindings);
            bool cannon = category == CategoryKey.Cannon;
            AssertPolicy(result.Policy, cannon, cannon, cannon, cannon);
            Assert.That(result.Contributions.Count, Is.EqualTo(cannon ? 1 : 0));
            Assert.That(result.Expectations.Count, Is.EqualTo(result.Contributions.Count));
            if (cannon)
                Assert.That(result.Contributions[0].Origin, Is.EqualTo(CategoryEffectOrigin.Create(
                    actor.EntityId, category, CategoryRuleKey.SafeBombardment)));
            Assert.That(bindings.Select(b => b.ExpectedState), Is.EqualTo(states));
            Assert.That(actor.Stats.Category, Is.SameAs(snapshot));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(snapshot.RefundUsedThisTurn, Is.EqualTo(category == CategoryKey.Rider ? (bool?)true : null));
            Assert.That(snapshot.DefenseFacing, Is.EqualTo(category == CategoryKey.Heavy ? (Rotation4?)Rotation4.R90 : null));
            Assert.That(snapshot.MovedThisTurn, Is.EqualTo(category == CategoryKey.Heavy ? (bool?)true : null));
            Assert.That(snapshot.DamageReductionPercent, Is.EqualTo(category == CategoryKey.Heavy ? (int?)40 : null));
            if (cannon)
            {
                var plan = engine.PlanAttackSucceeded(bindings, new AttackSucceededEffectContext(Array.Empty<EffectOriginKey>()));
                Assert.That(plan.Changes, Is.Empty);
                Assert.That(plan.Activations, Is.Empty);
            }
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        public void IndependentCapabilities_AreNotCoupled(bool empty, bool friendly, bool neutral, bool near)
        {
            var policy = new AttackTargetingPolicy(empty, friendly, neutral, near);
            var result = new CombatEffectEngine().EvaluateAttackTargeting(new[] { Binding(1, policy) });
            AssertPolicy(result.Policy, empty, friendly, neutral, near);
        }

        [Test]
        public void Contributions_CombineAndSortIndependentlyOfInputOrder()
        {
            var bindings = new[] { Binding(2, new AttackTargetingPolicy(false, true, false, true)),
                Binding(1, new AttackTargetingPolicy(true, false, true, false)), Binding(3, default) };
            var engine = new CombatEffectEngine();
            var forward = engine.EvaluateAttackTargeting(bindings);
            Array.Reverse(bindings);
            var reverse = engine.EvaluateAttackTargeting(bindings);
            AssertPolicy(forward.Policy, true, true, true, true);
            AssertPolicy(reverse.Policy, true, true, true, true);
            Assert.That(forward.Contributions.Select(c => c.Origin), Is.EqualTo(new[] { Origin(1), Origin(2), Origin(3) }));
            Assert.That(reverse.Contributions.Select(c => c.Origin), Is.EqualTo(forward.Contributions.Select(c => c.Origin)));
            Assert.That(forward.Expectations.Select(e => e.Origin), Is.EqualTo(forward.Contributions.Select(c => c.Origin)));
        }

        [Test]
        public void InvalidBindingsAndForeignContributions_AreRejected()
        {
            var engine = new CombatEffectEngine();
            var binding = Binding(1, default);
            Assert.Throws<ArgumentNullException>(() => engine.EvaluateAttackTargeting(null));
            Assert.Throws<ArgumentException>(() => engine.EvaluateAttackTargeting(new EffectRuleBinding[] { null }));
            Assert.Throws<ArgumentException>(() => engine.EvaluateAttackTargeting(new[] { binding, binding }));
            var foreign = new EffectRuleBinding(Origin(1), new FixedRule(default, Origin(2)), StatelessEffectStateSnapshot.Instance);
            Assert.Throws<InvalidOperationException>(() => engine.EvaluateAttackTargeting(new[] { foreign }));
        }

        [Test]
        public void Evaluation_CopiesSortsAndProtectsItsCollections()
        {
            var contributions = new[] { new EffectAttackTargetingContribution(Origin(2), default),
                new EffectAttackTargetingContribution(Origin(1), new AttackTargetingPolicy(true, false, false, false)) };
            var expectations = new[] { Expectation(1), Expectation(2) };
            var result = new EffectAttackTargetingEvaluation(contributions, expectations);
            Assert.That(contributions[0].Origin, Is.EqualTo(Origin(2)));
            contributions[0] = default;
            expectations[0] = default;
            Assert.That(result.Contributions.Select(c => c.Origin), Is.EqualTo(new[] { Origin(1), Origin(2) }));
            Assert.That(result.Expectations.Select(e => e.Origin), Is.EqualTo(new[] { Origin(1), Origin(2) }));
            AssertPolicy(result.Policy, true, false, false, false);
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<EffectAttackTargetingContribution>)result.Contributions)[0] = default);
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<EffectStateExpectation>)result.Expectations)[0] = default);
        }

        [Test]
        public void Evaluation_RejectsInvalidDuplicateOrUnmatchedOrigins()
        {
            var c = new EffectAttackTargetingContribution(Origin(1), default);
            var e = Expectation(1);
            Assert.Throws<ArgumentException>(() => new EffectAttackTargetingContribution(default, default));
            Assert.Throws<ArgumentNullException>(() => new EffectAttackTargetingEvaluation(null, Array.Empty<EffectStateExpectation>()));
            Assert.Throws<ArgumentNullException>(() => new EffectAttackTargetingEvaluation(Array.Empty<EffectAttackTargetingContribution>(), null));
            Assert.Throws<ArgumentException>(() => new EffectAttackTargetingEvaluation(new[] { c }, Array.Empty<EffectStateExpectation>()));
            Assert.Throws<ArgumentException>(() => new EffectAttackTargetingEvaluation(new[] { c }, new[] { Expectation(2) }));
            Assert.Throws<ArgumentException>(() => new EffectAttackTargetingEvaluation(new[] { c, c }, new[] { e, e }));
            Assert.Throws<ArgumentException>(() => new EffectAttackTargetingEvaluation(new[] { c, new EffectAttackTargetingContribution(Origin(2), default) }, new[] { e, e }));
            Assert.Throws<ArgumentException>(() => new EffectAttackTargetingEvaluation(new EffectAttackTargetingContribution[] { default }, new[] { e }));
            Assert.Throws<ArgumentException>(() => new EffectAttackTargetingEvaluation(new[] { c }, new EffectStateExpectation[] { default }));
        }

        private static EffectOriginKey Origin(int id) => new EffectOriginKey(EffectOwnerKind.Trait, 1, id, "targeting");
        private static EffectStateExpectation Expectation(int id) => new EffectStateExpectation(Origin(id), StatelessEffectStateSnapshot.Instance);
        private static EffectRuleBinding Binding(int id, AttackTargetingPolicy policy) => new EffectRuleBinding(
            Origin(id), new FixedRule(policy), StatelessEffectStateSnapshot.Instance);

        private static void AssertPolicy(AttackTargetingPolicy policy, bool empty, bool friendly, bool neutral, bool near)
        {
            Assert.That(policy.AllowsEmptySingle, Is.EqualTo(empty));
            Assert.That(policy.AllowsFriendlyTargets, Is.EqualTo(friendly));
            Assert.That(policy.AllowsNeutralTargets, Is.EqualTo(neutral));
            Assert.That(policy.ExcludesAdjacentDirectCells, Is.EqualTo(near));
        }

        private sealed class FixedRule : IEffectAttackTargetingRule
        {
            private readonly AttackTargetingPolicy _policy;
            private readonly EffectOriginKey? _origin;
            internal FixedRule(AttackTargetingPolicy policy, EffectOriginKey? origin = null)
            { _policy = policy; _origin = origin; }
            public EffectAttackTargetingContribution EvaluateAttackTargeting(in EffectRuleInput input)
                => new EffectAttackTargetingContribution(_origin ?? input.Origin, _policy);
        }

        [Test]
        public void CannonCapture_BindsStatelessSafeBombardment()
        {
            var input = CombatCommandTestData.Input();
            var actor = CombatCommandTestData.With(input.Characters[0],
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon));
            var profile = new CombatAttackProfile(
                CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Cannon), input.Profiles[0].Method);

            var bindings = CombatCommandEffects.Capture(actor, profile);

            Assert.That(bindings.Count(b => b.Rule is SafeBombardmentCategoryRule), Is.EqualTo(1));
            Assert.That(bindings.Single(b => b.Rule is SafeBombardmentCategoryRule).ExpectedState,
                Is.SameAs(StatelessEffectStateSnapshot.Instance));
        }
    }
}
