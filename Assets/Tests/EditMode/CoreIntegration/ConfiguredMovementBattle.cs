using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    // 운영 DB 어댑터·진입·명령·진영 시작을 거친다. 설치할 행동 상태나 예산을 직접 채우지 않는다.
    internal sealed class ConfiguredMovementBattle : IDisposable
    {
        private readonly FrameworkRuntime _runtime;
        private readonly CapturedSource _source;
        internal CoreIntegrationTestData Database { get; }
        internal IBufferQuery Buffers { get; }
        internal IInGameCoreSystemGroupService Core { get; }
        internal OwnedCharacterStatsSnapshot OwnedStats { get; }
        internal OwnedCharacterStatsSnapshot MainOwnedStats { get; }
        internal CharacterCombatFacts Actor => Character(EntityKey.Unit_Cowboy);
        internal CharacterCombatFacts Main => Character(EntityKey.Unit_Akimbo);
        internal static CategoryConfigData Config => new(3,5,3,2,0);

        internal ConfiguredMovementBattle(CategoryConfigData config, IBattleInitializationSource battleSource = null,
            bool startPlayerTurn = true, CoreIntegrationTestData database = null, bool deploySniper = true, bool enterCombat = true,
            Action<IBufferQuery> configureOwned = null)
        {
            Database = database ?? new CoreIntegrationTestData(config);
            _source = battleSource == null ? null : new CapturedSource(battleSource);
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create(_source).ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                _runtime.InitData(Database);
                _runtime.PostBindService();
                Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
                CoreIntegrationTestData.InstallDeck(Buffers);
                configureOwned?.Invoke(Buffers);
                var ownedStats = Core.CharacterStats.ReadOwned().Items;
                OwnedStats = ownedStats.Single(c => c.CharacterKey == EntityKey.Unit_Cowboy);
                MainOwnedStats = ownedStats.Single(c => c.Role == CharacterOwnershipRole.Main);
                Buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                    new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                    new StageProgressData(2, 1101, 1, 0, Array.Empty<int>()) });
                Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                Assert.That(Core.BattleEntry.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
                Assert.That(Actor.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
                Submit(CombatViewCommandData.PlaceMainCharacter(CellCoord.Zero, Rotation4.R0));
                Submit(CombatViewCommandData.PlaceUnit(Actor.EntityId, new CellCoord(0, 1), Rotation4.R0));
                Submit(CombatViewCommandData.PlaceUnit(Character(EntityKey.Unit_DynamiteHammer).EntityId, new CellCoord(2, 0), Rotation4.R0));
                if (deploySniper)
                    Submit(CombatViewCommandData.PlaceUnit(Character(EntityKey.Unit_Sniper).EntityId, new CellCoord(4, 0), Rotation4.R0));
                if (!enterCombat) return;
                Submit(CombatViewCommandData.EnterCombat());
                if (!startPlayerTurn)
                {
                    Assert.That(Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase,
                        Is.EqualTo(CombatControllerPhase.StartingPlayer));
                    return;
                }
                Assert.That(Core.CombatAdvance.AdvanceCombat(), Is.True);
                Assert.That(Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Player));
            }
            catch { _runtime.Shutdown(); throw; }
        }

        internal CharacterCombatFacts Character(EntityKey key) => Buffers.CharacterReadSink.Characters.Single(c => c.CharacterKey == key).ToFacts();
        internal CombatViewCommandData EndPlayerTurn() => CombatViewCommandData.EndPlayerTurn(
            Buffers.CombatStateReadSink.BattleInstanceId, Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence);

        internal CombatViewCommandData Attack(CharacterCombatFacts actor, CellCoord target) => CombatViewCommandData.Attack(
            actor.EntityId, AttackAim.ForCell(target), Buffers.CombatStateReadSink.BattleInstanceId,
            Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence);

        internal void EndRun()
        {
            var store = (IRunProgressBufferStore)Buffers.RunProgressReadSink;
            var selected = Buffers.CurrentProgressReadSink.CurrentStage;
            store.InstallCompletion(store.PrepareCompletion(new RunCompletionReceipt(RunOutcome.Defeat,
                Buffers.CurrentProgressReadSink.CurrentMapKey, new StageNodeId(selected.NodeId), new StageKey(selected.StageKey))),
                Buffers.CurrencyReadSink.RunSpent, Buffers.FameReadSink.RunEarned);
        }

        internal object[] CaptureGameplay() => CoreIntegrationSnapshot.CaptureGameplay(Buffers);
        internal object[] CaptureStores() => CoreIntegrationSnapshot.CaptureStores(Buffers);

        internal void Move(CharacterCombatFacts actor, CellCoord target) => Submit(PreviewMove(actor, actor.Anchor.Value, target));
        internal CombatViewCommandData PreviewMove(CharacterCombatFacts actor, params CellCoord[] cells)
        {
            var path = new MovementPath(cells);
            Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(actor.EntityId, path));
            Assert.That(Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            return CombatViewCommandData.Move(actor.EntityId, path, preview.MovementContext);
        }
        internal void Submit(CombatViewCommandData command)
        {
            int number = Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        internal void AdvanceToPlayerOrDay(int maximumSteps = 256)
        {
            for (int steps = 0; ; steps++)
            {
                var state = Buffers.CombatStateReadSink.Current.CommandState;
                if (state.Phase == CombatFlowPhase.Finished || state.Turn.StagePhase == StagePhase.Deployment ||
                    state.Turn.ControllerPhase == CombatControllerPhase.Player)
                    return;

                string resultEvidence = Buffers.CombatResultReadSink.TryGetLatest(out var result)
                    ? $"{result.Sequence}/{result.Kind}/{result.Accepted}/{result.Reason}"
                    : "none";
                string evidence = $"steps={steps}/{maximumSteps}, phase={state.Phase}, stage={state.Turn.StagePhase}, " +
                    $"controller={state.Turn.ControllerPhase}, turn={state.Turn.ControllerTurnSequence}, result={resultEvidence}";
                Assert.That(steps, Is.LessThan(maximumSteps), $"Automatic advancement exhausted: {evidence}");
                Assert.That(Core.CombatAdvance.AdvanceCombat(), Is.True, evidence);
            }
        }

        public void Dispose() => _runtime.Shutdown();

        internal PublicationServices Publication(IChapterProgressionCoordinator progression = null) =>
            new(this, _source?.Definition ?? throw new InvalidOperationException("명시적으로 포착한 전투 정의가 필요합니다."),
                progression ?? Core.ChapterProgression);

        // 계산·게시 검사는 진입에 실제 사용된 정의를 읽는다. 진행한 전투를 재준비하지 않는다.
        private sealed class CapturedSource : IBattleInitializationSource
        {
            private readonly IBattleInitializationSource _inner;
            internal BattleInitialization Definition { get; private set; }
            internal CapturedSource(IBattleInitializationSource inner) => _inner = inner;
            public BattleInitialization Read(ProjectL.Systems.DataBaseSystem.IDataBaseQueryContext database,
                int chapterKey, StageKey stageKey) => Definition = _inner.Read(database, chapterKey, stageKey);
        }

        internal sealed class PublicationServices
        {
            private readonly ConfiguredMovementBattle _battle;
            internal CombatInputAssembler Assembler { get; }
            internal CombatTransitionPublisher Publisher { get; }

            internal PublicationServices(ConfiguredMovementBattle battle, BattleInitialization definition,
                IChapterProgressionCoordinator progression)
            {
                _battle = battle;
                var builder = new SystemBuilderHelper();
                builder.AddService<IBufferQuery>(battle.Buffers);
                var reader = builder.BuildSystemReader().ServiceStoreReader;
                var artifacts = new ArtifactLoadoutReader();
                artifacts.InitData(battle.Database);
                artifacts.PostBindService(reader);
                Assembler = new CombatInputAssembler(new DefinitionRead(definition, battle.Buffers.CombatStateReadSink.BattleInstanceId), artifacts);
                Assembler.PostBindService(reader);
                var planner = new BattleProgressionPlanner(progression);
                planner.PostBindService(reader);
                Publisher = new CombatTransitionPublisher(planner);
                Publisher.PostBindService(reader);
            }

            internal CombatCommandInput Attack(CharacterCombatFacts actor, CellCoord target)
            {
                var b = _battle.Buffers;
                int number = b.CombatViewWriteSink.Submit(_battle.Attack(actor, target));
                return Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Attack, actor.EntityId,
                    AttackAim.ForCell(target), expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                    expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            }

            internal CombatCommandInput EnemyStep()
            {
                var b = _battle.Buffers;
                return Assembler.Assemble(new CombatActionRequest(0, CombatCommandKind.CompleteEnemyTurn, default, default,
                    processingKind: CombatProcessingKind.Advance, expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                    expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            }

            private sealed class DefinitionRead : IBattleDefinitionRead
            {
                private readonly BattleInitialization _definition;
                private readonly long _battleId;
                internal DefinitionRead(BattleInitialization definition, long battleId)
                {
                    _definition = definition;
                    _battleId = battleId;
                }
                public BattleInitialization Require(long battleInstanceId)
                {
                    Assert.That(battleInstanceId, Is.EqualTo(_battleId));
                    return _definition;
                }
            }
        }

        // 보드·플레이어 배치는 그대로 두고 가까운 적 또는 비공격 본부만 저작한다.
        // y=3의 적은 아군 배치 구역(아래 2줄) 밖이라 합법적으로 준비된다. 적 줄 수 인자(3)는 쓰지 않는 전환 입구 값이다.
        internal sealed class EncounterSource : IBattleInitializationSource
        {
            private readonly CellCoord? _enemy;
            private readonly bool _terminalHeadquarters;

            internal EncounterSource(CellCoord? enemy = null, bool terminalHeadquarters = false)
            {
                _enemy = enemy;
                _terminalHeadquarters = terminalHeadquarters;
            }

            public BattleInitialization Read(ProjectL.Systems.DataBaseSystem.IDataBaseQueryContext database,
                int chapterKey, StageKey stageKey)
            {
                Assert.That(chapterKey, Is.EqualTo(1));
                Assert.That(stageKey.Value, Is.EqualTo(1101));
                var query = database.InGameQuery;
                var units = query.UnitTableQuery.GetAllUnits().Select(UnitSpecAdapter.ReadUnit).ToArray();
                var stage = new StageConfig(8, 6, 8, 2, 3, 42,
                    _enemy.HasValue ? new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, _enemy.Value, Rotation4.R180) }
                        : Array.Empty<InitialEnemyPlacementSpec>(),
                    Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(),
                    new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters,
                        _terminalHeadquarters ? new CellCoord(1, 3) : new CellCoord(3, 5), Rotation4.R180) });
                return new BattleInitialization(stage, new CellCoord(1, 0), Rotation4.R0, units,
                    new[] { new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero },
                        _terminalHeadquarters ? 1 : 20, false, 0) },
                    UnitSpecAdapter.ReadCategories(query.CategoryConfigTableQuery.Config),
                    BattleDefinitionAdapter.ReadAttackProfiles(database, units),
                    clearRewards: query.StageEconomyTableQuery.GetStageEconomy(1).ClearRewards);
            }
        }
    }
}

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ConfiguredMovementBattleTask1Probe
    {
        [Test]
        public void SourceStartWatchdogAndRealProgress()
        {
            var config = new CategoryConfigData(3,5,3,2,0);
            using var normal = new ConfiguredMovementBattle(config);
            var definition = CoreIntegrationTestData.DatabaseSource(normal.Buffers).Read(
                new CoreIntegrationTestData(config),
                1, new StageKey(1101));
            var source = new Source(definition);
            using var battle = new ConfiguredMovementBattle(config, source, startPlayerTurn: false);
            Assert.That(source.ReadCount, Is.EqualTo(1));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase,
                Is.EqualTo(CombatControllerPhase.StartingPlayer));
            var sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            var exhausted = Assert.Throws<AssertionException>(() => battle.AdvanceToPlayerOrDay(0));
            Assert.That(exhausted.Message, Does.Contain("StartingPlayer").And.Contain("result="));
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            battle.AdvanceToPlayerOrDay(1);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            sequence++;
            battle.AdvanceToPlayerOrDay(0);
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            battle.Submit(CombatViewCommandData.EndPlayerTurn(battle.Buffers.CombatStateReadSink.BattleInstanceId,
                battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            battle.AdvanceToPlayerOrDay();
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase,
                Is.EqualTo(CombatControllerPhase.Player));
        }

        private sealed class Source : IBattleInitializationSource
        {
            private readonly BattleInitialization _definition;
            internal int ReadCount { get; private set; }
            internal Source(BattleInitialization definition) { _definition = definition; }
            public BattleInitialization Read(ProjectL.Systems.DataBaseSystem.IDataBaseQueryContext database,
                int chapterKey, StageKey stageKey)
            {
                Assert.That(chapterKey, Is.EqualTo(1));
                Assert.That(stageKey.Value, Is.EqualTo(1101));
                ReadCount++;
                return _definition;
            }
        }
    }
}
