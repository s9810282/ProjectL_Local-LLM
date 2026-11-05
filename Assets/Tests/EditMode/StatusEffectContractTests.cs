using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class StatusEffectContractTests
    {
        private static readonly EffectOriginKey Origin = new(EffectOwnerKind.Trait, 1, (int)TraitKey.Vanguard, "grant");

        [Test]
        public void Collection_CopiesInputAndRetainsRepeatedSameSourceGrants()
        {
            var grant = new AttackIncreaseStatus(Origin, 3, 2);
            var supplied = new AppliedStatus[] { grant, grant };
            var statuses = new StatusEffectSet(supplied);
            supplied[0] = new AttackIncreaseStatus(Origin, 9, 1);

            Assert.That(statuses.Entries.Count, Is.EqualTo(2));
            Assert.That(statuses.Entries.Cast<AttackIncreaseStatus>().Select(s => s.Amount), Is.EqualTo(new[] { 3, 3 }));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<AppliedStatus>)statuses.Entries)[0] = supplied[0]);
        }

        [Test]
        public void Equality_DetectsProvenancePayloadOrderAndCount()
        {
            var first = new AttackIncreaseStatus(Origin, 3, 1);
            var second = new AttackIncreaseStatus(Origin, 4, 2);
            var statuses = new StatusEffectSet(new AppliedStatus[] { first, second });
            Assert.That(statuses.SameValues(new StatusEffectSet(new AppliedStatus[] {
                new AttackIncreaseStatus(Origin, 3, 1), new AttackIncreaseStatus(Origin, 4, 2) })), Is.True);
            foreach (var changed in new[]
            {
                new StatusEffectSet(new AppliedStatus[] { second, first }),
                new StatusEffectSet(new AppliedStatus[] { first }),
                new StatusEffectSet(new AppliedStatus[] { first, new AttackIncreaseStatus(Origin, 4, 1) }),
                new StatusEffectSet(new AppliedStatus[] { first, new AttackIncreaseStatus(Origin, 5, 2) }),
                new StatusEffectSet(new AppliedStatus[] { first, new AttackIncreaseStatus(
                    new EffectOriginKey(EffectOwnerKind.Trait, 2, (int)TraitKey.Vanguard, "grant"), 4, 2) }),
                new StatusEffectSet(new AppliedStatus[] { first, new AttackIncreaseStatus(
                    new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.CleanSweep, "grant"), 4, 2) }),
                new StatusEffectSet(new AppliedStatus[] { first, new AttackIncreaseStatus(
                    new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Vanguard, "other"), 4, 2) })
            })
            {
                Assert.That(statuses.SameValues(changed), Is.False);
                Assert.That(changed.SameValues(statuses), Is.False);
            }
            Assert.That(statuses.SameValues(null), Is.False);
        }

        [Test]
        public void InvalidCollectionOrGrant_RejectsBeforeCreatingFacts()
        {
            Assert.Throws<ArgumentNullException>(() => new StatusEffectSet(null));
            Assert.Throws<ArgumentException>(() => new StatusEffectSet(new AppliedStatus[] { null }));
            Assert.Throws<ArgumentException>(() => new AttackIncreaseStatus(default, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackIncreaseStatus(Origin, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackIncreaseStatus(Origin, 1, 0));
            Assert.Throws<OverflowException>(() => Facts(new StatusEffectSet(new AppliedStatus[] {
                new AttackIncreaseStatus(Origin, int.MaxValue, 1), new AttackIncreaseStatus(Origin, 1, 1) })));
        }

        [Test]
        public void HealthAndPlacementCopies_PreserveCanonicalStatusAndLegacyFamilies()
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] { new ProbeStatus(8), new AttackIncreaseStatus(Origin, 3, 2) });
            var before = Facts(statuses).With(debuffs: new CharacterDebuffState(new IntimidationDebuffState(20)));
            var health = before.With(health: new HealthStateSnapshot(9, 20));
            var placement = before.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>());
            Assert.That(health.StatusEffects.SameValues(before.StatusEffects), Is.True);
            Assert.That(placement.StatusEffects.SameValues(before.StatusEffects), Is.True);
            Assert.That(before.StatusEffects.Entries.OfType<AttackDecreaseStatus>().Single().ReductionPercent, Is.EqualTo(20));
            Assert.That(statuses.Entries.Count, Is.EqualTo(2));
            Assert.That(health.TraitEffects, Is.SameAs(before.TraitEffects));
            Assert.That(placement.TraitEffects, Is.SameAs(before.TraitEffects));
            Assert.That(before.TraitEffects.AttackGrants.Single().ToStatus(),
                Is.SameAs(statuses.Entries.OfType<AttackIncreaseStatus>().Single()));
            Assert.That(health.Debuffs.SameValues(before.Debuffs), Is.True);
            Assert.That(placement.Debuffs.SameValues(before.Debuffs), Is.True);
            Assert.That(before.Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(before.Area, Is.EqualTo(CharacterRuntimeArea.Field));
        }

        [Test]
        public void LegacyPartialReplacement_ChangesOnlyRepresentableFamilies()
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] { new ProbeStatus(8), new AttackIncreaseStatus(Origin, 3, 2) });
            var before = Facts(statuses);
            var debuffs = before.With(debuffs: new CharacterDebuffState(new IntimidationDebuffState(20)));
            Assert.That(new StatusEffectSet(debuffs.StatusEffects.Entries.Where(s => s is not AttackDecreaseStatus).ToArray()).SameValues(statuses), Is.True);
            Assert.That(debuffs.StatusEffects.Entries.OfType<AttackDecreaseStatus>().Single().ReductionPercent, Is.EqualTo(20));
            Assert.That(before.With(traitEffects: before.TraitEffects).StatusEffects.SameValues(statuses), Is.True);

            var replacement = new CharacterTraitEffects(new[] { new TraitAttackGrant(Origin, 4, 1) });
            var changed = debuffs.With(traitEffects: replacement);
            Assert.That(changed.StatusEffects.Entries.OfType<ProbeStatus>().Single().Value, Is.EqualTo(8));
            Assert.That(changed.TraitEffects.AttackBonus, Is.EqualTo(4));
            Assert.That(changed.Debuffs.SameValues(debuffs.Debuffs), Is.True);
            var cleared = changed.With(traitEffects: CharacterTraitEffects.Empty);
            Assert.That(cleared.StatusEffects.Entries.Count, Is.EqualTo(2));
            Assert.That(cleared.StatusEffects.Entries.OfType<ProbeStatus>().Single().Value, Is.EqualTo(8));
            Assert.That(cleared.StatusEffects.Entries.OfType<AttackDecreaseStatus>().Single(),
                Is.SameAs(debuffs.StatusEffects.Entries.OfType<AttackDecreaseStatus>().Single()));
            Assert.That(before.TraitEffects.AttackBonus, Is.EqualTo(3));
        }

        [Test]
        public void LegacyInput_NormalizesAndCanonicalReplacementProjectsBack()
        {
            var shield = new TraitShieldGrant(new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Cohesion, "shield"), 5, true, 0);
            var legacy = new CharacterTraitEffects(new[] { new TraitAttackGrant(Origin, 3, 2) }, new[] { shield }, 7);
            var before = Facts(effects: legacy);
            Assert.That(before.StatusEffects.Entries.Select(s => s.Key), Is.EqualTo(new[] { StatusEffectKey.AttackIncrease, StatusEffectKey.Shield }));
            Assert.That(before.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.EqualTo(5));
            Assert.That(before.TraitEffects.SameValues(legacy), Is.True);
            var changed = before.WithStatusEffects(StatusEffectSet.Empty);
            Assert.That(changed.TraitEffects.AttackGrants, Is.Empty);
            Assert.That(changed.TraitEffects.Shields, Is.Empty);
            Assert.That(changed.TraitEffects.CohesionDefenseBonus, Is.EqualTo(7));
            Assert.That(before.TraitEffects.AttackBonus, Is.EqualTo(3));
            Assert.That(before.TraitEffects.Shields.Single().SameValues(shield), Is.True);
            Assert.Throws<ArgumentNullException>(() => before.WithStatusEffects(null));
        }

        [Test]
        public void ExplicitDualInput_RequiresAgreementWithoutMutatingEitherInput()
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] { new AttackIncreaseStatus(Origin, 3, 2) });
            var legacy = new CharacterTraitEffects(new[] { new TraitAttackGrant(Origin, 4, 2) });
            Assert.Throws<ArgumentException>(() => Facts(statuses, legacy));
            Assert.That(((AttackIncreaseStatus)statuses.Entries.Single()).Amount, Is.EqualTo(3));
            Assert.That(legacy.AttackBonus, Is.EqualTo(4));
            var matching = new CharacterTraitEffects(new[] { new TraitAttackGrant(Origin, 3, 2) });
            Assert.That(Facts(statuses, matching).StatusEffects.SameValues(statuses), Is.True);
        }

        [Test]
        public void Catalog_ReservesStableUniqueKeysAndSeparatesClassification()
        {
            var keys = (StatusEffectKey[])Enum.GetValues(typeof(StatusEffectKey));
            Assert.That(keys.Select(k => (int)k), Is.EqualTo(Enumerable.Range(0, 15)));
            Assert.That((int)StatusEffectKey.AttackIncrease, Is.EqualTo(1));
            Assert.That((int)StatusEffectKey.WeaknessExposure, Is.EqualTo(13));
            foreach (var key in keys.Where(k => k != StatusEffectKey.None))
            {
                bool debuff = key == StatusEffectKey.AttackDecrease || key == StatusEffectKey.DefenseDecrease ||
                    key == StatusEffectKey.AttackRangeDecrease || key == StatusEffectKey.Disarm ||
                    key == StatusEffectKey.Bind || key == StatusEffectKey.WeaknessExposure || key == StatusEffectKey.Taunted;
                Assert.That(StatusEffectCatalog.ClassificationOf(key), Is.EqualTo(debuff ? StatusEffectClassification.Debuff : StatusEffectClassification.Buff));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => StatusEffectCatalog.ClassificationOf(StatusEffectKey.None));
        }

        private static CharacterCombatFacts Facts(StatusEffectSet statuses = null, CharacterTraitEffects effects = null) =>
            new(new EntityId(1), null, EntityKey.Unit_Akimbo, Owner.Player, null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(10, 20), default, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero },
                traitEffects: effects, statusEffects: statuses);

        // 아직 이전하지 않은 효과가 레거시 부분 갱신에서 보존되는지 확인하는 테스트 전용 값입니다.
        private sealed class ProbeStatus : AppliedStatus
        {
            public int Value { get; }
            public ProbeStatus(int value) : base(StatusEffectKey.DefenseIncrease, null) => Value = value;
            public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.BattleEnd;
            public override bool SameValues(AppliedStatus other) => other is ProbeStatus status && Value == status.Value;
        }
    }
}
