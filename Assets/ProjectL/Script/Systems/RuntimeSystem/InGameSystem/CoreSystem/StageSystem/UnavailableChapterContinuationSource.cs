using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem
{
    internal sealed class UnavailableChapterContinuationSource : IChapterContinuationSource
    {
        public ChapterContinuationDefinition Read(int chapterKey)
            => throw new NotSupportedException("챕터 연결 정의가 제공되지 않았습니다.");
    }
}
