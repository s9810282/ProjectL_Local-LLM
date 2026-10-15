using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Plans;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 아티팩트 요청이 Plan·Job을 거쳐 Core를 한 번만 부르고, 거절·예외·취소·프리뷰 교체·무효화 규칙을 지키는지 확인합니다(계획 Phase B).
    /// </summary>
    public sealed class ArtifactEditSchedulerTests
    {
        private const int PlacedInstanceId = 7;

        private RunnerSpy _runner;
        private CoreFake _core;
        private BoardStub _board;
        private RuntimeFaultBoundary _faults;
        private ArtifactEditScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _runner = new RunnerSpy();
            _core = new CoreFake();
            _board = new BoardStub(Initialized());
            _faults = new RuntimeFaultBoundary(_ => { });
            _scheduler = new ArtifactEditScheduler(_runner, _core, _board, _core, _core, _faults);
        }

        #region 편집

        [Test]
        public void Place_CallsCoreOnce_AndStoresPublishedResult()
        {
            var request = ArtifactEditRequest.Place(3, new CellCoord(1, 2), Rotation4.R90);

            Assert.That(_scheduler.TryRequestEdit(request), Is.True);
            _runner.RunAll();

            Assert.That(_core.Calls, Is.EqualTo(new[] { "Place 3 (1, 2) R90" }));
            Assert.That(request.Status, Is.EqualTo(ArtifactRequestStatus.Completed));
            Assert.That(request.Result.Value.Succeeded, Is.True);
            Assert.That(_runner.Pending, Is.Zero);
            Assert.That(_runner.Jobs[0].Priority, Is.EqualTo(ArtifactPlan.RequestPriority));
        }

        [Test]
        public void NormalRejection_CompletesOnceWithFailureReason()
        {
            _core.NextEditResult = ArtifactActionResult.Failure(ArtifactActionFailureReason.CellOccupied);
            var request = ArtifactEditRequest.Move(PlacedInstanceId, CellCoord.Zero, Rotation4.R0);

            _scheduler.TryRequestEdit(request);
            _runner.RunAll();

            Assert.That(request.Status, Is.EqualTo(ArtifactRequestStatus.Completed));
            Assert.That(request.Result.Value.FailureReason, Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
            Assert.That(_core.Calls.Count, Is.EqualTo(1));
            Assert.That(_runner.Pending, Is.Zero, "정상 거절도 Job을 한 번 완료합니다.");
        }

        [Test]
        public void CoreException_MarksFaulted_AndPropagates()
        {
            _core.ThrowOnEdit = true;
            var request = ArtifactEditRequest.Return(PlacedInstanceId);

            _scheduler.TryRequestEdit(request);

            Assert.Throws<InvalidOperationException>(_runner.RunAll);
            Assert.That(request.Status, Is.EqualTo(ArtifactRequestStatus.Faulted));
            Assert.That(request.Result, Is.Null, "이전 결과를 이번 요청의 결과로 읽지 않습니다.");
        }

        [Test]
        public void CancelledEdit_CompletesWithoutCallingCore()
        {
            var request = ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0);
            _scheduler.TryRequestEdit(request);

            _scheduler.CancelPending();
            _runner.RunAll();

            Assert.That(request.Status, Is.EqualTo(ArtifactRequestStatus.Cancelled));
            Assert.That(_core.Calls, Is.Empty);
            Assert.That(_runner.Pending, Is.Zero, "취소된 Job도 큐에 남지 않습니다.");
        }

        [Test]
        public void SecondEdit_WhilePending_IsRejected_AndAcceptedAfterCompletion()
        {
            Assert.That(_scheduler.TryRequestEdit(ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0)), Is.True);
            Assert.That(_scheduler.TryRequestEdit(ArtifactEditRequest.Place(4, CellCoord.Zero, Rotation4.R0)), Is.False);
            Assert.That(_runner.Jobs.Count, Is.EqualTo(1));

            _runner.RunAll();

            Assert.That(_scheduler.TryRequestEdit(ArtifactEditRequest.Place(4, CellCoord.Zero, Rotation4.R0)), Is.True);
        }

        [Test]
        public void ZeroIds_AreValidAndPassedThrough()
        {
            _scheduler.TryRequestEdit(ArtifactEditRequest.Place(0, CellCoord.Zero, Rotation4.R0));
            _runner.RunAll();
            _scheduler.TryRequestEdit(ArtifactEditRequest.Move(0, new CellCoord(1, 0), Rotation4.R0));
            _runner.RunAll();

            Assert.That(_core.Calls, Is.EqualTo(new[] { "Place 0 (0, 0) R0", "Move 0 (1, 0) R0" }));
        }

        [Test]
        public void SameDefinitionTwice_IsTwoIndependentRequests()
        {
            var first = ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0);
            var second = ArtifactEditRequest.Place(3, new CellCoord(2, 0), Rotation4.R0);

            _scheduler.TryRequestEdit(first);
            _runner.RunAll();
            _scheduler.TryRequestEdit(second);
            _runner.RunAll();

            Assert.That(first.Status, Is.EqualTo(ArtifactRequestStatus.Completed));
            Assert.That(second.Status, Is.EqualTo(ArtifactRequestStatus.Completed));
            Assert.That(_core.Calls.Count, Is.EqualTo(2));
        }

        [Test]
        public void InvalidRequestTargets_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ArtifactEditRequest.Place(-1, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ArtifactEditRequest.Return(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ArtifactPreviewRequest.Move(-1, CellCoord.Zero, Rotation4.R0));
        }

        #endregion

        #region 받지 않는 상태

        [Test]
        public void Faulted_RejectsEditAndPreview_WithoutRegistering()
        {
            _faults.Report(new InvalidOperationException("test fault"));

            Assert.That(_scheduler.TryRequestEdit(ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0)), Is.False);
            Assert.That(_scheduler.TryRequestPreview(ArtifactPreviewRequest.Place(3, CellCoord.Zero, Rotation4.R0)), Is.False);
            Assert.That(_runner.Jobs, Is.Empty);
        }

        [Test]
        public void UninitializedBoard_RejectsEditAndPreview()
        {
            _board.Current = ArtifactBufferSnapshot.Empty;

            Assert.That(_scheduler.TryRequestEdit(ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0)), Is.False);
            Assert.That(_scheduler.TryRequestPreview(ArtifactPreviewRequest.Place(3, CellCoord.Zero, Rotation4.R0)), Is.False);
            Assert.That(_runner.Jobs, Is.Empty);
        }

        [Test]
        public void Preview_WhileEditPending_IsRejected()
        {
            _scheduler.TryRequestEdit(ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0));

            Assert.That(_scheduler.TryRequestPreview(ArtifactPreviewRequest.Place(3, CellCoord.Zero, Rotation4.R0)), Is.False);
        }

        #endregion

        #region 프리뷰

        [Test]
        public void ContinuousPreviews_RegisterOneJob_AndRunOnlyTheLatest()
        {
            var first = ArtifactPreviewRequest.Place(3, new CellCoord(0, 0), Rotation4.R0);
            var second = ArtifactPreviewRequest.Place(3, new CellCoord(1, 0), Rotation4.R0);
            var third = ArtifactPreviewRequest.Place(3, new CellCoord(2, 0), Rotation4.R0);

            _scheduler.TryRequestPreview(first);
            _scheduler.TryRequestPreview(second);
            _scheduler.TryRequestPreview(third);
            Assert.That(_runner.Jobs.Count, Is.EqualTo(1), "대기 중인 프리뷰 Job은 하나뿐입니다.");

            _runner.RunAll();

            Assert.That(_core.Calls, Is.EqualTo(new[] { "PreviewPlace 3 (2, 0) R0" }));
            Assert.That(first.Status, Is.EqualTo(ArtifactRequestStatus.Cancelled));
            Assert.That(second.Status, Is.EqualTo(ArtifactRequestStatus.Cancelled));
            Assert.That(_scheduler.LatestPreview, Is.SameAs(third));
            Assert.That(third.Result, Is.Not.Null);
        }

        [Test]
        public void PreviewAfterJobRan_RegistersNewJob()
        {
            _scheduler.TryRequestPreview(ArtifactPreviewRequest.Place(3, CellCoord.Zero, Rotation4.R0));
            _runner.RunAll();

            _scheduler.TryRequestPreview(ArtifactPreviewRequest.Place(3, new CellCoord(1, 0), Rotation4.R0));

            Assert.That(_runner.Pending, Is.EqualTo(1));
        }

        [Test]
        public void EditRequest_InvalidatesPendingAndLatestPreview()
        {
            _scheduler.TryRequestPreview(ArtifactPreviewRequest.Place(3, CellCoord.Zero, Rotation4.R0));
            _runner.RunAll();
            var pending = ArtifactPreviewRequest.Place(3, new CellCoord(1, 0), Rotation4.R0);
            _scheduler.TryRequestPreview(pending);

            _scheduler.TryRequestEdit(ArtifactEditRequest.Place(3, new CellCoord(1, 0), Rotation4.R0));
            _core.Calls.Clear();
            _runner.RunAll();

            Assert.That(pending.Status, Is.EqualTo(ArtifactRequestStatus.Cancelled));
            Assert.That(_scheduler.LatestPreview, Is.Null);
            Assert.That(_core.Calls, Is.EqualTo(new[] { "Place 3 (1, 0) R0" }), "취소된 프리뷰는 Core를 부르지 않습니다.");
        }

        [Test]
        public void InvalidatePreview_DropsLatest_AndPendingJobDoesNothing()
        {
            _scheduler.TryRequestPreview(ArtifactPreviewRequest.Place(3, CellCoord.Zero, Rotation4.R0));
            _runner.RunAll();
            var pending = ArtifactPreviewRequest.Place(3, new CellCoord(1, 0), Rotation4.R90);
            _scheduler.TryRequestPreview(pending);

            // 회전 변경·드래그 종료·닫기에서 부르는 경로입니다.
            _scheduler.InvalidatePreview();
            _core.Calls.Clear();
            _runner.RunAll();

            Assert.That(_scheduler.LatestPreview, Is.Null);
            Assert.That(pending.Status, Is.EqualTo(ArtifactRequestStatus.Cancelled));
            Assert.That(_core.Calls, Is.Empty);
            Assert.That(_runner.Pending, Is.Zero);
        }

        [Test]
        public void PreviewMove_ForInstanceNoLongerPlaced_IsNotExecuted()
        {
            var request = ArtifactPreviewRequest.Move(PlacedInstanceId, new CellCoord(1, 1), Rotation4.R0);
            _scheduler.TryRequestPreview(request);

            // 프리뷰 Job이 실행되기 전에 인스턴스가 반환된 상황입니다.
            _board.Current = new ArtifactBufferSnapshot(new ArtifactState(4, 4, null, null));
            _runner.RunAll();

            Assert.That(_core.Calls, Is.Empty);
            Assert.That(request.Status, Is.EqualTo(ArtifactRequestStatus.Cancelled));
            Assert.That(_scheduler.LatestPreview, Is.Null);
        }

        [Test]
        public void PreviewMove_ForPlacedInstance_RunsOnce()
        {
            _scheduler.TryRequestPreview(ArtifactPreviewRequest.Move(PlacedInstanceId, new CellCoord(1, 1), Rotation4.R180));
            _runner.RunAll();

            Assert.That(_core.Calls, Is.EqualTo(new[] { $"PreviewMove {PlacedInstanceId} (1, 1) R180" }));
        }

        #endregion

        private static ArtifactBufferSnapshot Initialized() => new(new ArtifactState(4, 4,
            null,
            new[] { new ArtifactItemState(3, PlacedInstanceId, true, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) }));

        private sealed class BoardStub : IArtifactBufferReadSink
        {
            public BoardStub(ArtifactBufferSnapshot current) => Current = current;
            public ArtifactBufferSnapshot Current { get; set; }
        }

        /// <summary>Job을 등록 순서대로 한 번씩 실행하고, 끝난 Job만 큐에서 뺍니다.</summary>
        private sealed class RunnerSpy : IJobRunner
        {
            public readonly List<IJobService> Jobs = new();
            private readonly List<IJobService> _queue = new();

            public int Pending => _queue.Count;
            public bool IsIdle => _queue.Count == 0;

            public void Register(IJobService job)
            {
                Jobs.Add(job);
                _queue.Add(job);
            }

            public void RegisterRange(IReadOnlyList<IJobService> jobs)
            {
                foreach (IJobService job in jobs)
                    Register(job);
            }

            public void Clear() => throw new NotSupportedException("공용 Runner를 비우면 안 됩니다.");

            public void RunAll()
            {
                foreach (IJobService job in _queue.ToArray())
                {
                    job.Execute();
                    if (job.IsCompleted)
                        _queue.Remove(job);
                }
            }
        }

        /// <summary>호출을 기록하고, 정상 반환한 호출마다 결과·프리뷰를 게시하는 Core 대역입니다.</summary>
        private sealed class CoreFake : IArtifactEditingService, IArtifactEditResultReadSink, IArtifactPreviewReadSink
        {
            public readonly List<string> Calls = new();
            public ArtifactActionResult NextEditResult = ArtifactActionResult.Success();
            public bool ThrowOnEdit;

            public ArtifactActionResult? LastEditResult { get; private set; }
            public ArtifactPlacementPreview LastPreview { get; private set; }

            public bool InitializeBoard(int width, int height) => throw new NotSupportedException();
            public void ExpandBoard(int width, int height) => throw new NotSupportedException();

            public void Place(int artifactId, CellCoord anchor, Rotation4 rotation) =>
                Edit($"Place {artifactId} ({anchor.X}, {anchor.Y}) {rotation}");

            public void Move(int instanceId, CellCoord anchor, Rotation4 rotation) =>
                Edit($"Move {instanceId} ({anchor.X}, {anchor.Y}) {rotation}");

            public void ReturnToPool(int instanceId) => Edit($"Return {instanceId}");

            public void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation) =>
                Preview($"PreviewPlace {artifactId} ({anchor.X}, {anchor.Y}) {rotation}", anchor);

            public void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation) =>
                Preview($"PreviewMove {instanceId} ({anchor.X}, {anchor.Y}) {rotation}", anchor);

            private void Edit(string call)
            {
                Calls.Add(call);
                if (ThrowOnEdit)
                    throw new InvalidOperationException("core failure");
                LastEditResult = NextEditResult;
            }

            private void Preview(string call, CellCoord anchor)
            {
                Calls.Add(call);
                LastPreview = new ArtifactPlacementPreview(null, anchor, ArtifactActionFailureReason.DefinitionNotFound);
            }
        }
    }
}
