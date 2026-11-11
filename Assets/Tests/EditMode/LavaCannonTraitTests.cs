using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class LavaCannonTraitTests
    {
        [Test]
        public void PrimaryAndSplash_ExcludeOutsideFootprintAndCorpses()
        {
            var input = CombatCommandTestData.Input(category: CategoryKey.Cannon);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(6, EntityKey.Unit_Demon, new CellCoord(0, 0), Owner.Enemy)).ToArray());
            input = ThornHeartTestData.Replace(input, 3, c => c.WithHealth(new HealthStateSnapshot(1, 100)));
            var result = TraitCombatFixtures.Attack(Equip(input));
            Assert.That(result.Accepted, Is.True, result.Reason);
            foreach (int id in new[] { 2, 4 }) AssertStatuses(TraitCombatFixtures.Final(result, id));
            foreach (int id in new[] { 1, 3, 6, 10 })
                Assert.That(TraitCombatFixtures.Final(result, id).StatusEffects.Entries.OfType<FireStatus>(), Is.Empty);
        }

        [Test]
        public void ReflectionKillsCannon_UndeliveredCapturedEnemyStillGetsBothStatuses()
        {
            var input = TraitCombatFixtures.Equip(ThornHeartTestData.Input(splash: true, hp: 1), EntityKey.Unit_Akimbo,
                new LavaCannonTraitSpec(), new BleedingTraitSpec(2));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(100));
            AssertStatuses(TraitCombatFixtures.Final(result, 2));
            AssertStatuses(TraitCombatFixtures.Final(result, 3));
            Assert.That(BleedingTraitTests.Status(TraitCombatFixtures.Final(result, 2)).Stacks.Count, Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 3).StatusEffects.Entries.OfType<BleedingStatus>(), Is.Empty);
            Assert.That(TraitCombatFixtures.Final(result, 3).StatusEffects.Entries.OfType<FireStatus>().Single().Origin.Value.OwnerInstanceId, Is.EqualTo(1));
        }

        [Test]
        public void SacrificeOutsideFootprint_DoesNotReceiveAreaStatus()
        {
            var input = TraitCombatFixtures.Equip(SacrificeTraitTests.Input(), EntityKey.Unit_Akimbo,
                new LavaCannonTraitSpec(), new BleedingTraitSpec(2));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            AssertStatuses(TraitCombatFixtures.Final(result, 2));
            Assert.That(TraitCombatFixtures.Final(result, 5).StatusEffects.Entries.OfType<FireStatus>(), Is.Empty);
            Assert.That(BleedingTraitTests.Status(TraitCombatFixtures.Final(result, 5)).Stacks.Single().Damage, Is.EqualTo(2));
        }

        [Test]
        public void TwoCannons_RefreshLatestSourceWithoutStacking()
        {
            var input = TraitMovementAttackTests.SingleTarget();
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(6, EntityKey.Unit_Akimbo, new CellCoord(1, 2), Owner.Player, 20)).ToArray());
            input = Equip(input);
            var first = TraitCombatFixtures.Attack(input);
            var second = TraitCombatFixtures.Attack(CombatCommandTestData.FreshCommand(CombatCommandComposition.Project(first),
                CombatCommandKind.Attack, new EntityId(6), input.Aim));
            Assert.That(second.Accepted, Is.True, second.Reason);
            var recipient = TraitCombatFixtures.Final(second, 2);
            AssertStatuses(recipient);
            Assert.That(recipient.StatusEffects.Entries.OfType<FireStatus>().Single().Origin.Value.OwnerInstanceId, Is.EqualTo(6));
            Assert.That(TraitCombatFixtures.Final(first, 2).StatusEffects.Entries.OfType<FireStatus>().Single().Origin.Value.OwnerInstanceId, Is.EqualTo(1));
        }

        [Test]
        public void Knockback_PreservesCapturedTargetIdentity()
        {
            var input = Equip(CombatCommandTestData.Input());
            var profile = input.Profiles.Single(p => p.Definition.Key == EntityKey.Unit_Akimbo);
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p != profile ? p : new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new LavaCannonTraitSpec(), new KnockbackTraitSpec(1)), p.Method)).ToArray());
            input = ThornHeartTestData.Replace(input, 1, c => c.With(stats: c.Stats.With(traitStatuses:
                c.Stats.TraitStatuses.ToArray().Append(new KnockbackTraitStatusSnapshot(1)).ToArray())));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 4).Anchor, Is.Not.EqualTo(input.Characters.Single(c => c.EntityId.Value == 4).Anchor));
            foreach (int id in new[] { 2, 3, 4 }) AssertStatuses(TraitCombatFixtures.Final(result, id));
        }

        [Test]
        public void ShieldAbsorbedAndReactionContacts_StillApplyAreaStatuses()
        {
            var input = Equip(TraitMovementAttackTests.SingleTarget());
            input = PeriodicStatusTurnTests.WithStatus(input, 2, new StatusEffectSet(new AppliedStatus[]
            {
                new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Trait, 2, (int)TraitKey.PositionSwap, "shield"), 100, true, 0, Owner.Enemy)
            }));
            var result = CombatCommandTestData.Reaction(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(100));
            AssertStatuses(TraitCombatFixtures.Final(result, 2));
        }

        [Test]
        public void RejectedAttack_AppliesNothing()
        {
            var input = Equip(CombatCommandTestData.Input());
            var result = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Events, Is.Empty);
            Assert.That(input.Characters.All(c => c.StatusEffects.Entries.Count == 0), Is.True);
        }

        [Test]
        public void ExtraAttack_UsesItsOwnCapturedSingleTarget()
        {
            var input = TraitMovementAttackTests.SingleTarget();
            input = ThornHeartTestData.Replace(input, 2, c => c.WithHealth(new HealthStateSnapshot(1, 100)));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(7, 5), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LavaCannonTraitSpec(), new KillChainAttackTraitSpec());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.LessThan(100));
            AssertStatuses(TraitCombatFixtures.Final(result, 3));
        }

        [Test]
        public void ZeroPercentSplash_AppliesLavaWithoutBleeding()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 0);
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(method), EntityKey.Unit_Akimbo,
                new LavaCannonTraitSpec(), new BleedingTraitSpec(2));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var splash = TraitCombatFixtures.Final(result, 3);
            Assert.That(splash.Health.CurrentHealth, Is.EqualTo(100));
            AssertStatuses(splash);
            Assert.That(splash.StatusEffects.Entries.OfType<BleedingStatus>(), Is.Empty);
        }

        [Test]
        public void StructureOnlyAttack_DoesNotPublishRecipientStatuses()
        {
            var input = Equip(CombatCommandTestData.Input(empty: true));
            var structure = CellEnvironmentTestData.Structure(30, cells: new[] { new CellCoord(4, 3) });
            input = CombatCommandTestData.With(input, state: input.State.With(structures: new[] { structure }));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.TraitKey == TraitKey.LavaCannon), Is.False);
            Assert.That(CombatCommandComposition.Project(result).Characters.All(c => c.StatusEffects.Entries.Count == 0), Is.True);
        }

        private static CombatCommandInput Equip(CombatCommandInput input) => TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LavaCannonTraitSpec());

        internal static void AssertStatuses(CharacterCombatFacts target)
        {
            var fire = target.StatusEffects.Entries.OfType<FireStatus>().Single();
            Assert.That(fire.Damage, Is.EqualTo(2));
            Assert.That(fire.RemainingAffectedSideStarts, Is.EqualTo(2));
            Assert.That(target.StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>().Single().Magnitude, Is.EqualTo(1));
        }
    }
}
