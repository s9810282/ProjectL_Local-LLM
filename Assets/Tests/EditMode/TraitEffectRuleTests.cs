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

    }
}
