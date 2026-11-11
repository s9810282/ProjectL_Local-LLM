using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class PeriodicStatusRulesTests
    {
        private static EffectOriginKey Origin(int id = 1) => new(EffectOwnerKind.Trait, id, 480, "bleeding");

        [Test]
        public void MixedRanks_KeepOneTwoThree()
        {
            var states = StatusEffectSet.Empty;
            for (int i = 1; i <= 3; i++) states = PeriodicStatusRules.ApplyBleeding(states, Origin(i), i);
            var bleeding = states.Entries.OfType<BleedingStatus>().Single();
            Assert.That(bleeding.Stacks.Select(s => s.Damage), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(bleeding.Stacks.Sum(s => s.Damage), Is.EqualTo(6));
            Assert.That(bleeding.Stacks.Select(s => s.Origin), Is.EqualTo(new[] { Origin(1), Origin(2), Origin(3) }));
            Assert.That(bleeding.RemainingAffectedSideStarts, Is.EqualTo(3));
            Assert.That(bleeding.Origin, Is.EqualTo(Origin(1)));
        }

        [Test]
        public void FiveStacks_RefreshWithoutReplacement()
        {
            var current = StatusEffectSet.Empty;
            for (int i = 1; i <= 5; i++) current = PeriodicStatusRules.ApplyBleeding(current, Origin(i), i);
            var original = current;
            current = PeriodicStatusRules.ConsumeStartTick(current, StatusEffectKey.Bleeding);
            Assert.That(current.Entries.OfType<BleedingStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(2));
            current = PeriodicStatusRules.ApplyBleeding(current, Origin(6), 99);
            Assert.That(current.SameValues(original), Is.True);
            Assert.That(original.Entries.OfType<BleedingStatus>().Single().Stacks.Count, Is.EqualTo(5));
        }

        [Test]
        public void Fire_ReapplyRefreshesWithoutStacking()
        {
            var current = PeriodicStatusRules.ApplyFire(StatusEffectSet.Empty, Origin());
            current = PeriodicStatusRules.ConsumeStartTick(current, StatusEffectKey.Fire);
            current = PeriodicStatusRules.ApplyFire(current, Origin(2));
            var fire = current.Entries.OfType<FireStatus>().Single();
            Assert.That(fire.Damage, Is.EqualTo(2));
            Assert.That(fire.Origin, Is.EqualTo(Origin(2)));
            Assert.That(fire.RemainingAffectedSideStarts, Is.EqualTo(2));
            Assert.That(PeriodicStatusRules.ConsumeStartTick(PeriodicStatusRules.ConsumeStartTick(current,
                StatusEffectKey.Fire), StatusEffectKey.Fire).Entries, Is.Empty);
        }

        [TestCase(CombatControllerPhase.Enemy, 12)]
        [TestCase(CombatControllerPhase.Player, 13)]
        public void Slow_ExpiresAfterNextFullAffectedTurn(CombatControllerPhase phase, long expiry)
        {
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: phase, completedControllerTurnCount: 10);
            var current = PeriodicStatusRules.ApplySlow(StatusEffectSet.Empty, Origin(), Owner.Player, turn);
            var slow = current.Entries.OfType<MovementRangeDecreaseStatus>().Single();
            Assert.That(slow.Magnitude, Is.EqualTo(1));
            Assert.That(slow.ExpiresAtCompletedControllerTurnCount, Is.EqualTo(expiry));
            Assert.That(PeriodicStatusRules.ExpireSlow(current, expiry - 1), Is.SameAs(current));
            Assert.That(PeriodicStatusRules.ExpireSlow(current, expiry).Entries, Is.Empty);
            var refreshed = PeriodicStatusRules.ApplySlow(current, Origin(2), Owner.Player, turn);
            Assert.That(refreshed.Entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void MovementPenalty_IsIndependentOfEnvironment_AndClamps()
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right);
            foreach (int range in new[] { 0, 1, 3 })
            {
                var actor = MovementTestData.WithMovement(input, range).Characters.Single(c => c.EntityId == input.ActorId);
                actor = actor.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new MovementRangeDecreaseStatus(Origin(), 2) }));
                Assert.That(VoluntaryMovementRules.Allowance(actor), Is.EqualTo(range == 0 ? (int?)null : range - 1));
            }
        }

        [Test]
        public void ImmutableValues_ValidateCopyCompareAndCleanup()
        {
            var supplied = new[] { new BleedingStack(Origin(), 1) };
            var bleeding = new BleedingStatus(supplied, 3);
            supplied[0] = new BleedingStack(Origin(2), 3);
            Assert.That(bleeding.Stacks[0].Damage, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<BleedingStack>)bleeding.Stacks)[0] = supplied[0]);
            Assert.Throws<ArgumentException>(() => new BleedingStack(default, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BleedingStack(Origin(), 0));
            Assert.Throws<ArgumentException>(() => new BleedingStatus(Array.Empty<BleedingStack>(), 3));
            Assert.Throws<ArgumentException>(() => new BleedingStatus(Enumerable.Repeat(supplied[0], 6).ToArray(), 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FireStatus(Origin(), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BleedingStatus(supplied, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementRangeDecreaseStatus(Origin(), -1));
            Assert.That(bleeding.SameValues(new BleedingStatus(new[] { new BleedingStack(Origin(), 1) }, 3)), Is.True);
            Assert.That(bleeding.SameValues(new BleedingStatus(supplied, 3)), Is.False);
            var states = new StatusEffectSet(new AppliedStatus[] { bleeding, new FireStatus(Origin(), 2), new MovementRangeDecreaseStatus(Origin(), 2) });
            var actor = MovementTestData.SafeRoute(CellCoord.Zero).Characters[0].WithStatusEffects(states);
            Assert.That(actor.WithHealth(actor.Health).StatusEffects, Is.SameAs(states));
            Assert.That(AppliedStatusRules.ClearForReserve(states).Entries, Is.Empty);
            Assert.That(AppliedStatusRules.ClearForDeathOrBattleEnd(states).Entries, Is.Empty);
            Assert.That(PeriodicStatusRules.ConsumeStartTick(PeriodicStatusRules.ConsumeStartTick(
                PeriodicStatusRules.ConsumeStartTick(states, StatusEffectKey.Bleeding), StatusEffectKey.Bleeding),
                StatusEffectKey.Bleeding).Entries.OfType<BleedingStatus>(), Is.Empty);
        }
    }
}
