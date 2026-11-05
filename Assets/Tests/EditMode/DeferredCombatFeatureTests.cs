using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Tests.Core
{
    public sealed class DeferredCombatFeatureTests
    {
        [TestCase(TraitKey.Focus)]
        [TestCase(TraitKey.ThornField)]
        [TestCase(TraitKey.Necromancy)]
        public void UnitClockTraits_AreExplicitlyUnavailable(TraitKey key)
        {
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(key), Is.False);
            Assert.That(DeferredCombatFeatures.GetTraitUnavailableReason(key), Is.EqualTo("DeferredUnitTurnTiming"));
        }

        [TestCase(TraitKey.Intimidation)]
        [TestCase(TraitKey.KillChainAttack)]
        [TestCase(TraitKey.Knockback)]
        [TestCase(TraitKey.Regeneration)]
        public void ImmediateTraitsRemainAvailable(TraitKey key) =>
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(key), Is.True);

        [Test]
        public void RopeDefinitionRemainsValid_ButRuntimeAvailabilityIsFalse()
        {
            var rules = ItemRuleRegistry.CreateDefault();
            var rope = new RopeItemSpec(10);
            Assert.DoesNotThrow(() => rules.Validate(rope));
            Assert.That(rules.IsAvailable(rope), Is.False);
            Assert.That(rules.GetUnavailableReason(rope), Is.EqualTo("DeferredUnitTurnTiming"));
            Assert.Throws<ArgumentNullException>(() => rules.IsAvailable(null));
        }

        [Test]
        public void FlagPickupDoesNotChangeHeldOrSourceState()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters.First();
            var flag = new FlagSourceFact(1, actor.Anchor.Value, 1, 50, true, new[] { Owner.Player });
            var final = HeldEnvironmentEffects.AcquireFlag(actor, flag);
            Assert.That(final, Is.SameAs(actor));
            Assert.That(flag.Active, Is.True);
        }

        [Test]
        public void FlagPickup_IsDeferred() => Assert.That(DeferredCombatFeatures.IsFlagPickupAvailable, Is.False);

        [Test]
        public void MoveThroughInactiveFlag_ChangesNoSourceOrHeldEnvironment()
        {
            var input = MineEnvironmentTests.Input(FlagLifecycleTests.Flag().WithActive(false));
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var result = CombatCommandCalculator.Calculate(input, FlagLifecycleTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var after = MovementTestData.Actor(result);
            Assert.That(after.Anchor, Is.EqualTo(MineEnvironmentTests.Center));
            Assert.That(after.HeldEnvironment.SameValues(before.HeldEnvironment), Is.True);
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.True);
            Assert.That(result.FinalState.EnvironmentProgress.SameValues(input.State.EnvironmentProgress), Is.True);
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>(), Is.Empty);
        }

        [Test]
        public void EnvironmentClocksDoNotActivateOrArmCounters()
        {
            var input = CombatCommandTestData.Input();
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId),
                new System.Collections.Generic.List<CombatEvent>());
            WeakGroundCalculator.RegisterEntry(projection, input.Characters[0].EntityId, input.Characters[0].Anchor.Value);
            WeakGroundCalculator.CollapsePending(projection);
            TornadoCalculator.ResolveGroupedEntry(projection, input.ActorId, input.Characters[0].Anchor.Value, Array.Empty<IMovementTileRule>());
            Assert.That(projection.Events, Is.Empty);
            Assert.That(CombatGimmickCalculator.TryAdvance(input), Is.Null);
        }
    }
}
