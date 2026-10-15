using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>챕터 맵을 준비하고 Buffer에 현재 챕터를 기록한다.</summary>
    internal struct StageJob : IJobService
    {
        private readonly IInGameSystemGroupService _service;
        private readonly ICurrentProgressBufferReadSink _current;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal StageJob(IInGameSystemGroupService service, int priority, ICurrentProgressBufferReadSink current)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted) return;
            if (_service == null)
                throw new InvalidOperationException("StageJob requires an in-game service.");

            // 등록 시점의 복사본 대신 앞선 초기화 Job까지 반영된 현재 챕터를 읽는다.
            int chapterKey = _current.HasMap ? _current.CurrentMapKey : 1;
            var result = _service.CoreGroup.ChapterProgression.EnterChapter(chapterKey);
            if (!result.Accepted)
                throw new InvalidOperationException($"Chapter entry rejected: {result.FailureReason}.");
            IsCompleted = true;
        }
    }

    /// <summary>
    /// 새 Run을 시작한다. 이전 Run 정리·시작 로스터·시작 Run 재화는 Core·Buffer가 한 번에 처리하고,
    /// 이 Job은 Plan이 정한 순서(챕터 진입 전)에 한 번 요청만 한다. 거절이면 예외로 흐름을 멈춘다.
    /// </summary>
    internal struct NewRunJob : IJobService
    {
        private readonly IInGameSystemGroupService _service;
        private readonly int _loadoutKey;
        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal NewRunJob(IInGameSystemGroupService service, int loadoutKey, int priority)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            if (loadoutKey < 1) throw new ArgumentOutOfRangeException(nameof(loadoutKey));
            _loadoutKey = loadoutKey;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted) return;
            var result = _service.CoreGroup.RunStart.StartNewRun(_loadoutKey);
            if (!result.Succeeded)
                throw new InvalidOperationException($"New run rejected: {result.FailureReason}. Loadout: {_loadoutKey}.");
            IsCompleted = true;
        }
    }
}
