using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>챕터 진입 제안이다. Core가 판정할 때 읽은 위치·챕터 상태를 기대값으로 담고,
    /// 새로 만든 그래프가 있으면 맵 선택과 함께 한 번에 설치한다.</summary>
    public sealed class ChapterEntryPreparation
    {
        public int ExpectedCurrentChapterKey { get; }
        public bool ExpectedCurrentChapterClear { get; }
        public int TargetChapterKey { get; }
        public bool ExpectedTargetChapterClear { get; }
        /// <summary>Core가 읽은 대상 챕터의 등록 그래프다. 새로 만든 경우 비어 있다.</summary>
        public IReadOnlyList<StageProgressData> ExpectedTargetStages { get; }
        /// <summary>등록할 새 그래프다. 이미 등록된 그래프가 있으면 비어 있다.</summary>
        public IReadOnlyList<StageProgressData> GeneratedTargetStages { get; }

        public ChapterEntryPreparation(int expectedCurrentChapterKey, bool expectedCurrentChapterClear,
            int targetChapterKey, bool expectedTargetChapterClear,
            IReadOnlyList<StageProgressData> expectedTargetStages, IReadOnlyList<StageProgressData> generatedTargetStages)
        {
            if (expectedCurrentChapterKey < 0) throw new ArgumentOutOfRangeException(nameof(expectedCurrentChapterKey));
            if (targetChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(targetChapterKey));
            ExpectedCurrentChapterKey = expectedCurrentChapterKey;
            ExpectedCurrentChapterClear = expectedCurrentChapterClear;
            TargetChapterKey = targetChapterKey;
            ExpectedTargetChapterClear = expectedTargetChapterClear;
            ExpectedTargetStages = Array.AsReadOnly((expectedTargetStages ?? throw new ArgumentNullException(nameof(expectedTargetStages))).ToArray());
            GeneratedTargetStages = Array.AsReadOnly((generatedTargetStages ?? throw new ArgumentNullException(nameof(generatedTargetStages))).ToArray());
            if ((ExpectedTargetStages.Count == 0) == (GeneratedTargetStages.Count == 0))
                throw new ArgumentException("대상 챕터는 기존 그래프 또는 새 그래프 중 하나만 가져야 합니다.");
        }
    }
}
