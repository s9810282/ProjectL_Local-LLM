using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    /// <summary>평가한 원본 버전과 후보 ID를 보존한다. 빈 후보도 규칙의 명시적 거절 대상이다.</summary>
    public sealed class DeckEditRequest
    {
        public RunId ExpectedRunId { get; }
        public long ExpectedRosterRevision { get; }
        public long ExpectedDeckRevision { get; }
        public IReadOnlyList<CharacterInstanceId> CandidateIds { get; }

        public DeckEditRequest(RunId expectedRunId, long expectedRosterRevision, long expectedDeckRevision,
            IReadOnlyList<CharacterInstanceId> candidateIds)
        {
            if (expectedRunId.IsEmpty) throw new ArgumentException("Run 식별자가 필요합니다.", nameof(expectedRunId));
            if (expectedRosterRevision < 0) throw new ArgumentOutOfRangeException(nameof(expectedRosterRevision));
            if (expectedDeckRevision < 0) throw new ArgumentOutOfRangeException(nameof(expectedDeckRevision));
            if (candidateIds == null) throw new ArgumentNullException(nameof(candidateIds));
            var copy = new CharacterInstanceId[candidateIds.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = candidateIds[i];
            ExpectedRunId = expectedRunId;
            ExpectedRosterRevision = expectedRosterRevision;
            ExpectedDeckRevision = expectedDeckRevision;
            CandidateIds = Array.AsReadOnly(copy);
        }
    }
}
