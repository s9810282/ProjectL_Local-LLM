using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>완료 연출 조립에 필요한 기록·효과·진행 읽기·작은 점프 의존성만 전달합니다.</summary>
    internal sealed class StageMapViewCompletionServices
    {
        internal StageMapViewCompletionServices(StageNodeClearHistory history, StageNodeBreakEffects effects,
            ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            IRunProgressBufferReadSink runProgress, IDelayService delay, IAudioSystemService audio,
            StageMapViewData mapData)
        {
            History = history ?? throw new ArgumentNullException(nameof(history));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));
            RunProgress = runProgress ?? throw new ArgumentNullException(nameof(runProgress));
            Delay = delay ?? throw new ArgumentNullException(nameof(delay));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            MapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
        }

        internal StageNodeClearHistory History { get; }
        internal StageNodeBreakEffects Effects { get; }
        internal ICurrentProgressBufferReadSink Current { get; }
        internal IStageProgressBufferReadSink Stages { get; }
        internal IRunProgressBufferReadSink RunProgress { get; }
        internal IDelayService Delay { get; }
        internal IAudioSystemService Audio { get; }
        internal StageMapViewData MapData { get; }
    }
}
