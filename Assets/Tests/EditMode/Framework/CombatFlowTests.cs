using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// CombatFlow의 등록 판단·확인·정지를 가짜 상태·가짜 결과 읽기와 실제 입력 Buffer로 확인한다.
    /// 등록된 Job은 실행하지 않고, 테스트가 Core 역할로 상태 값과 결과를 바꾼 뒤 Runner를 비운다.
    /// </summary>
    public sealed class CombatFlowTests
    {
        private static readonly EntityId Unit = new(7);
        private static readonly Regex FlowError = new(@"\[CombatFlow\]");

        /// <summary>자동 진행이 정상 반환한 뒤 Core가 남긴 결과의 잘못된 모양입니다.</summary>
        public enum AdvanceFailure
        {
            Rejected,
            Missing,
            CommandKind,
            SequenceUnchanged,
            SequenceMismatch
        }

        private FakeState _state;
        private FakeResults _results;
        private CombatViewBuffer _input;
        private FakeRunner _runner;
        private RecordingRegister _register;
        private RuntimeFaultBoundary _faults;
        private CombatFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _state = new FakeState();
            _results = new FakeResults();
            _input = new CombatViewBuffer();
            _runner = new FakeRunner();
            _register = new RecordingRegister(_runner);
            _faults = new RuntimeFaultBoundary(_ => { });
            _flow = CreateFlow(CombatExecutionMode.Live);
        }

        [Test]
        public void Constructor_RejectsNullDependencies()
        {
            const CombatExecutionMode live = CombatExecutionMode.Live;
            Assert.Throws<ArgumentNullException>(() => new CombatFlow(null, _results, _input, _runner, _register, live, _faults));
            Assert.Throws<ArgumentNullException>(() => new CombatFlow(_state, null, _input, _runner, _register, live, _faults));
            Assert.Throws<ArgumentNullException>(() => new CombatFlow(_state, _results, null, _runner, _register, live, _faults));
            Assert.Throws<ArgumentNullException>(() => new CombatFlow(_state, _results, _input, null, _register, live, _faults));
            Assert.Throws<ArgumentNullException>(() => new CombatFlow(_state, _results, _input, _runner, null, live, _faults));
            Assert.Throws<ArgumentNullException>(() => new CombatFlow(_state, _results, _input, _runner, _register, live, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CombatFlow(_state, _results, _input, _runner, _register, (CombatExecutionMode)99, _faults));
        }

        [Test]
        public void BeginPreparation_RejectsStoppedBusyRunnerExistingBattleAndPendingCheck_WithoutRegistering()
        {
            // 정상 Stop 뒤에도 Reset 없이는 새 등록을 하지 않는다.
            _flow.Stop();
            Assert.Throws<InvalidOperationException>(_flow.BeginPreparation);
            _flow.Reset();

            _runner.Idle = false;
            Assert.Throws<InvalidOperationException>(_flow.BeginPreparation);
            _runner.Idle = true;

            _state.HasBattle = true;
            Assert.Throws<InvalidOperationException>(_flow.BeginPreparation);
            _state.HasBattle = false;

            Assert.That(_register.Calls, Is.Empty);

            _flow.BeginPreparation();
            _runner.Idle = true;
            Assert.Throws<InvalidOperationException>(_flow.BeginPreparation);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.Start }));
        }

        [Test]
        public void Preparation_GeneralInputWaitsUntilBattleIsConfirmed()
        {
            _flow.BeginPreparation();
            _input.Submit(CombatViewCommandData.EndUnitTurn());

            _flow.Tick();
            _flow.Tick();
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.Start }));

            CompleteJob(() =>
            {
                _state.HasBattle = true;
                _state.Phase = CombatFlowPhase.PlayerInput;
            });
            _flow.Tick();

            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.Start, JobType.ProcessCommand }));
            Assert.That(_flow.IsStopped, Is.False);
        }

        [Test]
        public void Preparation_ReturningWithoutBattle_ReportsOnceAndStops()
        {
            _flow.BeginPreparation();
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            CompleteJob(() => { });

            LogAssert.Expect(LogType.Error, FlowError);
            _flow.Tick();
            _state.HasBattle = true;
            _state.Phase = CombatFlowPhase.PlayerInput;
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.True);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.Start }));
        }

        [Test]
        public void BusyRunner_RegistersNothing_AndSameCommandIsRegisteredOnce()
        {
            StartPlayerInput();
            _input.Submit(CombatViewCommandData.EndUnitTurn());

            _flow.Tick();
            _flow.Tick();
            _flow.Tick();

            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessCommand }));
        }

        [Test]
        public void CommandAndPreview_CommandFirstWhenPresented_PreviewOnlyWhilePresenting()
        {
            StartPlayerInput();
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));

            _flow.Tick();
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessCommand }));

            // Core가 명령 1을 처리해 결과 1을 남겼고, View는 아직 연출 중이다.
            CompleteJob(() =>
            {
                _state.HandledCommandNumber = 1;
                _state.LatestResultSequence = 1;
            });
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _flow.Tick();

            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessCommand, JobType.ProcessPreview }));
        }

        [Test]
        public void ContinuousPreviews_DoNotDelayPendingCommandAfterPresentation()
        {
            StartPlayerInput();
            _state.LatestResultSequence = 1;
            _input.Submit(CombatViewCommandData.EndUnitTurn());

            // 연출 중에는 Runner → View 새 요청 → Flow 순서로 미리보기만 계속 처리된다.
            for (int frame = 0; frame < 3; frame++)
            {
                int previewNumber = _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
                _flow.Tick();
                Assert.That(_register.Calls[_register.Calls.Count - 1], Is.EqualTo(JobType.ProcessPreview));
                CompleteJob(() => _state.AnsweredPreviewNumber = previewNumber);
            }

            _input.AcknowledgePresented(1);
            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            _flow.Tick();

            Assert.That(_register.Calls[_register.Calls.Count - 1], Is.EqualTo(JobType.ProcessCommand));
        }

        [Test]
        public void CommandResult_HandledNumberMoved_ContinuesEvenWhenRejected()
        {
            StartPlayerInput();
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _input.Submit(CombatViewCommandData.DrawCharacter());
            _flow.Tick();

            // 거절도 처리다: 처리 번호와 결과 순번만 움직이고 게임 상태는 그대로다.
            CompleteJob(() =>
            {
                _state.HandledCommandNumber = 1;
                _state.LatestResultSequence = 1;
            });
            _input.AcknowledgePresented(1);
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.False);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessCommand, JobType.ProcessCommand }));
        }

        [Test]
        public void CommandResult_HandledNumberUnchanged_ReportsOnceAndStops()
        {
            StartPlayerInput();
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _flow.Tick();
            CompleteJob(() => { });

            LogAssert.Expect(LogType.Error, FlowError);
            _flow.Tick();
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.True);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessCommand }));
        }

        [Test]
        public void PreviewResult_AnsweredClearedOrReplaced_DoesNotStop()
        {
            StartPlayerInput();

            int first = _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            _flow.Tick();
            CompleteJob(() => _state.AnsweredPreviewNumber = first);
            _flow.Tick();
            Assert.That(_flow.IsStopped, Is.False);

            _input.SetPreview(CombatViewPreviewData.AttackRange(Unit));
            _flow.Tick();
            CompleteJob(() => _input.ClearPreview());
            _flow.Tick();
            Assert.That(_flow.IsStopped, Is.False);

            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            _flow.Tick();
            int replaced = 0;
            CompleteJob(() => replaced = _input.SetPreview(CombatViewPreviewData.AttackRange(Unit)));
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.False);
            // 바뀐 요청은 새 번호로 다시 등록된다.
            Assert.That(_register.Calls.Count, Is.EqualTo(4));
            Assert.That(_register.Calls[3], Is.EqualTo(JobType.ProcessPreview));
            Assert.That(replaced, Is.GreaterThan(first));
        }

        [Test]
        public void PreviewResult_SameRequestUnanswered_ReportsOnceAndStops()
        {
            StartPlayerInput();
            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            _flow.Tick();
            CompleteJob(() => { });

            LogAssert.Expect(LogType.Error, FlowError);
            _flow.Tick();
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.True);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessPreview }));
        }

        [Test]
        public void Automatic_Presented_RegistersOneAdvance_AndIgnoresCommandAndPreview()
        {
            StartAutomatic();
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));

            _flow.Tick();
            _flow.Tick();

            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat }));
            Assert.That(_flow.IsStopped, Is.False);
        }

        [Test]
        public void Automatic_WhilePresenting_WaitsUntilAcknowledged()
        {
            StartAutomatic();
            _state.LatestResultSequence = 1;
            _results.Latest = new CombatResult(1, CombatProcessingKind.Command, 1, true, "");

            _flow.Tick();
            _flow.Tick();
            Assert.That(_register.Calls, Is.Empty);

            _input.AcknowledgePresented(1);
            _flow.Tick();
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat }));
        }

        [Test]
        public void AdvanceResult_AcceptedAfterBase_ContinuesToNextDecision()
        {
            StartAutomatic();
            _flow.Tick();

            // Core가 한 단위를 수락하고 PlayerInput으로 넘겼다. View는 연출을 마쳤다.
            CompleteJob(() =>
            {
                _state.LatestResultSequence = 1;
                _results.Latest = new CombatResult(1, CombatProcessingKind.Advance, 0, true, "");
                _state.Phase = CombatFlowPhase.PlayerInput;
            });
            _input.AcknowledgePresented(1);
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.False);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat, JobType.ProcessCommand }));
        }

        [Test]
        public void AdvanceResult_ReadAfterViewAcknowledgement_KeepsAdvancingInAutomatic()
        {
            StartAutomatic();
            _flow.Tick();

            // Tick 순서 Runner → View → Flow: View가 결과를 먼저 확인해도 Flow는 최신 결과를 읽는다.
            CompleteJob(() =>
            {
                _state.LatestResultSequence = 1;
                _results.Latest = new CombatResult(1, CombatProcessingKind.Advance, 0, true, "");
            });
            _input.AcknowledgePresented(1);
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.False);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat, JobType.AdvanceCombat }));
        }

        [Test]
        public void Automatic_NoResultWithPositiveSequence_UsesKeptNumbersForNextBattle()
        {
            // 이전 전투 결과를 소비·정리한 뒤: 결과는 없고 발행 번호 7과 View 확인 번호 7은 유지된다(Core 답변 2026-10-01).
            StartAutomatic();
            _state.LatestResultSequence = 7;
            _input.AcknowledgePresented(7);

            _flow.Tick();
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat }));

            CompleteJob(() =>
            {
                _state.LatestResultSequence = 8;
                _results.Latest = new CombatResult(8, CombatProcessingKind.Advance, 0, true, "");
                _state.Phase = CombatFlowPhase.PlayerInput;
            });
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.False);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat }));
        }

        [TestCase(AdvanceFailure.Rejected)]
        [TestCase(AdvanceFailure.Missing)]
        [TestCase(AdvanceFailure.CommandKind)]
        [TestCase(AdvanceFailure.SequenceUnchanged)]
        [TestCase(AdvanceFailure.SequenceMismatch)]
        public void AdvanceResult_NotAcceptedAdvanceAfterBase_ReportsOnceAndNeverReRegisters(AdvanceFailure failure)
        {
            // 기준 순번 S = 2. 이전 명령 결과 2는 연출까지 끝났다.
            StartAutomatic();
            _state.LatestResultSequence = 2;
            _results.Latest = new CombatResult(2, CombatProcessingKind.Command, 5, true, "");
            _input.AcknowledgePresented(2);
            _flow.Tick();
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat }));

            CompleteJob(() => ApplyAdvanceFailure(failure));

            LogAssert.Expect(LogType.Error, FlowError);
            _flow.Tick();

            // View가 거절 결과까지 확인해도 같은 자동 진행을 다시 등록하지 않는다.
            _input.AcknowledgePresented(_state.LatestResultSequence);
            _flow.Tick();
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.True);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.AdvanceCombat }));
        }

        [TestCase(CombatFlowPhase.Finished, true)]
        [TestCase(CombatFlowPhase.None, true)]
        [TestCase(CombatFlowPhase.PlayerInput, false)]
        public void FinishedNoneOrNoBattle_RegistersNothing(CombatFlowPhase phase, bool hasBattle)
        {
            _state.HasBattle = hasBattle;
            _state.Phase = phase;
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));

            _flow.Tick();

            Assert.That(_register.Calls, Is.Empty);
        }

        [Test]
        public void NormalStop_LogsNothing_BlocksRegistration_AndResetRestarts()
        {
            StartPlayerInput();
            _input.Submit(CombatViewCommandData.EndUnitTurn());

            _flow.Stop();
            _flow.Stop();
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.True);
            Assert.That(_register.Calls, Is.Empty);
            LogAssert.NoUnexpectedReceived();

            _flow.Reset();
            _flow.Tick();
            Assert.That(_flow.IsStopped, Is.False);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessCommand }));
        }

        [Test]
        public void FailureStop_BlocksRegistration_UntilReset()
        {
            StartPlayerInput();
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _flow.Tick();
            CompleteJob(() => { });
            LogAssert.Expect(LogType.Error, FlowError);
            _flow.Tick();

            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            _flow.Tick();
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.ProcessCommand }));

            // 이전 전투가 정리돼도 멈춤은 Reset으로만 풀린다. 새 전투를 초기화하면 다시 등록한다.
            _state.HasBattle = false;
            Assert.Throws<InvalidOperationException>(_flow.BeginPreparation);
            _flow.Reset();
            _flow.BeginPreparation();
            Assert.That(_register.Calls[_register.Calls.Count - 1], Is.EqualTo(JobType.Start));
        }

        [Test]
        public void PreviewRequestedAgainAfterStateChange_IsProcessedWithNewNumber()
        {
            StartPlayerInput();
            int oldPreview = _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            _flow.Tick();
            CompleteJob(() => _state.AnsweredPreviewNumber = oldPreview);

            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _flow.Tick();
            CompleteJob(() =>
            {
                _state.HandledCommandNumber = 2;
                _state.LatestResultSequence = 1;
            });

            // View 역할: 상태 변경 결과를 확인하면 기존 미리보기를 무효화하고 새 번호로 다시 요청한다.
            _input.RemoveCommandsThrough(2);
            _input.AcknowledgePresented(1);
            _input.ClearPreview();
            int newPreview = _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            _flow.Tick();

            Assert.That(newPreview, Is.GreaterThan(oldPreview));
            Assert.That(_register.Calls,
                Is.EqualTo(new[] { JobType.ProcessPreview, JobType.ProcessCommand, JobType.ProcessPreview }));
        }

        [Test]
        public void Faulted_BeginPreparation_ReturnsWithoutRegisteringOrThrowing()
        {
            _faults.Report(new InvalidOperationException("input callback"));

            Assert.DoesNotThrow(_flow.BeginPreparation);

            Assert.That(_register.Calls, Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Faulted_Tick_RegistersNothing_AndDoesNotConfirmOrReport()
        {
            // 같은 Tick 앞쪽(View 입력 콜백 등)에서 오류가 보고된 뒤 Flow.Tick이 불리는 경우다(골격 §4.1 0행).
            _flow.BeginPreparation();
            CompleteJob(() => { });
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _faults.Report(new InvalidOperationException("input callback"));

            _flow.Tick();
            StartPlayerInput();
            _flow.Tick();

            // 평소라면 "전투 없이 반환한 준비"를 실패로 보고했겠지만, 오류 뒤에는 확인·로그·등록을 모두 하지 않는다.
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.Start }));
            Assert.That(_flow.IsStopped, Is.False, "오류 상태는 공유 경계가 소유하며 Flow의 로컬 정지로 복제하지 않는다.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Faulted_ResetDoesNotClearSharedFault()
        {
            _faults.Report(new InvalidOperationException("boot"));

            _flow.Reset();
            StartPlayerInput();
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _flow.Tick();

            Assert.That(_register.Calls, Is.Empty);
        }

        [Test]
        public void RecordInputOnly_ConfirmsPreparation_ThenRegistersNoProcessingJob()
        {
            _flow = CreateFlow(CombatExecutionMode.RecordInputOnly);
            _flow.BeginPreparation();
            CompleteJob(StartPlayerInput);
            _input.Submit(CombatViewCommandData.EndUnitTurn());
            _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));

            _flow.Tick();
            _state.Phase = CombatFlowPhase.Automatic;
            _flow.Tick();

            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.Start }));
            Assert.That(_flow.IsStopped, Is.False);
            Assert.That(_input.TryGetNextCommand(0, out _), Is.True, "기록 모드에서도 입력은 지우지 않고 Buffer에 남는다.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RecordInputOnly_StillReportsPreparationWithoutBattle()
        {
            // 기록 모드는 이후 등록만 막는다. 실제 준비의 성공 확인은 그대로 한다.
            _flow = CreateFlow(CombatExecutionMode.RecordInputOnly);
            _flow.BeginPreparation();
            CompleteJob(() => { });

            LogAssert.Expect(LogType.Error, FlowError);
            _flow.Tick();

            Assert.That(_flow.IsStopped, Is.True);
            Assert.That(_register.Calls, Is.EqualTo(new[] { JobType.Start }));
        }

        private CombatFlow CreateFlow(CombatExecutionMode executionMode)
        {
            return new CombatFlow(_state, _results, _input, _runner, _register, executionMode, _faults);
        }

        /// <summary>전투가 설치된 PlayerInput 국면에서 시작한다.</summary>
        [TestCase(JobType.ProcessCommand)]
        [TestCase(JobType.ProcessPreview)]
        [TestCase(JobType.AdvanceCombat)]
        public void Execute_NoEligibleWorkCompletesButFlowChecksEvidence(JobType kind)
        {
            var probe = new CombatPlanTests.PreparationProbe { HasEligibleWork = false };
            var runner = new JobRunnerSystem();
            var registry = CombatPlanTests.CreateRegistry(runner, new CombatPlanTests.FakeInGameService(probe));
            var flow = new CombatFlow(_state, _results, _input, runner, registry, CombatExecutionMode.Live, new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            _state.HasBattle = true;
            _state.Phase = kind == JobType.AdvanceCombat ? CombatFlowPhase.Automatic : CombatFlowPhase.PlayerInput;
            if (kind == JobType.ProcessCommand) _input.Submit(CombatViewCommandData.EndUnitTurn());
            if (kind == JobType.ProcessPreview) _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            try
            {
                flow.Tick(); runner.Tick(0);
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(probe.ProcessingCalls, Is.EqualTo(new[] { kind }));
                LogAssert.Expect(LogType.Error, FlowError);

                flow.Tick(); flow.Tick();
                Assert.That(flow.IsStopped, Is.True);
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(probe.ProcessingCalls.Count, Is.EqualTo(1));
            }
            finally { runner.Clear(); }
        }

        [Test]
        public void RealRunner_PreviewsAndPublishesRoute_ThenWaitsForPresentationBeforeRejection()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = b.CharacterReadSink.Characters.Single().ToFacts();
            var path = new MovementPath(new[] { actor.Anchor.Value, actor.Anchor.Value + CellCoord.Up });
            var runner = new JobRunnerSystem();
            var registry = CombatPlanTests.CreateRegistry(runner, new CombatPlanTests.FakeInGameService(service), b);
            var flow = new CombatFlow(b.CombatStateReadSink, b.CombatResultReadSink, b.CombatViewReadSink, runner, registry, CombatExecutionMode.Live, new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            try
            {
                b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.MoveRange(actor.EntityId));
                flow.Tick();
                // 등록 시점의 요청이 교체되면 Job은 실제 실행 시점의 최신 요청을 처리한다.
                int request = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(actor.EntityId, path));
                runner.Tick(0); flow.Tick();
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                Assert.That(preview.RequestNumber, Is.EqualTo(request));
                Assert.That(preview.Move.Value.CanMove, Is.True);
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
                Assert.That(b.CharacterReadSink.Characters.Single().Anchor, Is.EqualTo(actor.Anchor));

                var command = CombatViewCommandData.Move(actor.EntityId, path, preview.MovementContext);
                int first = b.CombatViewWriteSink.Submit(command);
                flow.Tick(); runner.Tick(0);
                Assert.That(b.CombatResultReadSink.TryGetLatest(out var moved), Is.True);
                Assert.That(moved.Accepted, Is.True, moved.Reason);
                Assert.That(moved.CommandNumber, Is.EqualTo(first));
                Assert.That(moved.PathMovement.RequestedPath.Cells, Is.EqualTo(path.Cells));
                Assert.That(b.CharacterReadSink.Characters.Single().Anchor, Is.EqualTo(path.Endpoint));
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);

                int stale = b.CombatViewWriteSink.Submit(command);
                int duringPlayback = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.MoveRange(actor.EntityId));
                flow.Tick(); runner.Tick(0); flow.Tick(); flow.Tick();
                Assert.That(b.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(duringPlayback));
                Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(first));
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(moved.Sequence));
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(flow.IsStopped, Is.False);

                // 전체 결과 연출 소유자의 확인만 다음 상태 변경을 허용한다.
                b.CombatViewWriteSink.AcknowledgePresented(moved.Sequence);
                flow.Tick(); runner.Tick(0); flow.Tick();
                Assert.That(b.CombatResultReadSink.TryGetLatest(out var rejected), Is.True);
                Assert.That(rejected.Accepted, Is.False);
                Assert.That(rejected.Reason, Is.EqualTo(MovementFailureReason.StaleContext.ToString()));
                Assert.That(rejected.CommandNumber, Is.EqualTo(stale));
                Assert.That(rejected.Sequence, Is.EqualTo(moved.Sequence + 1));
                Assert.That(b.CharacterReadSink.Characters.Single().Anchor, Is.EqualTo(path.Endpoint));
                Assert.That(b.CharacterReadSink.Characters.Single().Stats.CurrentActionCost,
                    Is.EqualTo(actor.Stats.CurrentActionCost - moved.PathMovement.ActionCostSpent));
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(moved.Sequence));
                Assert.That(flow.IsStopped, Is.False);
            }
            finally { runner.Clear(); }
        }

        [Test]
        public void RealRunner_AutomaticTurnWaitsForPriorPresentation_AndPublishesAdvance()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = b.CharacterReadSink.Characters.Single().ToFacts();
            // 프리뷰 픽스처는 전투 국면이므로 EnterCombat의 배치 선행 조건을 명시한다.
            var current = b.CombatStateReadSink.Current.CommandState;
            var deployment = new CombatCommandState(current.BoardWidth, current.BoardHeight, CombatFlowPhase.PlayerInput,
                EntityId.Empty, false, current.Structures, current.ThornFields, Array.Empty<CombatTimelineEntrySnapshot>(),
                new CombatTurnFacts(StagePhase.Deployment), current.Environment);
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor }, CharacterBufferTestData.Combat(),
                current.Structures, commandState: deployment));
            var runner = new JobRunnerSystem();
            var registry = CombatPlanTests.CreateRegistry(runner, new CombatPlanTests.FakeInGameService(service), b);
            var flow = new CombatFlow(b.CombatStateReadSink, b.CombatResultReadSink, b.CombatViewReadSink, runner, registry, CombatExecutionMode.Live, new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            try
            {
                b.CombatViewWriteSink.Submit(CombatViewCommandData.EnterCombat());
                flow.Tick(); runner.Tick(0);
                Assert.That(b.CombatResultReadSink.TryGetLatest(out var entered), Is.True);
                Assert.That(entered.Accepted, Is.True, entered.Reason);
                Assert.That(b.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
                flow.Tick(); flow.Tick();
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(entered.Sequence));
                b.CombatViewWriteSink.AcknowledgePresented(entered.Sequence);
                flow.Tick(); runner.Tick(0); flow.Tick();
                Assert.That(b.CombatResultReadSink.TryGetLatest(out var advanced), Is.True);
                Assert.That(advanced.Kind, Is.EqualTo(CombatProcessingKind.Advance));
                Assert.That(advanced.Accepted, Is.True, advanced.Reason);
                Assert.That(advanced.Sequence, Is.EqualTo(entered.Sequence + 1));
                Assert.That(advanced.Events.OfType<CombatTurnEvent>().Any(e => e.Kind == CombatEventKind.TurnStarted), Is.True);
                Assert.That(b.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(actor.EntityId));
                Assert.That(b.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(entered.Sequence));
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(flow.IsStopped, Is.False);
            }
            finally { runner.Clear(); }
        }

        private void StartPlayerInput()
        {
            _state.HasBattle = true;
            _state.Phase = CombatFlowPhase.PlayerInput;
        }

        /// <summary>전투가 설치된 Automatic 국면에서 시작한다. 결과는 아직 없다.</summary>
        private void StartAutomatic()
        {
            _state.HasBattle = true;
            _state.Phase = CombatFlowPhase.Automatic;
        }

        /// <summary>기준 순번 2에서 자동 진행이 정상 반환한 뒤 Core가 남긴 잘못된 결과를 흉내 낸다.</summary>
        private void ApplyAdvanceFailure(AdvanceFailure failure)
        {
            switch (failure)
            {
                case AdvanceFailure.Rejected:
                    _state.LatestResultSequence = 3;
                    _results.Latest = new CombatResult(3, CombatProcessingKind.Advance, 0, false,
                        "Timeline.NoActiveUnit");
                    break;
                case AdvanceFailure.Missing:
                    _state.LatestResultSequence = 3;
                    _results.Latest = null;
                    break;
                case AdvanceFailure.CommandKind:
                    _state.LatestResultSequence = 3;
                    _results.Latest = new CombatResult(3, CombatProcessingKind.Command, 6, true, "");
                    break;
                case AdvanceFailure.SequenceUnchanged:
                    break;
                case AdvanceFailure.SequenceMismatch:
                    // 상태 순번은 올랐지만 결과는 이전 것 그대로다.
                    _state.LatestResultSequence = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failure), failure, null);
            }
        }

        /// <summary>Core 역할로 상태를 바꾸고 등록된 Job이 정상 반환한 것처럼 Runner를 비운다.</summary>
        private void CompleteJob(Action coreEffect)
        {
            coreEffect();
            _runner.Idle = true;
        }

        private sealed class FakeState : ICombatStateBufferReadSink
        {
            public long BattleInstanceId => 1;
            public bool TryGetLatestPreview(out CombatPreviewResult preview) { preview = null; return false; }
            public bool HasBattle { get; set; }
            public CombatStateData Current => null;
            public CombatFlowPhase Phase { get; set; }
            public SoulFieldSnapshot SoulField => SoulFieldSnapshot.Empty;
            public BattleCompletionChanges Completion => null;
            public int LatestResultSequence { get; set; }
            public int HandledCommandNumber { get; set; }
            public int AnsweredPreviewNumber { get; set; }
        }

        /// <summary>최신 결과 하나만 두는 결과 읽기. View 확인으로 지워지지 않는다(Core 인계 §4).</summary>
        private sealed class FakeResults : ICombatResultReadSink
        {
            internal CombatResult Latest;

            public bool TryGetLatest(out CombatResult result)
            {
                result = Latest;
                return result != null;
            }
        }

        private sealed class FakeRunner : IJobRunner
        {
            internal bool Idle = true;
            public bool IsIdle => Idle;
            public void Register(IJobService job) => throw new NotSupportedException();
            public void RegisterRange(IReadOnlyList<IJobService> jobs) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        /// <summary>등록을 기록하고, 실제 Runner처럼 등록 뒤에는 바쁜 상태로 만든다.</summary>
        private sealed class RecordingRegister : IJobRegisterSystem
        {
            public void RegisterCombatEntry(CombatEntryRequest request) => throw new NotSupportedException();
            private readonly FakeRunner _runner;
            internal readonly List<JobType> Calls = new();

            internal RecordingRegister(FakeRunner runner) => _runner = runner;

            public void Register(JobRootType rootType, JobType jobType)
            {
                Assert.That(rootType, Is.EqualTo(JobRootType.Combat));
                Calls.Add(jobType);
                _runner.Idle = false;
            }
        }
    }
}
