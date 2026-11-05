using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem
{
    /// <summary>
    /// 챕터(=맵) 데이터의 다음 챕터 키로 챕터 연결을 읽는 기본 원본이다(Run 요약 설계 §9 CC2).
    /// 0이면 마지막 챕터, 1 이상이면 그 챕터로 이어진다. 등록되지 않은 챕터는 정의가 없으며(null) 최종으로 보지 않는다.
    /// </summary>
    internal sealed class StageMapChapterContinuationSource : IChapterContinuationSource
    {
        private readonly StageServiceManager _stages;

        internal StageMapChapterContinuationSource(StageServiceManager stages)
        {
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
        }

        public ChapterContinuationDefinition Read(int chapterKey)
        {
            StageMapData map;
            try { map = _stages.GetStageMap(chapterKey); }
            catch (KeyNotFoundException) { return null; }
            if (map.MapKey != chapterKey)
                throw new InvalidOperationException($"챕터 {chapterKey}의 맵 데이터 키가 다릅니다: {map.MapKey}.");
            return map.NextChapterKey == 0
                ? ChapterContinuationDefinition.Final(chapterKey)
                : ChapterContinuationDefinition.Next(chapterKey, map.NextChapterKey);
        }
    }
}
