using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 실제 Flow·Register·Plan·Runner·Core·Buffer를 연결한다.
    /// 준비 정의와 진입 전 진행 데이터만 fixture로 제공하고 전투 상태·결과는 Core가 계산·게시한다.
    /// 표시 완료는 테스트 소비자로 모사하며 실제 View·F1·F2의 검증을 대신하지 않는다.
    /// </summary>
    public sealed class CombatFlowIntegrationTests
    {
        private static readonly Regex FlowError = new(@"\[CombatFlow\]");
        private static readonly CellCoord InitialMainCell = new(0, 1);
        private static readonly CellCoord HeadquartersCell = new(0, 3);

        public enum ResultFault
        {
            None,
            Missing,
            Rejected,
            WrongKind,
            WrongSequence
        }

        [Test]
        public void Preparation_RegistersStartOnce_BeforeQueuedCommandCanExecute()
        {
            using var fixture = new Fixture();
            int command = fixture.Input.Submit(CombatViewCommandData.PlaceMainCharacter(new CellCoord(1, 0), Rotation4.R0));

            fixture.Flow.BeginPreparation();
            fixture.Flow.Tick();
            fixture.Flow.Tick();

            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(fixture.Source.ReadCount, Is.Zero);
            Assert.That(fixture.Register.Calls, Is.EqualTo(new[] { JobType.Start }));

            fixture.Frame(false);

            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(fixture.Source.ReadCount, Is.EqualTo(1));
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(fixture.Register.Calls, Is.EqualTo(new[] { JobType.Start, JobType.ProcessCommand }));

            fixture.Frame(true);
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(command));
            Assert.That(fixture.Main.Anchor, Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(fixture.Runner.IsIdle, Is.True);
            Assert.That(fixture.Source.ReadCount, Is.EqualTo(1));
        }

        [Test]
        public void NoInput_LeavesRunnerIdleWithoutRegisteringProcessingJobs()
        {
            using var fixture = new Fixture();
            fixture.Prepare();

            fixture.Frame(true);
            fixture.Frame(true);

            Assert.That(fixture.Register.Calls, Is.EqualTo(new[] { JobType.Start }));
            Assert.That(fixture.Runner.IsIdle, Is.True);
            Assert.That(fixture.Buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Command_AcceptanceAndRejection_BothWaitForPresentationBeforeNextCommand(bool accepted)
        {
            using var fixture = new Fixture();
            fixture.Prepare();
            var target = accepted ? new CellCoord(1, 0) : new CellCoord(99, 99);
            int first = fixture.RegisterCommand(CombatViewCommandData.PlaceMainCharacter(target, Rotation4.R0));
            int next = fixture.Input.Submit(CombatViewCommandData.PlaceMainCharacter(InitialMainCell, Rotation4.R0));

            fixture.Frame(false);
            CombatResult result = fixture.Latest();
            Assert.That(result.Accepted, Is.EqualTo(accepted), result.Reason);
            Assert.That(result.CommandNumber, Is.EqualTo(first));
            Assert.That(result.Sequence, Is.EqualTo(1));
            Assert.That(fixture.Main.Anchor, Is.EqualTo(accepted ? target : InitialMainCell));
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(first));
            Assert.That(fixture.Buffers.CombatViewReadSink.Commands.Count, Is.EqualTo(2));
            Assert.That(fixture.Buffers.CombatViewReadSink.PresentedResultSequence, Is.Zero);

            fixture.Frame(false);
            Assert.That(fixture.Register.Calls.Count, Is.EqualTo(2));
            Assert.That(fixture.Latest(), Is.SameAs(result));
            Assert.That(fixture.Runner.IsIdle, Is.True);

            fixture.PresentLatest();
            fixture.Flow.Tick();
            Assert.That(fixture.Latest(), Is.SameAs(result));
            fixture.Frame(true);

            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(next));
            Assert.That(fixture.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(2));
            Assert.That(fixture.Buffers.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(fixture.Flow.IsStopped, Is.False);
            Assert.That(fixture.Runner.IsIdle, Is.True);
        }

        [Test]
        public void CommandRemovedAfterRegistration_NormalFalseReturnsRunnerToIdle_AndFlowStopsOnce()
        {
            using var fixture = new Fixture();
            fixture.Prepare();
            int number = fixture.RegisterCommand(CombatViewCommandData.PlaceMainCharacter(new CellCoord(1, 0), Rotation4.R0));
            // K14 위반을 고의 주입한다. 정상 View는 처리 전에 명령을 삭제하지 않는다.
            fixture.Input.RemoveCommandsThrough(number);

            LogAssert.Expect(LogType.Error, FlowError);
            fixture.Frame(false);
            fixture.Input.Submit(CombatViewCommandData.EnterCombat());
            fixture.Frame(false);
            fixture.Frame(false);

            Assert.That(fixture.Runner.IsIdle, Is.True);
            Assert.That(fixture.Flow.IsStopped, Is.True);
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(fixture.Buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(fixture.Register.Calls, Is.EqualTo(new[] { JobType.Start, JobType.ProcessCommand }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Preview_ReplacedOrClearedBeforeExecution_UsesLatestRequestWithoutGameplayPublication(bool clear)
        {
            using var fixture = new Fixture();
            fixture.Prepare();
            var before = fixture.Buffers.CombatStateReadSink.Current;
            int first = fixture.Input.SetPreview(CombatViewPreviewData.MainCharacterPlacement(new CellCoord(1, 0), Rotation4.R0));
            fixture.Flow.Tick();
            int latest = first;
            if (clear)
                fixture.Input.ClearPreview();
            else
                latest = fixture.Input.SetPreview(CombatViewPreviewData.MainCharacterPlacement(new CellCoord(2, 0), Rotation4.R0));

            fixture.Frame(false);
            fixture.Frame(false);

            Assert.That(fixture.Buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(clear ? 0 : latest));
            Assert.That(fixture.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.EqualTo(!clear));
            if (!clear)
            {
                Assert.That(preview.RequestNumber, Is.EqualTo(latest));
                Assert.That(preview.Placement.Value.CanPlace, Is.True);
                Assert.That(latest, Is.GreaterThan(first));
            }
            Assert.That(fixture.Buffers.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(fixture.Main.Anchor, Is.EqualTo(InitialMainCell));
            Assert.That(fixture.Buffers.CurrencyReadSink.Amount, Is.EqualTo(20));
            Assert.That(fixture.Buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(fixture.Buffers.CombatViewReadSink.PresentedResultSequence, Is.Zero);
            Assert.That(fixture.Runner.IsIdle, Is.True);
            Assert.That(fixture.Flow.IsStopped, Is.False);
            Assert.That(fixture.Register.Calls, Is.EqualTo(new[] { JobType.Start, JobType.ProcessPreview }));
        }

        [Test]
        public void CommandWinsOverPreview_ThenPreviewRunsWhileResultPresentationIsPending()
        {
            using var fixture = new Fixture();
            fixture.Prepare();
            int preview = fixture.Input.SetPreview(CombatViewPreviewData.MainCharacterPlacement(new CellCoord(2, 0), Rotation4.R0));
            int command = fixture.RegisterCommand(CombatViewCommandData.PlaceMainCharacter(new CellCoord(1, 0), Rotation4.R0));

            fixture.Frame(false);
            var result = fixture.Latest();
            Assert.That(fixture.Register.Calls, Is.EqualTo(new[] { JobType.Start, JobType.ProcessCommand, JobType.ProcessPreview }));
            Assert.That(fixture.Buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.Zero);
            fixture.Frame(false);

            Assert.That(fixture.Buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(preview));
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(command));
            Assert.That(command, Is.GreaterThan(preview));
            Assert.That(fixture.Latest(), Is.SameAs(result));
            Assert.That(fixture.Buffers.CombatViewReadSink.PresentedResultSequence, Is.Zero);
            fixture.PresentLatest();
            fixture.Flow.Tick();
            Assert.That(fixture.Flow.IsStopped, Is.False);
            Assert.That(fixture.Runner.IsIdle, Is.True);
        }

        [Test]
        public void Automatic_WaitsForEnterCombatPresentation_ThenConfirmsResultAfterViewAcknowledgement()
        {
            using var fixture = new Fixture();
            fixture.Prepare();
            int command = fixture.RegisterCommand(CombatViewCommandData.EnterCombat());
            fixture.Frame(false);

            Assert.That(fixture.Latest().Accepted, Is.True);
            Assert.That(fixture.Buffers.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            fixture.Frame(false);
            Assert.That(fixture.Register.Calls.Count, Is.EqualTo(2));
            Assert.That(fixture.Runner.IsIdle, Is.True);

            fixture.PresentLatest();
            fixture.Flow.Tick();
            Assert.That(fixture.Register.Calls.Last(), Is.EqualTo(JobType.AdvanceCombat));
            // 실제 순서: Runner → View 역할의 확인 → Flow. Flow가 확인할 때 결과는 이미 표시됐다.
            fixture.Frame(true);

            var automatic = fixture.Latest();
            Assert.That(automatic.Kind, Is.EqualTo(CombatProcessingKind.Advance));
            Assert.That(automatic.Accepted, Is.True, automatic.Reason);
            Assert.That(automatic.Sequence, Is.EqualTo(2));
            Assert.That(automatic.CommandNumber, Is.Zero);
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(command));
            Assert.That(fixture.Buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(automatic.Sequence));
            Assert.That(fixture.Buffers.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
            Assert.That(fixture.Flow.IsStopped, Is.False);
            Assert.That(fixture.Runner.IsIdle, Is.True);
        }

        [TestCase(ResultFault.Missing)]
        [TestCase(ResultFault.Rejected)]
        [TestCase(ResultFault.WrongKind)]
        [TestCase(ResultFault.WrongSequence)]
        public void Automatic_InvalidReadAfterRealPublication_StopsOnceWithoutRegisteringAgain(ResultFault fault)
        {
            using var fixture = new Fixture();
            fixture.Prepare();
            fixture.RegisterCommand(CombatViewCommandData.EnterCombat());
            fixture.Frame(true);
            Assert.That(fixture.Register.Calls.Last(), Is.EqualTo(JobType.AdvanceCombat));
            // Core의 실제 게시를 훼손하지 않고 Flow가 보는 읽기 경계에만 실패를 주입한다.
            fixture.Results.Fault = fault;

            LogAssert.Expect(LogType.Error, FlowError);
            fixture.Frame(true);
            fixture.Frame(true);
            fixture.Frame(true);

            Assert.That(fixture.Latest().Accepted, Is.True, "실제 Core의 정상 결과와 실패 주입을 구분한다.");
            Assert.That(fixture.Latest().Sequence, Is.EqualTo(2));
            Assert.That(fixture.Flow.IsStopped, Is.True);
            Assert.That(fixture.Register.Calls.Count, Is.EqualTo(3));
            Assert.That(fixture.Runner.IsIdle, Is.True);
        }

        [Test]
        public void Completion_EndBattleAndNextPreparation_PreserveAcknowledgedResultAndNumbers()
        {
            using var fixture = new Fixture();
            fixture.Prepare();
            int preview = fixture.Input.SetPreview(CombatViewPreviewData.MainCharacterPlacement(InitialMainCell, Rotation4.R0));
            fixture.Flow.Tick();
            fixture.Frame(false);
            fixture.RegisterCommand(CombatViewCommandData.EnterCombat());
            fixture.Frame(true);
            fixture.Frame(true);
            var path = new MovementPath(new[] { fixture.Main.Anchor.Value, new CellCoord(0, 2) });
            preview = fixture.Input.SetPreview(CombatViewPreviewData.Move(fixture.Main.EntityId, path));
            fixture.Flow.Tick(); fixture.Frame(false);
            Assert.That(fixture.Buffers.CombatStateReadSink.TryGetLatestPreview(out var movePreview), Is.True);
            fixture.RegisterCommand(CombatViewCommandData.Move(fixture.Main.EntityId, path, movePreview.MovementContext));
            fixture.Frame(true);
            int attack = fixture.RegisterCommand(CombatViewCommandData.Attack(fixture.Main.EntityId, AttackAim.ForCell(HeadquartersCell)));
            fixture.Frame(true);

            var final = fixture.Latest();
            var completion = fixture.Buffers.CombatStateReadSink.Completion;
            Assert.That(final.Accepted, Is.True, final.Reason);
            Assert.That(completion, Is.Not.Null);
            Assert.That(completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(fixture.Buffers.StageProgressReadSink.IsCleared(1, 1), Is.True);
            Assert.That(fixture.Buffers.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(fixture.Runner.IsIdle, Is.True);

            // 데이터 수명만 검증한다. 씬 이탈·결과 화면·F2가 구현됐다는 뜻은 아니다.
            fixture.Flow.Stop();
            fixture.Core.Combat.EndBattle();
            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(fixture.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(fixture.Buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            Assert.That(fixture.Latest(), Is.SameAs(final));
            Assert.That(fixture.Core.ChapterProgression.ConfirmStageEntry(1, new StageNodeId(2)).Accepted, Is.True);
            fixture.Input.Clear();
            fixture.Flow.Reset();
            fixture.Prepare();

            Assert.That(fixture.Buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(fixture.Buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            Assert.That(fixture.Latest(), Is.SameAs(final));
            Assert.That(fixture.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(final.Sequence));
            Assert.That(fixture.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(attack));
            Assert.That(fixture.Buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(preview));
            Assert.That(fixture.Buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(final.Sequence));
            Assert.That(fixture.Source.ReadCount, Is.EqualTo(2));
            int next = fixture.RegisterCommand(CombatViewCommandData.PlaceMainCharacter(new CellCoord(1, 0), Rotation4.R0));
            fixture.Frame(true);
            Assert.That(next, Is.GreaterThan(attack));
            Assert.That(fixture.Latest().Sequence, Is.EqualTo(final.Sequence + 1));
            Assert.That(fixture.Flow.IsStopped, Is.False);
        }

        [Test]
        public void RealPreparationException_PreservesCauseAndRunnerJobWithoutPublishingBattle()
        {
            using var fixture = new Fixture();
            var failure = new InvalidOperationException("준비 정의 실패 주입");
            fixture.Source.Failure = failure;
            fixture.Flow.BeginPreparation();

            Assert.That(Assert.Throws<InvalidOperationException>(() => fixture.Frame(false)), Is.SameAs(failure));
            Assert.That(fixture.Runner.IsIdle, Is.False);
            Assert.That(fixture.Source.ReadCount, Is.EqualTo(1));
            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(fixture.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(fixture.Buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
            // 예외 뒤 다시 Tick하지 않는다. 테스트 Dispose로 정리하며 F1 보장을 주장하지 않는다.
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly DefinitionSource Source = new();
            internal readonly JobRunnerSystem Runner = new();
            internal readonly IInGameCoreSystemGroupService Core;
            internal readonly IBufferQuery Buffers;
            internal readonly RegistrationLog Register;
            internal readonly ResultReadFault Results;
            internal readonly CombatFlow Flow;
            private readonly JobRegisterSystem _registry;

            internal ICombatViewBufferWriteSink Input => Buffers.CombatViewWriteSink;
            internal ProjectL.Systems.RuntimeSystem.BufferSystems.Character.CharacterData Main =>
                Buffers.CharacterReadSink.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);

            internal Fixture()
            {
                var root = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(root);
                var core = InGameCoreSystemFactory.Create(Source);
                Core = (IInGameCoreSystemGroupService)core;
                root.AddService<IInGameSystemGroupService>(new InGameServices(Core));
                TestLifeCycleGroup.Add(root);
                var resolver = root.BuildSystemReader().ServiceStoreReader;
                Buffers = resolver.GetService(BufferSystemKey.Buffer);
                ((ISystemInitData)core).InitData(new IntegrationDatabase());
                ((ISystemPostBindService)core).PostBindService(resolver);
                Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
                // 전투 진입 전 맵 그래프·선택·재화만 fixture로 제공한다. 전투 상태·번호는 직접 쓰지 않는다.
                Buffers.MapProgressWriteSink.RegisterMap(1);
                Buffers.StageProgressWriteSink.RegisterStages(1, new[]
                {
                    new StageProgressData(1, 100, 1, 0, new[] { 2 }, stageType: StageType.Combat),
                    new StageProgressData(2, 101, 2, 0, Array.Empty<int>(), stageType: StageType.Combat)
                });
                Buffers.CurrentProgressWriteSink.SelectMap(1);
                Buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(1), new StageKey(100), "Test combat");
                Buffers.CurrencyWriteSink.SetAmount(20);
                _registry = new JobRegisterSystem(Runner);
                _registry.PostBindService(resolver);
                _registry.Initialize();
                Register = new RegistrationLog(_registry);
                Results = new ResultReadFault(Buffers.CombatResultReadSink);
                Flow = new CombatFlow(Buffers.CombatStateReadSink, Results, Buffers.CombatViewReadSink, Runner, Register,
                    CombatExecutionMode.Live, new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            }

            internal void Prepare()
            {
                Flow.BeginPreparation();
                Frame(false);
                Assert.That(Buffers.CombatStateReadSink.HasBattle, Is.True);
            }

            internal int RegisterCommand(CombatViewCommandData data)
            {
                Assert.That(Runner.IsIdle, Is.True);
                int number = Input.Submit(data);
                Flow.Tick();
                Assert.That(Register.Calls.Last(), Is.EqualTo(JobType.ProcessCommand));
                Assert.That(Runner.IsIdle, Is.False);
                return number;
            }

            internal void Frame(bool present)
            {
                Runner.Tick(0);
                if (present)
                    PresentLatest();
                Flow.Tick();
            }

            internal CombatResult Latest()
            {
                Assert.That(Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                return result;
            }

            internal void PresentLatest()
            {
                int sequence = Buffers.CombatStateReadSink.LatestResultSequence;
                if (Buffers.CombatViewReadSink.PresentedResultSequence >= sequence)
                    return;
                var result = Latest();
                Assert.That(result.Sequence, Is.EqualTo(sequence));
                // 실제 표시 없이 완료만 모사한다. 후처리와 명령 정리 뒤 마지막에 확인한다.
                Input.ClearPreview();
                Input.RemoveCommandsThrough(Buffers.CombatStateReadSink.HandledCommandNumber);
                Input.AcknowledgePresented(sequence);
            }

            public void Dispose()
            {
                Flow.Stop();
                _registry.Shutdown();
                Runner.Clear();
            }
        }

        private sealed class RegistrationLog : IJobRegisterSystem
        {
            public void RegisterCombatEntry(CombatEntryRequest request) => _actual.RegisterCombatEntry(request);
            private readonly IJobRegisterSystem _actual;
            internal readonly List<JobType> Calls = new();

            internal RegistrationLog(IJobRegisterSystem actual)
            {
                _actual = actual;
            }

            public void Register(JobRootType rootType, JobType jobType)
            {
                _actual.Register(rootType, jobType);
                Calls.Add(jobType);
            }
        }

        private sealed class ResultReadFault : ICombatResultReadSink
        {
            private readonly ICombatResultReadSink _actual;
            internal ResultFault Fault;

            internal ResultReadFault(ICombatResultReadSink actual)
            {
                _actual = actual;
            }

            public bool TryGetLatest(out CombatResult result)
            {
                bool found = _actual.TryGetLatest(out result);
                if (!found || Fault == ResultFault.None)
                    return found;
                if (Fault == ResultFault.Missing)
                {
                    result = null;
                    return false;
                }
                result = new CombatResult(result.Sequence + (Fault == ResultFault.WrongSequence ? 1 : 0),
                    Fault == ResultFault.WrongKind ? CombatProcessingKind.Command : CombatProcessingKind.Advance,
                    Fault == ResultFault.WrongKind ? 1 : 0, Fault != ResultFault.Rejected,
                    Fault == ResultFault.Rejected ? "Injected.Rejection" : "");
                return true;
            }
        }

        private sealed class InGameServices : IInGameSystemGroupService
        {
            public IInGameCoreSystemGroupService CoreGroup { get; }
            public IInGameViewSystemGroupService ViewGroup => null;

            internal InGameServices(IInGameCoreSystemGroupService core)
            {
                CoreGroup = core;
            }
        }

        private sealed class DefinitionSource : IBattleInitializationSource
        {
            internal int ReadCount;
            internal Exception Failure;
            private readonly BattleInitialization _definition = CreateDefinition();

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                ReadCount++;
                if (Failure != null)
                    throw Failure;
                return _definition;
            }

            private static BattleInitialization CreateDefinition()
            {
                var seed = CharacterBattlePreparationTests.Initialization();
                var units = seed.Units.Concat(new[]
                {
                    new UnitSpec(EntityKey.Unit_Sniper, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero },
                        10, MoveMethod.LiftAndPlace, 3, 1, 2, CategoryKey.Base,
                        5, 4, 1, 0, 1, 1, true, Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly)
                }).ToArray();
                var stage = new StageConfig(8, 6, 8, 2, 3, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                    Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), 100,
                    new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, HeadquartersCell, Rotation4.R0) });
                var structures = new[] { new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 1, false, 0) };
                var profiles = units.Select(unit => new CombatAttackProfile(unit,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
                return new BattleInitialization(stage, InitialMainCell, Rotation4.R0, units, structures,
                    seed.Categories, profiles, new StageEconomySpec(0, 2));
            }
        }

        /// <summary>기존 로스터 DB fixture에 두 전투 노드의 조회만 덧붙인다. 운영 DB 준비를 구현하지 않는다.</summary>
        private sealed class IntegrationDatabase : IDataBaseQueryContext, IInGameQuery, IStageTableQuery
        {
            private readonly CharacterRosterDatabase _roster = new();
            public IInGameQuery InGameQuery => this;
            public IRenderQuery RenderQuery => _roster.RenderQuery;
            public IViewQuery ViewQuery => _roster.ViewQuery;
            public IStageTableQuery StageTableQuery => this;
            public IUnitTableQuery UnitTableQuery => _roster;
            public ITargetingMethodTableQuery TargetingMethodTableQuery => throw new NotSupportedException();
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster;
            public IStageMapTableQuery StageMapTableQuery => _roster.StageMapTableQuery;
            public IFieldTableQuery FieldTableQuery => _roster.FieldTableQuery;
            public IStructureTableQuery StructureTableQuery => _roster.StructureTableQuery;
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => _roster.EnemyLoadoutTableQuery;
            public IUnitRosterTableQuery UnitRosterTableQuery => _roster.UnitRosterTableQuery;
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _roster.ArtifactDefinitionTableQuery;
            public ICategoryConfigTableQuery CategoryConfigTableQuery => _roster.CategoryConfigTableQuery;
            public IStageEconomyTableQuery StageEconomyTableQuery => _roster.StageEconomyTableQuery;
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => _roster.RecoveryOptionTableQuery;

            public StageRootData GetStage(int key)
            {
                if (key != 100 && key != 101)
                    throw new KeyNotFoundException();
                return new StageRootData(key, 1, StageMapRole.Regular, new CombatStageDetail(42, "test-field", 1, 0,
                    1, 100, Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(),
                    EntityKey.Structure_Headquarters, InitialMainCell, Rotation4.R0, HeadquartersCell, Rotation4.R0, null));
            }

            public IReadOnlyList<StageRootData> GetStages(int mapKey) =>
                mapKey == 1 ? new[] { GetStage(100), GetStage(101) } : Array.Empty<StageRootData>();
        }
    }
}
