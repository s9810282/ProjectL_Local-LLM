using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    /// <summary>
    /// 확정 결과다. 요청의 기대 버전은 게시 전 원본 검증에만 쓰인다. 수락이면 Current는 실제로 설치된 게시 후 덱이다.
    /// 바뀐 덱은 새 인스턴스와 더 큰 덱 버전을, 같은 덱(no-op)은 기존 인스턴스와 같은 버전을 돌려준다.
    /// 수락 뒤 표시 조회가 실패해도 확정은 이미 반영된 것이며 다시 확정할 이유가 아니다.
    /// </summary>
    public sealed class DeckEditResult
    {
        public DeckEditRequest Request { get; }
        public bool Accepted => Reason == DeckFailureReason.None;
        public DeckFailureReason Reason { get; }
        public DeckRuleState Current { get; }

        public DeckEditResult(DeckEditRequest request, DeckFailureReason reason, DeckRuleState current)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            if (!Enum.IsDefined(typeof(DeckFailureReason), reason)) throw new ArgumentOutOfRangeException(nameof(reason));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            if (reason == DeckFailureReason.None)
            {
                if (current.Status != DeckReadStatus.Ready)
                    throw new ArgumentException("성공한 확정 결과에는 게시 후 덱이 필요합니다.", nameof(current));
                // 덱 확정은 로스터를 바꾸지 않고, 덱 버전은 줄지 않는다. 전체 Current가 요청 버전과 같을 필요는 없다.
                var deck = current.Deck;
                if (deck.RunId != request.ExpectedRunId || current.RosterRevision != request.ExpectedRosterRevision ||
                    deck.DeckRevision < request.ExpectedDeckRevision)
                    throw new ArgumentException("확정 결과의 게시 후 원본이 요청의 원본에서 이어지지 않습니다.", nameof(current));
                var candidates = new HashSet<CharacterInstanceId>(request.CandidateIds);
                if (candidates.Count != deck.OrderedIds.Count || !candidates.SetEquals(deck.OrderedIds))
                    throw new ArgumentException("설치된 덱은 요청 후보와 같은 ID여야 합니다.", nameof(current));
            }
            Reason = reason;
        }
    }
}
