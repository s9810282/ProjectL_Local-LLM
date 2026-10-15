using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.Factory.Runtime.InGame.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using InputRouter = ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View.InputSystem;

namespace ProjectL.Tests.Framework
{
    public sealed partial class CoreStageCombatCycleTests
    {
        [Test]
        public void HeldEffects_NightEndExpiresSuppliesWhileMainStaysFieldAndKeepsBadge()
        {
            _source.ShortNight = true;
            EnterStage(1, 2);
            SeedHeldMain();
            BeginMainTurn();
            Command(CombatViewCommandData.EndUnitTurn());
            Assert.That(Main().Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(Main().HeldEnvironment.SuppliesAttackBonus, Is.Zero);
            Assert.That(Main().HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(_buffers.CombatStateReadSink.Current.CommandState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
            Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().Single().Final.SuppliesAttackBonus, Is.Zero);
        }

        [Test]
        public void HeldBadge_DoesNotPreventRailwayInstantDeath()
        {
            _source.LethalRailway = true;
            EnterStage(1, 2);
            SeedHeldMain();
            BeginMainTurn();
            Command(CombatViewCommandData.EndUnitTurn());
            Assert.That(_core.Combat.AdvanceCombat(), Is.True);
            Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var death = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died);
            Assert.That(death.Expected.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(death.Final.Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().All(e => e.Origin == null), Is.True);
            Assert.That(Main().HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty), Is.True);
            _buffers.CombatViewWriteSink.AcknowledgePresented(result.Sequence);
            _core.Combat.EndBattle();
            AssertCleaned();
        }

        private void SeedHeldMain()
        {
            var main = Main();
            _buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(main,
                HeldEnvironmentEffects.WithHeldState(main, new CharacterHeldEnvironmentState(41, true, 7))) }, Array.Empty<SoulCountChange>()));
        }

        private FrameworkRuntime _runtime;
        private IInGameCoreSystemGroupService _core;
        private IBufferQuery _buffers;
        private OwnedCharacterRecord[] _owned;
        private CharacterInstanceId _mainId;
        private CycleDatabase _database;
        private CycleSource _source;
        private static readonly ArtifactLoadoutSnapshot EmptyLoadout = new(0, Array.Empty<ArtifactPlacedEffectSnapshot>());

        [SetUp]
        public void SetUp()
        {
            var database = _database = new CycleDatabase();
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            _source = new CycleSource(database);
            InGameCoreSystemFactory.Create(_source, new ChapterContinuationTestSource()).ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            _runtime.InitData(database);
            _runtime.PostBindService();
            _core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
            _buffers = _runtime.GetService(BufferSystemKey.Buffer);
            Assert.That(_core.Characters.InitializeRoster(1).Succeeded, Is.True);
            _owned = _buffers.CharacterReadSink.Owned.ToArray();
            _mainId = _owned.Single(x => x.Role == CharacterOwnershipRole.Main).Id;
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(_mainId, 0, 2) },
                new MainCharacterPersistentChange(main, CharacterBufferTestData.MainState(7))));
            _buffers.CurrencyWriteSink.SetAmount(91);
            Assert.That(_core.Artifacts.InitializeBoard(4, 4), Is.True);
            _core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            Assert.That(_buffers.ArtifactReadSink.Current.PlacedItems.Count, Is.EqualTo(1));
            // 그래프 생성 자체는 기존 테스트에서 검증한다. 여기서는 합법적인 연결만 입력한다.
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 101, 1, 0, new[] { 3 }, stageType: StageType.Combat),
                new StageProgressData(3, 102, 2, 0, Array.Empty<int>(), stageType: StageType.Combat, mapRole: StageMapRole.Boss) });
            _buffers.StageProgressWriteSink.RegisterStages(2, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 201, 1, 0, new[] { 3 }, stageType: StageType.Combat),
                new StageProgressData(3, 202, 2, 0, Array.Empty<int>(), stageType: StageType.Combat, mapRole: StageMapRole.Boss) });
            Assert.That(_core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
        }

        [TearDown]
        public void TearDown() => _runtime?.Shutdown();

        [Test]
        public void RegisteredPathCycle_PreviewMoveBarrierReturnAttackEndTurn()
        {
            _source.MovementCycle = true;
            _buffers.CharacterReadSink.TryGetMainState(out var persistent);
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(), new MainCharacterPersistentChange(persistent,
                    CharacterBufferTestData.MainState(7, maxAction: 20, moveRange: 20))));
            EnterStage(1, 2);
            var prepared = _buffers.CombatStateReadSink.Current;
            var actor = Main();
            var s = actor.Stats;
            actor = CharacterBufferTestData.WithSnapshot(actor, stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange,
                s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost,
                s.AttackCost, s.MoveRange, s.MaxActionCost, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category,
                s.ArtifactAttackBonus, s.EffectiveAttackPower, s.SoulAttackBonus, s.SoulMaxHealthBonus, s.PositionAttackBonus));
            var enemy = _buffers.CharacterReadSink.Characters.Single(c => c.Owner == Owner.Enemy).ToFacts();
            enemy = CharacterBufferTestData.WithSnapshot(enemy, new HealthStateSnapshot(20, 20));
            var turn = new CombatTurnFacts(StagePhase.Combat, participants: new[] {
                new CombatTurnParticipant(actor.EntityId, actor.Stats.Priority, actor.Stats.Delay, openingTurnPending: false),
                new CombatTurnParticipant(enemy.EntityId, enemy.Stats.Priority, enemy.Stats.Delay) });
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, actor.EntityId, true,
                prepared.Structures, new[] { new ThornFieldCellSnapshot(new CellCoord(2, 1), 10, 1) },
                turn.Participants.Select(p => CombatTimelineEntrySnapshot.ForUnit(p.EntityId, p.Priority, 0, p.EntityId == actor.EntityId)).ToArray(), turn);
            // 전투 진입/배치 자체는 기존 테스트가 검증한다. 이 픽스처는 활동 중인 플레이어 턴부터 시작한다.
            _buffers.BattleWriteSink.EndBattle();
            _buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor, enemy },
                new CombatPreparation(8, 6, 8, 100, 42, CombatFlowPhase.PlayerInput, Array.Empty<CharacterInstanceId>()),
                prepared.Structures, commandState: state));
            var emptyField = _buffers.CombatStateReadSink.SoulField;
            _buffers.BattleWriteSink.Apply(new SoulActionChanges(new CharacterChanges(
                Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(emptyField, new SoulFieldSnapshot(3, new[] {
                    new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 100, 1, "passage"), new CellCoord(1, 0), 5, 1),
                    new SoulFieldFacts(2, new EffectOriginKey(EffectOwnerKind.Artifact, 100, 1, "arrival"), new CellCoord(3, 2), 5, 0) })), Array.Empty<long>()));

            var slot = new StageInputReceiverSlot();
            var layers = new CombatMovementViewTests.ProbeLayers();
            var probe = new CombatMovementViewTests.Probe();
            var controllers = new IInputController[] { new DeploymentInputController(slot), new CombatInputController(slot) };
            var global = (GlobalSystemGroup)GlobalSystemFactory.Create(controllers, StageInputLayers.All);
            var router = (InputRouter)global.InputServiceCommand.InputSystemServices;
            router.Configure(layers, InputConfiguration.Create(controllers));
            var view = (InGameViewSystemGroup)InGameViewSystemFactory.Create();
            var groups = new SystemBuilderHelper();
            groups.AddService<IInGameCoreSystemGroupService>(_core); view.ExportServices(groups);
            var inGame = new InGameSystemGroup(groups.BuildSystemReader());
            var rootBuilder = new SystemBuilderHelper();
            rootBuilder.AddService<IBufferQuery>(_buffers); slot.ExportServices(rootBuilder);
            global.ExportServices(rootBuilder); inGame.ExportServices(rootBuilder);
            CombatMovementViewTests.BindView((CombatViewSystem)view.Combat, _buffers, _buffers.CombatViewWriteSink, slot, layers);
            var runner = new JobRunnerSystem();
            var registry = CombatPlanTests.CreateRegistry(runner, inGame, _buffers);
            var flow = new CombatFlow(_buffers.CombatStateReadSink, _buffers.CombatResultReadSink,
                _buffers.CombatViewReadSink, runner, registry, CombatExecutionMode.Live, new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            var root = new UnityEngine.GameObject("Registered drag cycle").AddComponent<CombatSceneRoot>();

            try
            {
                ((CombatViewSystem)view.Combat).AttachDisplay(probe); view.Tick(0);
                var origin = new CellCoord(1, 1);
                var stop = new CellCoord(3, 1);
                var first = PlanMove(origin, new CellCoord(1, 0), new CellCoord(2, 0), new CellCoord(3, 0), stop);
                Assert.That(first.PathMovement.EnteredCells, Is.EqualTo(new[] { new CellCoord(1, 0), new CellCoord(2, 0), new CellCoord(3, 0), stop }));
                Assert.That(first.PathMovement.DistanceSpent, Is.EqualTo(4));
                Assert.That(first.PathMovement.ReturnBarrierOccurred, Is.False);
                Assert.That(Main().PendingMoveDistance, Is.EqualTo(4));
                Assert.That(Main().Stats.CurrentActionCost, Is.EqualTo(16));
                Assert.That(_buffers.CombatStateReadSink.SoulField.Entries.Count, Is.EqualTo(2));
                slot.AttackModeRequested(); slot.MoveModeRequested();
                Assert.That(Main().PendingMoveDistance, Is.EqualTo(4));
                var rejected = Execute(CombatViewCommandData.Attack(new EntityId(999), AttackAim.ForCell(stop)));
                Assert.That(rejected.Accepted, Is.False);
                Assert.That(Main().PendingMoveDistance, Is.EqualTo(4));
                Return();
                Assert.That(Main().Anchor, Is.EqualTo(origin));
                Assert.That(Main().Stats.CurrentActionCost, Is.EqualTo(20));
                Assert.That(Main().PendingMoveDistance, Is.Zero);

                PlanMove(origin, new CellCoord(1, 2));
                var hazard = PlanMove(new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(2, 1), stop, new CellCoord(3, 2));
                var checkpoint = new CellCoord(3, 2);
                Assert.That(hazard.PathMovement.ReturnBarrierOccurred, Is.True);
                Assert.That(hazard.PathMovement.EnteredCells.Last(), Is.EqualTo(checkpoint));
                Assert.That(Main().MovementCheckpointAnchor, Is.EqualTo(checkpoint));
                Assert.That(Main().CommittedMoveDistance, Is.EqualTo(5));
                Assert.That(Main().PendingMoveDistance, Is.Zero);
                Assert.That(Main().Health.CurrentHealth, Is.EqualTo(6));
                Assert.That(_buffers.CombatStateReadSink.SoulField.Entries.Select(e => e.Id), Is.EqualTo(new long[] { 1 }));
                var soulCount = _buffers.CharacterReadSink.GetSoulCount(_mainId);
                PlanMove(checkpoint, stop); Return();
                Assert.That(Main().Anchor, Is.EqualTo(checkpoint));
                Assert.That(Main().Stats.CurrentActionCost, Is.EqualTo(15));
                Assert.That(Main().Health.CurrentHealth, Is.EqualTo(6));
                var afterReturnSouls = _buffers.CharacterReadSink.GetSoulCount(_mainId);
                Assert.That(afterReturnSouls, Is.EqualTo(soulCount));

                PlanMove(checkpoint, new CellCoord(3, 3));
                Assert.That(Main().PendingMoveDistance, Is.EqualTo(1));
                var attack = Execute(CombatViewCommandData.Attack(actor.EntityId, AttackAim.ForCell(new CellCoord(4, 3))));
                Assert.That(attack.Accepted, Is.True, attack.Reason);
                Assert.That(Main().PendingMoveDistance, Is.Zero);
                Assert.That(Main().PendingMoveActionCost, Is.Zero);
                Assert.That(Main().CommittedMoveDistance, Is.EqualTo(6));
                _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ReturnToCheckpoint(actor.EntityId));
                ProcessJob();
                _buffers.CombatStateReadSink.TryGetLatestPreview(out var sealedPreview);
                Assert.That(sealedPreview.CheckpointReturn.CanReturn, Is.False);
                var invalidReturn = Execute(CombatViewCommandData.ReturnToCheckpoint(actor.EntityId, sealedPreview.MovementContext));
                Assert.That(invalidReturn.Accepted, Is.False);
                PlanMove(new CellCoord(3, 3), new CellCoord(2, 3), new CellCoord(2, 2), new CellCoord(1, 2), origin);
                var hp = Main().Health.CurrentHealth;
                slot.EndTurnRequested(); ProcessJob();
                var ended = Present();
                Assert.That(ended.Accepted, Is.True, ended.Reason);
                Assert.That(ended.Events.Any(e => e.Kind == CombatEventKind.TurnEnded), Is.True);
                Assert.That(Main().PendingMoveDistance, Is.Zero);
                Assert.That(Main().PendingMoveActionCost, Is.Zero);
                Assert.That(Main().Health.CurrentHealth, Is.EqualTo(hp));
                var afterEndSouls = _buffers.CharacterReadSink.GetSoulCount(_mainId);
                Assert.That(afterEndSouls, Is.EqualTo(soulCount));
                Assert.That(_buffers.CombatStateReadSink.SoulField.Entries.Select(e => e.Id), Is.EqualTo(new long[] { 1 }));
                ProcessJob();
                var started = Present();
                Assert.That(started.Accepted, Is.True, started.Reason);
                Assert.That(started.Events.Any(e => e.Kind == CombatEventKind.TurnStarted), Is.True);
                Assert.That(_buffers.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(enemy.EntityId));
                ProcessJob();
                var automatic = Present();
                Assert.That(automatic.Accepted, Is.True, automatic.Reason);
                Assert.That(automatic.Kind, Is.EqualTo(CombatProcessingKind.Advance));
                Assert.That(automatic.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved && e.Final.Owner == Owner.Enemy), Is.True);
                Assert.That(automatic.Events.Any(e => e.Kind == CombatEventKind.Attacked), Is.True);
                Assert.That(flow.IsStopped, Is.False);
            }
            finally
            {
                flow.Stop();
                view.Shutdown(); runner.Clear(); global.Shutdown();
                UnityEngine.Object.DestroyImmediate(root.gameObject);
            }

            void ProcessJob()
            {
                flow.Tick(); Assert.That(runner.IsIdle, Is.False);
                runner.Tick(0); Assert.That(runner.IsIdle, Is.True);
                Assert.That(flow.IsStopped, Is.False);
            }
            CombatResult Present()
            {
                Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                view.Tick(0);
                for (int i = 0; i < 40 && view.Combat.HasPendingPresentation; i++) { probe.CompleteMovement(); view.Tick(0); }
                Assert.That(view.Combat.HasPendingPresentation, Is.False);
                Assert.That(_buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
                return result;
            }
            CombatResult Execute(CombatViewCommandData command)
            {
                _buffers.CombatViewWriteSink.Submit(command); ProcessJob(); return Present();
            }
            void Return()
            {
                view.Combat.RequestReturn(); ProcessJob(); view.Tick(0);
                _buffers.CombatStateReadSink.TryGetLatestPreview(out var returnPreview);
                Assert.That(returnPreview.CheckpointReturn.CanReturn, Is.True);
                ProcessJob();
                var result = Present();
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Returned), Is.True);
            }
            CombatResult PlanMove(params CellCoord[] cells)
            {
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.MoveMode,
                    InputPhase.Performed, InputValueData.FromButton(true), 0);
                var before = Main();
                var sequence = _buffers.CombatStateReadSink.LatestResultSequence;
                var field = _buffers.CombatStateReadSink.SoulField;
                Pointer(cells[0]);
                foreach (var cell in cells.Skip(1)) Pointer(cell);
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.Select, InputPhase.Started, InputValueData.FromButton(true), 0);
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.Select, InputPhase.Performed, InputValueData.FromButton(false), 0);
                ProcessJob();
                Assert.That(ProjectL.Systems.RuntimeSystem.BufferSystems.Character.CharacterCombatFactsComparer.SameFacts(before, Main()), Is.True);
                Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.CombatStateReadSink.SoulField, Is.SameAs(field));
                view.Tick(0);
                Assert.That(_buffers.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
                ProcessJob();
                var result = Present();
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
                return result;
            }
            void Pointer(CellCoord cell) => router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.PointerPosition,
                InputPhase.Performed, InputValueData.FromVector2(cell.X + .5f, cell.Y + .5f), 0);
        }

        [Test]
        public void ClearedBoss_CompatibilityPreparationPreservesCompletionAfterCleanup()
        {
            EnterStage(1, 2);
            WinCurrentBattle();
            _core.Combat.EndBattle();
            EnterStage(1, 3);
            WinCurrentBattle();
            _core.Combat.EndBattle();
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            var snapshot = new RecoveryBufferSnapshot(_buffers);
            int reads = _source.Reads.Count;
            var cache = _core.Combat.GetType().GetField("_definitions",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(cache.GetValue(_core.Combat), Is.Null);
            Assert.Throws<InvalidOperationException>(() => _core.Combat.PrepareBattle());
            snapshot.AssertUnchanged(_buffers);
            Assert.That(_source.Reads.Count, Is.EqualTo(reads));
            Assert.That(cache.GetValue(_core.Combat), Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FinalBattleLookupEndsRun_PreventsBattlePublication(bool compatibility)
        {
            if (compatibility) EnterStage(1, 2);
            if (compatibility) _buffers.BattleWriteSink.EndBattle();
            var snapshot = new RecoveryBufferSnapshot(_buffers);
            _source.BeforeReturn = () => RunTerminationAdmissionTests.Seed(_buffers, RunOutcome.Defeat);
            if (compatibility)
                Assert.That(Assert.Throws<InvalidOperationException>(() => _core.Combat.PrepareBattle()).Message,
                    Does.Contain("RunEnded"));
            else Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(2)).FailureReason.ToString(), Is.EqualTo("RunEnded"));
            snapshot.AssertUnchanged(_buffers);
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
        }

        [Test]
        public void FinalGraphLookupEndsRun_PreventsRegistration()
        {
            var snapshot = new RecoveryBufferSnapshot(_buffers);
            _database.OnStagesRead = () => RunTerminationAdmissionTests.Seed(_buffers, RunOutcome.Defeat);
            Assert.That(Assert.Throws<InvalidOperationException>(() => _core.StageGeneratorSystem.PrepareGraph(1)).Message,
                Does.Contain("RunEnded"));
            snapshot.AssertUnchanged(_buffers);
        }

        [TestCase("Command")]
        [TestCase("Advance")]
        [TestCase("Resolve")]
        [TestCase("Abandon")]
        public void FinalCompletionLookupEndsRun_ReturnsFalseWithoutPublication(string operation)
        {
            _source.LethalRailway = operation == "Advance";
            EnterStage(1, 2);
            if (operation == "Command")
            {
                BeginMainTurn();
                Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, Main().EntityId, _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == Main().EntityId).Anchor.Value, new CellCoord(1, 2)));
                _buffers.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(Main().EntityId,
                    AttackAim.ForCell(new CellCoord(1, 3))));
            }
            else if (operation == "Advance")
            {
                BeginMainTurn();
                Command(CombatViewCommandData.EndUnitTurn());
                Assert.That(_buffers.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            }
            else if (operation == "Resolve") _core.Combat.ApplyHealth(Main().EntityId, 0);
            var snapshot = new RecoveryBufferSnapshot(_buffers);
            int lookups = 0;
            _database.ReplaceStage = key => {
                lookups++;
                RunTerminationAdmissionTests.Seed(_buffers, RunOutcome.Defeat);
                return CycleDatabase.Stage(key, 1, StageMapRole.Regular);
            };
            bool result = operation switch {
                "Command" => _core.Combat.ProcessNextCommand(),
                "Advance" => _core.Combat.AdvanceCombat(),
                "Resolve" => _core.Combat.ResolveBattle(EmptyLoadout),
                _ => _core.Combat.AbandonBattle() == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned
            };
            Assert.That(result, Is.False);
            Assert.That(lookups, Is.EqualTo(1));
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            snapshot.AssertUnchanged(_buffers);
        }

        [Test]
        public void FinalTurnCompletionLookupEndsRun_ThrowsWithoutPublication()
        {
            EnterStage(1, 2);
            _buffers.CharacterReadSink.TryGetMainState(out var persistent);
            var main = Main();
            var field = _buffers.CombatStateReadSink.SoulField;
            var settled = new SoulActionChanges(
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.CharacterSettlementCalculator.SetHealth(main, persistent, 0),
                new SoulFieldChange(field, field), Array.Empty<long>());
            var snapshot = new RecoveryBufferSnapshot(_buffers);
            _database.ReplaceStage = key => {
                RunTerminationAdmissionTests.Seed(_buffers, RunOutcome.Defeat);
                return CycleDatabase.Stage(key, 1, StageMapRole.Regular);
            };
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                _core.Combat.FinalizeTurn(main.EntityId, settled, EmptyLoadout)).Message, Does.Contain("RunEnded"));
            snapshot.AssertUnchanged(_buffers);
        }

        [Test]
        public void EnterBattle_FirstEntryPublishesMatchingTarget()
        {
            _source.DistinctTargets = true;
            Assert.That(_buffers.CurrentProgressReadSink.HasSelection, Is.False);
            var result = _core.Combat.EnterBattle(1, new StageNodeId(2));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ChapterKey, Is.EqualTo(1));
            Assert.That(result.NodeId.Value, Is.EqualTo(2));
            Assert.That(result.StageKey.Value, Is.EqualTo(101));
            Assert.That(_source.Reads, Is.EqualTo(new[] { (1, 101) }));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(101));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageName, Is.EqualTo("Stage 101"));
            Assert.That(_buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(8));
            Assert.That(_buffers.CombatStateReadSink.Current.ManualDrawGoldCost, Is.EqualTo(2));
            Assert.That(_buffers.CombatStateReadSink.Current.DrawCursor, Is.EqualTo(3));
            AssertPersistent(7);
        }

        [TestCase("Chapter")]
        [TestCase("Unconnected")]
        [TestCase("Backward")]
        [TestCase("Cleared")]
        public void EnterBattle_NavigationRejectedBeforeSourceRead(string reason)
        {
            int chapter = reason == "Chapter" ? 2 : 1;
            int node = reason == "Unconnected" ? 3 : 2;
            if (reason == "Backward")
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(102), "Previous");
            if (reason == "Cleared") _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            var unchanged = CaptureEntryState();
            var result = _core.Combat.EnterBattle(chapter, new StageNodeId(node));
            Assert.That(result.Accepted, Is.False);
            Assert.That(_source.Reads, Is.Empty);
            unchanged();
        }

        [TestCase("Noncombat")]
        [TestCase("Chapter")]
        [TestCase("Identity")]
        public void EnterBattle_NoncombatOrMismatchedDefinitionFaultsBeforePublication(string fault)
        {
            _database.ReplaceStage = key => fault == "Noncombat"
                ? new StageRootData(key, 1, StageMapRole.Regular,
                    new ShopStageDetail(new[] { new ShopCandidate(EntityKey.Unit_Akimbo, 10) }))
                : CycleDatabase.Stage(fault == "Identity" ? 102 : key, fault == "Chapter" ? 2 : 1, StageMapRole.Regular);
            var unchanged = CaptureEntryState();
            Assert.Throws<InvalidOperationException>(() => _core.Combat.EnterBattle(1, new StageNodeId(2)));
            Assert.That(_source.Reads, Is.Empty);
            unchanged();
        }

        [TestCase("Null")]
        [TestCase("Throw")]
        [TestCase("Placement")]
        public void EnterBattle_SourceOrPreparationFailurePreservesStateAndRetries(string fault)
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Old stage");
            _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            _buffers.CurrentProgressWriteSink.SetShopOffers(Enumerable.Range(1, 3).Select(i =>
                new ShopPurchaseSnapshot(new ShopOfferId(i), EntityKey.Unit_Akimbo, 10)).ToArray());
            _source.DistinctTargets = true;
            _source.Fault = fault;
            var unchanged = CaptureEntryState();
            Assert.Throws<InvalidOperationException>(() => _core.Combat.EnterBattle(1, new StageNodeId(3)));
            Assert.That(_source.Reads, Is.EqualTo(new[] { (1, 102) }));
            unchanged();
            _source.Fault = null;
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(3)).Accepted, Is.True);
            Assert.That(_source.Reads, Is.EqualTo(new[] { (1, 102), (1, 102) }));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
            Assert.That(_buffers.CurrentProgressReadSink.Shop, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(10));
            Assert.That(_buffers.CombatStateReadSink.Current.ManualDrawGoldCost, Is.EqualTo(5));
            Assert.That(_buffers.CombatStateReadSink.Current.DrawCursor, Is.EqualTo(3));
            Assert.That(_buffers.CharacterReadSink.Characters.First().EntityId.Value, Is.EqualTo(1));
            AssertPersistent(7);
        }

        [TestCase("Origin")]
        [TestCase("Target")]
        [TestCase("Navigation")]
        public void EnterBattle_ChangedOriginOrTargetDoesNotCommit(string change)
        {
            int target = 2;
            if (change == "Origin")
            {
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Old");
                _buffers.StageProgressWriteSink.SetClear(1, 2, true);
                target = 3;
            }
            Action unchangedAfterMutation = null;
            _source.BeforeReturn = () => {
                if (change == "Origin")
                    _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Changed externally");
                else if (change == "Navigation") _buffers.StageProgressWriteSink.SetClear(1, 2, true);
                else
                {
                    _buffers.StageProgressWriteSink.Clear();
                    _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                        new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                        new StageProgressData(2, 102, 1, 0, Array.Empty<int>()) });
                }
                unchangedAfterMutation = CaptureEntryState();
            };
            var result = _core.Combat.EnterBattle(1, new StageNodeId(target));
            Assert.That(result.Accepted, Is.False);
            if (change != "Navigation") Assert.That(result.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageMismatch));
            Assert.That(unchangedAfterMutation, Is.Not.Null);
            unchangedAfterMutation();
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnterBattle_RetainedBattleRejectsWithoutReinitialization(bool finished)
        {
            EnterStage(1, 2);
            if (finished) WinCurrentBattle();
            _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(Main().EntityId));
            int request = _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(Main().EntityId));
            Assert.That(_core.Combat.ProcessLatestPreview(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.RequestNumber, Is.EqualTo(request));
            Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(request));
            Assert.That(request, Is.GreaterThan(1));
            int reads = _source.Reads.Count;
            var unchanged = CaptureEntryState();
            Assert.Throws<InvalidOperationException>(() => _core.Combat.EnterBattle(1, new StageNodeId(3)));
            Assert.That(_source.Reads.Count, Is.EqualTo(reads));
            unchanged();
            Assert.That(_buffers.CombatViewReadSink.HasPreview, Is.True);
            Assert.That(_buffers.CombatViewReadSink.Preview.Number, Is.EqualTo(request));
        }

        [Test]
        public void EnterBattle_NextStageUsesNewDefinitionsAndRetainsResults()
        {
            _source.DistinctTargets = true;
            EnterStage(1, 2);
            BeginMainTurn();
            Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, Main().EntityId, _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == Main().EntityId).Anchor.Value, new CellCoord(1, 2), new CellCoord(1, 3)));
            var firstRange = ReadAttackRange();
            Assert.That(firstRange, Has.Member(new CellCoord(2, 4)));
            Command(CombatViewCommandData.Attack(Main().EntityId, AttackAim.ForCell(new CellCoord(1, 4))));
            var result = _buffers.CombatResultReadSink.TryGetLatest(out var latest) ? latest : null;
            int handled = _buffers.CombatStateReadSink.HandledCommandNumber;
            int sequence = _buffers.CombatStateReadSink.LatestResultSequence;
            int pending = _buffers.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
            _core.Combat.EndBattle();
            EnterStage(1, 3);
            Assert.That(_source.Reads, Is.EqualTo(new[] { (1, 101), (1, 102) }));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
            Assert.That(_buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(10));
            Assert.That(_buffers.CombatStateReadSink.Current.ManualDrawGoldCost, Is.EqualTo(5));
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var retained), Is.True);
            Assert.That(retained, Is.SameAs(result));
            Assert.That(_buffers.CombatViewReadSink.Commands.Any(c => c.Number == pending), Is.True);
            AssertPersistent(10);
            // 오래된 입력을 버리는 권한은 외부 소유자에게 있다.
            _buffers.CombatViewWriteSink.Clear();
            BeginMainTurn();
            Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, Main().EntityId, _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == Main().EntityId).Anchor.Value, new CellCoord(1, 2), new CellCoord(1, 3)));
            Assert.That(ReadAttackRange(), Has.No.Member(new CellCoord(2, 4)));
            Assert.That(_source.Reads.Count, Is.EqualTo(2));
        }

        [TestCase(0, 2)]
        [TestCase(1, 0)]
        public void EnterBattle_InvalidArgumentsDoNotReadSource(int chapter, int node)
        {
            var unchanged = CaptureEntryState();
            Assert.Catch<ArgumentException>(() => _core.Combat.EnterBattle(chapter,
                node == 0 ? StageNodeId.Empty : new StageNodeId(node)));
            Assert.That(_source.Reads, Is.Empty);
            unchanged();
        }

        [TestCase("Database")]
        [TestCase("Buffers")]
        [TestCase("Source")]
        [TestCase("Progression")]
        public void EnterBattle_MissingDependenciesPreserveState(string missing)
        {
            var service = new CombatProcessingService(missing == "Source" ? null : _source, _core.Characters, System.Array.AsReadOnly(new IMovementTileRule[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornMovementTileRule() }),
                missing == "Progression" ? null : _core.ChapterProgression);
            if (missing != "Database") service.InitData(_database);
            if (missing != "Buffers")
            {
                var builder = new SystemBuilderHelper();
                builder.AddService<IBufferQuery>(_buffers);
                service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            }
            var unchanged = CaptureEntryState();
            Assert.Throws<InvalidOperationException>(() => service.EnterBattle(1, new StageNodeId(2)));
            Assert.That(_source.Reads, Is.Empty);
            unchanged();
        }

        private void BeginMainTurn()
        {
            Command(CombatViewCommandData.EnterCombat());
            for (int i = 0; i < 8 && _buffers.CombatStateReadSink.Phase == CombatFlowPhase.Automatic; i++)
                Assert.That(_core.Combat.AdvanceCombat(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(Main().EntityId));
        }

        private CellCoord[] ReadAttackRange()
        {
            _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(Main().EntityId));
            Assert.That(_core.Combat.ProcessLatestPreview(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Range.Value.Succeeded, Is.True);
            return preview.Range.Value.RangeCells.ToArray();
        }

        private void WinCurrentBattle()
        {
            BeginMainTurn();
            Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, Main().EntityId, _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == Main().EntityId).Anchor.Value, new CellCoord(1, 2)));
            Command(CombatViewCommandData.Attack(Main().EntityId, AttackAim.ForCell(new CellCoord(1, 3))));
            Assert.That(_buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
        }

        private Action CaptureEntryState()
        {
            var current = _buffers.CurrentProgressReadSink;
            int chapter = current.CurrentMapKey;
            bool selected = current.HasSelection;
            var stage = current.CurrentStage;
            var shop = current.Shop;
            var recovery = current.Recovery;
            var battle = _buffers.CombatStateReadSink.Current;
            var completion = _buffers.CombatStateReadSink.Completion;
            int sequence = _buffers.CombatStateReadSink.LatestResultSequence;
            int handled = _buffers.CombatStateReadSink.HandledCommandNumber;
            int answered = _buffers.CombatStateReadSink.AnsweredPreviewNumber;
            _buffers.CombatResultReadSink.TryGetLatest(out var result);
            _buffers.CombatStateReadSink.TryGetLatestPreview(out var preview);
            var artifact = _buffers.ArtifactReadSink.Current;
            var characters = _buffers.CharacterReadSink.Characters.ToArray();
            var owned = _buffers.CharacterReadSink.Owned.ToArray();
            var souls = owned.Select(o => _buffers.CharacterReadSink.GetSoulCount(o.Id)).ToArray();
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            int gold = _buffers.CurrencyReadSink.Amount;
            var commands = _buffers.CombatViewReadSink.Commands.ToArray();
            return () => {
                Assert.That(current.CurrentMapKey, Is.EqualTo(chapter));
                Assert.That(current.HasSelection, Is.EqualTo(selected));
                Assert.That(current.CurrentStage, Is.EqualTo(stage));
                Assert.That(current.Shop, Is.SameAs(shop));
                Assert.That(current.Recovery, Is.SameAs(recovery));
                Assert.That(_buffers.CombatStateReadSink.Current, Is.SameAs(battle));
                Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
                Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(answered));
                _buffers.CombatResultReadSink.TryGetLatest(out var afterResult);
                Assert.That(afterResult, Is.SameAs(result));
                _buffers.CombatStateReadSink.TryGetLatestPreview(out var afterPreview);
                Assert.That(afterPreview, Is.SameAs(preview));
                Assert.That(_buffers.CharacterReadSink.Characters, Is.EqualTo(characters));
                Assert.That(_buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
                Assert.That(owned.Select(o => _buffers.CharacterReadSink.GetSoulCount(o.Id)), Is.EqualTo(souls));
                _buffers.CharacterReadSink.TryGetMainState(out var afterMain);
                Assert.That(afterMain, Is.SameAs(main));
                Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(gold));
                Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
                Assert.That(_buffers.CombatViewReadSink.Commands, Is.EqualTo(commands));
            };
        }

        [Test]
        public void RegisteredCycle_VictoryCleanupAndNextPreparationPreservePersistentState()
        {
            var artifact = _buffers.ArtifactReadSink.Current;
            EnterStage(1, 2);
            for (int node = 2; node <= 3; node++)
            {
                Command(CombatViewCommandData.EnterCombat());
                for (int i = 0; i < 8 && _buffers.CombatStateReadSink.Phase == CombatFlowPhase.Automatic; i++)
                    Assert.That(_core.Combat.AdvanceCombat(), Is.True);
                var main = Main();
                Assert.That(_buffers.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(main.EntityId));
                Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, main.EntityId, _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == main.EntityId).Anchor.Value, new CellCoord(1, 2)));
                _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(main.EntityId));
                Assert.That(_core.Combat.ProcessLatestPreview(), Is.True);
                Command(CombatViewCommandData.Attack(main.EntityId, AttackAim.ForCell(new CellCoord(1, 3))));
                Assert.That(_buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
                Assert.That(_buffers.CombatStateReadSink.Current.Structures.Single().Health.IsDead, Is.True);
                Assert.That(Main().Stats.CurrentActionCost, Is.EqualTo(2));
                Assert.That(_buffers.StageProgressReadSink.IsCleared(1, node), Is.True);
                Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.EqualTo(node == 3));
                int health = node == 2 ? 10 : 12;
                AssertPersistent(health);
                Assert.That(_core.Combat.ProcessNextCommand(), Is.False);
                Assert.That(_core.Combat.ResolveBattle(EmptyLoadout), Is.False);
                Assert.That(_core.Combat.AbandonBattle(), Is.Not.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
                AssertPersistent(health);

                _buffers.CombatResultReadSink.TryGetLatest(out var result);
                int sequence = result.Sequence;
                int handled = _buffers.CombatStateReadSink.HandledCommandNumber;
                int unhandled = _buffers.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
                var completion = _buffers.CombatStateReadSink.Completion;
                // 결과 확인과 남은 입력 제거는 View/Flow 소유자의 동작을 모사한다.
                _buffers.CombatViewWriteSink.AcknowledgePresented(sequence);
                _core.Combat.EndBattle();
                AssertCleaned();
                Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
                Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var afterCleanup), Is.True);
                Assert.That(afterCleanup, Is.SameAs(result));
                Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
                AssertPersistent(health);
                Assert.That(_buffers.CombatViewReadSink.Commands.Any(x => x.Number == unhandled), Is.True);
                _buffers.CombatViewWriteSink.Clear();
                if (node == 2) EnterStage(1, 3);
                else
                {
                    Assert.That(_core.ChapterProgression.EnterChapter(2).Accepted, Is.True);
                    EnterStage(2, 2);
                }
                Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
                Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
                Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var retained), Is.True);
                Assert.That(retained, Is.SameAs(result));
                Assert.That(_core.Combat.ProcessNextCommand(), Is.False);
                Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
                Assert.That(_core.Artifacts.InitializeBoard(4, 4), Is.False);
                Assert.That(_core.Characters.InitializeRoster(1).Succeeded, Is.False);
                AssertPersistent(health);
                Assert.That(Main().Health.MaxHealth, Is.EqualTo(12));
                Assert.That(Main().Stats.EffectiveAttackPower, Is.EqualTo(7));
                Assert.That(Main().TemporaryAttackBonus, Is.Zero);
                Assert.That(Main().TemporaryMaxHealthBonus, Is.Zero);
            }
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 3), Is.True);
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            Assert.That(_buffers.MapProgressReadSink.GetMap(2).IsClear, Is.False);
        }

        [Test]
        public void RegisteredCycle_AbandonmentSettlesOnceBeforeCleanup()
        {
            EnterStage(1, 2);
            var battle = _buffers.CombatStateReadSink.Current;
            Assert.Throws<InvalidOperationException>(() => _core.Combat.EndBattle());
            Assert.That(_buffers.CombatStateReadSink.Current, Is.SameAs(battle));
            var field = _buffers.CombatStateReadSink.SoulField;
            _buffers.BattleWriteSink.Apply(new SoulActionChanges(new CharacterChanges(
                Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(field, new SoulFieldSnapshot(2, new[] {
                    new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 100, 1, "fixture"),
                        new CellCoord(3, 3), 5, 1) })), Array.Empty<long>()));
            _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(Main().EntityId));
            Assert.That(_core.Combat.ProcessLatestPreview(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.True);
            Assert.That(_buffers.CombatStateReadSink.SoulField.Entries.Count, Is.EqualTo(1));
            int sequence = _buffers.CombatStateReadSink.LatestResultSequence;
            // 공개 포기 API는 정산 입력을 받지 않는다. Core가 현재 Artifact 배치로 직접 조립한다.
            Assert.That(_core.Combat.AbandonBattle(), Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
            var completion = _buffers.CombatStateReadSink.Completion;
            Assert.That(completion.Outcome.Reason, Is.EqualTo(StageOutcomeReason.VoluntaryAbandonment));
            // 완료 기록만 남기고 일반 전투 결과는 새로 게시하지 않는다.
            Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            Assert.That(_core.Combat.AbandonBattle(), Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.AlreadyCompleted));
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            AssertPersistent(7);
            var artifact = _buffers.ArtifactReadSink.Current;
            // Core의 종료 API는 표시 확인을 강제하지 않는다. 실제 전환 순서는 F2에서 검증한다.
            Assert.That(_buffers.CombatViewReadSink.PresentedResultSequence, Is.Zero);
            _core.Combat.EndBattle();
            AssertCleaned();
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            // 정리 뒤에도 완료 기록은 결과 표시를 위해 남으므로 다시 정산하지 않고 AlreadyCompleted다.
            Assert.That(_core.Combat.AbandonBattle(), Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.AlreadyCompleted));
            _buffers.CombatViewWriteSink.Clear();
            // 패배는 Run 종료이므로 정리 뒤에도 재준비로 다시 열 수 없다.
            Assert.That(Assert.Throws<InvalidOperationException>(() => _core.Combat.PrepareBattle()).Message,
                Does.Contain("RunEnded"));
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
            AssertPersistent(7);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RegisteredResolution_MainDeathTakesPriorityOverDestroyedHeadquarters(bool headquartersDead)
        {
            EnterStage(1, 2);
            if (headquartersDead)
            {
                var sub = _buffers.CharacterReadSink.Characters.First(x => x.Area == CharacterRuntimeArea.Hand);
                Command(CombatViewCommandData.PlaceUnit(sub.EntityId, new CellCoord(2, 1), Rotation4.R0));
                Command(CombatViewCommandData.EnterCombat());
                for (int i = 0; i < 8; i++)
                {
                    if (_buffers.CombatStateReadSink.Phase == CombatFlowPhase.Automatic)
                        Assert.That(_core.Combat.AdvanceCombat(), Is.True);
                    else if (_buffers.CombatStateReadSink.Current.CommandState.ActiveActorId == sub.EntityId)
                        break;
                    else Command(CombatViewCommandData.EndUnitTurn());
                }
                Assert.That(_buffers.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(sub.EntityId));
                // 사망·퇴장 사실은 테스트 입력이다. 본부 파괴와 패배 정산은 실제 명령이 수행한다.
                _core.Combat.ApplyHealth(Main().EntityId, 0);
                var dead = Main();
                var excluded = new CharacterCombatFacts(dead.EntityId, dead.OwnedId, dead.CharacterKey,
                    dead.Owner, dead.OwnershipRole, CharacterRuntimeArea.Excluded, dead.Health, dead.Stats,
                    null, null, null, Array.Empty<CellCoord>(), dead.MovementCheckpointAnchor,
                    0, 0, dead.TemporaryAttackBonus, dead.TemporaryMaxHealthBonus);
                _buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(dead, excluded) },
                    Array.Empty<SoulCountChange>()));
                Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
                Command(CombatViewCommandData.Attack(sub.EntityId, AttackAim.ForCell(new CellCoord(1, 3))));
                Assert.That(_buffers.CombatStateReadSink.Current.Structures.Single().Health.IsDead, Is.True);
            }
            else
            {
                _core.Combat.ApplyHealth(Main().EntityId, 0);
                Assert.That(_core.Combat.ResolveBattle(EmptyLoadout), Is.True);
            }
            Assert.That(_buffers.CombatStateReadSink.Completion.Outcome.Reason, Is.EqualTo(StageOutcomeReason.PlayerMainCharacterDied));
            Assert.That(_buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            Assert.That(_core.Combat.ResolveBattle(EmptyLoadout), Is.False);
            _core.Combat.EndBattle();
            AssertCleaned();
            AssertPersistent(0);
        }

        private void EnterStage(int chapter, int node)
        {
            Assert.That(_core.Combat.EnterBattle(chapter, new StageNodeId(node)).Accepted, Is.True);
        }

        private void Command(CombatViewCommandData command)
        {
            int number = _buffers.CombatViewWriteSink.Submit(command);
            Assert.That(_core.Combat.ProcessNextCommand(), Is.True);
            Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        private CharacterCombatFacts Main() => _buffers.CharacterReadSink.Characters
            .Single(x => x.OwnershipRole == CharacterOwnershipRole.Main).ToFacts();

        private void AssertPersistent(int health)
        {
            Assert.That(_buffers.CharacterReadSink.Owned, Is.EqualTo(_owned));
            Assert.That(_buffers.CharacterReadSink.GetSoulCount(_mainId), Is.EqualTo(2));
            Assert.That(_buffers.CharacterReadSink.TryGetMainState(out var main), Is.True);
            Assert.That(main.CurrentHealth, Is.EqualTo(health));
            Assert.That(main.PersistentMaxHealth, Is.EqualTo(10));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(91));
        }

        private void AssertCleaned()
        {
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(_buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(_buffers.CharacterReadSink.TryGetOwnedInstance(new EntityId(1), out _), Is.False);
            Assert.That(_buffers.CombatStateReadSink.SoulField.Entries, Is.Empty);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
        }

        internal sealed class CycleSource : IBattleInitializationSource
        {
            private readonly BattleInitialization _definition;
            internal readonly List<(int Chapter, int Stage)> Reads = new();
            internal bool DistinctTargets;
            internal bool MovementCycle;
            internal bool LethalRailway;
            internal bool ShortNight;
            internal string Fault;
            internal Action BeforeReturn;
            internal CycleSource(CycleDatabase database)
            {
                var units = database.UnitTableQuery.GetAllUnits().Select(BattleDefinitionAdapter.ReadUnit).ToArray();
                var stage = new StageConfig(4, 5, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                    Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), 100,
                    new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(1, 3), Rotation4.R0) });
                var categories = CharacterBattlePreparationTests.Initialization().Categories;
                _definition = new BattleInitialization(stage, new CellCoord(1, 1), Rotation4.R0, units,
                    new[] { new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 5, false, 0) },
                    categories, units.Select(unit => new CombatAttackProfile(unit,
                        new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray(),
                    new StageEconomySpec(27, 2));
            }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                Reads.Add((chapterKey, stageKey.Value));
                BeforeReturn?.Invoke();
                if (Fault == "Throw") throw new InvalidOperationException("Fixture definition fault");
                if (Fault == "Null") return null;
                var definition = _definition;
                if (MovementCycle)
                {
                    var stage = new StageConfig(8, 6, 8, 2, 3, 42,
                        new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(4, 3), Rotation4.R0) },
                        Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), 100,
                        new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(1, 4), Rotation4.R0) });
                    definition = new BattleInitialization(stage, _definition.MainAnchor, _definition.MainRotation,
                        _definition.Units, _definition.Structures, _definition.Categories, _definition.AttackProfiles, _definition.Economy);
                }
                if (ShortNight)
                {
                    var stage = new StageConfig(4, 5, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                        Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), 1,
                        new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(1, 3), Rotation4.R0) });
                    definition = new BattleInitialization(stage, _definition.MainAnchor, _definition.MainRotation,
                        _definition.Units, _definition.Structures, _definition.Categories,
                        _definition.AttackProfiles, _definition.Economy);
                }
                if (LethalRailway)
                {
                    var stage = new StageConfig(4, 5, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                        new[] { new CellCoord(1, 1) }, new StageGimmickSpec[] { new RailwayGimmickSpec(1, 1) }, 100,
                        new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(1, 3), Rotation4.R0) });
                    definition = new BattleInitialization(stage, _definition.MainAnchor, _definition.MainRotation,
                        _definition.Units, _definition.Structures, _definition.Categories,
                        _definition.AttackProfiles, _definition.Economy);
                }
                if (DistinctTargets)
                {
                    bool next = stageKey.Value == 102;
                    var stage = new StageConfig(next ? 10 : 8, 6, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                        Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), 100,
                        new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(1, 4), Rotation4.R0) });
                    definition = new BattleInitialization(stage, _definition.MainAnchor, _definition.MainRotation,
                        _definition.Units, _definition.Structures, _definition.Categories,
                        _definition.Units.Select(unit => new CombatAttackProfile(unit,
                            new TargetingMethodSpec(next ? TargetingShape.CardinalLine : TargetingShape.RadialSquare,
                                TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray(), new StageEconomySpec(27, next ? 5 : 2));
                }
                return Fault == "Placement" ? new BattleInitialization(definition.Stage, new CellCoord(99, 99),
                    definition.MainRotation, definition.Units, definition.Structures, definition.Categories,
                    definition.AttackProfiles, definition.Economy) : definition;
            }
        }

        internal class CycleDatabase : IDataBaseQueryContext, IInGameQuery, IStageTableQuery,
            IStageMapTableQuery, IArtifactDefinitionTableQuery, IRecoveryOptionTableQuery
        {
            private readonly CharacterRosterDatabase _roster = new();
            internal Func<int, StageRootData> ReplaceStage;
            internal Action OnStagesRead;
            private readonly StageMapData[] _maps = { Map(1), Map(2) };
            private readonly StageRootData[] _stages = {
                Stage(101, 1, StageMapRole.Regular), Stage(102, 1, StageMapRole.Boss), Stage(201, 2, StageMapRole.Regular), Stage(202, 2, StageMapRole.Boss),
                new StageRootData(20, 1, StageMapRole.Regular, new EventStageDetail(new[] { 2, 1 })) };
            private readonly RecoveryOptionData[] _recoveryOptions = {
                new RecoveryOptionData(2, 3, RecoveryHealMode.Full, 0),
                new RecoveryOptionData(1, 0, RecoveryHealMode.Flat, 1) };
            private readonly ArtifactDefinitionData _artifact = new(100, 1, new[] { CellCoord.Zero }, new[] {
                new ArtifactEffectData { entryId = "chalice", trigger = ArtifactTriggerType.StageCleared,
                    source = new ArtifactSourceData { faction = ArtifactFactionFilter.Any },
                    target = new ArtifactTargetData { type = ArtifactTargetType.MainCharacter, faction = ArtifactFactionFilter.Ally },
                    effect = new ArtifactEffectPayloadData { type = ArtifactEffectType.Heal }, value = new ArtifactValueData { amount = 3 } } });
            private static StageMapData Map(int key) => new(key, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
            internal static StageRootData Stage(int key, int map, StageMapRole role) => new(key, map, role,
                new CombatStageDetail(42, "fixture", 1, 0, 1, 100, Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(),
                    Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters, new CellCoord(1, 1), Rotation4.R0,
                    new CellCoord(1, 3), Rotation4.R0, null));
            public virtual StageRootData GetStage(int key) => ReplaceStage?.Invoke(key) ?? _stages.Single(x => x.StageKey == key);
            public virtual IReadOnlyList<StageRootData> GetStages(int mapKey)
            {
                OnStagesRead?.Invoke();
                return _stages.Where(x => x.MapKey == mapKey).ToArray();
            }
            public virtual StageMapData GetStageMap(int key) => _maps.Single(x => x.MapKey == key);
            public virtual IReadOnlyCollection<StageMapData> GetAllStageMaps() => _maps;
            public ArtifactDefinitionData GetDefinition(int id) => id == 100 ? _artifact : throw new KeyNotFoundException();
            public bool TryGetDefinition(int id, out ArtifactDefinitionData data) { data = id == 100 ? _artifact : null; return data != null; }
            public IReadOnlyCollection<ArtifactDefinitionData> GetAllDefinitions() => new[] { _artifact };
            public IInGameQuery InGameQuery => this;
            public IStageTableQuery StageTableQuery => this;
            public IStageMapTableQuery StageMapTableQuery => this;
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => this;
            public IUnitTableQuery UnitTableQuery => _roster.UnitTableQuery;
            public ITargetingMethodTableQuery TargetingMethodTableQuery => throw new NotSupportedException();
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster.PlayerLoadoutTableQuery;
            public IFieldTableQuery FieldTableQuery => throw new NotSupportedException();
            public IStructureTableQuery StructureTableQuery => throw new NotSupportedException();
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException();
            public IUnitRosterTableQuery UnitRosterTableQuery => throw new NotSupportedException();
            public ICategoryConfigTableQuery CategoryConfigTableQuery => throw new NotSupportedException();
            public IStageEconomyTableQuery StageEconomyTableQuery => throw new NotSupportedException();
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => this;
            public RecoveryOptionData GetRecoveryOption(int key) => _recoveryOptions.Single(x => x.OptionId == key);
            public bool TryGetRecoveryOption(int key, out RecoveryOptionData data)
            { data = _recoveryOptions.SingleOrDefault(x => x.OptionId == key); return data != null; }
            public IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions() => _recoveryOptions;
            public IRenderQuery RenderQuery => throw new NotSupportedException();
            public IViewQuery ViewQuery => throw new NotSupportedException();
        }
    }
}
