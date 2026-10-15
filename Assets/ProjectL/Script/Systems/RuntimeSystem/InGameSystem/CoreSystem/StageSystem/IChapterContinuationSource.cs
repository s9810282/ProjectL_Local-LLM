using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem
{
    internal interface IChapterContinuationSource
    {
        ChapterContinuationDefinition Read(int chapterKey);
    }
}
