using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 노드 진입(전투 표시 소유권·진입 결과 관찰·노드 이동·출전 미리 이동)이 쓰는 값의 묶음입니다(스테이지 선택 리팩토링 설계 §2.9 C-19).
    /// </summary>
    internal sealed class StageMapViewEntryServices
    {
        internal StageMapViewEntryServices(IMapCombatPresentationOwnerSource presentationOwner,
            ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            IStageViewBufferReadSink requests, IStageViewBufferWriteSink requestWriter,
            IStageCommandResultReadSink results, IStageEntryAttemptRead attempts, StageMapViewData mapData,
            IDelayService delay, IAudioSystemService audio, StageNodeClearHistory clearHistory)
        {
            PresentationOwner = presentationOwner ?? throw new ArgumentNullException(nameof(presentationOwner));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));
            Requests = requests ?? throw new ArgumentNullException(nameof(requests));
            RequestWriter = requestWriter ?? throw new ArgumentNullException(nameof(requestWriter));
            Results = results ?? throw new ArgumentNullException(nameof(results));
            Attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
            MapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
            Delay = delay ?? throw new ArgumentNullException(nameof(delay));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            ClearHistory = clearHistory ?? throw new ArgumentNullException(nameof(clearHistory));
        }

        internal IMapCombatPresentationOwnerSource PresentationOwner { get; }
        internal ICurrentProgressBufferReadSink Current { get; }
        internal IStageProgressBufferReadSink Stages { get; }
        internal IStageViewBufferReadSink Requests { get; }
        internal IStageViewBufferWriteSink RequestWriter { get; }
        internal IStageCommandResultReadSink Results { get; }
        internal IStageEntryAttemptRead Attempts { get; }
        internal StageMapViewData MapData { get; }
        internal IDelayService Delay { get; }
        internal IAudioSystemService Audio { get; }
        internal StageNodeClearHistory ClearHistory { get; }
    }
}
