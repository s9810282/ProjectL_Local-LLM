using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class AttackBoundaryLocalityTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CompletionRunsOnceAcrossDeliveryBranches(bool killChain)
        {
            var input = HunterTraitTests.Input();
            var traits = new TraitSpec[] { new HunterTraitSpec(2, 3), new AccelerationTraitSpec(2) };
            if (killChain) traits = traits.Append(new KillChainAttackTraitSpec()).ToArray();
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, traits);
            input = TraitCombatFixtures.Characters(input, input.Characters
                .Where(c => c.Owner == Owner.Player || c.EntityId.Value == 2 || c.EntityId.Value == 3)
                .Select(c => c.EntityId.Value == 2 ? c.WithHealth(new HealthStateSnapshot(26, 100)) : c).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            input = ThornHeartTestData.Replace(input, 3, c => c.WithHealth(new HealthStateSnapshot(100, 100)));
            input = TraitMovementAttackTests.WithState(input, 1, 6, false);
            var before = HunterTraitTests.Actor(input);

            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(26));
            Assert.That(HunterTraitTests.Target(before), Is.EqualTo(new EntityId(2)));
            Assert.That(TraitMovementAttackTests.Held(before), Is.EqualTo(6));
            Assert.That(HunterTraitTests.Bonus(input), Is.Zero);

            var result = TraitCombatFixtures.Attack(input);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Activations(result, TraitKey.Acceleration).Single(a => a.TraitActivationKind ==
                TraitActivationKind.StatusConsumed).Magnitude, Is.EqualTo(6));
            Assert.That(Activations(result, TraitKey.Hunter).Single(a => a.TraitActivationKind ==
                TraitActivationKind.HunterTargetSelected).TargetId, Is.EqualTo(new EntityId(3)));
            Assert.That(Activations(result, TraitKey.KillChainAttack).Count(), Is.EqualTo(killChain ? 1 : 0));
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(killChain ? 72 : 100));
            Assert.That(TraitMovementAttackTests.Held(TraitCombatFixtures.Final(result, 1)), Is.Zero);
        }

        [Test]
        public void LethalReflection_StillFinishesAttackScope()
        {
            var input = TraitCombatFixtures.Equip(ThornHeartTestData.Input(splash: true, hp: 1),
                EntityKey.Unit_Akimbo, new AccelerationTraitSpec(2));
            input = TraitMovementAttackTests.WithState(input, 1, 6, false);
            var result = TraitCombatFixtures.Attack(input);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Count(e => e.Reflection != null), Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Activations(result, TraitKey.Acceleration).Single(a => a.TraitActivationKind ==
                TraitActivationKind.StatusConsumed).Magnitude, Is.EqualTo(6));
        }

        [Test]
        public void ContactPreservesOriginalAndSacrificeRecipient()
        {
            var input = TraitCombatFixtures.Equip(SacrificeTraitTests.Input(), EntityKey.Unit_Akimbo,
                new DisarmTraitSpec(1), new ArmorBreakerTraitSpec(10));
            var profile = CombatCommandCalculator.RequiredProfile(input, input.Characters.Single(c => c.EntityId == input.ActorId));
            var traits = profile.Definition.Traits.ToArray().Append(new HuntersMarkTraitSpec(20, 1)).ToArray();
            var markedProfile = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20,
                CategoryKey.Main, traits), profile.Method);
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p == profile ? markedProfile : p).ToArray());
            input = ThornHeartTestData.Replace(input, 1, c => c.With(stats: c.Stats.With(traitStatuses:
                c.Stats.TraitStatuses.ToArray().Append(new HuntersMarkTraitStatusSnapshot(20, 1)).ToArray())));
            var result = TraitCombatFixtures.Attack(input);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var hit = result.Attack.Value.Hits.ToArray().Single();
            Assert.That(result.Attack.Value.PrimaryTargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(hit.Contacts.Single().OriginalTargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(hit.TargetId, Is.EqualTo(new EntityId(5)));

            var contactKeys = new[] { TraitKey.HuntersMark, TraitKey.Disarm, TraitKey.ArmorBreaker };
            var contactEvents = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(a => contactKeys.Contains(a.TraitKey)).ToArray();
            Assert.That(contactEvents.Select(a => a.TraitKey), Is.EqualTo(contactKeys));
            Assert.That(contactEvents.All(a => a.TargetId == hit.TargetId &&
                a.ResultOrigin?.AttackGroup == hit.Contacts.Single().Origin.AttackGroup), Is.True);

            var original = TraitCombatFixtures.Final(result, 2);
            var recipient = TraitCombatFixtures.Final(result, 5);
            Assert.That(original.Debuffs.Disarm, Is.Null);
            Assert.That(recipient.Debuffs.Disarm, Is.Not.Null);
            Assert.That(recipient.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Count(), Is.EqualTo(1));
        }

        [TestCase(TraitKey.ArmorBreaker, true)]
        [TestCase(TraitKey.FinishingShot, true)]
        [TestCase(TraitKey.TrackingShot, true)]
        [TestCase(TraitKey.Hunter, true)]
        [TestCase(TraitKey.MoraleBoost, true)]
        [TestCase(TraitKey.Acceleration, false)]
        [TestCase(TraitKey.Disarm, false)]
        [TestCase(TraitKey.None, false)]
        public void OrderedCapabilityMatchesExistingTraits(TraitKey key, bool requiresOrderedHits)
        {
            var trait = key switch
            {
                TraitKey.ArmorBreaker => (TraitSpec)new ArmorBreakerTraitSpec(10),
                TraitKey.FinishingShot => new FinishingShotTraitSpec(50),
                TraitKey.TrackingShot => new TrackingShotTraitSpec(50, 10),
                TraitKey.Hunter => new HunterTraitSpec(2, 3),
                TraitKey.MoraleBoost => new MoraleBoostTraitSpec(20, 1),
                TraitKey.Acceleration => new AccelerationTraitSpec(2),
                TraitKey.Disarm => new DisarmTraitSpec(1),
                _ => null
            };
            var input = CombatCommandTestData.Input();
            if (trait != null) input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, trait);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var bindings = CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor));

            Assert.That(new CombatEffectEngine().RequiresOrderedHits(bindings), Is.EqualTo(requiresOrderedHits));
        }

        private static IEnumerable<TraitActivationSnapshot> Activations(
            CombatCommandTransition result, TraitKey key) => result.Events.OfType<CombatEffectsEvent>()
                .SelectMany(e => e.Traits).Where(a => a.TraitKey == key);
    }
}
