using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 덱 편집 창이 읽는 Stage 흐름의 사실입니다. Stage 수명이 서비스를 받은 뒤 한 번 만들어 창에 넘깁니다.
    /// 값은 부를 때마다 원본(오류 경계·LifeCycle 관리자·로딩·Runner·진행·Buffer)에서 읽고 저장하지 않습니다.
    /// 어떤 사실이 어떤 불가 이유가 되는지는 <see cref="StageDeckEditWindow"/> 한 곳이 정합니다(평가 C-23 — 수명 클래스 밖으로 옮김).
    /// </summary>
    internal sealed class StageDeckEditFacts : IStageDeckEditFacts
    {
        private readonly RuntimeFaultBoundary _faults;
        private readonly ILifeCycleManager _manager;
        private readonly ILodingSystemService _loading;
        private readonly IJobRunner _runner;
        private readonly StageProgress _progress;
        private readonly ICharacterBufferReadSink _roster;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageProgressBufferReadSink _stages;

        internal StageDeckEditFacts(RuntimeFaultBoundary faults, ILifeCycleManager manager, ILodingSystemService loading,
            IJobRunner runner, StageProgress progress, ICharacterBufferReadSink roster,
            ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _loading = loading ?? throw new ArgumentNullException(nameof(loading));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
        }

        public bool IsFaulted => _faults.IsFaulted;
        public bool IsStageActive => _manager.HasActiveLifeCycle && _manager.CurrentKey.Equals(LifeCycleKeys.Stage);
        public bool IsLoading => _loading.IsLoading;
        public bool IsRunnerIdle => _runner.IsIdle;
        public bool IsFlowStopped => _progress.IsStopped;
        public bool IsMapPrepared => _progress.IsMapReady;
        public bool HasRoster => _roster.Owned.Count > 0;
        public bool HasPendingFlowCheck => _progress.HasPendingCheck;
        public bool HasUnregisteredRequest => _progress.HasUnregisteredRequest;

        // 노드는 들어갈 때 선택되고 ResolveStage에서 클리어됩니다. 완료된 상점·회복 기록은 잠금이 아닙니다(RD-0 F1).
        public bool HasOpenInteraction =>
            _current.HasSelection && !_stages.IsCleared(_current.CurrentMapKey, _current.CurrentStage.NodeId);
    }
}
