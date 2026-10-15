using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Chapter
{
    public static class ChapterContinuationCommands
    {
        public static ChapterContinuationResult Complete(ChapterContinuationFacts facts, ChapterContinuationDefinition definition)
            => ChapterContinuationRules.Evaluate(facts, definition);

        public static bool IsMatchingVictory(BattleCompletionChanges completion, int chapterKey, int nodeId, int stageKey)
            => ChapterContinuationRules.IsMatchingVictory(completion, chapterKey, nodeId, stageKey);
    }
}
