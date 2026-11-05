using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    /// <summary>
    /// 후보의 평가 버전은 Request에 남고, Current는 평가에 쓴 규칙 원본을 설명한다. 평가는 쓰지 않으므로
    /// 수락이면 Current가 요청의 기대 버전과 같다. 거절이면 Current는 새 원본을 따로 설명한다.
    /// </summary>
    public sealed class DeckEvaluationResult
    {
        public DeckEditRequest Request { get; }
        public bool Accepted => Reason == DeckFailureReason.None;
        public DeckFailureReason Reason { get; }
        public DeckRuleState Current { get; }
        public IReadOnlyList<CharacterInstanceId> CanonicalIds { get; }

        public DeckEvaluationResult(DeckEditRequest request, DeckFailureReason reason, DeckRuleState current,
            IReadOnlyList<CharacterInstanceId> canonicalIds = null)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            var decision = new DeckSelectionDecision(reason, canonicalIds);
            if (decision.Accepted)
            {
                if (current.Status != DeckReadStatus.Ready || current.Deck.RunId != request.ExpectedRunId ||
                    current.RosterRevision != request.ExpectedRosterRevision || current.Deck.DeckRevision != request.ExpectedDeckRevision)
                    throw new ArgumentException("평가 성공의 원본 버전이 요청과 다릅니다.", nameof(current));
                var candidateSet = new HashSet<CharacterInstanceId>(request.CandidateIds);
                if (candidateSet.Count != request.CandidateIds.Count || candidateSet.Count != decision.CanonicalIds.Count)
                    throw new ArgumentException("정규화 결과는 요청과 같은 후보여야 합니다.", nameof(canonicalIds));
                foreach (var id in decision.CanonicalIds)
                    if (!candidateSet.Remove(id)) throw new ArgumentException("정규화 결과의 후보가 다릅니다.", nameof(canonicalIds));
            }
            Reason = reason;
            CanonicalIds = decision.CanonicalIds;
        }
    }
}
