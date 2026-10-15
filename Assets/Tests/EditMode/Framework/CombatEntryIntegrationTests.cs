using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.Factory.Runtime.LifeCycle;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 실제 Runtime·LifeCycle·Register·Plan·Runner·Core·Buffer로 명시적 진입을 검증한다.
    /// 씬 로드와 결과 표시는 테스트 대역이다. 운영 DB·Unity 씬 인수와 구분한다.
    /// </summary>
    public sealed class CombatEntryIntegrationTests
    {
        private static readonly CellCoord MainCell = new(0, 1);
        private static readonly CellCoord HeadquartersCell = new(0, 3);

        [TestCase(false)]
        [TestCase(true)]
        public void Entry_WhileCombatInactive_PreparesOnceThenActivatesOnce(bool recordOnly)
        {
            var mode = recordOnly ? CombatExecutionMode.RecordInputOnly : CombatExecutionMode.Live;
            using var f = new Fixture(mode);
            var request = f.Request(2);
            Assert.That(f.Buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(f.Entry.IsActive, Is.False);
            Assert.That(f.Source.Reads, Is.Empty);
            Assert.That(f.LifeCycles.LifeCycleManager.HasActiveLifeCycle, Is.False);
            Assert.That(f.Request(2), Is.SameAs(request));

            f.Frame();
            Assert.That(request.Result.Accepted, Is.True);
            Assert.That(request.IsJobCompleted, Is.True);
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Activating));
            Assert.That(f.Entry.IsActive, Is.False);
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.True);
            Assert.That(f.Buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(100));
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(f.Buffers.CombatResultReadSink.TryGetLatest(out _), Is.False);
            Assert.That(f.Entry.CancelEntry(request), Is.False);
            Assert.Throws<InvalidOperationException>(() => f.Request(2));

            // 로드 중 입력은 OnEnter에서 정리한다. 결과 순번은 진입 완료 신호가 아니다.
            f.Input.Submit(CombatViewCommandData.EnterCombat());
            f.Frame();
            f.Frame();
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Active));
            Assert.That(f.Entry.IsActive, Is.True);
            Assert.That(f.InputRead.Commands, Is.Empty);
            Assert.That(f.Source.Reads, Is.EqualTo(new[] { (1, 100) }));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));
            Assert.That(f.Activations, Is.EqualTo(1));

            int number = f.Input.Submit(CombatViewCommandData.PlaceMainCharacter(new CellCoord(1, 0), Rotation4.R0));
            f.Frame();
            f.Frame();
            Assert.That(f.Buffers.CombatStateReadSink.HandledCommandNumber,
                Is.EqualTo(mode == CombatExecutionMode.Live ? number : 0));
            Assert.That(f.InputRead.Commands.Count, Is.EqualTo(mode == CombatExecutionMode.Live ? 0 : 1));
        }

        [Test]
        public void ProgressReader_IsBoundThroughRuntime_AndReadsTheActiveBattle()
        {
            using var f = new Fixture();
            ICombatProgressReader progress = f.LifeCycles.CombatProgress;
            Assert.That(progress.Read().HasBattle, Is.False);

            f.Activate(2);
            CombatProgressFacts facts = progress.Read();
            Assert.That(facts.HasBattle, Is.True);
            Assert.That(facts.BattleInstanceId, Is.EqualTo(f.Buffers.CombatStateReadSink.BattleInstanceId));
            Assert.That(facts.Phase, Is.EqualTo(f.Buffers.CombatStateReadSink.Phase));
            Assert.That(facts.IsViewAttached, Is.True, "InGame View 그룹의 전투 View를 읽는다.");
            Assert.That(progress.TryReadSelection(out int chapterKey, out var stage), Is.True);
            Assert.That(chapterKey, Is.EqualTo(1));
            Assert.That(stage.NodeId, Is.EqualTo(2));
            Assert.That(stage.StageKey, Is.EqualTo(100));
        }

        [Test]
        public void NavigationRejection_CompletesJobWithoutActivation_ThenAcceptsNewRequest()
        {
            using var f = new Fixture();
            var rejected = f.Request(3);
            f.Frame();
            f.Frame();
            Assert.That(rejected.Status, Is.EqualTo(CombatEntryStatus.Rejected));
            Assert.That(rejected.Result.Accepted, Is.False);
            Assert.That(rejected.IsJobCompleted, Is.True);
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.True);
            Assert.That(f.Source.Reads, Is.Empty);
            Assert.That(f.Loading.LoadCalls, Is.Zero);
            Assert.That(f.Buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(f.Faults.IsFaulted, Is.False);
            f.Activate(2);
        }

        [Test]
        public void CancelledRequest_DrainsWithoutCoreCall_ThenDifferentTargetCanBeRequested()
        {
            using var f = new Fixture();
            var cancelled = f.Request(3);
            Assert.That(f.Entry.CancelEntry(cancelled), Is.True);
            Assert.That(f.Entry.CancelEntry(cancelled), Is.False);
            Assert.Throws<InvalidOperationException>(() => f.Request(2));
            f.Frame();
            Assert.That(cancelled.Status, Is.EqualTo(CombatEntryStatus.Cancelled));
            Assert.That(cancelled.IsJobCompleted, Is.True);
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.True);
            Assert.That(f.Source.Reads, Is.Empty);
            Assert.That(f.Loading.LoadCalls, Is.Zero);
            var next = f.Request(2);
            Assert.That(f.Entry.CancelEntry(cancelled), Is.False);
            f.Frame();
            f.Frame();
            Assert.That(next.Status, Is.EqualTo(CombatEntryStatus.Active));
            Assert.That(f.Source.Reads.Count, Is.EqualTo(1));
        }

        [Test]
        public void DuringCoreExecution_CannotCancelOrReplaceCapturedTarget()
        {
            using var f = new Fixture();
            var request = f.Request(2);
            f.Source.BeforeReturn = () =>
            {
                Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Running));
                Assert.That(f.Entry.CancelEntry(request), Is.False);
                Assert.Throws<InvalidOperationException>(() => f.Request(3));
            };
            f.Frame();
            f.Frame();
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Active));
            Assert.That(f.Source.Reads, Is.EqualTo(new[] { (1, 100) }));
        }

        [Test]
        public void CoreException_PreservesIncompleteJobAndCause_WithoutRetry()
        {
            using var f = new Fixture();
            var error = new InvalidOperationException("정의 실패 주입");
            f.Source.Failure = error;
            var request = f.Request(2);
            Assert.That(Assert.Throws<InvalidOperationException>(f.Frame), Is.SameAs(error));
            f.Frame();
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Faulted));
            Assert.That(request.IsJobCompleted, Is.False);
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.False);
            Assert.That(f.Faults.FirstError, Is.SameAs(error));
            Assert.That(f.Source.Reads.Count, Is.EqualTo(1));
            Assert.That(f.Loading.LoadCalls, Is.Zero);
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(f.Request(3).Status, Is.EqualTo(CombatEntryStatus.Faulted));
        }

        [Test]
        public void ActivationRequestException_DoesNotRepeatPreparationOrDiscardPreparedBattle()
        {
            using var f = new Fixture();
            var error = new InvalidOperationException("로딩 요청 실패 주입");
            f.Loading.Failure = error;
            var request = f.Request(2);
            Assert.That(Assert.Throws<InvalidOperationException>(f.Frame), Is.SameAs(error));
            f.Frame();
            Assert.That(request.Result.Accepted, Is.True);
            Assert.That(request.IsJobCompleted, Is.True);
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Faulted));
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.True);
            Assert.That(f.Entry.IsActive, Is.False);
            Assert.That(f.Source.Reads.Count, Is.EqualTo(1));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));
        }

        [Test]
        public void CurrentSelectionChangedBeforeSceneEnter_FaultsWithoutActivating()
        {
            using var f = new Fixture();
            var request = f.Request(2);
            f.Frame();
            f.Buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(101), "외부 변경 주입");
            Assert.Throws<InvalidOperationException>(f.Frame);
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Faulted));
            Assert.That(f.Entry.IsActive, Is.False);
            Assert.That(f.Activations, Is.Zero);
            Assert.That(f.Source.Reads.Count, Is.EqualTo(1));
        }

        [Test]
        public void DirectLifeCycleChange_WithoutAcceptedEntry_CannotActivate()
        {
            using var f = new Fixture();
            f.LifeCycles.LifeCycleManager.Change(LifeCycleKeys.Combat);
            Assert.Throws<InvalidOperationException>(f.Frame);
            Assert.That(f.Entry.IsActive, Is.False);
            Assert.That(f.Source.Reads, Is.Empty);
            Assert.That(f.Activations, Is.Zero);
        }

        [Test]
        public void ShutdownBeforeExecution_CancelsOwnedRequest_AndRejectsFurtherEntry()
        {
            using var f = new Fixture();
            var request = f.Request(2);
            f.Runtime.Shutdown();
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Cancelled));
            Assert.That(f.Source.Reads, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => f.Request(2));
        }

        [Test]
        public void ActiveBattle_RejectsNormalExitAndManagerBypass_WithoutStoppingProgress()
        {
            using var f = new Fixture();
            f.Activate(2);
            Assert.That(f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Stage), Is.False);
            Assert.Throws<InvalidOperationException>(() => f.LifeCycles.LifeCycleManager.Change(LifeCycleKeys.Stage));
            Assert.Throws<InvalidOperationException>(() => f.LifeCycles.LifeCycleManager.Exit());
            f.Input.Submit(CombatViewCommandData.EnterCombat());
            f.Frame(); f.Frame();
            Assert.That(f.Buffers.CombatStateReadSink.LatestResultSequence, Is.GreaterThan(0));
            Assert.That(f.Faults.IsFaulted, Is.False);
        }

        [Test]
        public void NextEntry_UsesNextTargetAndRetainsOldResultWithoutTreatingItAsAcceptance()
        {
            using var f = new Fixture();
            var first = f.Activate(2);
            int attack = WinBattle(f);
            Assert.That(f.Buffers.CombatResultReadSink.TryGetLatest(out var final), Is.True);
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.True);

            // 외부 요청 없이 LifeCycle이 일반 승리의 결과 표시를 시작한다. 중복 요청만 API로 확인한다.
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.Not.EqualTo(CombatExitStatus.Idle));
            Assert.That(f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Stage), Is.True);
            Assert.That(f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Stage), Is.True);
            Assert.That(f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Lobby), Is.False);
            f.Frame();
            Assert.That(f.View.CompletionShows, Is.EqualTo(1));
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Presenting));
            f.Frame();
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));
            Action oldConfirmation = f.View.Confirmed;
            oldConfirmation(); oldConfirmation();
            f.Frame();
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(f.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(2));
            f.Frame();
            Assert.That(first.Status, Is.EqualTo(CombatEntryStatus.Closed));
            Assert.That(f.Loading.CurrentSceneIndex, Is.EqualTo(SceneIndices.Stage));
            Assert.That(f.Buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(f.Buffers.CurrencyReadSink.Amount, Is.EqualTo(20));
            int oldInput = f.Input.Submit(CombatViewCommandData.DrawCharacter());
            var next = f.Request(3);
            Assert.That(next.Result, Is.Null);
            Assert.That(f.Entry.IsActive, Is.False);
            Assert.That(f.Buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(100));
            f.Frame();
            f.Frame();
            Assert.That(next.Status, Is.EqualTo(CombatEntryStatus.Active));
            Assert.That(next.Result.StageKey.Value, Is.EqualTo(101));
            Assert.That(f.Source.Reads, Is.EqualTo(new[] { (1, 100), (1, 101) }));
            Assert.That(f.Buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(9));
            Assert.That(f.Buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(f.Buffers.CombatResultReadSink.TryGetLatest(out var retained), Is.True);
            Assert.That(retained, Is.SameAs(final));
            Assert.That(f.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(attack));
            Assert.That(f.InputRead.PresentedResultSequence, Is.EqualTo(final.Sequence));
            Assert.That(f.InputRead.Commands, Is.Empty);
            Assert.That(f.Input.Submit(CombatViewCommandData.DrawCharacter()), Is.GreaterThan(oldInput));
            Assert.That(f.Activations, Is.EqualTo(2));
            oldConfirmation();
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Idle));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(3));
        }

        [Test]
        public void NormalExit_WaitsForFinalResultBeforeCompletionDisplay_WithoutClearingInput()
        {
            using var f = new Fixture();
            f.Activate(2);
            f.View.HoldTerminalResult = true;
            int attack = WinBattle(f);
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Draining));
            f.Frame(); f.Frame();
            Assert.That(f.View.CompletionShows, Is.Zero);
            Assert.That(f.InputRead.Commands.Any(c => c.Number == attack), Is.True);
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            f.View.HoldTerminalResult = false;
            f.Frame();
            Assert.That(f.View.CompletionShows, Is.EqualTo(1));
            Assert.That(f.InputRead.Commands, Is.Empty);
            Assert.That(f.InputRead.PresentedResultSequence, Is.EqualTo(f.Buffers.CombatStateReadSink.LatestResultSequence));
        }

        [Test]
        public void CompletionCallbackThenException_StopsWithoutEndBattleOrRetry()
        {
            using var f = new Fixture();
            f.Activate(2);
            var error = new InvalidOperationException("Completion 표시 실패");
            f.View.ShowAction = confirmed => { confirmed(); throw error; };
            WinBattle(f);
            f.Frame(); f.Frame();
            Assert.That(f.Faults.FirstError, Is.SameAs(error));
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(f.View.CompletionShows, Is.EqualTo(1));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));
        }

        [Test]
        public void DetachedCompletion_CannotAcknowledgeAndTransition()
        {
            using var f = new Fixture();
            f.Activate(2);
            WinBattle(f);
            f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Stage);
            f.Frame();
            f.View.Detached = true;
            f.View.Confirmed();
            f.Frame();
            Assert.That(f.Faults.IsFaulted, Is.True);
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));
        }

        [Test]
        public void TransitionFailure_AfterEndBattle_DoesNotRetryOrDropCompletion()
        {
            using var f = new Fixture();
            f.Activate(2);
            WinBattle(f);
            var completion = f.Buffers.CombatStateReadSink.Completion;
            f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Stage);
            f.Frame();
            f.View.Confirmed();
            var error = new InvalidOperationException("이탈 로딩 실패");
            f.Loading.Failure = error;
            f.Frame(); f.Frame();
            Assert.That(f.Faults.FirstError, Is.SameAs(error));
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(f.Buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(2));
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Failed));
        }

        [Test]
        public void NormalExit_WaitsForActualRunnerToDrain_WithoutClearingRegisteredWork()
        {
            using var f = new Fixture();
            f.Activate(2);
            f.View.HoldTerminalResult = true;
            WinBattle(f);
            var job = new DrainJob();
            f.Jobs.JobRunner.Register(job);
            Assert.That(f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Stage), Is.True);
            f.View.HoldTerminalResult = false;
            f.Frame();
            Assert.That(job.Calls, Is.EqualTo(1));
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.False);
            Assert.That(f.View.CompletionShows, Is.Zero);
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            job.AllowCompletion = true;
            f.Frame();
            Assert.That(job.Calls, Is.EqualTo(2));
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.True);
            Assert.That(f.View.CompletionShows, Is.EqualTo(1));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));
        }

        [Test]
        public void ChapterVictory_WithoutContinuationDefinition_StartsNewRunThenReturnsToStage()
        {
            // 현재 런타임처럼 챕터 연결 정의가 없으면 이어갈 챕터가 없으므로 Run을 끝낸다(설계 K21 임시 규칙).
            using var f = new Fixture(firstStageBoss: true);
            f.Activate(2);
            WinBattle(f);
            Assert.That(f.Buffers.CombatStateReadSink.Completion.Progression.ClearMap, Is.True);
            AssertNewRunThenStageReturn(f);
        }

        [Test]
        public void FinalChapterVictory_RunCompleted_StartsNewRunThenReturnsToStage()
        {
            using var f = new Fixture(firstStageBoss: true, continuation: new FinalChapterSource());
            f.Activate(2);
            WinBattle(f);
            AssertNewRunThenStageReturn(f);
        }

        [Test]
        public void Defeat_EndsRun_StartsNewRunThenReturnsToStage()
        {
            // 메인 옆에 적 하나를 두고 실제 적 턴의 공격으로 패배 결과를 게시한다.
            using var f = new Fixture(enemyCell: new CellCoord(1, 3));
            f.Activate(2);
            LoseBattle(f);
            Assert.That(f.Buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            AssertNewRunThenStageReturn(f);
        }

        [Test]
        public void Abandonment_DoesNotStartAutomaticExit()
        {
            using var f = new Fixture();
            f.Activate(2);
            // 포기는 Core 포기 API 인수 뒤 별도 경로로 연결한다. 이 테스트의 Core 직접 호출은 운영 연결이 아니다.
            Assert.That(f.Core.Combat.AbandonBattle(), Is.EqualTo(BattleAbandonResult.Abandoned));
            var completion = f.Buffers.CombatStateReadSink.Completion;
            Assert.That(completion.Outcome.Reason, Is.EqualTo(StageOutcomeReason.VoluntaryAbandonment));
            f.Frame(); f.Frame();
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Idle));
            Assert.That(f.View.CompletionShows, Is.Zero);
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(f.Buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));
            Assert.That(f.Faults.IsFaulted, Is.False);
        }

        /// <summary>
        /// Run 종료 결과: 결과 확인 → EndBattle → 새 Run 준비 Job(초기화 → 로스터 지급) 완료 → StageSelect 전환 요청.
        /// StageSelect 진입 뒤 맵 준비는 스테이지 맵 구간의 책임이라 여기서는 전환 요청까지만 본다.
        /// </summary>
        private static void AssertNewRunThenStageReturn(Fixture f)
        {
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.Not.EqualTo(CombatExitStatus.Idle));
            f.Frame();
            Assert.That(f.View.CompletionShows, Is.EqualTo(1));
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Presenting));
            f.View.Confirmed();

            f.Frame();
            Assert.That(f.Faults.IsFaulted, Is.False, f.Faults.FirstError?.ToString());
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.PreparingNewRun));
            Assert.That(f.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.False);
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(1));

            f.Frame();
            Assert.That(f.Faults.IsFaulted, Is.False, f.Faults.FirstError?.ToString());
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.True);
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Transitioning));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(2));
            Assert.That(f.Buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(f.Buffers.CurrentProgressReadSink.HasMap, Is.False);
            Assert.That(f.Buffers.CurrencyReadSink.Amount, Is.Zero);
            Assert.That(f.Buffers.CharacterReadSink.TryGetMainState(out _), Is.True);
        }

        private static void LoseBattle(Fixture f)
        {
            var main = f.Buffers.CharacterReadSink.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            // 테스트 준비: 적의 공격 한 번에 쓰러지도록 메인 체력만 낮춘다. 승패는 Core가 판정한다.
            f.Core.Combat.ApplyHealth(main.EntityId, 1);
            f.Input.Submit(CombatViewCommandData.EnterCombat());
            for (int i = 0; i < 30 && f.Buffers.CombatStateReadSink.Completion == null; i++)
            {
                f.Frame();
                var state = f.Buffers.CombatStateReadSink;
                // 메인 차례가 오면 턴을 넘겨 적 턴을 진행시킨다.
                if (state.Completion == null && f.Jobs.JobRunner.IsIdle && state.Phase == CombatFlowPhase.PlayerInput &&
                    f.InputRead.Commands.Count == 0 && f.InputRead.PresentedResultSequence >= state.LatestResultSequence &&
                    state.LatestResultSequence > 0)
                    f.Input.Submit(CombatViewCommandData.EndUnitTurn());
            }
            Assert.That(f.Buffers.CombatStateReadSink.Completion, Is.Not.Null, "실제 처리로 패배 결과가 게시되지 않았습니다.");
            Assert.That(f.Buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
        }

        /// <summary>첫 챕터를 최종 챕터로 정의한다. 진행 상태를 보관하지 않는다.</summary>
        private sealed class FinalChapterSource : IChapterContinuationSource
        {
            public ChapterContinuationDefinition Read(int chapterKey) => ChapterContinuationDefinition.Final(chapterKey);
        }

        private sealed class DrainJob : IJobService
        {
            internal int Calls;
            internal bool AllowCompletion;
            public int Priority => 0;
            public bool IsCompleted { get; private set; }
            public void Execute() { Calls++; IsCompleted = AllowCompletion; }
        }

        private static int WinBattle(Fixture f, Action afterFinalAttackRegistered = null)
        {
            f.Input.Submit(CombatViewCommandData.EnterCombat());
            f.Frame();
            f.Frame();
            for (int i = 0; i < 8 && !f.Jobs.JobRunner.IsIdle; i++)
                f.Frame();
            Assert.That(f.Buffers.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
            var main = f.Buffers.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Main);
            var path = new MovementPath(new[] { main.Anchor.Value, new CellCoord(0, 2) });
            f.Input.SetPreview(CombatViewPreviewData.Move(main.EntityId, path));
            f.Frame(); f.Frame();
            Assert.That(f.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            f.Input.Submit(CombatViewCommandData.Move(main.EntityId, path, preview.MovementContext));
            f.Frame();
            f.Frame();
            int attack = f.Input.Submit(CombatViewCommandData.Attack(main.EntityId, AttackAim.ForCell(HeadquartersCell)));
            f.Frame();
            // 이 시점에는 공격 Job이 등록됐고 아직 실행되지 않았다.
            afterFinalAttackRegistered?.Invoke();
            f.Frame();
            Assert.That(f.Buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            return attack;
        }

        [Test]
        public void Abandon_InDeployment_RecordsAbandonmentThenStartsNewRunAndReturnsToStage()
        {
            using var f = new Fixture();
            f.Activate(2);
            int sequence = f.Buffers.CombatStateReadSink.LatestResultSequence;
            CompletionSnapshot shown = null;
            f.View.ShowAction = _ => shown = new CompletionSnapshot(f);

            Assert.That(f.LifeCycles.CombatExit.RequestAbandon(), Is.True);
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Draining));
            // 같은 포기는 합치고, 다른 이탈은 받지 않는다.
            Assert.That(f.LifeCycles.CombatExit.RequestAbandon(), Is.True);
            Assert.That(f.LifeCycles.CombatExit.RequestNormalExit(LifeCycleKeys.Stage), Is.False);

            AssertNewRunThenStageReturn(f);
            Assert.That(shown, Is.Not.Null);
            Assert.That(shown.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(shown.Reason, Is.EqualTo(StageOutcomeReason.VoluntaryAbandonment));
            // 포기는 일반 결과를 게시하지 않는다.
            Assert.That(shown.LatestResultSequence, Is.EqualTo(sequence));
        }

        [Test]
        public void Abandon_WhileCommandIsQueued_ProcessesItFirstThenAbandonsOnce()
        {
            using var f = new Fixture();
            f.Activate(2);
            int command = f.Input.Submit(CombatViewCommandData.EnterCombat());
            f.Frame();
            Assert.That(f.Jobs.JobRunner.IsIdle, Is.False, "명령 Job이 등록돼 있어야 한다.");
            CompletionSnapshot shown = null;
            f.View.ShowAction = _ => shown = new CompletionSnapshot(f);

            Assert.That(f.LifeCycles.CombatExit.RequestAbandon(), Is.True);
            AssertNewRunThenStageReturn(f);

            // 이미 등록된 명령을 끝까지 처리하고 그 결과를 표시한 뒤에 포기했다.
            Assert.That(shown, Is.Not.Null);
            Assert.That(shown.HandledCommandNumber, Is.EqualTo(command));
            Assert.That(shown.LatestResultSequence, Is.GreaterThan(0));
            Assert.That(shown.PresentedResultSequence, Is.EqualTo(shown.LatestResultSequence));
            Assert.That(shown.Reason, Is.EqualTo(StageOutcomeReason.VoluntaryAbandonment));
        }

        [Test]
        public void Abandon_WhenQueuedAttackWinsFirst_PresentsVictoryAndKeepsRun()
        {
            using var f = new Fixture();
            f.Activate(2);
            WinBattle(f, () => Assert.That(f.LifeCycles.CombatExit.RequestAbandon(), Is.True));

            // 포기 대기 중 실제 승리가 먼저 기록됐다. 포기로 덮어쓰지 않고 승리를 표시한다.
            Assert.That(f.Faults.IsFaulted, Is.False, f.Faults.FirstError?.ToString());
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Presenting));
            Assert.That(f.View.CompletionShows, Is.EqualTo(1));
            Assert.That(f.Buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            f.View.Confirmed();
            f.Frame();
            Assert.That(f.Faults.IsFaulted, Is.False, f.Faults.FirstError?.ToString());
            // 일반 승리는 Run이 이어지므로 새 Run 없이 바로 전환한다.
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Transitioning));
            Assert.That(f.Loading.LoadCalls, Is.EqualTo(2));
            Assert.That(f.Buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(f.Buffers.CurrentProgressReadSink.HasMap, Is.True);
        }

        [Test]
        public void Abandon_IsRejectedInRecordModeAfterCompletionAndDuringNormalExit()
        {
            using (var record = new Fixture(CombatExecutionMode.RecordInputOnly))
            {
                record.Activate(2);
                Assert.That(record.LifeCycles.CombatExit.RequestAbandon(), Is.False);
                Assert.That(record.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Idle));
            }

            using var f = new Fixture();
            // 진입 전에는 포기할 전투가 없다.
            Assert.That(f.LifeCycles.CombatExit.RequestAbandon(), Is.False);
            f.Activate(2);
            f.View.HoldTerminalResult = true;
            WinBattle(f);
            // 승리가 기록되면 자동 정상 이탈이 시작되고 포기는 받지 않는다.
            Assert.That(f.LifeCycles.CombatExit.ExitStatus, Is.EqualTo(CombatExitStatus.Draining));
            Assert.That(f.LifeCycles.CombatExit.RequestAbandon(), Is.False);
            Assert.That(f.Buffers.CombatStateReadSink.Completion.Outcome.Reason,
                Is.Not.EqualTo(StageOutcomeReason.VoluntaryAbandonment));
        }

        /// <summary>완료 표시 순간의 상태를 기록한다.</summary>
        private sealed class CompletionSnapshot
        {
            internal readonly StageOutcome Outcome;
            internal readonly StageOutcomeReason Reason;
            internal readonly int LatestResultSequence;
            internal readonly int PresentedResultSequence;
            internal readonly int HandledCommandNumber;

            internal CompletionSnapshot(Fixture f)
            {
                var state = f.Buffers.CombatStateReadSink;
                Outcome = state.Completion.Outcome.Outcome;
                Reason = state.Completion.Outcome.Reason;
                LatestResultSequence = state.LatestResultSequence;
                PresentedResultSequence = f.InputRead.PresentedResultSequence;
                HandledCommandNumber = state.HandledCommandNumber;
            }
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly DefinitionSource Source = new();
            internal readonly LoadingStub Loading = new();
            internal readonly RuntimeFaultBoundary Faults = new(_ => { });
            internal readonly FrameworkRuntime Runtime;
            internal readonly IBufferQuery Buffers;
            internal readonly IJobSystemServiceContext Jobs;
            internal readonly IInGameCoreSystemGroupService Core;
            internal readonly ILifeCycleSystemGroupService LifeCycles;
            internal readonly PresentationConsumer View;
            internal int Activations;
            internal ICombatEntryService Entry => LifeCycles.CombatEntry;
            internal ICombatViewBufferWriteSink Input => Buffers.CombatViewWriteSink;
            internal ICombatViewBufferReadSink InputRead => Buffers.CombatViewReadSink;

            internal Fixture(CombatExecutionMode mode = CombatExecutionMode.Live, bool firstStageBoss = false,
                IChapterContinuationSource continuation = null, CellCoord? enemyCell = null)
            {
                var root = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(root);
                Source.EnemyCell = enemyCell;
                var core = InGameCoreSystemFactory.Create(Source, continuation);
                Core = (IInGameCoreSystemGroupService)core;
                core.ExportServices(root);
                View = new PresentationConsumer(this);
                root.AddService<IInGameSystemGroupService>(new InGameServices(Core, View));
                var jobs = JobSystemFactory.Create();
                Jobs = (IJobSystemServiceContext)jobs;
                jobs.ExportServices(root);
                root.AddService<IGlobalSystemGroupService>(new GlobalServices(Jobs, Loading));
                // 실제 View 위치에 결과 확인만 하는 테스트 소비자를 둔다.
                root.AddModule<ISystemModule>(View);
                LifeCycleSystemFactory.Create(mode, Faults).ExportServices(root);
                Runtime = new FrameworkRuntime(root.BuildSystemReader());
                Buffers = Runtime.GetService(BufferSystemKey.Buffer);
                LifeCycles = Runtime.GetService(SystemGroupsService.LifeCycle);
                Runtime.InitData(new EntryDatabase(firstStageBoss));
                Runtime.PostBindService();
                Runtime.Initialize();
                Loading.Registry.RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(100, () =>
                {
                    Assert.That(Entry.IsActive, Is.True);
                    Activations++;
                }));
                Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
                Buffers.StageProgressWriteSink.RegisterStages(1, new[]
                {
                    new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                    new StageProgressData(2, 100, 1, 0, firstStageBoss ? Array.Empty<int>() : new[] { 3 },
                        stageType: StageType.Combat, mapRole: firstStageBoss ? StageMapRole.Boss : StageMapRole.Regular),
                    new StageProgressData(3, 101, 2, 0, Array.Empty<int>(), stageType: StageType.Combat)
                });
                Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                Buffers.CurrencyWriteSink.SetAmount(20);
            }

            internal CombatEntryRequest Request(int node) => Entry.RequestEntry(1, new StageNodeId(node));

            internal CombatEntryRequest Activate(int node)
            {
                var request = Request(node);
                Frame();
                Frame();
                Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Active));
                return request;
            }

            internal void Frame()
            {
                // Bootstrap F1의 호출 경계를 모사한다. 실제 Bootstrap 재생 검증은 별도로 수행한다.
                if (Faults.IsFaulted)
                    return;
                try
                {
                    Loading.Complete();
                    Runtime.Tick(0);
                }
                catch (Exception error)
                {
                    Faults.Report(error);
                    throw;
                }
            }

            public void Dispose() => Runtime.Shutdown();
        }

        private sealed class PresentationConsumer : ISystemTick, ICombatViewService
        {
            private readonly Fixture _fixture;
            internal PresentationConsumer(Fixture fixture) => _fixture = fixture;
            internal int CompletionShows;
            internal Action Confirmed;
            internal Action<Action> ShowAction;
            internal bool HoldTerminalResult;
            internal bool Detached;
            public bool IsAttached => !Detached && _fixture.Entry.IsActive;
            public bool HasPendingPresentation => _fixture.Buffers.CombatStateReadSink.LatestResultSequence > _fixture.InputRead.PresentedResultSequence;
            public void SuspendSubmissions() { }
            public void PresentCompletion(BattleCompletionChanges completion, Action completed)
            {
                CompletionShows++;
                Confirmed = completed;
                ShowAction?.Invoke(completed);
            }
            // ICombatProgressView: 재구성 R3에서 진행 판단이 읽기 전까지 옛 이탈 순서만 쓴다.
            public bool IsPlaybackBusy => false;
            public bool IsSubmissionSuspended => false;
            public CombatCompletionDisplay CompletionDisplay => CombatCompletionDisplay.None;
            public void PresentCompletion(BattleCompletionChanges completion) => throw new NotSupportedException();
            public bool IsAbandonRequested => false;
            public void Attach(CombatSceneRoot scene) { }
            public void Detach() { }
            public void FocusLost() { }
            public void CaptureLost() { }
            public void RequestReturn() { }
            public void Tick(float deltaTime)
            {
                var f = _fixture;
                if (!f.Entry.IsActive)
                    return;
                if (HoldTerminalResult && f.Buffers.CombatStateReadSink.Completion != null)
                    return;
                int sequence = f.Buffers.CombatStateReadSink.LatestResultSequence;
                if (sequence <= f.InputRead.PresentedResultSequence)
                    return;
                f.Input.ClearPreview();
                f.Input.RemoveCommandsThrough(f.Buffers.CombatStateReadSink.HandledCommandNumber);
                f.Input.AcknowledgePresented(sequence);
            }
        }

        private sealed class LoadingStub : ILodingSystemService, ILodingSystemServiceContext
        {
            internal readonly LodingRegisterSystem Registry = new();
            internal int LoadCalls;
            internal Exception Failure;
            private int _pending = -1;
            public int CurrentSceneIndex { get; private set; } = -1;
            public bool IsLoading => _pending >= 0;
            public float LoadingProgress => IsLoading ? 0f : 1f;
            public ILodingRegisterService LodingRegisterService => Registry;
            public ILodingRegisterReader LodingRegisterReader => Registry;
            public ILodingSystemService LodingService => this;
            public void Load(int sceneIndex)
            {
                LoadCalls++;
                if (Failure != null)
                    throw Failure;
                _pending = sceneIndex;
            }
            internal void Complete()
            {
                if (_pending < 0)
                    return;
                Unload();
                CurrentSceneIndex = _pending;
                _pending = -1;
                foreach (var node in Registry.GetSceneEnterLoadedNodes(CurrentSceneIndex))
                    node.Execute();
            }
            public void Unload()
            {
                if (CurrentSceneIndex < 0)
                    return;
                foreach (var node in Registry.GetSceneExitLoadedNodes(CurrentSceneIndex))
                    node.Execute();
                CurrentSceneIndex = -1;
            }
        }

        private sealed class GlobalServices : IGlobalSystemGroupService
        {
            internal GlobalServices(IJobSystemServiceContext jobs, ILodingSystemServiceContext loading)
            {
                JobServiceCommand = jobs;
                LodingServiceCommand = loading;
            }
            public IJobSystemServiceContext JobServiceCommand { get; }
            public ILodingSystemServiceContext LodingServiceCommand { get; }
            public IInputSystemServiceContext InputServiceCommand => null;
            public IRenderServiceContext RenderServiceCommand => null;
            public IUISystemService UIServiceCommand => null;
            public IUtilitySystemServiceContext UtilityServiceCommand => null;
            public ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem.IAudioSystemService AudioServiceCommand => null;
        }

        private sealed class InGameServices : IInGameSystemGroupService
        {
            internal InGameServices(IInGameCoreSystemGroupService core, ICombatViewService view)
            {
                CoreGroup = core;
                ViewGroup = new ViewServices(view);
            }
            public IInGameCoreSystemGroupService CoreGroup { get; }
            public IInGameViewSystemGroupService ViewGroup { get; }
        }

        private sealed class ViewServices : IInGameViewSystemGroupService
        {
            internal ViewServices(ICombatViewService combat) => Combat = combat;
            public ICombatViewService Combat { get; }
        }

        private sealed class DefinitionSource : IBattleInitializationSource
        {
            internal readonly List<(int chapter, int stage)> Reads = new();
            internal Exception Failure;
            internal Action BeforeReturn;
            internal CellCoord? EnemyCell;
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                Reads.Add((chapterKey, stageKey.Value));
                if (Failure != null)
                    throw Failure;
                BeforeReturn?.Invoke();
                var seed = CharacterBattlePreparationTests.Initialization();
                var units = new[] { EntityKey.Unit_MainCharacter, EntityKey.Unit_Akimbo, EntityKey.Unit_Sniper, EntityKey.Unit_Demon }
                    .Select(key => new UnitSpec(key, key == EntityKey.Unit_MainCharacter ? UnitRole.MainCharacter : UnitRole.Regular,
                        1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 3, 1,
                        key == EntityKey.Unit_MainCharacter ? 1 : 2, CategoryKey.Base,
                        5, 4, 1, 0, 1, 1, true, Array.Empty<TraitSpec>(),
                        key == EntityKey.Unit_Demon ? UnitOwnerEligibility.EnemyOnly : UnitOwnerEligibility.PlayerOnly)).ToArray();
                var stage = new StageConfig(stageKey.Value == 100 ? 8 : 9, 7, 8, 2, 4, 42,
                    EnemyCell.HasValue
                        ? new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, EnemyCell.Value, Rotation4.R0) }
                        : Array.Empty<InitialEnemyPlacementSpec>(),
                    Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), 100,
                    new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, HeadquartersCell, Rotation4.R0) });
                var structures = new[] { new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 1, false, 0) };
                var profiles = units.Select(unit => new CombatAttackProfile(unit,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
                return new BattleInitialization(stage, MainCell, Rotation4.R0, units, structures, seed.Categories, profiles, new StageEconomySpec(0, 2));
            }
        }

        private sealed class BoardConfig : IArtifactBoardConfigTableQuery
        {
            public ArtifactBoardConfigData Config { get; } = new(3, 3);
        }

        private sealed class EntryDatabase : IDataBaseQueryContext, IInGameQuery, IStageTableQuery, IStageMapTableQuery,
            IHasArtifactBoardConfig
        {
            private readonly bool _firstStageBoss;
            internal EntryDatabase(bool firstStageBoss) => _firstStageBoss = firstStageBoss;
            private readonly CharacterRosterDatabase _roster = new();
            private readonly StageMapData _map = new(1, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
            public IInGameQuery InGameQuery => this;
            // StageSelect 진입(StageLifeCycle)이 아티팩트 보드 처음 크기를 요구한다(5376b174).
            public IArtifactBoardConfigTableQuery ArtifactBoardConfig { get; } = new BoardConfig();
            public IRenderQuery RenderQuery => _roster.RenderQuery;
            public IViewQuery ViewQuery => _roster.ViewQuery;
            public IStageTableQuery StageTableQuery => this;
            public IUnitTableQuery UnitTableQuery => _roster;
            public ITargetingMethodTableQuery TargetingMethodTableQuery => throw new NotSupportedException();
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster;
            public IStageMapTableQuery StageMapTableQuery => this;
            public StageMapData GetStageMap(int key) => key == 1 ? _map : throw new KeyNotFoundException();
            public IReadOnlyCollection<StageMapData> GetAllStageMaps() => new[] { _map };
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
                return new StageRootData(key, 1, key == 100 && _firstStageBoss ? StageMapRole.Boss : StageMapRole.Regular,
                    new CombatStageDetail(42, "entry-fixture", 1, 0, 1, 100,
                    Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(),
                    EntityKey.Structure_Headquarters, MainCell, Rotation4.R0, HeadquartersCell, Rotation4.R0, null));
            }
            public IReadOnlyList<StageRootData> GetStages(int mapKey) =>
                mapKey == 1 ? new[] { GetStage(100), GetStage(101) } : Array.Empty<StageRootData>();
        }
    }
}
