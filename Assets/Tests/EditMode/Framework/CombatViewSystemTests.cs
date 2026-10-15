using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// View 계획 C.2·C.4·C.4a·C.4b·C.5·C.6: 씬 연결, 결과 소비 순서, 정상 종료 입력 해제, 오류 정지.
    /// 상태·진입 읽기와 표시는 가짜, 입력 Buffer와 Interaction은 실제 구현을 쓴다. B3의 활성화·최초 표시 경계도 검증한다.
    /// </summary>
    public sealed class CombatViewSystemTests
    {
        private Rig _rig;

        [TestCase(false)]
        [TestCase(true)]
        public void AttachBeforeEntryActivation_IsRejected_AndDrawsOnlyAfterActivation(bool recordOnly)
        {
            _rig.Dispose();
            _rig = new Rig(recordOnly ? CombatExecutionMode.RecordInputOnly : CombatExecutionMode.Live);
            _rig.Entry.Active = false;
            _rig.State.HasBattle = true;
            _rig.State.Phase = CombatFlowPhase.PlayerInput;

            // 첫 그리기는 Attach가 한다(개선 설계 D13). 진입이 활성이 아니면 연결 자체를 거부한다.
            Assert.Throws<InvalidOperationException>(() => _rig.View.AttachDisplay(_rig.Display));
            _rig.View.Tick(0);
            _rig.View.Input.EnterCombatRequested();
            _rig.View.Input.DrawRequested();
            _rig.View.Input.AttackModeRequested();
            _rig.Display.DevCombat?.Invoke();
            Assert.That(_rig.View.IsAttached, Is.False);
            Assert.That(_rig.Display.InstallCount, Is.Zero);
            Assert.That(_rig.Slot.BindCount, Is.Zero);
            Assert.That(_rig.InputRead.Commands, Is.Empty);
            Assert.That(_rig.InputRead.HasPreview, Is.False);
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));

            _rig.Entry.Active = true;
            _rig.View.AttachDisplay(_rig.Display);
            _rig.View.Tick(0);
            Assert.That(_rig.Display.InstallCount, Is.EqualTo(1));
            Assert.That(_rig.Slot.BindCount, Is.EqualTo(1));
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
            _rig.View.Input.DrawRequested();
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActiveEntry_DoesNotReceiveInputUntilFirstDisplayReturns(bool recordOnly)
        {
            _rig.Dispose();
            _rig = new Rig(recordOnly ? CombatExecutionMode.RecordInputOnly : CombatExecutionMode.Live);
            _rig.Display.OnInstall = () =>
            {
                _rig.View.Input.DrawRequested();
                _rig.Display.DevCombat?.Invoke();
                Assert.That(_rig.Slot.BindCount, Is.Zero);
                Assert.That(_rig.InputRead.Commands, Is.Empty);
                Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
            };

            _rig.AttachAndShowBattle();
            _rig.View.Input.DrawRequested();

            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
        }

        [Test]
        public void EntryClosedDuringFirstDisplay_DoesNotBindInput()
        {
            _rig.Display.OnInstall = () => _rig.Entry.Active = false;

            _rig.AttachAndShowBattle();
            _rig.View.Input.DrawRequested();

            Assert.That(_rig.Slot.BindCount, Is.Zero);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
            Assert.That(_rig.InputRead.Commands, Is.Empty);
        }

        [Test]
        public void EntryClosedDuringPlayback_LateCompletionDoesNotFinalizeOrReceiveInput()
        {
            _rig.Presentation.CompleteImmediately = false;
            _rig.AttachAndShowBattle();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);
            _rig.View.Tick(0);

            _rig.Entry.Active = false;
            _rig.Presentation.Finish(CombatPresentationOutcome.Completed);
            _rig.View.Input.DrawRequested();
            _rig.View.Tick(0);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
            Assert.That(_rig.Display.RefreshCount, Is.Zero);
            Assert.That(_rig.Slot.BindCount, Is.EqualTo(1));
            Assert.That(_rig.Faults.IsFaulted, Is.False);
        }

        [SetUp]
        public void SetUp()
        {
            _rig = new Rig();
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        #region 조립·씬 연결 (C.1·C.2)

        [Test]
        public void Constructor_RejectsMissingArguments()
        {
            var faults = new RuntimeFaultBoundary(_ => { });
            var presentation = new FakeCombatPresentation();

            Assert.Throws<ArgumentNullException>(() => new CombatViewSystem(CombatExecutionMode.Live, null, presentation));
            Assert.Throws<ArgumentNullException>(() => new CombatViewSystem(CombatExecutionMode.Live, faults, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatViewSystem((CombatExecutionMode)99, faults, presentation));
        }

        [Test]
        public void Initialize_RegistersCombatSceneNodes_AroundLifeCyclePriority()
        {
            _rig.View.Initialize();

            Assert.That(_rig.Loading.Enter.Single().SceneIndex, Is.EqualTo(SceneIndices.Combat));
            Assert.That(_rig.Loading.Exit.Single().SceneIndex, Is.EqualTo(SceneIndices.Combat));
            // LifeCycle은 Enter·Exit 모두 0이다. View는 Enter에서 뒤(100), Exit에서 앞(-100)이어야 한다(설계 §11).
            Assert.That(_rig.Loading.Enter.Single().Node.Priority, Is.GreaterThan(0));
            Assert.That(_rig.Loading.Exit.Single().Node.Priority, Is.LessThan(0));
        }

        [Test]
        public void Initialize_BeforeServices_IsRejected()
        {
            var view = new CombatViewSystem(CombatExecutionMode.Live, new RuntimeFaultBoundary(_ => { }), new FakeCombatPresentation());

            Assert.Throws<InvalidOperationException>(view.Initialize);
        }

        [Test]
        public void ExitNode_DetachesTheScene()
        {
            _rig.View.Initialize();
            _rig.AttachAndShowBattle();

            _rig.Loading.Exit.Single().Node.Execute();

            Assert.That(_rig.View.IsAttached, Is.False);
            Assert.That(_rig.Slot.Receiver, Is.Null);
        }

        [Test]
        public void Attach_Twice_IsRejected()
        {
            _rig.State.HasBattle = true;
            _rig.View.AttachDisplay(_rig.Display);

            Assert.Throws<InvalidOperationException>(() => _rig.View.AttachDisplay(_rig.Display));
        }

        [Test]
        public void Attach_AfterFault_DoesNothing()
        {
            _rig.Faults.Report(new InvalidOperationException("앞선 오류"));

            _rig.View.AttachDisplay(_rig.Display);

            Assert.That(_rig.View.IsAttached, Is.False);
        }

        [Test]
        public void AttachWithoutBattle_IsRejected_AndBindsNoInput()
        {
            // Combat 씬은 준비된 전투를 활성화한 뒤 연결된다(골격 §6.3). 전투가 없으면 연결 자체를 거부한다(D13).
            Assert.Throws<InvalidOperationException>(() => _rig.View.AttachDisplay(_rig.Display));

            _rig.View.Tick(0f);

            Assert.That(_rig.View.IsAttached, Is.False);
            Assert.That(_rig.Display.InstallCount, Is.Zero);
            Assert.That(_rig.Slot.BindCount, Is.Zero);
            Assert.That(_rig.Layers.ChangeCount, Is.Zero);
        }

        [Test]
        public void FirstDisplay_BindsInputAndCoreStageLayerOnce()
        {
            _rig.AttachAndShowBattle();
            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.Slot.Receiver, Is.SameAs(_rig.View.Input));
            Assert.That(_rig.Slot.BindCount, Is.EqualTo(1));
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
            Assert.That(_rig.Layers.ChangeCount, Is.EqualTo(1), "같은 Layer를 다시 적용하지 않는다.");
        }

        [Test]
        public void FirstDisplay_OfFinishedBattle_DoesNotBindInput()
        {
            _rig.State.HasBattle = true;
            _rig.State.Phase = CombatFlowPhase.Finished;
            _rig.View.AttachDisplay(_rig.Display);

            _rig.View.Tick(0f);

            Assert.That(_rig.Slot.BindCount, Is.Zero);
            Assert.That(_rig.Layers.ChangeCount, Is.Zero);
        }

        [Test]
        public void BattleRemoved_DetachesAndReleasesInput()
        {
            _rig.AttachAndShowBattle();
            _rig.State.HasBattle = false;
            _rig.State.Phase = CombatFlowPhase.None;

            _rig.View.Tick(0f);

            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
        }

        #endregion

        #region 결과 소비 순서 (C.4·C.5)

        [Test]
        public void ImmediateResult_RemovesHandledCommands_ThenAcknowledges()
        {
            _rig.AttachAndShowBattle();
            int first = _rig.InputWrite.Submit(CombatViewCommandData.EnterCombat());
            _rig.InputWrite.Submit(CombatViewCommandData.DrawCharacter());
            _rig.Publish(Command(1, first, accepted: true), handled: first);

            _rig.View.Tick(0f);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
            Assert.That(_rig.InputRead.Commands.Select(c => c.Number), Is.EqualTo(new[] { first + 1 }),
                "처리 번호까지만 지운다(K14).");
            Assert.That(_rig.Faults.IsFaulted, Is.False);
        }

        [Test]
        public void RejectedResult_IsAcknowledgedToo()
        {
            _rig.AttachAndShowBattle();
            int number = _rig.InputWrite.Submit(CombatViewCommandData.EnterCombat());
            _rig.Publish(Command(1, number, accepted: false), handled: number);

            _rig.View.Tick(0f);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
            Assert.That(_rig.InputRead.Commands, Is.Empty);
        }

        [Test]
        public void AdvanceResult_IsAcknowledged_WithoutRemovingUnhandledCommands()
        {
            _rig.AttachAndShowBattle();
            _rig.InputWrite.Submit(CombatViewCommandData.EndUnitTurn());
            _rig.Publish(new CombatResult(1, CombatProcessingKind.Advance, 0, true, string.Empty), handled: 0);

            _rig.View.Tick(0f);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void DelayedResult_IsNotAcknowledgedBeforeCompletion_AndIsNotPlayedTwice()
        {
            _rig.Presentation.CompleteImmediately = false;
            _rig.AttachAndShowBattle();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);

            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.Presentation.Played.Count, Is.EqualTo(1));

            _rig.Presentation.Finish(CombatPresentationOutcome.Completed);
            _rig.View.Tick(0f);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
        }

        [Test]
        public void ExitSubmissionStop_PreservesPendingResultAndInput_UntilPlaybackCompletes()
        {
            _rig.Presentation.CompleteImmediately = false;
            _rig.AttachAndShowBattle();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);
            _rig.View.Tick(0);
            int cancels = _rig.Presentation.CancelCount;
            _rig.View.SuspendSubmissions();
            _rig.View.SuspendSubmissions();
            _rig.View.Input.DrawRequested();
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
            Assert.That(_rig.Presentation.CancelCount, Is.EqualTo(cancels));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            _rig.Presentation.Finish(CombatPresentationOutcome.Completed);
            _rig.View.Tick(0);
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
            Assert.That(_rig.InputRead.Commands, Is.Empty);
            Assert.That(_rig.Slot.Receiver, Is.Null, "결과 후처리에서 입력을 다시 열지 않는다.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompletionConfirmation_IsOnceAndInvalidAfterDetach(bool detachFirst)
        {
            _rig.AttachAndShowBattle();
            var outcome = new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed);
            var completion = new BattleCompletionChanges(
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                outcome, ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(1), false));
            _rig.State.Completion = completion;
            _rig.View.SuspendSubmissions();
            int calls = 0;
            _rig.View.PresentCompletion(completion, () => calls++);
            Action confirmed = _rig.Display.CompletionConfirmed;
            if (detachFirst) _rig.View.Detach();
            confirmed(); confirmed();
            Assert.That(calls, Is.EqualTo(detachFirst ? 0 : 1));
            Assert.That(_rig.Display.CompletionShows, Is.EqualTo(1));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero, "Completion은 일반 결과 확인 번호를 만들지 않는다.");
        }

        [Test]
        public void ProgressView_CompletionStateMovesShowingThenConfirmed_AndResetsOnReattach()
        {
            _rig.AttachAndShowBattle();
            Assert.That(_rig.View.CompletionDisplay, Is.EqualTo(CombatCompletionDisplay.None));
            Assert.That(_rig.View.IsSubmissionSuspended, Is.False);
            Assert.That(_rig.View.IsAbandonRequested, Is.False);
            var outcome = new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed);
            var completion = new BattleCompletionChanges(
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                outcome, ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(1), false));
            _rig.State.Completion = completion;
            _rig.View.SuspendSubmissions();
            Assert.That(_rig.View.IsSubmissionSuspended, Is.True);

            _rig.View.PresentCompletion(completion);
            Assert.That(_rig.View.CompletionDisplay, Is.EqualTo(CombatCompletionDisplay.Showing));
            Assert.Throws<InvalidOperationException>(() => _rig.View.PresentCompletion(completion), "표시는 한 번만 한다.");
            _rig.Display.CompletionConfirmed();
            _rig.Display.CompletionConfirmed();
            Assert.That(_rig.View.CompletionDisplay, Is.EqualTo(CombatCompletionDisplay.Confirmed));
            Assert.That(_rig.Display.CompletionShows, Is.EqualTo(1));

            // 다시 연결하면 새 수명이다. 지난 표시·제출 정지는 남지 않는다.
            _rig.View.Detach();
            Assert.That(_rig.View.CompletionDisplay, Is.EqualTo(CombatCompletionDisplay.None));
            _rig.State.Completion = null;
            _rig.View.AttachDisplay(_rig.Display);
            Assert.That(_rig.View.CompletionDisplay, Is.EqualTo(CombatCompletionDisplay.None));
            Assert.That(_rig.View.IsSubmissionSuspended, Is.False);
        }

        [Test]
        public void AcknowledgedResult_IsNotReplayed()
        {
            _rig.AttachAndShowBattle();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);

            _rig.View.Tick(0f);
            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.Presentation.Played.Count, Is.EqualTo(1));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
        }

        [Test]
        public void FailedPresentation_ReportsOnce_WithoutAcknowledging()
        {
            _rig.Presentation.CompleteImmediately = false;
            _rig.AttachAndShowBattle();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);
            _rig.View.Tick(0f);

            _rig.Presentation.Finish(CombatPresentationOutcome.Failure(new InvalidOperationException("연출 실패")));
            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.Faults.IsFaulted, Is.True);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1), "오류 정지는 미처리 명령을 지우지 않는다.");
            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
        }

        [Test]
        public void PublicationViolation_ReportsOnce_WithoutRaisingPresentedSequence()
        {
            _rig.AttachAndShowBattle();
            _rig.State.LatestResultSequence = 1; // 결과 없이 발행 순번만 오름: 유실

            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.Faults.IsFaulted, Is.True);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.Presentation.Played, Is.Empty);
        }

        [Test]
        public void FaultReportedInsidePlay_StopsBeforeAcknowledging_EvenIfPlayCompletesNormally()
        {
            _rig.AttachAndShowBattle();
            _rig.Presentation.OnPlay = () => _rig.Faults.Report(new InvalidOperationException("재진입 오류"));
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);

            _rig.View.Tick(0f);

            Assert.That(_rig.Faults.IsFaulted, Is.True);
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void DetachInsidePlay_StopsBeforeAcknowledging()
        {
            _rig.AttachAndShowBattle();
            _rig.Presentation.OnPlay = () => _rig.View.Detach();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);

            _rig.View.Tick(0f);

            Assert.That(_rig.View.IsAttached, Is.False);
            Assert.That(_rig.Faults.IsFaulted, Is.False);
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
        }

        [Test]
        public void SynchronousPlayException_Propagates_WithoutAcknowledging()
        {
            _rig.AttachAndShowBattle();
            _rig.Presentation.ThrowAfterNotify = true;
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);

            // Tick의 예상하지 못한 예외는 Bootstrap까지 전파되어 오류 경계에 보고된다(설계 §10.1).
            Assert.Throws<InvalidOperationException>(() => _rig.View.Tick(0f));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
        }

        #endregion

        #region 정상 종료 입력 해제 (C.4a, W15)

        [Test]
        public void Finished_ReleasesInputOnly_AndStillAcknowledgesTheLastDelayedResultOnce()
        {
            _rig.Presentation.CompleteImmediately = false;
            _rig.AttachAndShowBattle();
            // Core는 종료(Finished)를 그 결과와 함께 게시한다. View는 새 결과를 처음 관측할 때 종료를 본다(D12).
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);
            _rig.State.Phase = CombatFlowPhase.Finished;
            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
            Assert.That(_rig.Presentation.CancelCount, Is.Zero, "정상 종료는 진행 중 연출을 취소하지 않는다.");
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);

            _rig.Presentation.Finish(CombatPresentationOutcome.Completed);
            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
            Assert.That(_rig.Slot.BindCount, Is.EqualTo(1), "종료 뒤 입력을 다시 연결하지 않는다.");
            Assert.That(_rig.Layers.ChangeCount, Is.EqualTo(1), "종료 뒤 Layer를 다시 켜지 않는다.");
            Assert.That(_rig.Faults.IsFaulted, Is.False);
        }

        [Test]
        public void Finished_WithoutNewResult_KeepsPresentedSequenceAndCommands()
        {
            _rig.AttachAndShowBattle();
            _rig.InputWrite.Submit(CombatViewCommandData.EndUnitTurn());

            // 결과 없이 끝나는 길(포기 등)은 골격 이탈이 먼저 SuspendSubmissions를 부른다(D12).
            _rig.State.Phase = CombatFlowPhase.Finished;
            _rig.View.SuspendSubmissions();
            _rig.View.Tick(0f);
            _rig.View.Tick(0f);

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
            Assert.That(_rig.Slot.UnbindCount, Is.EqualTo(1), "반복 Tick에서 해제를 다시 하지 않는다.");
            Assert.That(_rig.Layers.ClearCount, Is.EqualTo(1));
        }

        [Test]
        public void SelectCanceledFromUnbind_DuringRelease_DoesNothing()
        {
            _rig.AttachAndShowBattle();
            _rig.Slot.HasSelection = true;

            _rig.State.Phase = CombatFlowPhase.Finished;
            _rig.View.SuspendSubmissions();
            _rig.View.Tick(0f);

            Assert.That(_rig.Slot.SelectCanceledSent, Is.True);
            Assert.That(_rig.Faults.IsFaulted, Is.False);
            Assert.That(_rig.InputRead.Commands, Is.Empty);
        }

        #endregion

        #region 오류·Detach 정리 (C.4b·C.6)

        [Test]
        public void InputException_IsReportedOnce_ThenInputAndTickStop()
        {
            _rig.AttachAndShowBattle();
            _rig.State.ThrowOnRead = true;

            _rig.Slot.Receiver.PointerMoved(1f, 2f);

            Assert.That(_rig.Faults.IsFaulted, Is.True);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));

            _rig.State.ThrowOnRead = false;
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);
            _rig.View.Tick(0f);
            _rig.View.Input.SelectPressed(1f, 2f);
            _rig.View.Input.EndTurnRequested();

            Assert.That(_rig.Presentation.Played, Is.Empty, "오류 뒤에는 Tick이 결과를 소비하지 않는다.");
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReturnButtonException_UsesSharedFaultBoundary_AndLateRequestsAreIgnored()
        {
            _rig.AttachAndShowBattle();
            _rig.State.ThrowOnRead = true;

            Assert.DoesNotThrow(_rig.View.RequestReturn);

            Assert.That(_rig.Faults.IsFaulted, Is.True);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
            Assert.DoesNotThrow(_rig.View.RequestReturn);
            Assert.That(_rig.InputRead.Commands, Is.Empty);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FocusOrCaptureCleanupException_UsesSharedBoundary_AndStillUnbinds(bool capture)
        {
            _rig.AttachAndShowBattle();
            _rig.Display.ThrowOnReturnState = true;

            Assert.DoesNotThrow(() =>
            {
                if (capture) _rig.View.CaptureLost();
                else _rig.View.FocusLost();
            });

            Assert.That(_rig.Faults.IsFaulted, Is.True);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
            Assert.DoesNotThrow(_rig.View.FocusLost);
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
        }

        [Test]
        public void SceneDisableCleanupFailure_ReportsThroughSharedBoundary_AfterReleasingDisplay()
        {
            _rig.AttachAndShowBattle();
            _rig.Presentation.ThrowOnDetach = true;
            var callback = (Action)Delegate.CreateDelegate(typeof(Action), _rig.View,
                typeof(CombatViewSystem).GetMethod("OnCombatSceneDisabled",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));

            Assert.DoesNotThrow(() => callback());
            Assert.DoesNotThrow(() => callback());

            Assert.That(_rig.Faults.IsFaulted, Is.True);
            Assert.That(_rig.Logged.Count, Is.EqualTo(1));
            Assert.That(_rig.Display.ReleaseCount, Is.EqualTo(1));
            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.View.IsAttached, Is.False);
        }

        [Test]
        public void StopInput_ContinuesCleanupAfterPartialFailure_AndReportsBoth()
        {
            _rig.AttachAndShowBattle();
            _rig.Layers.ThrowOnClear = true;

            _rig.Faults.Report(new InvalidOperationException("최초 원인"));

            Assert.That(_rig.Slot.Receiver, Is.Null, "Layer 해제가 실패해도 슬롯 해제는 한다.");
            Assert.That(_rig.Logged.Single(), Is.InstanceOf<AggregateException>());
        }

        [Test]
        public void StopInput_AbortsPlaybackAndCancelsPresentation()
        {
            _rig.Presentation.CompleteImmediately = false;
            _rig.AttachAndShowBattle();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);
            _rig.View.Tick(0f);

            _rig.Faults.Report(new InvalidOperationException("오류"));
            _rig.Presentation.Finish(CombatPresentationOutcome.Completed);

            Assert.That(_rig.Presentation.CancelCount, Is.EqualTo(1));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
        }

        // 구조 개선 VR1: CombatInteractionTests의 "Stop이 대기를 지운다" 기대값을 Gate 소유자로 옮겼다.
        [Test]
        public void StopInput_ClearsPendingStart_ButSuspendSubmissionsKeepsIt()
        {
            _rig.AttachAndShowBattle();
            _rig.View.Input.EnterCombatRequested();
            Assert.That(_rig.View.HasPendingCommand, Is.True);

            _rig.View.SuspendSubmissions();
            Assert.That(_rig.View.HasPendingCommand, Is.True, "정상 종료의 새 제출 정지는 대기를 보존한다.");

            _rig.View.StopInput();
            Assert.That(_rig.View.HasPendingCommand, Is.False);
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1), "로컬 대기만 끝내고 명령 Buffer는 보존한다.");
        }

        [Test]
        public void Detach_CancelsAndReleases_AndOldCompletionDoesNotFinishTheNextAttach()
        {
            _rig.Presentation.CompleteImmediately = false;
            _rig.AttachAndShowBattle();
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);
            _rig.View.Tick(0f);

            _rig.View.Detach();
            _rig.View.Detach();

            Assert.That(_rig.Presentation.CancelCount, Is.EqualTo(1));
            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.That(_rig.Layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
            Assert.That(_rig.View.IsAttached, Is.False);

            // 다시 연결하면 현재 전투를 처음부터 그리고, 이전 재생의 늦은 완료는 새 재생을 끝내지 않는다.
            _rig.View.AttachDisplay(_rig.Display);
            _rig.View.Tick(0f);
            Assert.That(_rig.Slot.BindCount, Is.EqualTo(2));
            Assert.That(_rig.Presentation.Played.Count, Is.EqualTo(2));

            _rig.Presentation.Callbacks[0](CombatPresentationOutcome.Completed);
            _rig.View.Tick(0f);
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);

            _rig.Presentation.Finish(CombatPresentationOutcome.Completed);
            _rig.View.Tick(0f);
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
        }

        [Test]
        public void PresentationTick_DetachStopsTheCurrentTick()
        {
            _rig.AttachAndShowBattle();
            _rig.Presentation.OnTick = _rig.View.Detach;
            Assert.DoesNotThrow(() => _rig.View.Tick(0));
            Assert.That(_rig.View.IsAttached, Is.False);
            Assert.That(_rig.Display.InstallCount, Is.EqualTo(1));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
        }

        [Test]
        public void PresentationDetachFailure_StillReleasesDisplay()
        {
            _rig.AttachAndShowBattle();
            _rig.Presentation.ThrowOnDetach = true;
            Assert.Throws<AggregateException>(() => _rig.View.Detach());
            Assert.That(_rig.Display.ReleaseCount, Is.EqualTo(1));
            Assert.That(_rig.View.IsAttached, Is.False);
        }

        [Test]
        public void Shutdown_DetachesAndDisconnectsStopInput()
        {
            _rig.AttachAndShowBattle();

            _rig.View.Shutdown();
            _rig.View.Shutdown();

            Assert.That(_rig.View.IsAttached, Is.False);
            Assert.That(_rig.Slot.Receiver, Is.Null);
            Assert.DoesNotThrow(() => _rig.Faults.ConnectStopInput(() => { }),
                "Shutdown 뒤에는 경계가 View의 정리 콜백을 들고 있지 않다.");
        }

        [Test]
        public void StopInput_BeforeServices_DoesNotThrow()
        {
            var faults = new RuntimeFaultBoundary(_ => { });
            var view = new CombatViewSystem(CombatExecutionMode.Live, faults, new FakeCombatPresentation());

            Assert.DoesNotThrow(view.StopInput);
        }

        #endregion

        #region 입력 연결·개발 Layer 패널 (E.4·E.4a·E.5)

        [Test]
        public void Live_EnterCombat_RecordsOnce_AndMatchingResultReleasesWaitAfterDrawing()
        {
            _rig.AttachAndShowBattle();
            _rig.View.Input.EnterCombatRequested();
            _rig.View.Input.EnterCombatRequested();
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
            int first = _rig.InputRead.Commands[0].Number;

            // 거절 결과도 그린 뒤 대기를 풀고 확인한다. 다음 입력은 S == P와 현재 국면으로 다시 허용된다.
            _rig.Publish(Command(1, first, accepted: false), handled: first);
            _rig.View.Tick(0f);
            _rig.View.Input.EnterCombatRequested();

            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
            Assert.That(_rig.InputRead.Commands.Single().Number, Is.GreaterThan(first));
        }

        [Test]
        public void Finished_DirectInputBeforeNextTick_RecordsNothing()
        {
            _rig.AttachAndShowBattle();
            _rig.State.Phase = CombatFlowPhase.Finished;

            // Tick이 입력을 해제하기 전에 도착한 콜백도 Policy의 종료 검사로 막힌다(§5.6).
            _rig.View.Input.EnterCombatRequested();
            _rig.View.Input.DrawRequested();

            Assert.That(_rig.InputRead.Commands, Is.Empty);
            Assert.That(_rig.InputRead.HasPreview, Is.False);
        }

        [Test]
        public void Live_DoesNotShowDevLayerPanel()
        {
            _rig.AttachAndShowBattle();

            Assert.That(_rig.Display.DevShowCount, Is.Zero);
            Assert.That(_rig.Display.DevHideCount, Is.GreaterThan(0));
            Assert.That(_rig.Display.DevCombat, Is.Null);
        }

        [Test]
        public void RecordInputOnly_DevButtons_ChangeActualLayerOnly()
        {
            var rig = new Rig(CombatExecutionMode.RecordInputOnly);
            try
            {
                rig.AttachAndShowBattle();
                Assert.That(rig.Display.DevShowCount, Is.EqualTo(1));

                rig.Display.DevCombat();
                Assert.That(rig.Layers.CurrentLayer, Is.EqualTo(StageInputLayers.Combat));

                rig.Display.DevDeployment();
                Assert.That(rig.Layers.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
                Assert.That(rig.InputRead.Commands, Is.Empty, "Layer 선택은 Core 상태·명령을 바꾸지 않는다.");
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void RecordInputOnly_DevButtons_AreIgnored_WhilePendingOrTerminal()
        {
            var rig = new Rig(CombatExecutionMode.RecordInputOnly);
            try
            {
                rig.AttachAndShowBattle();
                rig.View.Input.EnterCombatRequested();
                Assert.That(rig.InputRead.Commands.Count, Is.EqualTo(1), "기록 모드도 시작 명령은 기록한다.");

                int changes = rig.Layers.ChangeCount;
                rig.Display.DevCombat();
                Assert.That(rig.Layers.ChangeCount, Is.EqualTo(changes), "대기 중에는 Layer를 바꾸지 않는다.");
                Assert.That(rig.Layers.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
            }
            finally
            {
                rig.Dispose();
            }

            var finished = new Rig(CombatExecutionMode.RecordInputOnly);
            try
            {
                finished.AttachAndShowBattle();
                finished.State.Phase = CombatFlowPhase.Finished;
                int changes = finished.Layers.ChangeCount;

                finished.Display.DevCombat();

                Assert.That(finished.Layers.ChangeCount, Is.EqualTo(changes));
            }
            finally
            {
                finished.Dispose();
            }
        }

        [Test]
        public void RecordInputOnly_StopInputAndDetach_HideDevLayerPanel()
        {
            var rig = new Rig(CombatExecutionMode.RecordInputOnly);
            try
            {
                rig.AttachAndShowBattle();

                rig.Faults.Report(new InvalidOperationException("오류"));
                Assert.That(rig.Display.DevHideCount, Is.EqualTo(1));
                Assert.That(rig.Display.DevCombat, Is.Null);

                rig.View.Detach();
                Assert.That(rig.Display.ReleaseCount, Is.EqualTo(1));
            }
            finally
            {
                rig.Dispose();
            }
        }

        #endregion

        #region 포기 버튼·확인창 (포기 UI 설계, 계획 4-V)

        [Test]
        public void Abandon_LiveIdle_OpenBlocksInput_ConfirmRequestsOnce()
        {
            _rig.AttachAndShowBattle();
            _rig.View.Tick(0f);
            Assert.That(_rig.Display.AbandonVisible, Is.True);
            Assert.That(_rig.Display.AbandonInteractable, Is.True);

            _rig.Display.AbandonOpen();
            Assert.That(_rig.Display.AbandonConfirmShown, Is.True);
            // 확인창이 열린 동안 전투 입력은 기록하지 않는다(P3).
            _rig.View.Input.EnterCombatRequested();
            Assert.That(_rig.InputRead.Commands, Is.Empty);

            _rig.Display.AbandonConfirm();
            Assert.That(_rig.Abandon.Requests, Is.EqualTo(1));
            Assert.That(_rig.Display.AbandonConfirmShown, Is.False);
            _rig.Display.AbandonConfirm();
            Assert.That(_rig.Abandon.Requests, Is.EqualTo(1), "확인창이 닫힌 뒤의 확인은 다시 요청하지 않는다.");
        }

        [Test]
        public void Abandon_Cancel_ClosesAndRestoresInput_RejectedRequestChangesNothing()
        {
            _rig.AttachAndShowBattle();
            _rig.View.Tick(0f);

            _rig.Display.AbandonOpen();
            _rig.Display.AbandonCancel();
            Assert.That(_rig.Display.AbandonConfirmShown, Is.False);
            Assert.That(_rig.Abandon.Requests, Is.Zero);
            _rig.View.Input.EnterCombatRequested();
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1), "취소하면 입력을 다시 받는다.");

            _rig.Abandon.Accept = false;
            _rig.Display.AbandonOpen();
            _rig.Display.AbandonConfirm();
            Assert.That(_rig.Abandon.Requests, Is.EqualTo(1));
            Assert.That(_rig.Logged, Is.Empty, "골격이 거절하면 아무것도 하지 않는다(C4).");
        }

        [Test]
        public void Abandon_ExitNotIdle_DisablesButton_AndClosesOpenConfirm()
        {
            _rig.AttachAndShowBattle();
            _rig.View.Tick(0f);
            _rig.Display.AbandonOpen();

            // 골격 Begin은 이탈 상태를 바꾼 뒤 같은 호출 안에서 SuspendSubmissions를 부른다(D12).
            _rig.Abandon.Status = CombatExitStatus.Draining;
            _rig.View.SuspendSubmissions();
            _rig.View.Tick(0f);
            Assert.That(_rig.Display.AbandonInteractable, Is.False);
            Assert.That(_rig.Display.AbandonConfirmShown, Is.False);

            _rig.Display.AbandonOpen();
            Assert.That(_rig.Display.AbandonConfirmShown, Is.False, "이탈 중에는 확인창을 열지 않는다.");
        }

        [Test]
        public void Abandon_RecordInputOnly_HidesButton_AndIgnoresOpen()
        {
            var rig = new Rig(CombatExecutionMode.RecordInputOnly);
            try
            {
                rig.AttachAndShowBattle();
                rig.View.Tick(0f);
                Assert.That(rig.Display.AbandonVisible, Is.False);
                Assert.That(rig.Display.AbandonInteractable, Is.False);

                rig.Display.AbandonOpen();
                Assert.That(rig.Display.AbandonConfirmShown, Is.False);
                Assert.That(rig.Abandon.Requests, Is.Zero);
            }
            finally
            {
                rig.Dispose();
            }
        }

        #endregion

        #region 표시 연결 (D.5)

        [Test]
        public void Install_PassesBufferValuesOnce_ThenBindsInput()
        {
            // HUD 골드는 전투 상태의 스테이지 골드다. Run 재화(75)는 표시에 쓰지 않는다.
            _rig.CurrencyWrite.SetAmount(75);

            _rig.AttachAndShowBattle();
            _rig.View.Tick(0f);

            Assert.That(_rig.Display.InstallCount, Is.EqualTo(1), "정적 필드는 설치 때 한 번만 구성한다.");
            Assert.That(_rig.Display.RefreshCount, Is.Zero);
            Assert.That(_rig.Display.LastState, Is.SameAs(_rig.State.Prepared));
            Assert.That(_rig.Display.LastStageGold, Is.EqualTo(Rig.StageGold));
            Assert.That(_rig.Display.LastRemainingDraws,
                Is.EqualTo(_rig.State.Prepared.DrawOrder.Count - _rig.State.Prepared.DrawCursor));
            Assert.That(_rig.Slot.BindCount, Is.EqualTo(1));
        }

        [Test]
        public void InstallFailure_Propagates_WithoutBindingInput()
        {
            _rig.Display.ThrowOnInstall = true;
            _rig.State.HasBattle = true;
            _rig.State.Phase = CombatFlowPhase.PlayerInput;

            // 첫 그리기는 Attach 안에서 한다(D13). 실패는 로딩 노드를 거쳐 Bootstrap 오류 경계로 간다.
            Assert.Throws<InvalidOperationException>(() => _rig.View.AttachDisplay(_rig.Display));
            Assert.That(_rig.Slot.BindCount, Is.Zero);
            Assert.That(_rig.Layers.ChangeCount, Is.Zero);

            // 반쯤 연결된 상태로 남지 않는다: 이어지는 Tick은 입력을 붙이지 않고, Detach가 표시를 한 번 돌려준다.
            _rig.View.Tick(0f);
            Assert.That(_rig.Slot.BindCount, Is.Zero);
            _rig.View.Detach();
            Assert.That(_rig.Display.ReleaseCount, Is.EqualTo(1));
        }

        [Test]
        public void Result_RefreshesDisplayBeforeAcknowledging_EvenWhenRejected()
        {
            _rig.AttachAndShowBattle();
            int observedPresented = -1;
            _rig.Display.OnRefresh = () => observedPresented = _rig.InputRead.PresentedResultSequence;
            int number = _rig.InputWrite.Submit(CombatViewCommandData.EnterCombat());
            _rig.Publish(Command(1, number, accepted: false), handled: number);

            _rig.View.Tick(0f);

            Assert.That(_rig.Display.RefreshCount, Is.EqualTo(1));
            Assert.That(observedPresented, Is.Zero, "확인은 그리기 뒤 마지막에 한다.");
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.EqualTo(1));
        }

        [Test]
        public void RefreshFailure_Propagates_WithoutAcknowledging()
        {
            _rig.AttachAndShowBattle();
            _rig.Display.ThrowOnRefresh = true;
            _rig.Publish(Command(1, 1), handled: 1, submitFirst: true);

            Assert.Throws<InvalidOperationException>(() => _rig.View.Tick(0f));
            Assert.That(_rig.InputRead.PresentedResultSequence, Is.Zero);
            Assert.That(_rig.InputRead.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void BattleRemoved_ClearsDisplay_AndDetachReleasesItOnce()
        {
            _rig.AttachAndShowBattle();
            _rig.State.HasBattle = false;
            _rig.State.Phase = CombatFlowPhase.None;
            _rig.View.Tick(0f);

            _rig.View.Detach();
            _rig.View.Detach();

            Assert.That(_rig.Display.ClearCount, Is.EqualTo(1));
            Assert.That(_rig.Display.ReleaseCount, Is.EqualTo(1));
        }

        [Test]
        public void AttachRoot_WithoutViewDatabase_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => _rig.View.Attach(_rig.Root));
            Assert.That(_rig.View.IsAttached, Is.False);
        }

        #endregion

        private static CombatResult Command(int sequence, int commandNumber, bool accepted = true)
        {
            return new CombatResult(sequence, CombatProcessingKind.Command, commandNumber, accepted,
                accepted ? string.Empty : "Rejected");
        }

        internal sealed class EntryRead : ICombatEntryRead
        {
            internal bool Active = true;
            public bool IsActive => Active;
        }

        /// <summary>골격 포기 요청의 대역이다. 이탈 상태와 응답을 정하고 요청 횟수를 센다.</summary>
        internal sealed class AbandonProbe : ICombatAbandonRequest
        {
            internal CombatExitStatus Status = CombatExitStatus.Idle;
            internal bool Accept = true;
            internal int Requests;

            public CombatExitStatus ExitStatus => Status;

            public bool RequestAbandon()
            {
                Requests++;
                return Accept;
            }
        }

        private sealed class Rig
        {
            internal readonly EntryRead Entry = new();
            internal readonly AbandonProbe Abandon = new();
            internal readonly List<Exception> Logged = new();
            internal readonly RuntimeFaultBoundary Faults;
            internal readonly FakeState State = new();
            internal readonly FakeResults Results = new();
            internal readonly ICombatViewBufferReadSink InputRead;
            internal readonly ICombatViewBufferWriteSink InputWrite;
            internal readonly FakeSlot Slot = new();
            internal readonly FakeLayers Layers = new();
            internal readonly FakeLoadingRegister Loading = new();
            internal readonly FakeCombatPresentation Presentation = new() { CompleteImmediately = true };
            internal readonly FakeDisplay Display = new();
            internal readonly ICurrencyBufferWriteSink CurrencyWrite;
            internal readonly CombatViewSystem View;
            internal readonly CombatSceneRoot Root;

            private readonly GameObject _rootObject;

            internal Rig(CombatExecutionMode mode = CombatExecutionMode.Live)
            {
                Faults = new RuntimeFaultBoundary(Logged.Add);
                IBufferQuery inputBuffers = CreateBuffers();
                InputRead = inputBuffers.CombatViewReadSink;
                InputWrite = inputBuffers.CombatViewWriteSink;
                CurrencyWrite = inputBuffers.CurrencyWriteSink;
                State.Prepared = PrepareDeploymentState();

                // Factory와 같이 연결한다: 같은 오류 경계, StopInput 콜백.
                View = new CombatViewSystem(mode, Faults, Presentation);
                Faults.ConnectStopInput(View.StopInput);
                View.BindServices(State, Results, inputBuffers.CharacterReadSink, inputBuffers.CurrentProgressReadSink,
                    InputRead, InputWrite, Slot, Layers, Loading, new FakeColors(), new FakeAreaOutlines(), Entry, Abandon);

                _rootObject = new GameObject("CombatSceneRoot (Test)");
                Root = _rootObject.AddComponent<CombatSceneRoot>();
            }

            /// <summary>Core가 배치 단계 전투를 설치한 뒤 씬에 연결한다. 첫 표시·입력 연결은 Attach가 한다(D13). 한 번 Tick한다.</summary>
            internal void AttachAndShowBattle()
            {
                State.HasBattle = true;
                State.Phase = CombatFlowPhase.PlayerInput;
                View.AttachDisplay(Display);
                View.Tick(0f);
            }

            /// <summary>Core 역할로 결과·발행 순번·처리 번호를 함께 게시한다.</summary>
            internal void Publish(CombatResult result, int handled, bool submitFirst = false)
            {
                if (submitFirst)
                    InputWrite.Submit(CombatViewCommandData.EnterCombat());

                Results.Latest = result;
                State.LatestResultSequence = result.Sequence;
                State.HandledCommandNumber = handled;
            }

            internal void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_rootObject);
            }

            private static CombatStateData PrepareDeploymentState()
            {
                IBufferQuery buffers = CreateBuffers();
                OwnedCharacterRecord main = buffers.CharacterWriteSink.Acquire(new[]
                {
                    new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
                }, CharacterBufferTestData.MainState()).Acquired.Single();

                buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                    new[] { CharacterBufferTestData.MainFacts(main.Id, CharacterBufferTestData.MainState()) },
                    new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.PlayerInput,
                        Array.Empty<CharacterInstanceId>(), initialStageGold: StageGold)));
                return buffers.CombatStateReadSink.Current;
            }

            /// <summary>준비된 전투의 스테이지 골드. Run 재화 값과 겹치지 않게 고른다.</summary>
            internal const int StageGold = 30;

            private static IBufferQuery CreateBuffers()
            {
                var builder = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(builder);
                return builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            }
        }

        /// <summary>Core가 게시하는 값을 테스트가 정한다. 전투 데이터는 실제 Buffer에서 준비한 배치 단계 상태를 쓴다.</summary>
        private sealed class FakeState : ICombatStateBufferReadSink
        {
            public long BattleInstanceId => 1;
            private CombatFlowPhase _phase;

            internal CombatStateData Prepared;
            internal bool ThrowOnRead;

            public bool HasBattle { get; set; }
            public CombatStateData Current => HasBattle ? Prepared : null;

            public CombatFlowPhase Phase
            {
                get
                {
                    if (ThrowOnRead)
                        throw new InvalidOperationException("상태 읽기 실패(테스트)");
                    return _phase;
                }
                set => _phase = value;
            }

            public SoulFieldSnapshot SoulField => SoulFieldSnapshot.Empty;
            public BattleCompletionChanges Completion { get; set; }
            public int LatestResultSequence { get; set; }
            public int HandledCommandNumber { get; set; }
            public int AnsweredPreviewNumber { get; set; }
            public bool TryGetLatestPreview(out CombatPreviewResult preview) { preview = null; return false; }
        }

        private sealed class FakeResults : ICombatResultReadSink
        {
            internal CombatResult Latest;

            public bool TryGetLatest(out CombatResult result)
            {
                result = Latest;
                return result != null;
            }
        }

        /// <summary>실제 슬롯처럼 같은 대상만 Unbind하고, 선택 중이었으면 SelectCanceled를 한 번 보낸다.</summary>
        private sealed class FakeSlot : IStageInputReceiverSlot
        {
            internal IStageInputReceiver Receiver;
            internal bool HasSelection;
            internal bool SelectCanceledSent;
            internal int BindCount;
            internal int UnbindCount;

            public void Bind(IStageInputReceiver receiver)
            {
                if (Receiver != null)
                    throw new InvalidOperationException("이미 연결돼 있다.");
                Receiver = receiver;
                BindCount++;
            }

            public void Unbind(IStageInputReceiver receiver)
            {
                if (!ReferenceEquals(Receiver, receiver))
                    return;

                UnbindCount++;
                Receiver = null;
                if (HasSelection)
                {
                    HasSelection = false;
                    SelectCanceledSent = true;
                    receiver.SelectCanceled();
                }
            }
        }

        private sealed class FakeLayers : IInputLayerService
        {
            internal bool ThrowOnClear;
            internal int ChangeCount;
            internal int ClearCount;

            public InputLayer CurrentLayer { get; private set; }

            public void ChangeLayer(InputLayer nextLayer)
            {
                CurrentLayer = nextLayer;
                ChangeCount++;
            }

            public void ClearLayer()
            {
                ClearCount++;
                if (ThrowOnClear)
                    throw new InvalidOperationException("Layer 해제 실패(테스트)");
                CurrentLayer = default;
            }
        }

        /// <summary>표시 호출을 기록하는 가짜 표시입니다. 실제 Presenter 검증은 D.6a와 재생에서 한다.</summary>
        internal class FakeDisplay : ICombatBattleDisplay
        {
            public virtual float InnerCellInset => .15f;
            public virtual bool IsMovementComplete => true;
            public virtual bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool cancel) { grid = default; cancel = false; return false; }
            public virtual void ShowDraft(MovementPath path, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.MovePreview? preview, bool valid, CellCoord? candidate = null) { }
            public virtual void ClearDraft() { }
            public virtual void SetPathPlanning(bool planning) { }
            public virtual void BeginStep(CombatCharacterEvent step) { }
            public virtual void ShowReturn(CombatCharacterEvent returned) { }
            public virtual void CancelMovement() { }
            internal bool ThrowOnReturnState;
            public virtual void SetReturnState(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.CheckpointReturnPreview preview)
            {
                if (ThrowOnReturnState) throw new InvalidOperationException("Return 표시 정리 실패(테스트)");
            }
            public virtual bool CanPresent(CombatEvent e) => true;
            public virtual void BeginEvent(CombatEvent e, Action completed) => completed();
            public void BindReturn(Action requested) { }
            public void TickPresentation(float deltaTime) { }
            public virtual void CancelPresentation() { }
            internal Action CompletionConfirmed;
            internal int CompletionShows;
            public virtual void ShowCompletion(BattleCompletionChanges completion, Action confirmed)
            {
                CompletionShows++;
                CompletionConfirmed = confirmed;
            }
            internal int InstallCount;
            internal int RefreshCount;
            internal int ClearCount;
            internal int ReleaseCount;
            internal bool ThrowOnInstall;
            internal bool ThrowOnRefresh;
            internal Action OnInstall;
            internal Action OnRefresh;
            internal CombatStateData LastState;
            internal int LastStageGold;
            internal int LastRemainingDraws;

            public virtual void Install(in CombatDisplayValues values)
            {
                InstallCount++;
                OnInstall?.Invoke();
                if (ThrowOnInstall)
                    throw new InvalidOperationException("설치 그리기 실패(테스트)");
                Record(values);
            }

            public virtual void Refresh(in CombatDisplayValues values)
            {
                RefreshCount++;
                OnRefresh?.Invoke();
                if (ThrowOnRefresh)
                    throw new InvalidOperationException("갱신 그리기 실패(테스트)");
                Record(values);
            }

            public void Clear() => ClearCount++;

            public void Release() => ReleaseCount++;

            // 개발 Layer 패널(W20)
            internal Action DevDeployment;
            internal Action DevCombat;
            internal int DevShowCount;
            internal int DevHideCount;

            public void ShowDevLayerPanel(Action onDeployment, Action onCombat)
            {
                DevShowCount++;
                DevDeployment = onDeployment;
                DevCombat = onCombat;
            }

            public void HideDevLayerPanel()
            {
                DevHideCount++;
                DevDeployment = null;
                DevCombat = null;
            }

            internal Action AbandonOpen;
            internal Action AbandonConfirm;
            internal Action AbandonCancel;
            internal bool AbandonVisible;
            internal bool AbandonInteractable;
            internal bool AbandonConfirmShown;

            public void BindAbandon(Action open, Action confirm, Action cancel)
            {
                AbandonOpen = open;
                AbandonConfirm = confirm;
                AbandonCancel = cancel;
            }

            public void SetAbandonAvailable(bool visible, bool interactable)
            {
                AbandonVisible = visible;
                AbandonInteractable = interactable;
            }

            public void ShowAbandonConfirm() => AbandonConfirmShown = true;

            public void HideAbandonConfirm() => AbandonConfirmShown = false;

            // 집기·표시 의도: 이 묶음은 ViewSystem 연결만 보므로 아무것도 집지 않는다. 해석은 CombatInteractionTests에서 본다.
            internal EntityKey? GhostKey;

            public bool TryPickCell(float screenX, float screenY, out CellCoord cell)
            {
                cell = default;
                return false;
            }

            public bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId)
            {
                entityId = EntityId.Empty;
                return false;
            }

            public bool IsOverHandDropZone(float screenX, float screenY) => false;

            public void ShowGhost(EntityKey entityKey) => GhostKey = entityKey;

            public void MoveGhostToCell(CellCoord cell)
            {
            }

            public void MoveGhostToPointer(float screenX, float screenY)
            {
            }

            public void HideGhost() => GhostKey = null;

            public void HideOriginal(EntityId entityId)
            {
            }

            public void ShowOriginal()
            {
            }

            public void SetGhostPlaceable(bool placeable)
            {
            }

            public void SetHoverCell(CellCoord cell, CombatHoverState state)
            {
            }

            public void ClearHoverCell()
            {
            }

            public void ShowRangeCells(IReadOnlyList<CellCoord> cells)
            {
            }

            public void ClearRangeCells()
            {
            }

            public void ShowAffectedCells(IReadOnlyList<CellCoord> cells)
            {
            }

            public void ClearAffectedCells()
            {
            }

            public void ShowAttackCostPreview(EntityId actorId, int cost) { }
            public void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts) { }
            public void ClearAttackBlockers() { }

            private void Record(in CombatDisplayValues values)
            {
                LastState = values.State;
                LastStageGold = values.StageGold;
                LastRemainingDraws = values.RemainingDraws;
            }
        }

        internal sealed class FakeColors : IColorSystemService
        {
            public IColorHandle CreateColorHandle(Renderer renderer) => null;
        }

        /// <summary>칸 테두리 서비스 대역입니다. 만든 핸들과 마지막 갱신 값을 기록합니다.</summary>
        internal sealed class FakeAreaOutlines : IAreaOutlineSystemService
        {
            internal readonly List<FakeAreaOutlineHandle> Handles = new();

            public IAreaOutlineHandle Create()
            {
                var handle = new FakeAreaOutlineHandle();
                Handles.Add(handle);
                return handle;
            }
        }

        internal sealed class FakeAreaOutlineHandle : IAreaOutlineHandle
        {
            internal AreaOutlineData? Last { get; private set; }
            internal int Updates { get; private set; }
            internal bool IsDisposed { get; private set; }
            internal int DisposeCalls { get; private set; }

            /// <summary>true면 Dispose가 예외를 던집니다(해제 실패 주입).</summary>
            internal bool ThrowOnDispose { get; set; }

            public void Update(AreaOutlineData data)
            {
                Last = data;
                Updates++;
            }

            public void Dispose()
            {
                DisposeCalls++;
                if (ThrowOnDispose)
                    throw new InvalidOperationException("injected dispose failure");
                IsDisposed = true;
            }
        }

        internal sealed class FakeLoadingRegister : ILodingRegisterService
        {
            internal readonly List<(int SceneIndex, LodingNode Node)> Enter = new();
            internal readonly List<(int SceneIndex, LodingNode Node)> Exit = new();

            public void RegisterBeforeLoad(LodingNode node) => throw new NotSupportedException();
            public void RegisterAfterLoad(LodingNode node) => throw new NotSupportedException();
            public void RegisterSceneEnterLoaded(int sceneIndex, LodingNode node) => Enter.Add((sceneIndex, node));
            public void RegisterSceneExitLoaded(int sceneIndex, LodingNode node) => Exit.Add((sceneIndex, node));
        }
    }
}
