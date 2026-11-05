using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.ArtifactPreparation
{
    /// <summary>
    /// 아티팩트 보드를 처음 크기로 한 번 초기화합니다. 이미 초기화돼 있으면 상태를 바꾸지 않고 정상 완료합니다.
    /// </summary>
    /// <remarks>
    /// Core는 두 번째 호출부터 false를 돌려주고 상태를 보존합니다. false인데 Buffer가 초기화되지 않았다면
    /// 정상 거절이 아니라 계약 위반이므로 예외로 알립니다(아티팩트 팝업 계획 D7b).
    /// 보드는 전투도 읽고 새 Run에도 다시 만들지 않는(부트~종료 동안 유지되는) 게임 데이터라,
    /// 부트 때 한 번 도는 <see cref="ArtifactBoardPreparation"/>과 함께 ArtifactPreparation에 둡니다.
    /// </remarks>
    internal struct ArtifactBoardInitializeJob : IJobService
    {
        /// <summary>보드 초기화 Job의 기존 우선순위입니다. 최초 준비는 독립 배치로 실행합니다.</summary>
        internal const int InitializationPriority = 2;

        private readonly IArtifactEditingService _artifacts;
        private readonly IArtifactBufferReadSink _board;
        private readonly int _width;
        private readonly int _height;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        /// <summary>보드를 처음 크기로 초기화하는 Job을 만듭니다. Core와 읽기 Sink만 받습니다(FW-101).</summary>
        internal static IJobService Create(IArtifactEditingService artifacts, IArtifactBufferReadSink board,
            int initialWidth, int initialHeight)
        {
            return new ArtifactBoardInitializeJob(artifacts, board, initialWidth, initialHeight, InitializationPriority);
        }

        internal ArtifactBoardInitializeJob(IArtifactEditingService artifacts, IArtifactBufferReadSink board,
            int width, int height, int priority)
        {
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            _width = width;
            _height = height;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (!_artifacts.InitializeBoard(_width, _height) && !_board.Current.IsInitialized)
                throw new InvalidOperationException(
                    "[ArtifactBoardInitializeJob] InitializeBoard returned false, but the board is not initialized.");

            IsCompleted = true;
        }
    }
}
