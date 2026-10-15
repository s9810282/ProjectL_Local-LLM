using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class GameFlowContractTests
    {
        [Test]
        public void StageEntryId_DefaultIsEmptyAndConstructorRejectsGuidEmpty()
        {
            Guid value = Guid.NewGuid();

            Assert.That(default(StageEntryId).IsEmpty, Is.True);
            Assert.That(StageEntryId.Empty.IsEmpty, Is.True);
            Assert.Throws<ArgumentException>(() => new StageEntryId(Guid.Empty));
            Assert.That(new StageEntryId(value), Is.EqualTo(new StageEntryId(value)));
        }

        [Test]
        public void StageStartSpec_CopiesInputListsBeforeExposingThem()
        {
            UnitSpec originalUnit = CreateUnitSpec();
            StructureSpec originalStructure = CreateStructureSpec();
            var units = new List<UnitSpec> { originalUnit };
            var structures = new List<StructureSpec> { originalStructure };

            var spec = new StageStartSpec(
                new StageKey(1),
                CreateStageConfig(),
                units,
                structures,
                CategoryMatchupSpec.CreateDefault());

            units[0] = null;
            structures[0] = null;

            Assert.That(spec.Units[0], Is.SameAs(originalUnit));
            Assert.That(spec.Structures[0], Is.SameAs(originalStructure));
        }

        [Test]
        public void StageStartSpec_RejectsInvalidCollections()
        {
            StageKey key = new StageKey(1);
            StageConfig config = CreateStageConfig();

            Assert.Throws<ArgumentException>(() =>
                new StageStartSpec(key, config, Array.Empty<UnitSpec>(), Array.Empty<StructureSpec>(), CategoryMatchupSpec.CreateDefault()));
            Assert.Throws<ArgumentException>(() =>
                new StageStartSpec(key, config, new UnitSpec[] { null }, Array.Empty<StructureSpec>(), CategoryMatchupSpec.CreateDefault()));
            Assert.Throws<ArgumentException>(() =>
                new StageStartSpec(key, config, new[] { CreateUnitSpec() }, new StructureSpec[] { null }, CategoryMatchupSpec.CreateDefault()));
        }

        [Test]
        public void CharacterDeckContracts_CopyEntriesAndPreserveStageProvenance()
        {
            var runId = new RunId(new Guid("11111111-1111-1111-1111-111111111111"));
            var entryId = new StageEntryId(new Guid("22222222-2222-2222-2222-222222222222"));
            var entries = new[] { new CharacterDeckEntry(EntityKey.Unit_Akimbo, 4) };

            var runSpec = new RunCharacterDeckSpec(entries, initialDrawCount: 3, drawBaseSeed: 17);
            var stageSpec = new StageDrawSpec(
                runId, entryId, deckRevision: 4, entries, initialDrawCount: 3, seed: 29);
            var snapshot = new RunCharacterDeckSnapshot(runId, deckRevision: 4, entries);
            entries[0] = new CharacterDeckEntry(EntityKey.Unit_Demon, 9);

            Assert.That(runSpec.Entries.Span[0].Key, Is.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(runSpec.InitialDrawCount, Is.EqualTo(3));
            Assert.That(runSpec.DrawBaseSeed, Is.EqualTo(17));
            Assert.That(stageSpec.RunId, Is.EqualTo(runId));
            Assert.That(stageSpec.EntryId, Is.EqualTo(entryId));
            Assert.That(stageSpec.DeckRevision, Is.EqualTo(4));
            Assert.That(stageSpec.Entries.Span[0].Count, Is.EqualTo(4));
            Assert.That(stageSpec.InitialDrawCount, Is.EqualTo(3));
            Assert.That(stageSpec.Seed, Is.EqualTo(29));
            Assert.That(snapshot.RunId, Is.EqualTo(runId));
            Assert.That(snapshot.DeckRevision, Is.EqualTo(4));
            Assert.That(snapshot.Entries.Span[0].Key, Is.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(snapshot.TotalCount, Is.EqualTo(4));
        }

        [Test]
        public void CharacterDeckContracts_RejectInvalidEntriesAndIdentity()
        {
            var runId = new RunId(new Guid("11111111-1111-1111-1111-111111111111"));
            var entryId = new StageEntryId(new Guid("22222222-2222-2222-2222-222222222222"));
            var entry = new CharacterDeckEntry(EntityKey.Unit_Akimbo, 1);

            Assert.Throws<ArgumentException>(() => new CharacterDeckEntry(EntityKey.None, 1));
            Assert.Throws<ArgumentNullException>(() => new RunCharacterDeckSpec(null, 0, 0));
            Assert.Throws<ArgumentException>(() => new RunCharacterDeckSpec(
                new[] { entry, entry }, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunCharacterDeckSpec(new[] { entry }, -1, 0));
            Assert.Throws<ArgumentException>(() =>
                new StageDrawSpec(RunId.Empty, entryId, 0, new[] { entry }, 0, 0));
            Assert.Throws<ArgumentException>(() =>
                new StageDrawSpec(runId, StageEntryId.Empty, 0, new[] { entry }, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StageDrawSpec(runId, entryId, -1, new[] { entry }, 0, 0));
            Assert.Throws<ArgumentNullException>(() =>
                new StageDrawSpec(runId, entryId, 0, null, 0, 0));
            Assert.Throws<ArgumentException>(() =>
                new RunCharacterDeckSnapshot(RunId.Empty, 0, new[] { entry }));
        }

        [Test]
        public void PlayerWalletSnapshot_RejectsNegativeBalance()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerWalletSnapshot(-1));
        }

        [TestCase(StageOutcome.Victory)]
        [TestCase(StageOutcome.Defeat)]
        public void CompletedStageSnapshot_RequiresTerminalStageAndNonemptyIdentifiers(StageOutcome outcome)
        {
            StageEntryId entryId = new StageEntryId(Guid.NewGuid());
            StageNodeId nodeId = new StageNodeId(1);
            StageKey stageKey = new StageKey(1);
            StageSnapshot terminalStage = CreateStageSnapshot(outcome);
            var completed = new CompletedStageSnapshot(entryId, nodeId, stageKey, terminalStage);

            Assert.That(completed.ExecutionKind, Is.EqualTo(StageExecutionKind.Combat));
            Assert.That(completed.FinalStage, Is.SameAs(terminalStage));
            Assert.That(completed.Recovery, Is.Null);
            Assert.That(completed.Outcome, Is.EqualTo(terminalStage.Outcome));

            Assert.Throws<ArgumentException>(() =>
                new CompletedStageSnapshot(StageEntryId.Empty, nodeId, stageKey, terminalStage));
            Assert.Throws<ArgumentException>(() =>
                new CompletedStageSnapshot(entryId, StageNodeId.Empty, stageKey, terminalStage));
            Assert.Throws<ArgumentException>(() =>
                new CompletedStageSnapshot(entryId, nodeId, StageKey.Empty, terminalStage));
            Assert.Throws<ArgumentNullException>(() =>
                new CompletedStageSnapshot(entryId, nodeId, stageKey, (StageSnapshot)null));
            Assert.Throws<ArgumentException>(() =>
                new CompletedStageSnapshot(entryId, nodeId, stageKey, CreateStageSnapshot(StageOutcome.InProgress)));
        }

        [Test]
        public void CompletedStageSnapshot_RecoveryHasExclusivePayloadAndNoCombatOutcome()
        {
            var entryId = new StageEntryId(Guid.NewGuid());
            var nodeId = new StageNodeId(1);
            var stageKey = new StageKey(1);
            var recovery = new RecoveryReceiptSnapshot(new RecoveryOptionId(2), 3, 5);
            var completed = new CompletedStageSnapshot(entryId, nodeId, stageKey, recovery);

            Assert.That(completed.ExecutionKind, Is.EqualTo(StageExecutionKind.NonCombat));
            Assert.That(completed.FinalStage, Is.Null);
            Assert.That(completed.Recovery, Is.SameAs(recovery));
            Assert.That(completed.Recovery.AppliedOptionId, Is.EqualTo(new RecoveryOptionId(2)));
            Assert.That(completed.Recovery.SpentAmount, Is.EqualTo(3));
            Assert.That(completed.Recovery.HealedAmount, Is.EqualTo(5));
            Assert.Throws<InvalidOperationException>(() => { _ = completed.Outcome; });
            Assert.Throws<ArgumentNullException>(() =>
                new CompletedStageSnapshot(entryId, nodeId, stageKey, (RecoveryReceiptSnapshot)null));
            Assert.Throws<ArgumentException>(() =>
                new CompletedStageSnapshot(StageEntryId.Empty, nodeId, stageKey, recovery));
            Assert.Throws<ArgumentException>(() =>
                new CompletedStageSnapshot(entryId, StageNodeId.Empty, stageKey, recovery));
            Assert.Throws<ArgumentException>(() =>
                new CompletedStageSnapshot(entryId, nodeId, StageKey.Empty, recovery));
        }

        [Test]
        public void RecoveryReceiptSnapshot_RejectsInvalidFactsWithoutEnforcingPurchasePolicy()
        {
            var optionId = new RecoveryOptionId(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecoveryOptionId(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecoveryOptionId(-1));
            Assert.Throws<ArgumentException>(() => new RecoveryReceiptSnapshot(default(RecoveryOptionId), 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecoveryReceiptSnapshot(optionId, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecoveryReceiptSnapshot(optionId, 0, -1));
            Assert.Throws<ArgumentException>(() => new RecoveryReceiptSnapshot(null, 1, 0));
            Assert.Throws<ArgumentException>(() => new RecoveryReceiptSnapshot(null, 0, 1));

            // Receipt facts do not decide whether a free or zero-effect choice is playable.
            Assert.DoesNotThrow(() => new RecoveryReceiptSnapshot(optionId, 0, 0));
        }

        [Test]
        public void GameFlowSnapshot_RejectsInvalidPhaseStageAndRunOutcomeCombinations()
        {
            StageMapSnapshot map = CreateStageMapSnapshot();
            PlayerWalletSnapshot wallet = new PlayerWalletSnapshot(0);
            StageEntryId entryId = new StageEntryId(Guid.NewGuid());
            StageNodeId nodeId = new StageNodeId(1);
            StageKey stageKey = new StageKey(1);
            StageSnapshot activeStage = CreateStageSnapshot(StageOutcome.InProgress);
            StageSnapshot terminalStage = CreateStageSnapshot(StageOutcome.Victory);
            var completed = new CompletedStageSnapshot(entryId, nodeId, stageKey, terminalStage);

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateFlow(
                (GameFlowPhase)999, map, null, null, null, null, null, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateFlow(
                GameFlowPhase.StageSelection, map, null, null, null, null, null, (RunOutcome)999, wallet));
            Assert.Throws<ArgumentNullException>(() => CreateFlow(
                GameFlowPhase.StageSelection, null, null, null, null, null, null, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentNullException>(() => CreateFlow(
                GameFlowPhase.StageSelection, map, null, null, null, null, null, RunOutcome.InProgress, null));
            Assert.Throws<ArgumentException>(() => CreateFlow(
                GameFlowPhase.StageActive, map, StageEntryId.Empty, nodeId, stageKey, activeStage, null, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentException>(() => CreateFlow(
                GameFlowPhase.StageSelection, map, entryId, nodeId, stageKey, activeStage, null, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentException>(() => CreateFlow(
                GameFlowPhase.StageActive, map, entryId, nodeId, stageKey, terminalStage, null, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentException>(() => CreateFlow(
                GameFlowPhase.StageResult, map, entryId, nodeId, stageKey, activeStage, null, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentException>(() => CreateFlow(
                GameFlowPhase.StageActive, map, entryId, nodeId, stageKey, activeStage, null, RunOutcome.Victory, wallet));
            Assert.Throws<ArgumentException>(() => CreateFlow(
                GameFlowPhase.RunEnded, map, null, null, null, null, completed, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentException>(() => CreateFlow(
                GameFlowPhase.StageActive, map, entryId, null, stageKey, activeStage, null, RunOutcome.InProgress, wallet));
        }

        [Test]
        public void GameFlowSnapshot_RejectsNonCombatPhase()
        {
            StageMapSnapshot map = CreateStageMapSnapshot();
            var wallet = new PlayerWalletSnapshot(0);

            Assert.Throws<ArgumentException>(() => CreateFlow(GameFlowPhase.NonCombatStageActive,
                map, null, null, null, null, null, RunOutcome.InProgress, wallet));
            Assert.Throws<ArgumentException>(() => CreateFlow(GameFlowPhase.NonCombatStageActive,
                map, new StageEntryId(Guid.NewGuid()), new StageNodeId(1), new StageKey(1),
                CreateStageSnapshot(StageOutcome.InProgress), null, RunOutcome.InProgress, wallet));
        }

        [Test]
        public void RunProgressSnapshot_NonCombatRequiresLiveInProgressStageIdentity()
        {
            var runId = new RunId(Guid.NewGuid());
            var entryId = new StageEntryId(Guid.NewGuid());
            var nodeId = new StageNodeId(1);
            var stageKey = new StageKey(1);

            RunProgressSnapshot Create(StageEntryId? entry, StageNodeId? node, StageKey? key,
                int health = 7, RunOutcome outcome = RunOutcome.InProgress) =>
                new RunProgressSnapshot(runId, 1, GameFlowPhase.NonCombatStageActive, outcome,
                    EntityKey.Unit_MainCharacter, new HealthStateSnapshot(health, 20),
                    entry, node, key, null, new PlayerWalletSnapshot(0), NonCombatStageKind.Recovery);

            RunProgressSnapshot active = Create(entryId, nodeId, stageKey);
            Assert.That(active.Phase, Is.EqualTo(GameFlowPhase.NonCombatStageActive));
            Assert.That(active.MainCharacterHealth, Is.EqualTo(new HealthStateSnapshot(7, 20)));

            for (int mask = 0; mask < 7; mask++)
            {
                Assert.Throws<ArgumentException>(() => Create(
                    (mask & 1) != 0 ? entryId : null,
                    (mask & 2) != 0 ? nodeId : null,
                    (mask & 4) != 0 ? stageKey : null), $"Missing identity: mask={mask}");
            }
            Assert.Throws<ArgumentException>(() => Create(entryId, nodeId, stageKey, health: 0));
            Assert.Throws<ArgumentException>(() => Create(entryId, nodeId, stageKey, outcome: RunOutcome.Victory));
            Assert.Throws<ArgumentException>(() => Create(entryId, nodeId, stageKey, health: 0, outcome: RunOutcome.Defeat));
        }

        [Test]
        public void RunProgressSnapshot_RetainsRecoveryWithoutReadingCombatOutcome()
        {
            var runId = new RunId(Guid.NewGuid());
            var entryId = new StageEntryId(Guid.NewGuid());
            var nodeId = new StageNodeId(1);
            var stageKey = new StageKey(1);
            var completed = new CompletedStageSnapshot(entryId, nodeId, stageKey,
                new RecoveryReceiptSnapshot(null, 0, 0));

            RunProgressSnapshot Create(GameFlowPhase phase, RunOutcome outcome, bool current, int health = 7) =>
                new RunProgressSnapshot(runId, 2, phase, outcome, EntityKey.Unit_MainCharacter,
                    new HealthStateSnapshot(health, 20), current ? entryId : null,
                    current ? nodeId : null, current ? stageKey : null, completed, new PlayerWalletSnapshot(0), phase == GameFlowPhase.NonCombatStageActive ? NonCombatStageKind.Recovery : (NonCombatStageKind?)null);

            foreach (GameFlowPhase phase in new[]
                { GameFlowPhase.StageSelection, GameFlowPhase.NonCombatStageActive, GameFlowPhase.StageActive })
            {
                RunProgressSnapshot snapshot = Create(phase, RunOutcome.InProgress, phase != GameFlowPhase.StageSelection);
                Assert.That(snapshot.LastCompletedStage, Is.SameAs(completed));
            }
            Assert.Throws<ArgumentException>(() => Create(GameFlowPhase.StageResult, RunOutcome.InProgress, true));
            Assert.Throws<ArgumentException>(() => Create(GameFlowPhase.RunEnded, RunOutcome.Victory, false));
            Assert.Throws<ArgumentException>(() => Create(GameFlowPhase.RunEnded, RunOutcome.Defeat, false, health: 0));
        }

        [Test]
        public void RunProgressSnapshot_PreservesCombatHandoffAndResultInvariants()
        {
            var runId = new RunId(Guid.NewGuid());
            var entryId = new StageEntryId(Guid.NewGuid());
            var nodeId = new StageNodeId(1);
            var stageKey = new StageKey(1);
            var completed = new CompletedStageSnapshot(entryId, nodeId, stageKey, CreateStageSnapshot(StageOutcome.Victory));

            RunProgressSnapshot Create(GameFlowPhase phase, RunOutcome outcome, int health,
                StageEntryId? entry, StageNodeId? node, StageKey? key, CompletedStageSnapshot receipt) =>
                new RunProgressSnapshot(runId, 2, phase, outcome, EntityKey.Unit_MainCharacter,
                    new HealthStateSnapshot(health, 20), entry, node, key, receipt, new PlayerWalletSnapshot(0), null);

            // A lethal combat action is readable before its completion handoff.
            Assert.DoesNotThrow(() => Create(GameFlowPhase.StageActive, RunOutcome.InProgress, 0,
                entryId, nodeId, stageKey, null));
            Assert.Throws<ArgumentException>(() => Create(GameFlowPhase.StageSelection, RunOutcome.InProgress, 0,
                null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => Create(GameFlowPhase.StageResult, RunOutcome.InProgress, 7,
                entryId, nodeId, stageKey, null));
            Assert.Throws<ArgumentException>(() => Create(GameFlowPhase.StageResult, RunOutcome.InProgress, 7,
                new StageEntryId(Guid.NewGuid()), nodeId, stageKey, completed));
            Assert.Throws<ArgumentException>(() => Create(GameFlowPhase.RunEnded, RunOutcome.Defeat, 0,
                null, null, null, completed));
        }

        [Test]
        public void GameFlowCommandResult_EnforcesSuccessAndFailureReasonConsistency()
        {
            GameFlowSnapshot flow = CreateFlow(
                GameFlowPhase.StageSelection,
                CreateStageMapSnapshot(),
                null,
                null,
                null,
                null,
                null,
                RunOutcome.InProgress,
                new PlayerWalletSnapshot(0));

            GameFlowCommandResult success = GameFlowCommandResult.Success(flow);
            GameFlowCommandResult failure = GameFlowCommandResult.Failure(
                GameFlowFailureReason.NodeNotFound,
                flow);

            Assert.That(success.Succeeded, Is.True);
            Assert.That(success.FailureReason, Is.EqualTo(GameFlowFailureReason.None));
            Assert.That(failure.Succeeded, Is.False);
            Assert.That(failure.FailureReason, Is.EqualTo(GameFlowFailureReason.NodeNotFound));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameFlowCommandResult.Failure(GameFlowFailureReason.None, flow));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameFlowCommandResult.Failure((GameFlowFailureReason)999, flow));
            Assert.Throws<ArgumentNullException>(() => GameFlowCommandResult.Success(null));
            Assert.Throws<ArgumentNullException>(() =>
                GameFlowCommandResult.Failure(GameFlowFailureReason.NodeNotFound, null));
        }

        private static GameFlowSnapshot CreateFlow(
            GameFlowPhase phase,
            StageMapSnapshot map,
            StageEntryId? currentEntryId,
            StageNodeId? currentNodeId,
            StageKey? currentStageKey,
            StageSnapshot currentStage,
            CompletedStageSnapshot lastCompletedStage,
            RunOutcome runOutcome,
            PlayerWalletSnapshot wallet)
        {
            return new GameFlowSnapshot(
                phase,
                map,
                currentEntryId,
                currentNodeId,
                currentStageKey,
                currentStage,
                lastCompletedStage,
                runOutcome,
                wallet);
        }

        private static StageConfig CreateStageConfig()
        {
            return new StageConfig(
                mapWidth: 7,
                mapHeight: 7,
                reserveCapacity: 4,
                playerDeploymentDepth: 2,
                enemyDeploymentDepth: 2);
        }

        private static UnitSpec CreateUnitSpec()
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                footprintWidth: 1,
                footprintHeight: 1,
                footprintLocalCells: new[] { CellCoord.Zero },
                maxHealth: 10,
                MoveMethod.LiftAndPlace,
                moveRange: 3,
                moveCost: 1,
                1,
                attackPower: 2,
                maxActionCost: 10,
                attackCost: 1);
        }

        private static StructureSpec CreateStructureSpec()
        {
            return new StructureSpec(
                EntityKey.Structure_Headquarters,
                footprintWidth: 1,
                footprintHeight: 1,
                footprintLocalCells: new[] { CellCoord.Zero },
                maxHealth: 10,
                isAttackable: false,
                attackPower: 0);
        }

        private static StageMapSnapshot CreateStageMapSnapshot()
        {
            StageNodeId startNodeId = new StageNodeId(1);
            StageNodeId targetNodeId = new StageNodeId(2);
            return new StageMapSnapshot(
                depthCount: 2,
                widthMax: 1,
                currentNodeId: startNodeId,
                isAtFinalNode: false,
                nodes: new[]
                {
                    new StageMapNodeSnapshot(
                        startNodeId,
                        StageKey.Empty,
                        StageExecutionKind.Combat,
                        depth: 0,
                        widthIndex: 0,
                        isStart: true,
                        isBoss: false,
                        isCurrent: true,
                        isSelectable: false,
                        nextNodeIds: new[] { targetNodeId }),
                    new StageMapNodeSnapshot(
                        targetNodeId,
                        new StageKey(1),
                        StageExecutionKind.Combat,
                        depth: 1,
                        widthIndex: 0,
                        isStart: false,
                        isBoss: true,
                        isCurrent: false,
                        isSelectable: true,
                        nextNodeIds: Array.Empty<StageNodeId>()),
                });
        }

        private static StageSnapshot CreateStageSnapshot(StageOutcome outcome)
        {
            StageOutcomeReason reason = outcome == StageOutcome.InProgress
                ? StageOutcomeReason.None
                : outcome == StageOutcome.Victory
                    ? StageOutcomeReason.EnemyHeadquartersDestroyed
                    : StageOutcomeReason.PlayerHeadquartersDestroyed;

            return new StageSnapshot(
                mapWidth: 7,
                mapHeight: 7,
                currentPhase: StagePhase.Deployment,
                entities: Array.Empty<EntitySnapshot>(),
                reserveSlots: Array.Empty<ReserveSlotSnapshot>(),
                combatTurnSnapshot: new CombatTurnSnapshot(false, null),
                outcome: new StageOutcomeSnapshot(outcome, reason),
                night: new StageNightSnapshot(1000, 0));
        }
    }
}
