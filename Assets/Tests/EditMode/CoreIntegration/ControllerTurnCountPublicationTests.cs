using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ControllerTurnCountPublicationTests
    {
        [Test]
        public void RealProgressCountsEachSideOnce_PreservesAcrossDayAndNewNight()
        {
            using var battle = Create();
            var b = battle.Buffers;
            Assert.That(Count(battle), Is.Zero);
            for (int turn = 1; turn <= 8; turn++)
            {
                battle.Submit(battle.EndPlayerTurn());
                Assert.That(Count(battle), Is.EqualTo(turn * 2 - 1));
                Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
                Assert.That(Count(battle), Is.EqualTo(turn * 2 - 1));
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                Assert.That(Count(battle), Is.EqualTo(turn * 2 - 1), "적 시작은 종료가 아니다.");
                battle.AdvanceToPlayerOrDay();
                Assert.That(Count(battle), Is.EqualTo(turn * 2));
                Assert.That(b.CombatResultHistoryReadSink.Results.SelectMany(r => r.Events).OfType<CombatGimmickEvent>(), Is.Empty);
            }
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
            battle.Submit(CombatViewCommandData.EnterCombat());
            Assert.That(Count(battle), Is.EqualTo(16));
            battle.AdvanceToPlayerOrDay();
            Assert.That(Count(battle), Is.EqualTo(16));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(Count(battle), Is.EqualTo(17));
            b.BattleWriteSink.EndBattle();
            using var fresh = Create();
            Assert.That(Count(fresh), Is.Zero);
        }

        [TestCase(false, 0)]
        [TestCase(false, 2)]
        [TestCase(true, 1)]
        [TestCase(true, 3)]
        public void WrongEndDeltaRejectsAtomically_ThenValidEndAppliesAndCannotReplay(bool enemy, long wrongCount)
        {
            using var battle = Create();
            var b = battle.Buffers;
            if (enemy)
            {
                battle.Submit(battle.EndPlayerTurn());
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            }
            var parts = battle.Publication();
            CombatCommandInput input;
            if (enemy)
                input = parts.EnemyStep();
            else
            {
                int number = b.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
                input = parts.Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.EndPlayerTurn, default, default,
                    expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                    expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            }
            var valid = enemy ? CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>())
                : CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(valid.Accepted, Is.True, valid.Reason);
            Assert.That(valid.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(enemy ? 2 : 1));
            var t = valid.FinalState.Turn;
            var wrong = new CombatTurnFacts(t.StagePhase, t.PreparationIndex, t.MainPlacementLocked, t.ControllerPhase,
                t.ControllerTurnSequence, t.CompletedPlayerTurnCount, t.NightStartPlayerTurnCount, t.EnemyOrigins, t.Summons, wrongCount);
            var corrupt = new CombatCommandTransition(input, true, "", valid.Characters, valid.Field,
                valid.FinalState.With(turn: wrong), valid.Outcome, events: valid.Events);
            var snapshot = new CoreIntegrationSnapshot(b);
            Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(corrupt)).Message,
                Does.Contain("누적값"));
            snapshot.AssertUnchanged(b);
            b.BattleWriteSink.Apply(valid);
            Assert.That(Count(battle), Is.EqualTo(enemy ? 2 : 1));
            snapshot = new CoreIntegrationSnapshot(b);
            if (enemy)
                Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(valid));
            else
                b.BattleWriteSink.Apply(valid);
            snapshot.AssertUnchanged(b);
        }

        [Test]
        public void ExistingAssemblerAndEveryInputCopyKeepImmutableRailwaySettings()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RailwaySource());
            var input = battle.Publication().EnemyStep();
            var current = battle.Buffers.CombatStateReadSink.Current;
            Assert.That(input.Battle.RailCells, Is.EqualTo(current.RailCells));
            Assert.That(input.Battle.GimmickDefinitions, Is.EqualTo(current.StageGimmicks));
            Assert.That(input.Battle.RailCells, Is.EqualTo(new[] { new CellCoord(0, 0) }));
            var railway = input.Battle.GimmickDefinitions.OfType<RailwayGimmickSpec>().Single();
            Assert.That(railway.FirstActivationAfterControllerTurns, Is.EqualTo(5));
            Assert.That(railway.RepeatIntervalControllerTurns, Is.EqualTo(5));
            foreach (var copy in new[] { input.WithActor(input.ActorId),
                input.WithIntent(input.Kind, input.Aim, input.TargetCell, input.Path, input.MovementContext),
                input.WithWorld(input.State.With(), input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings),
                input.WithPersistentFacts(input.Owned, input.ItemInventory, input.GoldBalance, input.FameBalance) })
                Assert.That(copy.Battle, Is.SameAs(input.Battle));
            for (int turn = 0; turn < 2; turn++)
            {
                battle.Submit(battle.EndPlayerTurn());
                battle.AdvanceToPlayerOrDay();
            }
            Assert.That(Count(battle), Is.EqualTo(4));
            Assert.That(battle.Buffers.CombatResultHistoryReadSink.Results.SelectMany(r => r.Events).OfType<CombatGimmickEvent>(), Is.Empty);
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(Count(battle), Is.EqualTo(5));
            Assert.That(battle.Main.Health.IsDead, Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(battle.Buffers.CombatResultHistoryReadSink.Results.SelectMany(r => r.Events).OfType<CombatGimmickEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void PreviewAndOrdinaryMoveDoNotCountAnEnd()
        {
            using var battle = Create();
            var command = battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(0, 2));
            Assert.That(Count(battle), Is.Zero);
            battle.Submit(command);
            Assert.That(Count(battle), Is.Zero);
        }

        [Test]
        public void FreshPreparationRejectsOldCountBeforeInstall_AndZeroIsAccepted()
        {
            using var battle = Create();
            var b = battle.Buffers;
            var current = b.CombatStateReadSink.Current;
            var characters = b.CharacterReadSink.Characters.Select(c => c.ToFacts().With(actionUse: UnitActionUse.Deferred)).ToArray();
            var combat = new CombatPreparation(current.BoardWidth, current.BoardHeight, current.ReserveCapacity,
                current.TimelineSeed, CombatFlowPhase.PlayerInput, current.DrawOrder, current.TraitSettings, current.DrawCursor,
                current.PlayerDeploymentDepth, current.EnemyDeploymentDepth, current.RailCells.ToArray(), current.StageGimmicks.ToArray(),
                current.Environment, current.EnvironmentBindings, current.ObstacleContents, current.CommandState.WallBlocking);
            var initial = current.CommandState.With(phase: CombatFlowPhase.PlayerInput,                isNight: false,turn: new CombatTurnFacts(StagePhase.Deployment),rewards: CombatRewardFacts.Empty);
            var invalid = new BattlePreparation(characters, combat, initial.Structures,
                commandState: initial.With(turn: new CombatTurnFacts(StagePhase.Deployment, completedControllerTurnCount: 16)),
                objective: initial.Objective, membership: current.Membership);
            var valid = new BattlePreparation(characters, combat, initial.Structures, commandState: initial,
                objective: initial.Objective, membership: current.Membership);
            b.BattleWriteSink.EndBattle();
            var snapshot = new CoreIntegrationSnapshot(b);
            var store = (ICombatStateBufferStore)b.CombatStateReadSink;
            Assert.That(Assert.Throws<InvalidOperationException>(() => store.PrepareBattle(invalid)).Message,
                Does.Contain("새 전투"));
            snapshot.AssertUnchanged(b);
            Assert.That(store.PrepareBattle(valid), Is.Not.Null);
            snapshot.AssertUnchanged(b);
        }

        private sealed class RailwaySource : IBattleInitializationSource
        {
            public BattleInitialization Read(ProjectL.Systems.DataBaseSystem.IDataBaseQueryContext database,
                int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource().Read(database, chapterKey, stageKey);
                var s = basis.Stage;
                var stage = new StageConfig(s.MapWidth, s.MapHeight, s.ReserveCapacity, s.PlayerDeploymentDepth,
                    s.EnemyDeploymentDepth, s.TimelineSeed, s.InitialEnemyPlacements.ToArray(), new[] { new CellCoord(0, 0) },
                    new StageGimmickSpec[] { new RailwayGimmickSpec(5, 5) }, s.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(stage, basis.MainAnchor, basis.MainRotation, basis.Units, basis.Structures,
                    basis.Categories, basis.AttackProfiles, basis.Environment, basis.EnvironmentBindings, basis.ObstaclePlacements,
                    basis.WallBlocking, basis.ObjectiveKind, basis.ItemDefinitions, basis.Progression,
                    basis.InitialRandomTornadoPlacements, basis.ClearRewards, basis.TraitGrowth);
            }
        }

        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource());

        private static long Count(ConfiguredMovementBattle battle) =>
            battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.CompletedControllerTurnCount;
    }
}
