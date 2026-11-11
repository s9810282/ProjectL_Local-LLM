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
    public sealed class IntimidationTraitTests
    {
        [TestCase(0)]
        [TestCase(101)]
        public void Contracts_RejectInvalidParameters(int percent)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntimidationTraitSpec(percent));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntimidationTraitDefinition(percent));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntimidationTraitStatusSnapshot(percent));
        }

        [TestCase(1)]
        [TestCase(20)]
        [TestCase(100)]
        public void RegistryAndProvider_PreserveConfiguredAttackReduction(int percent)
        {
            var registry = TraitRegistry.CreateDefault();
            var spec = CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 10, CategoryKey.Main, new IntimidationTraitSpec(percent));
            var definition = new UnitCatalog(new[] { spec }, registry).GetRequired(EntityKey.Unit_Akimbo);
            var trait = (IntimidationTraitDefinition)definition.TraitDefinitions.Span[0];
            var registration = registry.GetRequiredRegistration(TraitKey.Intimidation);
            var state = registration.TraitRuntimeStateFactory.BuildRuntimeState(trait);
            var statuses = new TraitEffectProvider(registry).CreateStatuses(definition, new UnitTraitStateSet(new[] { state }));

            Assert.That(trait.AttackReductionPercent, Is.EqualTo(percent));
            Assert.That(state.TraitKey, Is.EqualTo(TraitKey.Intimidation));
            Assert.That(statuses, Has.Length.EqualTo(1));
            var status = (IntimidationTraitStatusSnapshot)statuses[0];
            Assert.That(status.AttackReductionPercent, Is.EqualTo(percent));
            Assert.That(registration.EffectRuleFactory(trait), Is.TypeOf<IntimidationEffectRule>());
        }
        [Test]
        public void Registry_CoversEveryNonPendingTraitExactlyOnce()
        {
            var keys = TraitRegistry.CreateDefault().OrderedRegistrations.Select(r => r.TraitKey).ToArray();
            var declared = Enum.GetValues(typeof(TraitKey)).Cast<TraitKey>()
                .Where(k => k != TraitKey.None && !DeferredCombatFeatures.IsTraitRetired(k) &&
                    DeferredCombatFeatures.GetTraitUnavailableReason(k) != "ImplementationPending").ToArray();
            Assert.That(keys, Is.EquivalentTo(declared));
            Assert.That(keys.Distinct().Count(), Is.EqualTo(44));
        }

        [Test]
        public void Factories_RejectNullAndMismatchedInputs()
        {
            var definitions = new IntimidationTraitDefinitionFactory();
            var states = new IntimidationRuntimeStateFactory();
            Assert.Throws<ArgumentNullException>(() => definitions.BuildDefinition(null));
            Assert.Throws<ArgumentException>(() => definitions.BuildDefinition(new KnockbackTraitSpec(1)));
            Assert.Throws<ArgumentNullException>(() => states.BuildRuntimeState(null));
            Assert.Throws<ArgumentException>(() => states.BuildRuntimeState(new KnockbackTraitDefinition(1)));
            Assert.Throws<ArgumentNullException>(() => new IntimidationEffectRule(null, new IntimidationTraitRule()));
            Assert.Throws<ArgumentNullException>(() => new IntimidationEffectRule(new IntimidationTraitDefinition(20), null));
        }

        [Test]
        public void RuntimeState_ClonesAndRoundTripsWithoutVictimState()
        {
            var state = new IntimidationRuntimeState();
            var clone = state.Clone();
            Assert.That(clone, Is.TypeOf<IntimidationRuntimeState>());
            Assert.That(clone, Is.Not.SameAs(state));
            Assert.DoesNotThrow(() => state.CopyFinalValuesFrom(clone));
            Assert.Throws<ArgumentNullException>(() => state.CopyFinalValuesFrom(null));
            Assert.Throws<ArgumentException>(() => state.CopyFinalValuesFrom(new FocusRuntimeState()));

            var captured = TraitEffectStateCodec.Capture(state);
            Assert.That(captured, Is.TypeOf<IntimidationEffectStateSnapshot>());
            Assert.That(captured.ValueEquals(TraitEffectStateCodec.Capture(clone)), Is.True);
            Assert.That(captured.ValueEquals(new KnockbackEffectStateSnapshot()), Is.False);
            Assert.That(captured.ValueEquals(null), Is.False);
            var restored = TraitEffectStateCodec.Restore(captured);
            Assert.That(restored, Is.TypeOf<IntimidationRuntimeState>());
            Assert.That(restored, Is.Not.SameAs(state));
            Assert.That(restored.TraitKey, Is.EqualTo(TraitKey.Intimidation));
        }

        [Test]
        public void StatusEquality_DistinguishesAttackReductionAndTraitKind()
        {
            var status = new IntimidationTraitStatusSnapshot(20);
            Assert.That(status.SameValues(new IntimidationTraitStatusSnapshot(20)), Is.True);
            Assert.That(status.SameValues(new IntimidationTraitStatusSnapshot(21)), Is.False);
            Assert.That(status.SameValues(new KnockbackTraitStatusSnapshot(3)), Is.False);
            Assert.That(status.SameValues(null), Is.False);
        }

        [Test]
        public void Capture_MarkerDoesNotContributeToAttackMoveOrOwnTurnEffects()
        {
            var input = CombatCommandTestData.Input();
            var actor = CombatCommandTestData.With(input.Characters[0],
                traits: new TraitStatusSnapshot[] { new IntimidationTraitStatusSnapshot(20) });
            var profile = new CombatAttackProfile(
                CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, new IntimidationTraitSpec(20)),
                input.Profiles[0].Method);
            var bindings = CombatCommandEffects.Capture(actor, profile);
            Assert.That(bindings, Has.Length.EqualTo(1));
            Assert.That(bindings[0].ExpectedState, Is.TypeOf<IntimidationEffectStateSnapshot>());
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
