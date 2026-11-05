using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;

namespace ProjectL.Tests.Core
{
    public sealed class WeaknessExposureLifecycleTests
    {
        [Test]
        public void SourceDeathAndSourceReturn_LeaveRecipientSourcesIntact()
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? HuntersMarkAttackTests.Expose(c, 20, 3, 2) : c.EntityId.Value == 2 ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            var result = HuntersMarkAttackTests.Run(input);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(HuntersMarkAttackTests.Exposures(TraitCombatFixtures.Final(result, input.ActorId.Value)).Single().RemainingAffectedSideTurnEnds, Is.EqualTo(3));
            var target = HuntersMarkAttackTests.Expose(input.Characters.Single(c => c.EntityId.Value == 2), 30, 3, input.ActorId.Value);
            var returned = CombatPlacementCalculator.Location(input.Characters.Single(c => c.EntityId == input.ActorId), CharacterRuntimeArea.Hand, 0, null, null);
            var world = input.Characters.ToDictionary(c => c.EntityId);
            world[target.EntityId] = target;
            world[returned.EntityId] = returned;
            TraitPresenceReconciler.Apply(input, world);
            Assert.That(HuntersMarkAttackTests.Exposures(world[target.EntityId]).Single().RemainingAffectedSideTurnEnds, Is.EqualTo(3));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TargetDeathAndFall_ClearExposure(bool fall, bool untimed)
        {
            var input = fall ? CliffFallTests.Knockback() : CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? HuntersMarkAttackTests.Expose(CombatCommandTestData.With(c, hp: fall ? 100 : 1)) : c).ToArray());
            if (untimed) input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new WeaknessExposureStatus(
                    new EffectOriginKey(EffectOwnerKind.Trait, input.ActorId.Value, (int)TraitKey.ReconSquad, "main"), 30,
                    WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath) })) : c).ToArray());
            if (fall) input = CliffFallTests.WithEnvironment(input, CliffFallTests.Ground(StaticTerrainKind.Cliff, cell: new CellCoord(5, 3)));
            var result = HuntersMarkAttackTests.Run(input);
            var target = TraitCombatFixtures.Final(result, 2);
            Assert.That(target.Health.IsDead, Is.True);
            Assert.That(HuntersMarkAttackTests.Exposures(target), Is.Empty);
            Assert.That(result.Events.OfType<CombatFallEvent>().Any(), Is.EqualTo(fall));
            Assert.That(HuntersMarkAttackTests.Exposures(input.Characters.Single(c => c.EntityId.Value == 2)), Has.Length.EqualTo(1));
        }

        [TestCase(1, false)]
        [TestCase(8, true)]
        public void EnemyEndAndNightRecall_UseAffectedClockThenClearBothReserves(long playerTurns, bool nightEnds)
        {
            var seed = CombatCommandTestData.Input();
            seed = CombatCommandTestData.With(seed, seed.Characters.Select(c => HuntersMarkAttackTests.Expose(c, 30, 3)).ToArray());
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1, completedPlayerTurnCount: playerTurns);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, true, seed.State.Structures, seed.State.ThornFields, turn);
            var input = new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, seed.ActorId, seed.Aim,
                state, seed.Characters, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(4, 1, 8, 8, Array.Empty<CharacterInstanceId>(), 0), processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: seed.ExpectedBattleInstanceId, expectedControllerTurnSequence: seed.ExpectedControllerTurnSequence, battleInstanceId: seed.BattleInstanceId);
            var result = CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            foreach (var character in CombatCommandComposition.Project(result).Characters)
            {
                bool recalled = nightEnds && character.OwnershipRole != CharacterOwnershipRole.Main;
                var status = HuntersMarkAttackTests.Exposures(character);
                if (recalled) Assert.That(status, Is.Empty, character.EntityId.ToString());
                else Assert.That(status.Single().RemainingAffectedSideTurnEnds, Is.EqualTo(character.Owner == Owner.Enemy ? 2 : 3));
            }
        }

        [Test]
        public void HandReturnClearsTemporaryDebuffs_WhileFieldRelocationPreservesThem()
        {
            var before = HuntersMarkAttackTests.Expose(CombatCommandTestData.Input().Characters[0])
                .With(debuffs: new CharacterDebuffState(new IntimidationDebuffState(20)));
            var moved = CombatPlacementCalculator.Location(before, CharacterRuntimeArea.Field, null, CellCoord.Right, before.Rotation);
            Assert.That(moved.StatusEffects.SameValues(before.StatusEffects), Is.True);
            var returned = CombatPlacementCalculator.Location(before, CharacterRuntimeArea.Hand, 0, null, null);
            Assert.That(HuntersMarkAttackTests.Exposures(returned), Is.Empty);
            Assert.That(returned.Debuffs.Intimidation, Is.Null);
            Assert.That(HuntersMarkAttackTests.Exposures(before), Has.Length.EqualTo(1));
        }
    }
}
