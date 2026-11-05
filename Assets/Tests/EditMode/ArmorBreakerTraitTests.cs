using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ArmorBreakerTraitTests
    {
        [Test]
        public void CurrentStrike_UsesOldDefense_NextStrikeUsesNewDefense()
        {
            var input = Input();
            var first = TraitCombatFixtures.Attack(input);
            Assert.That(first.Accepted, Is.True, first.Reason);
            var victim = TraitCombatFixtures.Final(first, 2);
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(960));
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(MovementTestData.After(first), victim), Is.EqualTo(10));
            var second = TraitCombatFixtures.Attack(CombatCommandTestData.NextPlayerTurn(MovementTestData.After(first)));
            Assert.That(second.Accepted, Is.True, second.Reason);
            victim = TraitCombatFixtures.Final(second, 2);
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(915));
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(MovementTestData.After(second), victim), Is.EqualTo(5));
            Assert.That(victim.Stats.PhysicalDefense, Is.EqualTo(20));
        }

        [Test]
        public void MultiHit_AppliesEachReductionBeforeNextIndependentStrike()
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 50)) : c).Append(
                    CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy).WithHealth(new HealthStateSnapshot(1, 100))).Append(
                    CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)
                        .With(stats: CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy).Stats.With(physicalDefense: 20))).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new ArmorBreakerTraitSpec(50), new KillChainAttackTraitSpec());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var victim = TraitCombatFixtures.Final(result, 3);
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(15));
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(MovementTestData.After(result), victim), Is.EqualTo(5));
            Assert.That(Stacks(victim).GetType().GetProperty("Stacks").GetValue(Stacks(victim)), Has.Count.EqualTo(2));
        }

        [Test]
        public void ExistingDefenseDebuff_IsNotOverwritten_AndCopiesPreserveArmor()
        {
            var input = Input();
            var timed = new DefenseReductionDebuffState(30, 2);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.With(debuffs: new CharacterDebuffState(defenseReduction: timed)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            var victim = TraitCombatFixtures.Final(result, 2);
            Assert.That(victim.Debuffs.DefenseReduction.SameValues(timed), Is.True);
            Assert.That(victim.Debuffs.WithRestrictions(null, null).SameValues(victim.Debuffs), Is.True);
            Assert.That(victim.WithHealth(victim.Health).Debuffs.SameValues(victim.Debuffs), Is.True);
            Assert.That(victim.Debuffs.SameValues(input.Characters.Single(c => c.EntityId.Value == 2).Debuffs), Is.False);
        }

        [Test]
        public void NightPreservesStageEndClears()
        {
            var result = TraitCombatFixtures.Attack(Input());
            var after = MovementTestData.After(result);
            var victim = after.Characters.Single(c => c.EntityId.Value == 2);
            var next = CombatCommandTestData.NextPlayerTurn(after);
            Assert.That(next.Characters.Single(c => c.EntityId.Value == 2).Debuffs.SameValues(victim.Debuffs), Is.True);
            var cleared = CombatTemporaryStateCleanup.Clear(victim);
            Assert.That(cleared.Debuffs.SameValues(CharacterDebuffState.Empty), Is.True);
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(cleared), Is.EqualTo(20));
        }

        [Test]
        public void PreviewAndRejectedAttack_DoNotInstallStacks()
        {
            var input = Input();
            var original = input.Characters.Single(c => c.EntityId.Value == 2);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(40));
            Assert.That(original.Debuffs.SameValues(CharacterDebuffState.Empty), Is.True);
            var rejected = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
        }

        [Test]
        public void MultipleRates_RoundOnlyFinalDerivedDefense()
        {
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.With(stats: c.Stats.With(physicalDefense: 3)) : c).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new ArmorBreakerTraitSpec(34));
            var first = TraitCombatFixtures.Attack(input);
            var second = TraitCombatFixtures.Attack(CombatCommandTestData.NextPlayerTurn(MovementTestData.After(first)));
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(TraitCombatFixtures.Final(second, 2)), Is.EqualTo(1));
        }

        internal static CombatCommandInput Input()
        {
            var input = TraitMovementAttackTests.SingleTarget();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 50)) : c.EntityId.Value == 2
                ? c.With(health: new HealthStateSnapshot(1000, 1000), stats: c.Stats.With(physicalDefense: 20)) : c).ToArray());
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new ArmorBreakerTraitSpec(50));
        }

        internal static object Stacks(CharacterCombatFacts facts)
        {
            var property = typeof(CharacterDebuffState).GetProperty("ArmorBreak");
            Assert.That(property, Is.Not.Null);
            var value = property.GetValue(facts.Debuffs);
            Assert.That(value, Is.Not.Null);
            return value;
        }
    }
}
