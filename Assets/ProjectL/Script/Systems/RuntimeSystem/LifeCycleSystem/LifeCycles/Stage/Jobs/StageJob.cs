using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.RunStart;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>챕터 맵을 준비하고 Buffer에 현재 챕터를 기록한다.</summary>
    internal struct StageJob : IJobService
    {
        // 새 Run의 첫 진입에 아직 현재 맵이 없을 때 들어가는 맵입니다. 새 Run 시작(RunStart)도 1번 챕터로 시작합니다.
        private const int FirstChapterKey = 1;
        private readonly IChapterProgressionCoordinator _chapters;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageViewBufferWriteSink _stageInput;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal StageJob(IChapterProgressionCoordinator chapters, int priority, ICurrentProgressBufferReadSink current,
            IStageViewBufferWriteSink stageInput)
        {
            _chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stageInput = stageInput ?? throw new ArgumentNullException(nameof(stageInput));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted) return;
            if (_chapters == null)
                throw new InvalidOperationException("StageJob requires a chapter progression service.");

            // 등록 시점의 복사본 대신 앞선 초기화 Job까지 반영된 현재 챕터를 읽는다.
            int chapterKey = _current.HasMap ? _current.CurrentMapKey : FirstChapterKey;
            // 교체 전 입력을 취소한다. 발급 번호와 이미 게시한 결과는 유지한다.
            _stageInput.Clear();
            var result = _chapters.EnterChapter(chapterKey);
            if (!result.Accepted)
                throw new InvalidOperationException($"Chapter entry rejected: {result.FailureReason}.");
            IsCompleted = true;
        }
    }

    /// <summary>
    /// Stage 첫 진입의 새 Run 결과를 받습니다. 받는 요청이 없어 늘 시작하고, 거절이면 예외로 흐름을 멈춥니다(옛 NewRunJob과 같음).
    /// 호출·검사는 <see cref="RunStartJob"/>이 합니다.
    /// </summary>
    internal sealed class StageRunStartOutcome : IRunStartOutcome
    {
        internal static readonly StageRunStartOutcome Instance = new();

        private StageRunStartOutcome()
        {
        }

        public bool TryStart() => true;

        public void Publish(RosterAcquireResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"New run rejected: {result.FailureReason}. Loadout: {RunStartJob.DefaultLoadoutKey}.");
        }

        public void Fail()
        {
        }
    }
}
