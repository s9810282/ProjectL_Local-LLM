#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;

namespace ProjectL.Systems.Factory.Runtime.Development
{
    /// <summary>두 챕터 개발 경로의 정의만 제공한다. 진행 상태나 실제 DB 실패의 대체 경로를 보관하지 않는다.</summary>
    internal sealed class TemporaryChapterContinuationSource : IChapterContinuationSource
    {
        public ChapterContinuationDefinition Read(int chapterKey) => chapterKey switch {
            TemporaryCoreRuntimeFactory.ChapterKey => ChapterContinuationDefinition.Next(
                TemporaryCoreRuntimeFactory.ChapterKey, TemporaryCoreRuntimeFactory.SecondChapterKey),
            TemporaryCoreRuntimeFactory.SecondChapterKey => ChapterContinuationDefinition.Final(
                TemporaryCoreRuntimeFactory.SecondChapterKey),
            _ => throw new KeyNotFoundException("임시 챕터 연결 정의가 없습니다.")
        };
    }
}
#endif
