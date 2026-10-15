using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// CombatPathInput 단독 테스트용 사실 공급자입니다(구조 개선 VR1).
    /// CombatViewSystem.ReadFacts(includeUnhandledCommand: true)와 같은 값을 실제 Buffer에서 읽고,
    /// 씬 연결·대상(Combat Layer의 경로 입력)만 테스트가 정한 값으로 둡니다.
    /// </summary>
    internal static class PathInputTestFacts
    {
        internal static CombatInputFacts Read(IBufferQuery buffers, bool inputAvailable)
        {
            var state = buffers.CombatStateReadSink;
            var input = buffers.CombatViewReadSink;
            bool hasBattle = state.HasBattle;
            int latest = state.LatestResultSequence;
            int presented = input.PresentedResultSequence;
            return new CombatInputFacts(
                isBattleDisplayed: true,
                hasBattle: hasBattle,
                phase: state.Phase,
                stagePhase: hasBattle ? state.Current.CommandState.Turn.StagePhase : StagePhase.Deployment,
                hasCompletion: state.Completion != null,
                executionMode: CombatExecutionMode.Live,
                currentLayer: StageInputLayers.Combat,
                latestResultSequence: latest,
                presentedResultSequence: presented,
                isFinalizing: false,
                activeActorId: hasBattle ? state.Current.CommandState.ActiveActorId : EntityId.Empty,
                hasUnhandledCommand: input.TryGetNextCommand(state.HandledCommandNumber, out _),
                previewAnswer: null,
                hasPendingCommand: false,
                isPresenting: hasBattle && latest > presented,
                isInputAvailable: inputAvailable,
                isPathTarget: true);
        }
    }
}
