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

namespace ProjectL.Tests.Core
{
    public sealed class BleedingTraitTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LevelDamage_AppliesToPrimaryAndSplash(int damage)
        {
            var input = Equip(CombatCommandTestData.Input(), damage);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            foreach (int id in new[] { 2, 3, 4 })
            {
                var status = Status(TraitCombatFixtures.Final(result, id));
                Assert.That(status.Stacks.Single().Damage, Is.EqualTo(damage));
                Assert.That(status.Stacks.Single().Origin.OwnerInstanceId, Is.EqualTo(1));
                Assert.That(status.RemainingAffectedSideStarts, Is.EqualTo(3));
            }
            Assert.That(input.Characters.All(c => !c.StatusEffects.Entries.OfType<BleedingStatus>().Any()), Is.True);
        }

        [Test]
        public void ShieldAbsorbedContactStillApplies_DeadTargetDoesNot()
        {
            var input = Equip(CombatCommandTestData.Input());
            var shield = new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.PositionSwap, "shield"),
                100, true, 0, Owner.Enemy);
            input = PeriodicStatusTurnTests.WithStatus(input, 2, new StatusEffectSet(new AppliedStatus[] { shield }));
            input = ThornHeartTestData.Replace(input, 3, c => c.WithHealth(new HealthStateSnapshot(1, 100)));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Status(TraitCombatFixtures.Final(result, 2)).Stacks.Count, Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 3).StatusEffects.Entries.OfType<BleedingStatus>(), Is.Empty);
        }

        [Test]
        public void Sacrifice_AppliesToActualRecipient()
        {
            var result = TraitCombatFixtures.Attack(Equip(SacrificeTraitTests.Input(), 2));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).StatusEffects.Entries.OfType<BleedingStatus>(), Is.Empty);
            Assert.That(Status(TraitCombatFixtures.Final(result, 5)).Stacks.Single().Damage, Is.EqualTo(2));
        }

        [Test]
        public void ReactionContacts_RefreshAtCapAndKeepApplicationTimeDamage()
        {
            var input = Equip(TraitMovementAttackTests.SingleTarget());
            input = ThornHeartTestData.Replace(input, 2, c => c.WithHealth(new HealthStateSnapshot(1000, 1000)));
            var first = TraitCombatFixtures.Attack(input);
            var original = Status(TraitCombatFixtures.Final(first, 2));
            input = Equip(CombatCommandComposition.Project(first), 3);
            for (int i = 0; i < 6; i++)
            {
                var result = CombatCommandTestData.Reaction(input, Array.Empty<IMovementTileRule>());
                Assert.That(result.Accepted, Is.True, result.Reason);
                input = CombatCommandComposition.Project(result);
            }
            var capped = Status(input.Characters.Single(c => c.EntityId.Value == 2));
            Assert.That(capped.Stacks.Select(s => s.Damage), Is.EqualTo(new[] { 1, 3, 3, 3, 3 }));
            Assert.That(capped.RemainingAffectedSideStarts, Is.EqualTo(3));
            Assert.That(original.Stacks.Single().Damage, Is.EqualTo(1));
        }

        [Test]
        public void ExtraAttack_AddsAStackForEachRealContact()
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy).WithHealth(new HealthStateSnapshot(1, 100))).Append(
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new BleedingTraitSpec(2), new KillChainAttackTraitSpec());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Status(TraitCombatFixtures.Final(result, 3)).Stacks.Select(s => s.Damage), Is.EqualTo(new[] { 2, 2 }));
        }

        [Test]
        public void ReflectionAndPeriodicDamage_DoNotApplyContactTraits()
        {
            var input = Equip(ThornHeartTestData.Input(splash: true, hp: 10));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new BleedingTraitSpec(3));
            var attack = TraitCombatFixtures.Attack(input);
            Assert.That(TraitCombatFixtures.Final(attack, 1).StatusEffects.Entries.OfType<BleedingStatus>(), Is.Empty);
            input = CombatCommandComposition.Project(attack);
            var tick = PeriodicStatusTurnTests.Start(input, Owner.Player);
            Assert.That(TraitCombatFixtures.Final(tick, 1).StatusEffects.Entries.OfType<BleedingStatus>(), Is.Empty);
            Assert.That(Status(TraitCombatFixtures.Final(tick, 2)).Stacks.Count, Is.EqualTo(1));
        }

        [Test]
        public void PreviewAndRejectedAttack_DoNotMutateCanonicalState()
        {
            var input = Equip(CombatCommandTestData.Input());
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            var result = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(input.Characters.All(c => c.StatusEffects.Entries.Count == 0), Is.True);
        }

        internal static CombatCommandInput Equip(CombatCommandInput input, int damage = 1) =>
            TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new BleedingTraitSpec(damage));
        internal static BleedingStatus Status(CharacterCombatFacts character) => character.StatusEffects.Entries.OfType<BleedingStatus>().Single();
    }
}
