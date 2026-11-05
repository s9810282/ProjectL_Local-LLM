#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>경로 판정 당시의 진행 원본과 마지막 선택만 보관하는 불변 설치 제안이다.</summary>
    internal sealed class DevelopmentProgressPreparation
    {
        internal DevelopmentContext Expected { get; }
        internal StageSelectionData ExpectedSelection { get; }
        internal IReadOnlyList<StageProgressData> ExpectedStages { get; }
        internal bool ExpectedMapClear { get; }
        internal IReadOnlyList<StageNodeId> BypassedNodes { get; }
        internal StageNodeId TargetNodeId { get; }
        internal StageKey TargetStageKey { get; }
        internal StageNodeId? FinalSelectedNodeId { get; }
        internal StageKey FinalSelectedStageKey { get; }
        internal string FinalSelectedStageName { get; }

        internal DevelopmentProgressPreparation(DevelopmentContext expected, StageSelectionData expectedSelection,
            IReadOnlyList<StageProgressData> expectedStages, bool expectedMapClear,
            IReadOnlyList<StageNodeId> bypassedNodes, StageNodeId targetNodeId, StageKey targetStageKey,
            StageNodeId? finalSelectedNodeId, StageKey finalSelectedStageKey, string finalSelectedStageName)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            ExpectedSelection = expectedSelection;
            ExpectedStages = Array.AsReadOnly((expectedStages ?? throw new ArgumentNullException(nameof(expectedStages))).ToArray());
            ExpectedMapClear = expectedMapClear;
            BypassedNodes = Array.AsReadOnly((bypassedNodes ?? throw new ArgumentNullException(nameof(bypassedNodes))).ToArray());
            TargetNodeId = targetNodeId;
            TargetStageKey = targetStageKey;
            FinalSelectedNodeId = finalSelectedNodeId;
            FinalSelectedStageKey = finalSelectedStageKey;
            FinalSelectedStageName = finalSelectedStageName;
        }
    }
}
#endif
