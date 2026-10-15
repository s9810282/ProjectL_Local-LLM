using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    public sealed class ChapterContinuationPreparation
    {
        public ChapterContinuationResult Decision { get; }
        public StageSelectionData ExpectedSelection { get; }
        public BattleCompletionChanges ExpectedCompletion { get; }
        public IReadOnlyList<StageProgressData> ExpectedSourceStages { get; }
        public IReadOnlyList<StageProgressData> ExpectedTargetStages { get; }
        public IReadOnlyList<StageProgressData> GeneratedTargetStages { get; }
        /// <summary>Core가 판정할 때 읽은 원본·대상 챕터의 완료 상태다. Buffer는 현재 값과 같은지만 비교한다.</summary>
        public bool ExpectedSourceMapClear { get; }
        public bool ExpectedTargetMapClear { get; }

        public ChapterContinuationPreparation(ChapterContinuationResult decision, StageSelectionData expectedSelection,
            BattleCompletionChanges expectedCompletion, IReadOnlyList<StageProgressData> expectedSourceStages,
            IReadOnlyList<StageProgressData> expectedTargetStages, IReadOnlyList<StageProgressData> generatedTargetStages,
            bool expectedSourceMapClear, bool expectedTargetMapClear)
        {
            Decision = decision ?? throw new ArgumentNullException(nameof(decision));
            if (!decision.Accepted) throw new ArgumentException("승인된 챕터 완료가 필요합니다.", nameof(decision));
            ExpectedSelection = expectedSelection;
            ExpectedCompletion = expectedCompletion ?? throw new ArgumentNullException(nameof(expectedCompletion));
            ExpectedSourceStages = Copy(expectedSourceStages);
            ExpectedTargetStages = Copy(expectedTargetStages);
            GeneratedTargetStages = Copy(generatedTargetStages);
            ExpectedSourceMapClear = expectedSourceMapClear;
            ExpectedTargetMapClear = expectedTargetMapClear;
            if (decision.Kind == ChapterContinuationKind.RunCompleted
                ? ExpectedTargetStages.Count != 0 || GeneratedTargetStages.Count != 0
                : decision.Kind != ChapterContinuationKind.NextChapterSelected ||
                  (ExpectedTargetStages.Count == 0) == (GeneratedTargetStages.Count == 0))
                throw new ArgumentException("챕터 경로와 준비한 그래프가 일치해야 합니다.");
        }

        private static IReadOnlyList<StageProgressData> Copy(IReadOnlyList<StageProgressData> values)
            => Array.AsReadOnly((values ?? throw new ArgumentNullException(nameof(values))).ToArray());

        internal static bool SameGraph(IReadOnlyList<StageProgressData> expected, IReadOnlyList<StageProgressData> actual)
        {
            if (expected.Count != actual.Count) return false;
            for (int i = 0; i < expected.Count; i++)
            {
                var a = expected[i]; var b = actual[i];
                if (a.NodeId != b.NodeId || a.StageKey != b.StageKey || a.IsClear != b.IsClear || a.Depth != b.Depth ||
                    a.WidthIndex != b.WidthIndex || a.StageType != b.StageType || a.MapRole != b.MapRole ||
                    !a.NextNodeIds.SequenceEqual(b.NextNodeIds)) return false;
            }
            return true;
        }
    }
}
