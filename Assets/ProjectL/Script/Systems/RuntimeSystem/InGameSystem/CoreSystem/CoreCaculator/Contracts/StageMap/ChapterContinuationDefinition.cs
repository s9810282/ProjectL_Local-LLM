using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    public enum ChapterContinuationDefinitionKind { NextChapter, FinalChapter }

    public sealed class ChapterContinuationDefinition
    {
        public int ChapterKey { get; }
        public ChapterContinuationDefinitionKind Kind { get; }
        public int NextChapterKey { get; }

        public ChapterContinuationDefinition(int chapterKey, ChapterContinuationDefinitionKind kind, int nextChapterKey)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (!Enum.IsDefined(typeof(ChapterContinuationDefinitionKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == ChapterContinuationDefinitionKind.NextChapter
                ? nextChapterKey < 1 || nextChapterKey == chapterKey : nextChapterKey != 0)
                throw new ArgumentOutOfRangeException(nameof(nextChapterKey));
            ChapterKey = chapterKey;
            Kind = kind;
            NextChapterKey = nextChapterKey;
        }

        public static ChapterContinuationDefinition Next(int chapterKey, int nextChapterKey)
            => new(chapterKey, ChapterContinuationDefinitionKind.NextChapter, nextChapterKey);
        public static ChapterContinuationDefinition Final(int chapterKey)
            => new(chapterKey, ChapterContinuationDefinitionKind.FinalChapter, 0);
    }
}
