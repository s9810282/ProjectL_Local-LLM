using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using static ProjectL.Tests.Core.MainDeckInitialPlacementTests;

namespace ProjectL.Tests.Core
{
    public sealed class CombatRewardParticipationTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void FirstAcceptedStart_CapturesOnlyDeployedOwnedAllies_MainThenStableIds(int deployedSubs)
        {
            var input = Deployed(deployedSubs);
            var expected = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field)
                .OrderBy(c => c.OwnershipRole == CharacterOwnershipRole.Main ? 0 : 1)
                .ThenBy(c => c.OwnedId.Value).Select(c => c.OwnedId.Value).ToArray();
            var reversed = CombatCommandTestData.With(input, characters: input.Characters.Reverse().ToArray());
            var result = Calculate(reversed, CombatCommandKind.EnterCombat);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Rewards.ParticipantsCaptured, Is.True);
            Assert.That(result.FinalState.Rewards.ClearParticipantIds, Is.EqualTo(expected));
            Assert.That(result.FinalState.Rewards.ClearParticipantIds.Count, Is.EqualTo(1 + deployedSubs));
            Assert.That(input.State.Rewards, Is.SameAs(CombatRewardFacts.Empty));
            Assert.That(result.FinalState.Rewards.KillGoldEarned, Is.Zero);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(6)]
        public void ExplicitCustomHandSize_RemainsMeaningful_AndCohortDoesNotTruncateAtFour(int size)
        {
            var (initialization, roster) = Setup(6);
            var prepared = Prepare(initialization, roster, new InitialDrawPlan(roster.Skip(1).Select(c => c.Id).ToArray(), size - 1));
            Assert.That(prepared.Characters.Count, Is.EqualTo(size));
            var input = Input(initialization, roster, prepared);
            input = After(input, Calculate(input, CombatCommandKind.PlaceMainCharacter, CellCoord.Zero));
            int x = 1;
            foreach (var sub in input.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Sub).ToArray())
                input = After(input, Calculate(input, CombatCommandKind.PlaceUnit, new CellCoord(x++, 0), sub.EntityId));
            var result = Calculate(input, CombatCommandKind.EnterCombat);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Rewards.ClearParticipantIds.Count, Is.EqualTo(size));
        }

        [Test]
        public void RejectedMainPlacementOrStart_CannotFreezeCohort()
        {
            var (initialization, roster) = Setup(3);
            var input = Input(initialization, roster, Prepare(initialization, roster, Draw(roster)));
            var rejected = Calculate(input, CombatCommandKind.EnterCombat);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.FinalState.Rewards, Is.SameAs(CombatRewardFacts.Empty));
            input = After(input, Calculate(input, CombatCommandKind.PlaceMainCharacter, CellCoord.Zero));
            var later = Calculate(input, CombatCommandKind.EnterCombat);
            Assert.That(later.Accepted, Is.True, later.Reason);
            Assert.That(later.FinalState.Rewards.ClearParticipantIds, Is.EqualTo(new[] { roster[0].Id }));
        }

        [Test]
        public void RejectedRedeployment_DoesNotInstallProposedCapture()
        {
            var seed = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat);
            var main = CombatCommandTestData.With(seed.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main), position: CellCoord.Zero);
            var cell = new CellCoord(0, 1);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, cell, Owner.Enemy);
            var waiting = CombatPlacementCalculator.Location(enemy, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var hq = Headquarters(cell);
            var turn = new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, cell, Rotation4.R0) });
            var state = new CombatCommandState(1,2,CombatFlowPhase.PlayerInput,false,                new[] { hq },Array.Empty<ThornFieldCellSnapshot>(),turn);
            var input = Copy(seed, state, new[] { main, waiting }, 1, 1);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("EnemyRedeploymentBlocked"));
            Assert.That(result.FinalState.Rewards, Is.SameAs(CombatRewardFacts.Empty));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalEntryMine_UsesPreEffectCohort_EvenWhenParticipantDies(bool killMain)
        {
            var seed = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat);
            var cell = new CellCoord(2, 7);
            var main = seed.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            if (killMain) main = CombatCommandTestData.With(main, position: new CellCoord(2, 6));
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, cell, Owner.Enemy);
            var waiting = CombatPlacementCalculator.Location(enemy, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var turn = new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, cell, Rotation4.R0) });
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                Array.Empty<StructureCombatFacts>(),Array.Empty<ThornFieldCellSnapshot>(),turn,                new MovementEnvironmentFacts(new[] { MineEnvironmentTests.Mine(cell: cell, damage: 100) }),                objective: BattleObjective.DesignatedBoss(enemy.EntityId));
            var input = Copy(seed, state, new[] { main, waiting }, 2, 2, ObjectArrivalOrderTests.Bindings(11));
            input = WithClearRewards(input);
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(killMain ? StageOutcome.Defeat : StageOutcome.Victory));
            Assert.That(result.FinalState.Rewards.ClearParticipantIds, Is.EqualTo(new[] { main.OwnedId.Value }));
            Assert.That(result.FinalState.Rewards.ParticipantsCaptured, Is.True);
            Assert.That(result.FinalState.Rewards.KillGoldEarned, Is.Zero);
            Assert.That(result.Characters.Owned.Count, Is.EqualTo(killMain ? 0 : 1));
            if (killMain) Assert.That(result.RewardChange, Is.Null);
            else
            {
                Assert.That(result.Characters.Owned.Single().Final.Experience, Is.EqualTo(100));
                Assert.That((result.RewardChange.FinalGold, result.RewardChange.FinalFame), Is.EqualTo((100, 50)));
            }
        }

        [Test]
        public void DeploymentMineVictoryBeforeStart_LeavesCohortUncaptured()
        {
            var seed = CombatCommonCommandTests.Command(CombatCommandKind.PlaceUnit);
            var sub = seed.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Sub);
            var hand = CombatPlacementCalculator.Location(sub, CharacterRuntimeArea.Hand, 0, null, null);
            var main = seed.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var cell = new CellCoord(2, 0);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy);
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                Array.Empty<StructureCombatFacts>(),Array.Empty<ThornFieldCellSnapshot>(),                new CombatTurnFacts(StagePhase.Deployment),                new MovementEnvironmentFacts(new[] { MineEnvironmentTests.Mine(cell: cell, damage: 100) }),                objective: BattleObjective.DesignatedBoss(enemy.EntityId));
            var input = new CombatCommandInput(1, CombatCommandKind.PlaceUnit, hand.EntityId, default, state,
                new[] { hand, main, enemy }, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls,
                seed.Artifacts, cell, battle: new CombatBattleFacts(10, 1, 2, 2, Array.Empty<CharacterInstanceId>(), 0),
                environmentBindings: ObjectArrivalOrderTests.Bindings(11),
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
            input = WithClearRewards(input);
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.FinalState.Rewards, Is.SameAs(CombatRewardFacts.Empty));
        }

        [Test]
        public void CapturedFacts_SurviveTurnMovementFinishAndCompositionCopies()
        {
            var input = Deployed(1);
            var entered = Calculate(input, CombatCommandKind.EnterCombat);
            var captured = entered.FinalState.Rewards;
            var next = CombatCommandComposition.Project(entered);
            Assert.That(CombatCommandComposition.Intent(next, CombatCommandKind.EnterCombat).State.Rewards, Is.SameAs(captured));
            Assert.That(CombatTurnCalculator.State(next.State, next.State.Turn, next.State.Phase).Rewards, Is.SameAs(captured));
            var projection = MovementProjection.ForArrival(next, next.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            Assert.That(projection.CurrentInput().State.Rewards, Is.SameAs(captured));
            var finished = projection.Complete();
            Assert.That(finished.FinalState.Rewards, Is.SameAs(captured));
            Assert.That(CombatCommandComposition.Merge(input, new[] { entered, finished }).FinalState.Rewards, Is.SameAs(captured));
            var late = CombatCommandTestData.Character(40, EntityKey.Unit_Akimbo, new CellCoord(4, 0), Owner.Player);
            var dead = next.Characters.Select(c => c.OwnershipRole == CharacterOwnershipRole.Sub && c.Area == CharacterRuntimeArea.Field
                ? CombatCommandTestData.With(c, hp: 0) : c).Append(late).ToArray();
            Assert.That(CombatParticipationCalculator.Capture(captured, dead), Is.SameAs(captured));
        }

        [Test]
        public void CaptureFiltersDeadAndHand_AndPreservesAccounting()
        {
            var input = Deployed(2);
            var deadId = input.Characters.First(c => c.OwnershipRole == CharacterOwnershipRole.Sub && c.Area == CharacterRuntimeArea.Field).EntityId;
            var characters = input.Characters.Select(c => c.EntityId == deadId ? CombatCommandTestData.With(c, hp: 0) : c).ToArray();
            var facts = CombatParticipationCalculator.Capture(new CombatRewardFacts(false, Array.Empty<CharacterInstanceId>(), 7), characters);
            Assert.That(facts.ClearParticipantIds.Count, Is.EqualTo(2));
            Assert.That(facts.KillGoldEarned, Is.EqualTo(7));
        }

        [Test]
        public void FactsCopyAndValueComparison_RejectInvalidIdentityAndMutation()
        {
            var ids = new[] { new CharacterInstanceId(1), new CharacterInstanceId(2) };
            var facts = new CombatRewardFacts(true, ids, 10);
            ids[1] = new CharacterInstanceId(3);
            Assert.That(facts.ClearParticipantIds[1].Value, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterInstanceId>)facts.ClearParticipantIds)[0] = ids[1]);
            Assert.That(facts.SameValues(new CombatRewardFacts(true, new[] { new CharacterInstanceId(1), new CharacterInstanceId(2) }, 10)), Is.True);
            Assert.That(facts.SameValues(CombatRewardFacts.Empty), Is.False);
            Assert.Throws<ArgumentException>(() => new CombatRewardFacts(false, ids, 0));
            Assert.Throws<ArgumentException>(() => new CombatRewardFacts(true, new[] { ids[0], ids[0] }, 0));
            Assert.Throws<ArgumentException>(() => new CombatRewardFacts(true, new[] { default(CharacterInstanceId) }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatRewardFacts(false, Array.Empty<CharacterInstanceId>(), -1));
            var state = Deployed(0).State;
            var changed = new CombatCommandState(state.BoardWidth,state.BoardHeight,state.Phase,                state.IsNight,state.Structures,state.ThornFields,state.Turn,state.Environment,                state.EnvironmentProgress,state.WallBlocking,state.Objective,state.Allocation,state.ItemRandomState,facts);
            Assert.That(state.SameValues(changed), Is.False);
        }

        internal static CombatCommandInput Deployed(int subs)
        {
            var (initialization, roster) = Setup(6);
            var input = Input(initialization, roster, Prepare(initialization, roster, Draw(roster)));
            input = After(input, Calculate(input, CombatCommandKind.PlaceMainCharacter, CellCoord.Zero));
            int x = 1;
            foreach (var sub in input.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Sub).Take(subs).ToArray())
                input = After(input, Calculate(input, CombatCommandKind.PlaceUnit, new CellCoord(x++, 0), sub.EntityId));
            return input;
        }

        private static CombatCommandInput WithClearRewards(CombatCommandInput s) =>
            new(s.Number, s.Kind, s.ActorId, s.Aim, s.State, s.Characters, s.Profiles, s.Main, s.Owned, s.SoulCounts,
                s.Souls, s.Artifacts, s.TargetCell, s.Rotation,
                battle: new CombatBattleFacts(10, 1, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                    progression: new LevelProgressionSpec(150, 100, 3, 1, 1), clearRewards: new StageClearRewardSpec(100, 50, 100)),
                environmentBindings: s.EnvironmentBindings, goldBalance: 0, fameBalance: 0,
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId);

        private static StructureCombatFacts Headquarters(CellCoord cell) => new(new EntityId(20),
            new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
            new HealthStateSnapshot(100, 100), cell, Rotation4.R0, new[] { cell }, Owner.Enemy);

        private static CombatCommandInput Copy(CombatCommandInput seed, CombatCommandState state,
            CharacterCombatFacts[] characters, int playerDepth, int enemyDepth, EnvironmentSemanticBindings bindings = null) =>
            new(1, CombatCommandKind.EnterCombat, EntityId.Empty, default, state, characters, seed.Profiles,
                seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(10, 1, playerDepth, enemyDepth, Array.Empty<CharacterInstanceId>(), 0),
                environmentBindings: bindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
    }
}
