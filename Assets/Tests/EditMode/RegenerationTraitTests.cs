using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class RegenerationTraitTests
    {
        [TestCase(6, 10, 3, 9)]
        [TestCase(9, 10, 3, 10)]
        [TestCase(10, 10, 3, 10)]
        [TestCase(0, 10, 3, 0)]
        [TestCase(12, 15, 3, 15)]
        [TestCase(int.MaxValue - 1, int.MaxValue, int.MaxValue, int.MaxValue)]
        public void Heal_RespectsCurrentMaximumAndDoesNotRevive(int current, int maximum, int amount, int expected)
        {
            var before = new HealthStateSnapshot(current, maximum);

            var after = RegenerationTraitRule.Heal(before, amount);

            Assert.That(after.CurrentHealth, Is.EqualTo(expected));
            Assert.That(after.MaxHealth, Is.EqualTo(maximum));
            Assert.That(before.CurrentHealth, Is.EqualTo(current));
            Assert.That(before.MaxHealth, Is.EqualTo(maximum));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Heal_RejectsNonpositiveAmountEvenWhenNoHealingIsPossible(int amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RegenerationTraitRule.Heal(new HealthStateSnapshot(6, 10), amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => RegenerationTraitRule.Heal(new HealthStateSnapshot(0, 10), amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => RegenerationTraitRule.Heal(new HealthStateSnapshot(10, 10), amount));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Contracts_RejectNonpositiveAmount(int amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitSpec(amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitDefinition(amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitStatusSnapshot(amount));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void RegistryAndProvider_PreserveConfiguredAmount(int amount)
        {
            var registry = TraitRegistry.CreateDefault();
            var spec = CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 10, CategoryKey.Main, new RegenerationTraitSpec(amount));
            var definition = new UnitCatalog(new[] { spec }, registry).GetRequired(EntityKey.Unit_Akimbo);
            var trait = (RegenerationTraitDefinition)definition.TraitDefinitions.Span[0];
            var registration = registry.GetRequiredRegistration(TraitKey.Regeneration);
            var state = registration.TraitRuntimeStateFactory.BuildRuntimeState(trait);
            var statuses = new TraitEffectProvider(registry).CreateStatuses(definition, new UnitTraitStateSet(new[] { state }));

            Assert.That(trait.HealAmount, Is.EqualTo(amount));
            Assert.That(state.TraitKey, Is.EqualTo(TraitKey.Regeneration));
            Assert.That(statuses, Has.Length.EqualTo(1));
            Assert.That(statuses[0], Is.TypeOf<RegenerationTraitStatusSnapshot>());
            Assert.That(((RegenerationTraitStatusSnapshot)statuses[0]).HealAmount, Is.EqualTo(amount));
            Assert.That(registration.EffectRuleFactory(trait), Is.TypeOf<RegenerationEffectRule>());
        }

        [Test]
        public void Factories_RejectNullAndMismatchedInputs()
        {
            var definitions = new RegenerationTraitDefinitionFactory();
            var states = new RegenerationRuntimeStateFactory();
            Assert.Throws<ArgumentNullException>(() => definitions.BuildDefinition(null));
            Assert.Throws<ArgumentException>(() => definitions.BuildDefinition(new KnockbackTraitSpec(1)));
            Assert.Throws<ArgumentNullException>(() => states.BuildRuntimeState(null));
            Assert.Throws<ArgumentException>(() => states.BuildRuntimeState(new KnockbackTraitDefinition(1)));
            Assert.Throws<ArgumentNullException>(() => new RegenerationEffectRule(null, new RegenerationTraitRule()));
            Assert.Throws<ArgumentNullException>(() => new RegenerationEffectRule(new RegenerationTraitDefinition(3), null));
        }

        [Test]
        public void RuntimeState_ClonesAndRoundTripsWithoutMutableHealth()
        {
            var state = new RegenerationRuntimeState();
            var clone = state.Clone();
            Assert.That(clone, Is.TypeOf<RegenerationRuntimeState>());
            Assert.That(clone, Is.Not.SameAs(state));
            Assert.DoesNotThrow(() => state.CopyFinalValuesFrom(clone));
            Assert.Throws<ArgumentNullException>(() => state.CopyFinalValuesFrom(null));
            Assert.Throws<ArgumentException>(() => state.CopyFinalValuesFrom(new FocusRuntimeState()));

            var captured = TraitEffectStateCodec.Capture(state);
            Assert.That(captured, Is.TypeOf<RegenerationEffectStateSnapshot>());
            Assert.That(captured.ValueEquals(TraitEffectStateCodec.Capture(clone)), Is.True);
            Assert.That(captured.ValueEquals(new KnockbackEffectStateSnapshot()), Is.False);
            Assert.That(captured.ValueEquals(null), Is.False);
            var restored = TraitEffectStateCodec.Restore(captured);
            Assert.That(restored, Is.TypeOf<RegenerationRuntimeState>());
            Assert.That(restored, Is.Not.SameAs(state));
            Assert.That(restored.TraitKey, Is.EqualTo(TraitKey.Regeneration));
        }

        [Test]
        public void StatusEquality_DistinguishesAmountAndTraitKind()
        {
            var status = new RegenerationTraitStatusSnapshot(3);
            Assert.That(status.SameValues(new RegenerationTraitStatusSnapshot(3)), Is.True);
            Assert.That(status.SameValues(new RegenerationTraitStatusSnapshot(1)), Is.False);
            Assert.That(status.SameValues(new KnockbackTraitStatusSnapshot(3)), Is.False);
            Assert.That(status.SameValues(null), Is.False);
        }

        [Test]
        public void Capture_MarkerDoesNotContributeToAttackMoveOrOwnTurnEffects()
        {
            var input = CombatCommandTestData.Input();
            var actor = CombatCommandTestData.With(input.Characters[0],
                traits: new TraitStatusSnapshot[] { new RegenerationTraitStatusSnapshot(3) });
            var profile = new CombatAttackProfile(
                CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, new RegenerationTraitSpec(3)),
                input.Profiles[0].Method);
            var bindings = CombatCommandEffects.Capture(actor, profile);
            Assert.That(bindings.Length, Is.EqualTo(1));
            Assert.That(((RegenerationEffectStateSnapshot)bindings[0].ExpectedState).CompletedEligibleTurnEnds, Is.Zero);
            var engine = new CombatEffectEngine();

            var damage = engine.EvaluateAttackDamage(bindings, new AttackDamageEffectContext(10));
            Assert.That(damage.EffectiveDamage, Is.EqualTo(10));
            Assert.That(damage.Contributions, Is.Empty);
            AssertNoChanges(engine.PlanMoveSucceeded(bindings, new MoveSucceededEffectContext(1,true)));
            AssertNoChanges(engine.PlanAttackSucceeded(bindings, new AttackSucceededEffectContext()));
            AssertNoChanges(engine.PlanTurnStarted(bindings));
            var ended = engine.PlanTurnEnded(bindings);
            AssertNoChanges(ended);
            var applied = CombatCommandEffects.Apply(actor.Stats, ended);
            Assert.That(applied.TraitStatuses.Span[0].SameValues(actor.Stats.TraitStatuses.Span[0]), Is.True);
        }
        private static void AssertNoChanges(CombatEffectPlan plan)
        {
            Assert.That(plan.Expectations, Is.Empty);
            Assert.That(plan.Changes, Is.Empty);
            Assert.That(plan.Activations, Is.Empty);
        }
    }
}
