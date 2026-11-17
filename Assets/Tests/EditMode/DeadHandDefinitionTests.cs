using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandDefinitionTests
    {
        [Test]
        public void SpecAcceptsNonnegativeNumbersAndComparesDamagePercent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeadHandTraitSpec(-1));
            Assert.That(new DeadHandTraitSpec(0).DamagePercent, Is.Zero);
            Assert.That(new DeadHandTraitSpec(125).SameValues(new DeadHandTraitSpec(125)), Is.True);
            Assert.That(new DeadHandTraitSpec(125).SameValues(new DeadHandTraitSpec(126)), Is.False);
        }

        [Test]
        public void RegisteredDefinitionAndStatelessCopiesPreserveValuesWhenRuntimeIsAvailable()
        {
            var registry = TraitRegistry.CreateDefault();
            var registration = registry.OrderedRegistrations.Single(r => r.TraitKey == TraitKey.DeadHand);
            var definition = (DeadHandTraitDefinition)registration.TraitDefinitionFactory.BuildDefinition(new DeadHandTraitSpec(135));
            Assert.That(definition.DamagePercent, Is.EqualTo(135));
            var state = registration.TraitRuntimeStateFactory.BuildRuntimeState(definition);
            var copy = state.Clone();
            Assert.That(copy, Is.TypeOf<DeadHandRuntimeState>().And.Not.SameAs(state));
            Assert.DoesNotThrow(() => state.CopyFinalValuesFrom(copy));
            Assert.That(registration.EffectRuleFactory(definition), Is.TypeOf<DeadHandEffectRule>());
            Assert.That(new DeadHandEffectStateSnapshot().ValueEquals(new DeadHandEffectStateSnapshot()), Is.True);
            Assert.That(DeferredCombatFeatures.GetTraitUnavailableReason(TraitKey.DeadHand), Is.Empty);
            Assert.That(registry.GetRequiredRegistration(TraitKey.DeadHand), Is.SameAs(registration));
            Assert.That(TraitEffectStateCodec.Restore(TraitEffectStateCodec.Capture(state)), Is.TypeOf<DeadHandRuntimeState>());
        }
    }
}
