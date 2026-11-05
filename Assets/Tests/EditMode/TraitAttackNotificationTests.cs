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
    public sealed class TraitAttackNotificationTests
    {
        [Test]
        public void FirstAttack_VanguardExcludesOwner()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new VanguardTraitSpec(3, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 1).Stats.TraitAttackBonus, Is.Zero);
            Assert.That(TraitCombatFixtures.Final(result, 10).Stats.TraitAttackBonus, Is.EqualTo(3));
            Assert.That(TraitCombatFixtures.Final(result, 10).Stats.AttackPower, Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 10).TemporaryAttackBonus, Is.Zero);
        }

        [Test]
        public void AoERecipients_CountOneAction()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_MainCharacter, new CleanSweepTraitSpec(3, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Attack.Value.Hits.Length, Is.GreaterThan(1));
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 10).Stats.TraitAttackBonus, Is.EqualTo(3));
        }

        [Test]
        public void KillChainExtraAttacks_EachCountAsAction()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method);
            var characters = input.Characters.Select(c => c.Owner == Owner.Enemy ? c.WithHealth(new HealthStateSnapshot(10, 100)) : c)
                .Append(CombatCommandTestData.Character(9, EntityKey.Unit_Demon, new CellCoord(7, 0), Owner.Enemy)).ToArray();
            input = TraitCombatFixtures.Characters(input, characters);
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new KillChainAttackTraitSpec());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_MainCharacter, new CleanSweepTraitSpec(3, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.GreaterThan(1));
            // 처치 연쇄 추가 공격도 한 번씩 공격 행동으로 세고, 깔끔한 뒷정리는 그때마다 쌓인다(2026-11-02 결정).
            int extra = TraitActivationOutcomeTests.Activations(result.Events, TraitKey.KillChainAttack).Count();
            Assert.That(extra, Is.GreaterThan(0));
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(1 + extra));
            Assert.That(TraitCombatFixtures.Final(result, 10).Stats.TraitAttackBonus, Is.EqualTo(3 * (1 + extra)));
        }

        [Test]
        public void TwoOrdinaryActors_HaveDistinctActionBoundaries()
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy).WithHealth(new HealthStateSnapshot(10, 100)),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy) }).ToArray());
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(5, EntityKey.Unit_Akimbo, new CellCoord(3, 3), Owner.Player, 20)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_MainCharacter, new CleanSweepTraitSpec(3, 1));
            var first = TraitCombatFixtures.Attack(input);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(first.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(1));
            var result = TraitCombatFixtures.Attack(MovementTestData.After(first).WithActor(new EntityId(5)));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(2));
            Assert.That(TraitCombatFixtures.Final(result, 10).Stats.TraitAttackBonus, Is.EqualTo(6));
        }

        [Test]
        public void CleanSweep_OnlyOtherLivingFieldAllies()
        {
            var input = CombatCommandTestData.Input();
            var field = CombatCommandTestData.Character(5, EntityKey.Unit_Shielder, new CellCoord(0, 0), Owner.Player);
            var hand = CombatCommandTestData.Character(6, EntityKey.Unit_Shielder, CellCoord.Zero, Owner.Player)
                .WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>());
            var dead = CombatCommandTestData.Character(7, EntityKey.Unit_Shielder, CellCoord.Zero, Owner.Player)
                .WithHealth(new HealthStateSnapshot(0, 100)).WithPlacement(CharacterRuntimeArea.Excluded, null, null, null, Array.Empty<CellCoord>());
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { field, hand, dead }).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Shielder, 1), input.Profiles[0].Method)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new CleanSweepTraitSpec(3, 1));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new CleanSweepTraitSpec(3, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(TraitCombatFixtures.Final(result, 1).Stats.TraitAttackBonus, Is.Zero);
            Assert.That(TraitCombatFixtures.Final(result, 5).Stats.TraitAttackBonus, Is.EqualTo(3));
            Assert.That(TraitCombatFixtures.Final(result, 6).Stats.TraitAttackBonus, Is.Zero);
            Assert.That(TraitCombatFixtures.Final(result, 7).Stats.TraitAttackBonus, Is.Zero);
        }

        [Test]
        public void PreviewAndRejectedAttack_DoNotChangeOriginalGrantsOrCounts()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new VanguardTraitSpec(3, 1));
            var original = input.Characters.ToArray();
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            var rejected = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(input.State.TraitTurn.SameValues(TraitTurnFacts.Empty), Is.True);
            for (int i = 0; i < original.Length; i++) Assert.That(input.Characters[i].SameValues(original[i]), Is.True);
        }
    }
}
