using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class TraitEffectiveStatsProjectionTests
    {
        [Test]
        public void CohesionProjection_CopiesAndEqualityPreserveUnrelatedState()
        {
            var effects = new CharacterTraitEffects(Array.Empty<TraitAttackGrant>(), Array.Empty<TraitShieldGrant>(), 5);
            var before = CombatCommandTestData.Input().Characters[0].With(traitEffects: effects);
            Assert.That(before.WithHealth(before.Health).TraitEffects.CohesionDefenseBonus, Is.EqualTo(5));
            Assert.That(before.WithPlacement(before.Area, before.ReserveSlotIndex, before.Anchor, before.Rotation, before.OccupiedCells).SameValues(before), Is.True);
            Assert.That(effects.SameValues(CharacterTraitEffects.Empty), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterTraitEffects(Array.Empty<TraitAttackGrant>(), Array.Empty<TraitShieldGrant>(), -1));
        }
        [Test]
        public void EventTimeDefense_CombinesProjectedCohesionAndArmorWithoutChangingBaseline()
        {
            var before = CombatCommandTestData.Input().Characters[0];
            before = before.With(stats: before.Stats.With(physicalDefense: 20),
                traitEffects: new CharacterTraitEffects(Array.Empty<TraitAttackGrant>(), Array.Empty<TraitShieldGrant>(), 5));
            var origin = new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects.EffectOriginKey(
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects.EffectOwnerKind.Trait, 9, (int)TraitKey.ArmorBreaker, "main");
            var after = before.With(debuffs: new CharacterDebuffState(armorBreak: new ArmorBreakState(new[] { new ArmorBreakStack(origin, 50) })));
            var events = new System.Collections.Generic.List<CombatEvent>();
            CombatCommandEvents.TraitState(events, before, after);
            var change = (CombatTraitStateEvent)events.Single();
            Assert.That(change.ExpectedStats.PhysicalDefense, Is.EqualTo(25));
            Assert.That(change.FinalStats.PhysicalDefense, Is.EqualTo(12));
            Assert.That(change.Final.Stats.PhysicalDefense, Is.EqualTo(20));
            Assert.That(change.ExpectedStats.SameValues(new CombatEffectiveStatsSnapshot(change.ExpectedStats.AttackPower, 25)), Is.True);
        }
        [Test]
        public void Cleanup_RecordsRemovedCohesionContributionEvenWithoutShieldBudget()
        {
            var before = CombatCommandTestData.Input().Characters[0].With(
                traitEffects: new CharacterTraitEffects(Array.Empty<TraitAttackGrant>(), Array.Empty<TraitShieldGrant>(), 5));
            var events = new System.Collections.Generic.List<CombatEvent>();
            var final = CombatTemporaryStateCleanup.Clear(before, events, ItemEventReason.BattleEnded);
            Assert.That(final.TraitEffects.CohesionDefenseBonus, Is.Zero);
            var value = TraitActivationOutcomeTests.Activations(events, TraitKey.Cohesion).Single();
            Assert.That(value.TraitActivationKind, Is.EqualTo(TraitActivationKind.DefenseChanged));
            Assert.That(value.Magnitude, Is.EqualTo(5));
        }
    }
}
