using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>보유 카드의 영혼 개수에 대한 변경 제안이며 저장 상태를 직접 바꾸지 않는다.</summary>
    public sealed class SoulCountChange
    {
        public CharacterInstanceId Id { get; }
        public int ExpectedCount { get; }
        public int FinalCount { get; }

        public SoulCountChange(CharacterInstanceId id, int expectedCount, int finalCount)
        {
            if (id.IsEmpty)
                throw new ArgumentException("보유 ID는 비어 있을 수 없습니다.", nameof(id));
            if (expectedCount < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedCount));
            if (finalCount < expectedCount)
                throw new ArgumentOutOfRangeException(nameof(finalCount));
            Id = id;
            ExpectedCount = expectedCount;
            FinalCount = finalCount;
        }
    }
}
