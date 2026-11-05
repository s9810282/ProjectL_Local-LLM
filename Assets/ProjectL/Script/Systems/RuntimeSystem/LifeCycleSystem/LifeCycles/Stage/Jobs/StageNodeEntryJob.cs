using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageCommandSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>기대 번호의 처리를 Core에 요청합니다. 입력 조회·판정·결과 게시는 Core가 담당합니다.</summary>
    internal struct StageNodeEntryJob : IJobService
    {
        private readonly int _expectedNumber;
        private readonly IStageCommandProcessingService _core;
        private readonly Func<int> _nextSeed;
        private bool _started;
        public int Priority => 3;
        public bool IsCompleted { get; private set; }

        internal StageNodeEntryJob(int expectedNumber, IStageCommandProcessingService core, Func<int> nextSeed)
        {
            if (expectedNumber < 1) throw new ArgumentOutOfRangeException(nameof(expectedNumber));
            _expectedNumber = expectedNumber;
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _nextSeed = nextSeed ?? throw new ArgumentNullException(nameof(nextSeed));
            _started = false;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (_started) return;
            _started = true;
            _core.ProcessNextNodeCommand(_expectedNumber, _nextSeed);
            // 실행 전 취소로 인한 false도 정상 완료이며 Flow가 입력 수명과 처리 번호를 확인합니다.
            IsCompleted = true;
        }
    }
}
