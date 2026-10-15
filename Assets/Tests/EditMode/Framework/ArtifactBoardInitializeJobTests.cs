using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Plans;

namespace ProjectL.Tests.Framework
{
    /// <summary>보드 초기화 Job이 첫 호출과 재호출을 정상 완료하고, 계약 위반만 예외로 알리는지 확인합니다.</summary>
    public sealed class ArtifactBoardInitializeJobTests
    {
        [Test]
        public void FirstInitialization_PassesConfiguredSizeAndCompletes()
        {
            var artifacts = new ArtifactsSpy(initializeResult: true);
            var board = new BoardStub(ArtifactBufferSnapshot.Empty);
            IJobService job = ArtifactPlan.CreateBoardInitialization(artifacts, board, 4, 5);

            job.Execute();

            Assert.That(job.IsCompleted, Is.True);
            Assert.That(artifacts.Calls, Is.EqualTo(1));
            Assert.That(artifacts.Width, Is.EqualTo(4));
            Assert.That(artifacts.Height, Is.EqualTo(5));
        }

        [Test]
        public void AlreadyInitialized_FalseIsNormalCompletion()
        {
            var artifacts = new ArtifactsSpy(initializeResult: false);
            var board = new BoardStub(new ArtifactBufferSnapshot(new ArtifactState(6, 6, null, null)));
            IJobService job = ArtifactPlan.CreateBoardInitialization(artifacts, board, 4, 4);

            job.Execute();

            Assert.That(job.IsCompleted, Is.True);
        }

        [Test]
        public void FalseWithoutInitializedBoard_Throws()
        {
            var artifacts = new ArtifactsSpy(initializeResult: false);
            var board = new BoardStub(ArtifactBufferSnapshot.Empty);
            IJobService job = ArtifactPlan.CreateBoardInitialization(artifacts, board, 4, 4);

            Assert.Throws<InvalidOperationException>(job.Execute);
            Assert.That(job.IsCompleted, Is.False);
        }

        [Test]
        public void CompletedJob_DoesNotCallCoreAgain()
        {
            var artifacts = new ArtifactsSpy(initializeResult: true);
            IJobService job = ArtifactPlan.CreateBoardInitialization(
                artifacts, new BoardStub(ArtifactBufferSnapshot.Empty), 4, 4);

            job.Execute();
            job.Execute();

            Assert.That(artifacts.Calls, Is.EqualTo(1));
        }

        [Test]
        public void Priority_RunsAfterStageMapPreparation()
        {
            IJobService job = ArtifactPlan.CreateBoardInitialization(
                new ArtifactsSpy(true), new BoardStub(ArtifactBufferSnapshot.Empty), 4, 4);

            // 맵 준비 StageJob은 0(새 Run이면 1)입니다. 작은 Priority가 먼저 실행됩니다.
            Assert.That(job.Priority, Is.GreaterThan(1));
        }

        [TestCase(0, 4)]
        [TestCase(4, 0)]
        public void InvalidSize_Throws(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ArtifactPlan.CreateBoardInitialization(
                new ArtifactsSpy(true), new BoardStub(ArtifactBufferSnapshot.Empty), width, height));
        }

        private sealed class BoardStub : IArtifactBufferReadSink
        {
            public BoardStub(ArtifactBufferSnapshot current) => Current = current;
            public ArtifactBufferSnapshot Current { get; }
        }

        private sealed class ArtifactsSpy : IArtifactEditingService
        {
            private readonly bool _initializeResult;
            public int Calls;
            public int Width;
            public int Height;

            public ArtifactsSpy(bool initializeResult) => _initializeResult = initializeResult;

            public bool InitializeBoard(int width, int height)
            {
                Calls++;
                Width = width;
                Height = height;
                return _initializeResult;
            }

            public void Place(int artifactId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
            public void Move(int instanceId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
            public void ReturnToPool(int instanceId) => throw new NotSupportedException();
            public void ExpandBoard(int width, int height) => throw new NotSupportedException();
            public void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
            public void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
        }
    }
}
