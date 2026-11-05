using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class WeaknessExposurePolicyTests
    {
        [Test]
        public void DifferentSources_MaxThenFallback()
        {
            var statuses = Grant(Grant(StatusEffectSet.Empty, 1, 20, 2), 2, 30, 1);
            Assert.That(WeaknessExposureRules.EffectivePercent(statuses), Is.EqualTo(30));
            var first = AppliedStatusRules.EndControllerTurn(statuses, Owner.Enemy, Owner.Enemy);
            Assert.That(WeaknessExposureRules.EffectivePercent(first), Is.EqualTo(20));
            var second = AppliedStatusRules.EndControllerTurn(first, Owner.Enemy, Owner.Enemy);
            Assert.That(WeaknessExposureRules.EffectivePercent(second), Is.Zero);
            Assert.That(second.Entries, Is.Empty);
            Assert.That(WeaknessExposureRules.EffectivePercent(statuses), Is.EqualTo(30));
        }

        [Test]
        public void SameSource_ReplacesWeakerAndShorter()
        {
            var before = Grant(StatusEffectSet.Empty, 1, 30, 3);
            var after = Grant(before, 1, 10, 1);
            var entry = (WeaknessExposureStatus)after.Entries.Single();
            Assert.That(WeaknessExposureRules.EffectivePercent(after), Is.EqualTo(10));
            Assert.That(entry.RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            Assert.That(WeaknessExposureRules.EffectivePercent(before), Is.EqualTo(30));
            var fallback = Grant(before, 2, 20, 2);
            Assert.That(WeaknessExposureRules.EffectivePercent(Grant(fallback, 1, 10, 1)), Is.EqualTo(20));
        }

        [Test]
        public void SourceIdentity_IsCharacterAndTrait()
        {
            var statuses = Grant(StatusEffectSet.Empty, 1, 30, 3);
            statuses = Grant(statuses, 1, 20, 2, TraitKey.Vanguard);
            statuses = Grant(statuses, 2, 25, 2);
            statuses = Grant(statuses, 1, 10, 1, rule: "another-callback");
            Assert.That(statuses.Entries.Count, Is.EqualTo(3));
            Assert.That(statuses.Entries.Cast<WeaknessExposureStatus>().Select(s => s.IncreasePercent), Is.EqualTo(new[] { 10, 20, 25 }));
            Assert.That(statuses.Entries[0].Origin.Value.RuleKey, Is.EqualTo("another-callback"));
        }

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void DurationOne_ExpiresOnCurrentAffectedEndOnly(Owner owner)
        {
            var statuses = Grant(StatusEffectSet.Empty, 1, 30, 1);
            var opposite = owner == Owner.Player ? Owner.Enemy : Owner.Player;
            Assert.That(AppliedStatusRules.EndControllerTurn(statuses, owner, opposite), Is.SameAs(statuses));
            Assert.That(AppliedStatusRules.EndControllerTurn(statuses, owner, owner).Entries, Is.Empty);
        }

        [TestCase(CharacterRuntimeArea.Hand, true)]
        [TestCase(CharacterRuntimeArea.EnemyWaiting, true)]
        [TestCase(CharacterRuntimeArea.Field, false)]
        public void TargetAreaTransition_RemovesTemporaryRecipientsOnlyOnReserveEntry(CharacterRuntimeArea destination, bool clears)
        {
            var attack = new AttackIncreaseStatus(Origin(9), 7, 3);
            var before = Grant(Grant(new StatusEffectSet(new AppliedStatus[] { attack }), 1, 20, 2), 2, 30, 1);
            var after = AppliedStatusRules.OnAreaChanged(before, CharacterRuntimeArea.Field, destination);
            if (clears)
                Assert.That(after.Entries, Is.Empty);
            else
                Assert.That(after.Entries.OfType<AttackIncreaseStatus>().Single(), Is.SameAs(attack));
            Assert.That(WeaknessExposureRules.EffectivePercent(after), Is.EqualTo(clears ? 0 : 30));
            Assert.That(before.Entries.Count, Is.EqualTo(3));
        }

        [TestCase(100, 30, 130)]
        [TestCase(1, 30, 1)]
        [TestCase(0, 30, 0)]
        [TestCase(100, 0, 100)]
        public void Amplify_UsesWideIntegerArithmeticAndFinalTruncation(int damage, int percent, int expected)
        {
            Assert.That(WeaknessExposureRules.Amplify(damage, Grant(StatusEffectSet.Empty, 1, percent, 1)), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidMagnitudeLifetimeAndOverflow_LeaveInputUnchanged()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaknessExposureStatus(Origin(1), -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaknessExposureStatus(Origin(1), 10, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaknessExposureStatus(Origin(1), 10, -1));
            Assert.Throws<ArgumentException>(() => new WeaknessExposureStatus(default, 10, 1));
            var before = Grant(StatusEffectSet.Empty, 1, int.MaxValue, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => WeaknessExposureRules.Amplify(-1, before));
            Assert.Throws<OverflowException>(() => WeaknessExposureRules.Amplify(int.MaxValue, before));
            Assert.That(((WeaknessExposureStatus)before.Entries.Single()).RemainingAffectedSideTurnEnds, Is.EqualTo(2));
            Assert.Throws<ArgumentNullException>(() => WeaknessExposureRules.Apply(null, new WeaknessExposureStatus(Origin(1), 10, 1)));
            Assert.Throws<ArgumentNullException>(() => WeaknessExposureRules.Apply(before, null));
        }

        [Test]
        public void ReadSnapshot_SeparatesEffectiveMaximumAndEachSourceClock()
        {
            var statuses = Grant(Grant(StatusEffectSet.Empty, 1, 20, 2), 2, 30, 1);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, CellCoord.Zero, Owner.Enemy).WithStatusEffects(statuses);
            var row = StatusEffectReadModel.Capture(target).OfType<WeaknessExposureEffectSnapshot>().Single();
            Assert.That(row.Key, Is.EqualTo(StatusEffectKey.WeaknessExposure));
            Assert.That(row.Classification, Is.EqualTo(StatusEffectClassification.Debuff));
            Assert.That(row.EffectiveIncreasePercent, Is.EqualTo(30));
            Assert.That(row.Sources.Select(s => s.RemainingAffectedSideTurnEnds), Is.EqualTo(new int?[] { 2, 1 }));
            Assert.That(row.Sources.All(s => s.LifetimeKind == StatusEffectLifetimeKind.AffectedSideTurnEnds), Is.True);
            Assert.That(target.With(debuffs: CharacterDebuffState.Empty).StatusEffects.SameValues(statuses), Is.True);
            Assert.That(AppliedStatusRules.ClearForDeathOrBattleEnd(statuses).Entries, Is.Empty);
            Assert.That(row.Sources.Count, Is.EqualTo(2));
        }

        private static StatusEffectSet Grant(StatusEffectSet current, int source, int percent, int duration,
            TraitKey trait = TraitKey.HuntersMark, string rule = "hit") =>
            AppliedStatusRules.Apply(current, new WeaknessExposureStatus(Origin(source, trait, rule), percent, duration));
        private static EffectOriginKey Origin(int source, TraitKey trait = TraitKey.HuntersMark, string rule = "hit") =>
            new(EffectOwnerKind.Trait, source, (int)trait, rule);
    }
}