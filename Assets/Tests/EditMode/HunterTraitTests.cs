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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class HunterTraitTests
    {
        [Test]
        public void EnterCombat_HunterTie_PublishesTargetAndAdvancedRng()
        {
            var input = Input();
            var enemies = input.Characters.Where(c => c.EntityId.Value == 2 || c.EntityId.Value == 3).ToArray();
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.Owner == Owner.Player)
                .Concat(enemies.Select(c => c.WithHealth(new HealthStateSnapshot(100, 100))
                    .WithPlacement(CharacterRuntimeArea.EnemyWaiting, null, null, null, Array.Empty<CellCoord>()))).ToArray());
            var state = input.State.With(phase: CombatFlowPhase.PlayerInput,isNight: false,itemRandomState: 123,                allocation: new CombatAllocationFacts(50, 17),                turn: new CombatTurnFacts(StagePhase.Deployment, preparationIndex: 2, mainPlacementLocked: true,
                    controllerTurnSequence: 16, completedPlayerTurnCount: 8,
                    enemyOrigins: enemies.Select(c => new CombatEnemyOrigin(c.EntityId, c.Anchor.Value, c.Rotation.Value)).ToArray(),
                    completedControllerTurnCount: 16));
            input = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat, input, state);
            Assert.That(Target(Actor(input)), Is.Null);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Target(TraitCombatFixtures.Final(result, 1)), Is.EqualTo(new EntityId(2)));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(1015568748u));
            Assert.That(input.State.EnvironmentProgress.RandomState, Is.EqualTo(1u));
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingPlayer));
            Assert.That(result.FinalState.Turn.PreparationIndex, Is.EqualTo(2));
            Assert.That(result.FinalState.Turn.NightStartPlayerTurnCount, Is.EqualTo(8));
            Assert.That(result.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(16));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            Assert.That(result.FinalState.IsNight, Is.True);
            Assert.That(result.FinalState.Allocation.SameValues(state.Allocation), Is.True);
            Assert.That(result.FinalState.ItemRandomState, Is.EqualTo(123u));
            var begun = CombatTurnCalculator.BeginControllerTurn(CombatCommandComposition.Project(result));
            Assert.That(begun.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(1015568748u));
            Assert.That(Target(TraitCombatFixtures.Final(begun, 1)), Is.EqualTo(new EntityId(2)));
        }

        [Test]
        public void HighestCurrentHp_TieUsesCommandRngAndStableIds()
        {
            var input = Input();
            var before = input.State.EnvironmentProgress;
            var first = CombatCommandTestData.NextPlayerTurn(input);
            var second = CombatCommandTestData.NextPlayerTurn(CombatCommandTestData.With(input, characters: input.Characters.Reverse().ToArray()));
            Assert.That(Target(Actor(first)), Is.EqualTo(Target(Actor(second))));
            Assert.That(Target(Actor(first)).HasValue, Is.True);
            Assert.That(first.State.EnvironmentProgress.RandomState, Is.Not.EqualTo(before.RandomState));
            Assert.That(second.State.EnvironmentProgress.SameValues(first.State.EnvironmentProgress), Is.True);
            Assert.That(input.State.EnvironmentProgress.SameValues(before), Is.True);
        }

        [Test]
        public void HpChange_DoesNotRetarget()
        {
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(2, 100)) : c).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            Assert.That(Target(Actor(input)), Is.EqualTo(new EntityId(2)));
            var random = input.State.EnvironmentProgress;
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c.EntityId.Value == 3
                ? c.WithHealth(new HealthStateSnapshot(100, 100)) : c).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            Assert.That(Target(Actor(input)), Is.EqualTo(new EntityId(2)));
            Assert.That(input.State.EnvironmentProgress.SameValues(random), Is.True);
        }

        [Test]
        public void OtherKiller_RetargetsWithoutRewardOrCap()
        {
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(2, 100)) : c).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(5, EntityKey.Unit_Cowboy, new CellCoord(3, 3), Owner.Player, 200)).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Cowboy, 200), input.Profiles[0].Method)).ToArray());
            var result = TraitCombatFixtures.Attack(CombatCommandTestData.FreshCommand(input, CombatCommandKind.Attack,
                new EntityId(5), AttackAim.ForCell(new CellCoord(4, 3))));
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = MovementTestData.After(result);
            Assert.That(Rewards(Actor(final)), Is.Zero);
            Assert.That(Bonus(final), Is.Zero);
            Assert.That(Target(Actor(final)), Is.Not.EqualTo(new EntityId(2)));
        }

        [Test]
        public void ThreeRewards_StopMarkingAndHoldRunSix()
        {
            var input = CombatCommandTestData.NextPlayerTurn(Input());
            for (int count = 1; count <= 3; count++)
            {
                var target = input.Characters.Single(c => c.EntityId == Target(Actor(input)));
                var result = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(target.Anchor.Value)));
                Assert.That(result.Accepted, Is.True, result.Reason);
                input = MovementTestData.After(result);
                Assert.That(Rewards(Actor(input)), Is.EqualTo(count));
                Assert.That(Bonus(input), Is.EqualTo(count * 2));
                Assert.That(Actor(input).Stats.TraitAttackBonus, Is.EqualTo(count * 2));
                if (count < 3) input = CombatCommandTestData.NextPlayerTurn(input);
            }
            Assert.That(Target(Actor(input)), Is.Null);
            var next = CombatCommandTestData.NextPlayerTurn(input);
            Assert.That(Target(Actor(next)), Is.Null);
            Assert.That(Rewards(Actor(next)), Is.EqualTo(3));
            Assert.That(Bonus(next), Is.EqualTo(6));
        }

        [Test]
        public void AoE_DoesNotRetargetBetweenVictims()
        {
            var input = Input(splash: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 6).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(9, EntityKey.Unit_Demon, new CellCoord(7, 0), Owner.Enemy)).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = MovementTestData.After(result);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(3));
            Assert.That(Rewards(Actor(final)), Is.EqualTo(1));
            Assert.That(Bonus(final), Is.EqualTo(2));
            Assert.That(Target(Actor(final)), Is.EqualTo(new EntityId(9)));
        }

        [Test]
        public void PreviewAndRejectedAttack_DoNotCommitSelectionRngOrAwards()
        {
            var input = Input(splash: true);
            var state = input.State;
            var original = Actor(input);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            Assert.That(input.State.SameValues(state), Is.True);
            Assert.That(Actor(input).SameValues(original), Is.True);
            Assert.That(Target(Actor(input)), Is.Null);
            Assert.That(Bonus(input), Is.Zero);
            var rejected = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Owned, Is.Empty);
        }

        [Test]
        public void InitiallyEmptyEnemyField_SelectsWhenAnEnemyAppears()
        {
            var input = Input();
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.Owner == Owner.Player).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            Assert.That(Target(Actor(input)), Is.Null);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            Assert.That(Target(Actor(input)), Is.EqualTo(new EntityId(2)));
        }

        [Test]
        public void LaterRepeatedStrike_SeesRunAwardAndMoraleWithoutReusingTarget()
        {
            var input = Input();
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.Owner == Owner.Player || c.EntityId.Value == 2 || c.EntityId.Value == 3)
                .Select(c => c.EntityId.Value == 2 ? c.With(health: new HealthStateSnapshot(26, 100), stats: c.Stats.With(attackPower: 13))
                    : c.EntityId.Value == 3 ? c.WithHealth(new HealthStateSnapshot(25, 100)) : c).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new HunterTraitSpec(2, 3), new AccelerationTraitSpec(2),
                new MoraleBoostTraitSpec(20, 1), new KillChainAttackTraitSpec());
            input = CombatCommandTestData.NextPlayerTurn(TraitMovementAttackTests.WithState(input, 1, 6, false));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(CombatTraitTestFixtures.Activations(result, TraitKey.KillChainAttack).Single().Magnitude, Is.EqualTo(31));
            var final = MovementTestData.After(result);
            Assert.That(Bonus(final), Is.EqualTo(2));
            Assert.That(Rewards(Actor(final)), Is.EqualTo(1));
            Assert.That(Actor(final).Stats.TraitAttackBonus, Is.EqualTo(5));
            Assert.That(TraitMovementAttackTests.Held(Actor(final)), Is.Zero);
        }

        [Test]
        public void CreditedKnockbackFall_AwardsAtTheSameKillBoundary()
        {
            var input = Input();
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.Owner == Owner.Player || c.EntityId.Value == 2)
                .Select(c => c.EntityId.Value == 2 ? c.WithHealth(new HealthStateSnapshot(100, 100)) : c.EntityId.Value == 1
                    ? c.With(stats: c.Stats.With(attackPower: 1, traitStatuses: c.Stats.TraitStatuses.ToArray().Append(new KnockbackTraitStatusSnapshot(1)).ToArray())) : c).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 1, CategoryKey.Main,
                    new HunterTraitSpec(2, 3), new KnockbackTraitSpec(1)), p.Method) : p).ToArray());
            input = TraitTrailTestData.Environment(input, new StaticTerrainFact(70, new CellCoord(5, 3), StaticTerrainKind.Cliff, true,
                new[] { Owner.Player, Owner.Enemy }));
            input = CombatCommandTestData.NextPlayerTurn(input);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatFallEvent>().Count(), Is.EqualTo(1));
            var final = MovementTestData.After(result);
            Assert.That(Bonus(final), Is.EqualTo(2));
            Assert.That(Rewards(Actor(final)), Is.EqualTo(1));
        }

        [TestCase(false, 100, 100, 51)]
        [TestCase(false, 102, 50, 26)]
        [TestCase(true, 101, 100, 36)]
        public void DirectDelivery_LaterHitUsesImmediateRunAward(bool piercing, int attack, int splashPercent, int secondHealth)
        {
            var input = Input(splash: !piercing);
            var method = new TargetingMethodSpec(piercing ? TargetingShape.EightDirectionLine : TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, piercing ? TargetingSplash.None : TargetingSplash.Radial, piercing ? 0 : splashPercent);
            var traits = piercing ? new TraitSpec[] { new HunterTraitSpec(2, 3), new PiercingFalloffTraitSpec(30, int.MaxValue) }
                : new TraitSpec[] { new HunterTraitSpec(2, 3) };
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.Owner == Owner.Player || c.EntityId.Value == 2 || c.EntityId.Value == 3)
                .Select(c => c.EntityId.Value == 1 ? c.With(stats: c.Stats.With(attackPower: attack,
                    traitStatuses: piercing ? c.Stats.TraitStatuses.ToArray().Append(new PiercingFalloffTraitStatusSnapshot(30)).ToArray()
                        : c.Stats.TraitStatuses.ToArray()))
                    : c.EntityId.Value == 2 ? c.WithHealth(new HealthStateSnapshot(100, 100))
                    : c.EntityId.Value == 3 ? c.With(health: new HealthStateSnapshot(secondHealth, 100), stats: c.Stats.With(physicalDefense: 50))
                        .WithPlacement(CharacterRuntimeArea.Field, null, piercing ? new CellCoord(5, 3) : new CellCoord(4, 4),
                            Rotation4.R0, new[] { piercing ? new CellCoord(5, 3) : new CellCoord(4, 4) }) : c).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, attack, CategoryKey.Main, traits), method) : p).ToArray());
            input = CombatCommandTestData.NextPlayerTurn(input);
            Assert.That(Target(Actor(input)), Is.EqualTo(new EntityId(2)));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.IsDead, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(3)).DamageDealt, Is.EqualTo(secondHealth));
            var final = MovementTestData.After(result);
            Assert.That(Bonus(final), Is.EqualTo(2));
            Assert.That(Rewards(Actor(final)), Is.EqualTo(1));
        }

        internal static EntityId? Target(CharacterCombatFacts actor) => (EntityId?)Value(actor, "TargetId");
        internal static int Rewards(CharacterCombatFacts actor) => (int)Value(actor, "SuccessfulRewards");
        private static object Value(CharacterCombatFacts actor, string property)
        {
            var status = actor.Stats.TraitStatuses.ToArray().Single(t => t.Key == TraitKey.Hunter);
            var field = status.GetType().GetProperty(property);
            Assert.That(field, Is.Not.Null, property);
            return field.GetValue(status);
        }
        internal static CharacterCombatFacts Actor(CombatCommandInput input) => input.Characters.Single(c => c.EntityId.Value == 1);
        internal static int Bonus(CombatCommandInput input) => input.Owned.Single(o => o.Id == Actor(input).OwnedId).HunterAttackBonus;
        internal static CombatCommandInput Input(bool splash = false)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single,
                splash ? TargetingSplash.Radial : TargetingSplash.None, splash ? 70 : 0);
            var input = CombatCommandTestData.Input(method);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.Owner == Owner.Enemy
                ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c).Append(
                    CombatCommandTestData.Character(6, EntityKey.Unit_Demon, new CellCoord(5, 4), Owner.Enemy)
                        .WithHealth(new HealthStateSnapshot(1, 100))).ToArray());
            input = CombatCommandTestData.With(input, state: input.State.With(structures: new[] {
                new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                    new[] { CellCoord.Zero }, 1000, false, 0), new HealthStateSnapshot(1000, 1000), new CellCoord(0, 7), Rotation4.R0,
                    new[] { new CellCoord(0, 7) }, Owner.Enemy) }));
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new HunterTraitSpec(2, 3));
        }
    }
}
