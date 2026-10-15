using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Tests.Core
{
    public sealed class TraitEffectRuleTests
    {
        [Test]
        public void Focus_UsesCapturedStateAndLeavesLiveStateUntouched()
        {
            var live = new FocusRuntimeState(1, true, false);
            IEffectStateSnapshot snapshot = TraitEffectStateCodec.Capture(live);
            var origin = TraitEffectOrigin.Create(new EntityId(100), TraitKey.Focus);
            var rule = new FocusEffectRule(new FocusTraitDefinition(3, 3), new FocusTraitRule());
            var input = new EffectRuleInput(origin, snapshot);

            live.CopyFinalValuesFrom(new FocusRuntimeState(0, false, true));

            EffectAttackDamageContribution damage = rule.EvaluateAttackDamage(
                input,
                new AttackDamageEffectContext(10));
            EffectRuleResult plan = rule.PlanTurnStarted(input);
            var after = (FocusEffectStateSnapshot)plan.Change.Value.FinalState;

            Assert.That(damage.FlatBonus, Is.EqualTo(3));
            Assert.That(after.ActiveStacks, Is.EqualTo(2));
            Assert.That(live.ActiveStacks, Is.Zero);
            Assert.That(((FocusEffectStateSnapshot)snapshot).ActiveStacks, Is.EqualTo(1));
        }

        [Test]
        public void DiscountAdapter_PreservesLegacyConsumptionAndActivation()
        {
            var definition = new MoveAttackDiscountTraitDefinition(2, 1);
            var legacy = new MoveAttackDiscountTraitRule();
            var live = new MoveAttackDiscountRuntimeState(true, true);
            var origin = TraitEffectOrigin.Create(
                new EntityId(100),
                TraitKey.MoveAttackDiscount);
            TraitStateChange expected = legacy.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,
                    live,
                    new[] { TraitKey.MoveAttackDiscount }));
            var adapter = new MoveAttackDiscountEffectRule(definition, legacy);

            EffectRuleResult actual = adapter.PlanAttackSucceeded(
                new EffectRuleInput(origin, TraitEffectStateCodec.Capture(live)),
                new AttackSucceededEffectContext(new[] { origin }));

            Assert.That(actual.Change.HasValue, Is.EqualTo(expected.HasChange));
            Assert.That(actual.Change.Value.FinalState.ValueEquals(
                TraitEffectStateCodec.Capture(expected.FinalState)), Is.True);
            Assert.That(actual.Activations.Count, Is.EqualTo(1));
            Assert.That(actual.Activations[0].Code, Is.EqualTo((int)expected.ActivationKind));
            Assert.That(actual.Activations[0].Magnitude, Is.EqualTo(expected.ActivationMagnitude));
            Assert.That(actual.Activations[0].TargetId, Is.EqualTo(EntityId.Empty));
            Assert.That(live.DiscountArmed, Is.True);
        }

        [Test]
        public void AttackContributors_IgnoreOtherProvidersButRejectForeignTraitOwners()
        {
            var unitId = new EntityId(100);
            var origin = TraitEffectOrigin.Create(unitId, TraitKey.MoveAttackDiscount);
            var adapter = new MoveAttackDiscountEffectRule(
                new MoveAttackDiscountTraitDefinition(2, 1),
                new MoveAttackDiscountTraitRule());
            var input = new EffectRuleInput(
                origin,
                TraitEffectStateCodec.Capture(
                    new MoveAttackDiscountRuntimeState(true, true)));
            var artifact = new EffectOriginKey(EffectOwnerKind.Artifact, 0, 1, "entry");

            EffectRuleResult result = adapter.PlanAttackSucceeded(
                input,
                new AttackSucceededEffectContext(new[] { artifact, origin }));

            Assert.That(result.Change.HasValue, Is.True);
            Assert.Throws<ArgumentException>(() => adapter.PlanAttackSucceeded(
                input,
                new AttackSucceededEffectContext(new[]
                {
                    TraitEffectOrigin.Create(new EntityId(101), TraitKey.Focus),
                })));
        }
    }
}
