using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class AppliedRestrictionStatusTests
    {
        private static readonly EffectOriginKey First = new(EffectOwnerKind.Trait, 1, (int)TraitKey.Vanguard, "grant");
        private static readonly EffectOriginKey Second = new(EffectOwnerKind.Trait, 2, (int)TraitKey.CleanSweep, "grant");

        [Test]
        public void AttackReduction_RetainsFirstUntilAttackCompletion_WithoutConsumingOtherEffects()
        {
            var statuses = AppliedStatusRules.Apply(StatusEffectSet.Empty, new AttackIncreaseStatus(First, 3, 2));
            statuses = AppliedStatusRules.Apply(statuses, new AttackDecreaseStatus(First, 20));
            statuses = AppliedStatusRules.Apply(statuses, new AttackDecreaseStatus(Second, 30));
            Assert.That(AppliedStatusRules.ApplyAttackDecrease(100, statuses), Is.EqualTo(80));
            Assert.That(statuses.Entries.OfType<AttackDecreaseStatus>().Single().Origin, Is.EqualTo(First));
            var ended = AppliedStatusRules.EndControllerTurn(statuses, Owner.Player, Owner.Enemy);
            Assert.That(AppliedStatusRules.ApplyAttackDecrease(100, ended), Is.EqualTo(80));
            var completed = AppliedStatusRules.CompleteAttack(ended);
            Assert.That(AppliedStatusRules.ApplyAttackDecrease(100, completed), Is.EqualTo(100));
            Assert.That(AppliedStatusRules.AttackIncrease(completed), Is.EqualTo(3));
            Assert.That(AppliedStatusRules.ApplyAttackDecrease(100, statuses), Is.EqualTo(80));
        }

        [TestCase(StatusEffectKey.Disarm, Owner.Player)]
        [TestCase(StatusEffectKey.Disarm, Owner.Enemy)]
        [TestCase(StatusEffectKey.Bind, Owner.Player)]
        [TestCase(StatusEffectKey.Bind, Owner.Enemy)]
        public void Restrictions_RefreshRatherThanAdd_AndOnlyTickAffectedSide(StatusEffectKey key, Owner side)
        {
            var statuses = AppliedStatusRules.Apply(StatusEffectSet.Empty, new ActionRestrictionStatus(key, First, 2));
            var opposite = side == Owner.Player ? Owner.Enemy : Owner.Player;
            Assert.That(AppliedStatusRules.EndControllerTurn(statuses, side, opposite), Is.SameAs(statuses));
            var once = AppliedStatusRules.EndControllerTurn(statuses, side, side);
            Assert.That(once.Entries.OfType<ActionRestrictionStatus>().Single().RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            var refreshed = AppliedStatusRules.Apply(once, new ActionRestrictionStatus(key, Second, 2));
            Assert.That(refreshed.Entries.Count, Is.EqualTo(1));
            Assert.That(refreshed.Entries.Single().Origin, Is.EqualTo(Second));
            Assert.That(AppliedStatusRules.IsRestricted(refreshed, key), Is.True);
            var expired = AppliedStatusRules.EndControllerTurn(AppliedStatusRules.EndControllerTurn(refreshed, side, side), side, side);
            Assert.That(AppliedStatusRules.IsRestricted(expired, key), Is.False);
            Assert.That(statuses.Entries.OfType<ActionRestrictionStatus>().Single().RemainingAffectedSideTurnEnds, Is.EqualTo(2));
        }

        [Test]
        public void DefenseStacks_KeepRepeatedSourcesAndRoundOnlyAfterAllFactors()
        {
            var statuses = AppliedStatusRules.Apply(StatusEffectSet.Empty, new DefenseDecreaseStatus(First, 20));
            statuses = AppliedStatusRules.Apply(statuses, new DefenseDecreaseStatus(First, 20));
            Assert.That(AppliedStatusRules.ApplyDefenseDecrease(100, statuses), Is.EqualTo(64));
            Assert.That(AppliedStatusRules.ApplyDefenseDecrease(2, statuses), Is.EqualTo(1));
            Assert.That(statuses.Entries.Count, Is.EqualTo(2));
            Assert.That(AppliedStatusRules.CompleteAttack(statuses), Is.SameAs(statuses));
        }

        [Test]
        public void CanonicalProjection_AllowsAnotherTraitSource_AndLegacyPartialUpdatePreservesOtherFamilies()
        {
            var statuses = new StatusEffectSet(new AppliedStatus[]
            {
                new AttackIncreaseStatus(First, 3, 2), new AttackDecreaseStatus(Second, 20),
                new ActionRestrictionStatus(StatusEffectKey.Disarm, First, 2),
                new ActionRestrictionStatus(StatusEffectKey.Bind, Second, 2), new DefenseDecreaseStatus(First, 20), new ProbeStatus()
            });
            var facts = Facts(statuses);
            Assert.That(facts.Debuffs.Disarm.Origin, Is.EqualTo(First));
            Assert.That(facts.Debuffs.AnkleBreaker.Origin, Is.EqualTo(Second));
            Assert.That(facts.Debuffs.ArmorBreak.Stacks.Single().Origin, Is.EqualTo(First));
            var changed = facts.With(debuffs: facts.Debuffs.WithRestrictions(null, facts.Debuffs.AnkleBreaker));
            Assert.That(changed.StatusEffects.Entries.OfType<ProbeStatus>().Count(), Is.EqualTo(1));
            Assert.That(changed.StatusEffects.Entries.OfType<AttackIncreaseStatus>().Count(), Is.EqualTo(1));
            Assert.That(changed.StatusEffects.Entries.OfType<DefenseDecreaseStatus>().Count(), Is.EqualTo(1));
            Assert.That(changed.TraitEffects, Is.SameAs(facts.TraitEffects));
            Assert.That(changed.Debuffs.Disarm, Is.Null);
            Assert.That(facts.Debuffs.Disarm, Is.Not.Null);
            Assert.That(facts.WithHealth(new HealthStateSnapshot(9, 20)).Debuffs, Is.SameAs(facts.Debuffs));
            Assert.That(facts.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()).StatusEffects, Is.SameAs(statuses));
        }

        [Test]
        public void ConflictingExplicitLegacyDebuffs_AreRejectedBeforeConstruction()
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] { new AttackDecreaseStatus(First, 20) });
            Assert.Throws<ArgumentException>(() => Facts(statuses, new CharacterDebuffState(new IntimidationDebuffState(30))));
            var synthetic = Facts(null, new CharacterDebuffState(new IntimidationDebuffState(20)));
            Assert.That(synthetic.StatusEffects.Entries.OfType<AttackDecreaseStatus>().Single().Origin, Is.Null);
            Assert.Throws<ArgumentException>(() => AppliedStatusRules.Apply(StatusEffectSet.Empty, new AttackDecreaseStatus(null, 20)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActionRestrictionStatus(StatusEffectKey.AttackIncrease, First, 2));
        }

        [TestCase(TraitKey.Vanguard)]
        [TestCase(TraitKey.CleanSweep)]
        public void ActualAttackCompletion_AttributesConsumptionToTheGrantingTrait(TraitKey producer)
        {
            var input = CombatCommandTestData.Input();
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, input.Characters[0].EntityId.Value, (int)producer, "attack-decrease");
            var actor = input.Characters[0].WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new AttackDecreaseStatus(origin, 20) }));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[actor.EntityId] = actor;
            var events = new System.Collections.Generic.List<CombatEvent>();
            CombatDebuffApplication.CompleteAttack(input, actor, Array.Empty<EntityId>(), projected, events);
            Assert.That(projected[actor.EntityId].StatusEffects.Entries, Is.Empty);
            Assert.That(actor.StatusEffects.Entries.Count, Is.EqualTo(1));
            var consumed = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Single();
            Assert.That(consumed.TraitKey, Is.EqualTo(producer));
            Assert.That(consumed.TraitActivationKind, Is.EqualTo(TraitActivationKind.StatusConsumed));
        }

        [Test]
        public void NonTraitProvenance_ClearsAppliedEffectsWithoutInventingTraitActivations()
        {
            var input = CombatCommandTestData.Input();
            var origin = new EffectOriginKey(EffectOwnerKind.Item, input.Characters[0].EntityId.Value, 999, "status");
            var actor = input.Characters[0].WithStatusEffects(new StatusEffectSet(new AppliedStatus[]
            {
                new AttackDecreaseStatus(origin, 20), new DefenseDecreaseStatus(origin, 20),
                new ActionRestrictionStatus(StatusEffectKey.Disarm, origin, 2), new ShieldStatus(origin, 10, true, 0)
            }));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[actor.EntityId] = actor;
            var events = new System.Collections.Generic.List<CombatEvent>();
            CombatDebuffApplication.CompleteAttack(input, actor, Array.Empty<EntityId>(), projected, events);
            Assert.That(events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits), Is.Empty);
            Assert.That(projected[actor.EntityId].StatusEffects.Entries.OfType<AttackDecreaseStatus>(), Is.Empty);
            Assert.That(events.OfType<CombatTraitStateEvent>().Count(), Is.EqualTo(1));

            events.Clear();
            var cleared = CombatTemporaryStateCleanup.Clear(actor, events, ItemEventReason.Death);
            Assert.That(cleared.StatusEffects.Entries, Is.Empty);
            Assert.That(events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits), Is.Empty);
            Assert.That(events.OfType<CombatTraitStateEvent>().Count(), Is.EqualTo(1));
            Assert.That(actor.StatusEffects.Entries.Count, Is.EqualTo(4));
        }

        private static CharacterCombatFacts Facts(StatusEffectSet statuses, CharacterDebuffState debuffs = null) =>
            new(new EntityId(9), null, EntityKey.Unit_Akimbo, Owner.Player, null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(10, 20), default, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero },
                debuffs: debuffs, statusEffects: statuses);

        private sealed class ProbeStatus : AppliedStatus
        {
            public ProbeStatus() : base(StatusEffectKey.WeaknessExposure, null) { }
            public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.BattleEnd;
            public override bool SameValues(AppliedStatus other) => other is ProbeStatus;
        }
    }
}
