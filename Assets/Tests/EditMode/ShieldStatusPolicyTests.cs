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
    public sealed class ShieldStatusPolicyTests
    {
        private static readonly EffectOriginKey First = new(EffectOwnerKind.Trait, 1, (int)TraitKey.Vanguard, "shield");
        private static readonly EffectOriginKey Second = new(EffectOwnerKind.Trait, 2, (int)TraitKey.CleanSweep, "shield");

        [TestCase(40, 0, 10)]
        [TestCase(60, 10, 0)]
        public void Absorption_UsesExistingSourceOrder_AndPreservesUnrelatedEffects(int incoming, int hpRequest, int budget)
        {
            var current = new StatusEffectSet(new AppliedStatus[]
            {
                new ShieldStatus(Second, 30, true, 0), new AttackIncreaseStatus(First, 3, 2), new ShieldStatus(First, 20, true, 0)
            });
            var after = ShieldStatusRules.Absorb(current, incoming, out int remaining);
            Assert.That(remaining, Is.EqualTo(hpRequest));
            Assert.That(after.Entries.OfType<ShieldStatus>().Sum(s => s.RemainingBudget), Is.EqualTo(budget));
            Assert.That(after.Entries.OfType<ShieldStatus>().Single(s => s.Origin == First).RemainingBudget, Is.Zero);
            Assert.That(AppliedStatusRules.AttackIncrease(after), Is.EqualTo(3));
            Assert.That(current.Entries.OfType<ShieldStatus>().Sum(s => s.RemainingBudget), Is.EqualTo(50));
        }

        [Test]
        public void EligibilityChanges_DoNotRefill_AndOnlyDueResetChangesBudget()
        {
            var spent = new ShieldStatus(First, 0, true, 0);
            var departed = ShieldStatusRules.Reconcile(spent, First, 30, false, 1, 2, false);
            var returned = ShieldStatusRules.Reconcile(departed, First, 30, true, 1, 2, false);
            Assert.That(returned.RemainingBudget, Is.Zero);
            Assert.That(ShieldStatusRules.Reconcile(returned, First, 30, true, 2, 2, false).RemainingBudget, Is.Zero);
            var reset = ShieldStatusRules.Reconcile(returned, First, 30, true, 2, 2, true);
            Assert.That(reset.RemainingBudget, Is.EqualTo(30));
            Assert.That(reset.LastResetControllerTurnCount, Is.EqualTo(2));
            Assert.That(ShieldStatusRules.Reconcile(reset, First, 99, true, 2, 2, true).RemainingBudget, Is.EqualTo(30));
            Assert.Throws<InvalidOperationException>(() => ShieldStatusRules.Reconcile(reset, First, 30, true, 0, 2, true));
        }

        [Test]
        public void SuppressedSources_RemainStoredWithoutAbsorptionOrAnotherSourcesRemoval()
        {
            var current = new StatusEffectSet(new AppliedStatus[] { new ShieldStatus(First, 30, false, 0), new ShieldStatus(Second, 20, true, 0) });
            var after = ShieldStatusRules.Absorb(current, 25, out int remaining);
            Assert.That(remaining, Is.EqualTo(5));
            Assert.That(after.Entries.OfType<ShieldStatus>().Single(s => s.Origin == First).RemainingBudget, Is.EqualTo(30));
            Assert.That(after.Entries.OfType<ShieldStatus>().Single(s => s.Origin == Second).RemainingBudget, Is.Zero);
        }

        [Test]
        public void CanonicalShieldProjectsLegacyValues_AndPartialTraitUpdateKeepsDebuffs()
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] { new ShieldStatus(First, 30, true, 2), new AttackDecreaseStatus(Second, 20) });
            var facts = Facts(statuses);
            Assert.That(facts.TraitEffects.Shields.Single().Origin, Is.EqualTo(First));
            Assert.That(facts.TraitEffects.Shields.Single().RemainingBudget, Is.EqualTo(30));
            var copied = facts.With(traitEffects: new CharacterTraitEffects(Array.Empty<TraitAttackGrant>(), facts.TraitEffects.Shields, 5));
            Assert.That(copied.StatusEffects.SameValues(statuses), Is.True);
            Assert.That(copied.Debuffs, Is.SameAs(facts.Debuffs));
            Assert.That(copied.TraitEffects.CohesionDefenseBonus, Is.EqualTo(5));
            var absorbed = facts.WithStatusEffects(ShieldStatusRules.Absorb(statuses, 10, out _));
            Assert.That(absorbed.TraitEffects.ActiveShieldTotal, Is.EqualTo(20));
            Assert.That(facts.TraitEffects.ActiveShieldTotal, Is.EqualTo(30));
        }

        [Test]
        public void UnrelatedPresenceReconciliation_DoesNotDeleteAnotherProducersShield()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters[0].WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new ShieldStatus(First, 30, true, 0) }));
            var projected = input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray();
            var changes = TraitPresenceReconciler.Calculate(input, projected);
            var after = changes.Characters.SingleOrDefault(c => c.Final.EntityId == actor.EntityId)?.Final ?? actor;
            Assert.That(after.StatusEffects.SameValues(actor.StatusEffects), Is.True);
            Assert.That(after.TraitEffects.ActiveShieldTotal, Is.EqualTo(30));
        }

        [Test]
        public void CanonicalShield_RequestsOrderedDamageWithoutProducerTraitLookup()
        {
            var actor = CombatCommandTestData.Input().Characters[0].WithStatusEffects(
                new StatusEffectSet(new AppliedStatus[] { new ShieldStatus(First, 30, true, 0) }));
            Assert.That(TraitPresenceReconciler.RequiresOrderedDamage(new[] { actor }), Is.True);
        }

        [TestCase(EffectOwnerKind.Item, 999)]
        [TestCase(EffectOwnerKind.Trait, (int)TraitKey.Vanguard)]
        public void ActualDamage_ConsumesCanonicalShieldAndPreservesItsSourceKind(EffectOwnerKind kind, int definition)
        {
            var input = CombatCommandTestData.Input();
            var original = input.Characters[0];
            var origin = new EffectOriginKey(kind, original.EntityId.Value, definition, "shield");
            var actor = original.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new ShieldStatus(origin, 10, true, 0) }));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[actor.EntityId] = actor;
            var events = new System.Collections.Generic.List<CombatEvent>();
            CombatDamageApplication.For(input, projected, events).Apply(actor.EntityId, CombatDamageKind.EnvironmentDamage,
                new CombatDamageOrigin(CellCoord.Zero, new EntityId(2), 1), 15);

            Assert.That(projected[actor.EntityId].Health.CurrentHealth, Is.EqualTo(actor.Health.CurrentHealth - 5));
            Assert.That(projected[actor.EntityId].TraitEffects.ActiveShieldTotal, Is.Zero);
            Assert.That(actor.TraitEffects.ActiveShieldTotal, Is.EqualTo(10));
            Assert.That(events.OfType<CombatTraitStateEvent>().Count(), Is.EqualTo(1));
            var activations = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).ToArray();
            if (kind == EffectOwnerKind.Trait)
            {
                Assert.That(activations.Single().TraitKey, Is.EqualTo((TraitKey)definition));
                Assert.That(activations.Single().TraitActivationKind, Is.EqualTo(TraitActivationKind.ShieldAbsorbed));
                Assert.That(activations.Single().Magnitude, Is.EqualTo(10));
            }
            else Assert.That(activations, Is.Empty);
        }

        private static CharacterCombatFacts Facts(StatusEffectSet statuses) =>
            new(new EntityId(9), null, EntityKey.Unit_Akimbo, Owner.Player, null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(10, 20), default, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, statusEffects: statuses);
    }
}
