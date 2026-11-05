using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    /// <summary>
    /// 덱 평가·확정 결과가 돌려주는 규칙 판정용 원본 상태다. 표시용 보유 목록·정의·능력치는 담지 않는다.
    /// 표시 사실은 덱 조회(<see cref="DeckReadResult"/>)가 같은 원본에서 따로 제공한다.
    /// </summary>
    public sealed class DeckRuleState
    {
        public DeckReadStatus Status { get; }
        /// <summary>Ready일 때 포착한 로스터 버전이다. 그 밖에는 0이다.</summary>
        public long RosterRevision { get; }
        /// <summary>Ready일 때의 확정 덱이다. 그 밖에는 null이다.</summary>
        public RunDeckSnapshot Deck { get; }
        /// <summary>Ready일 때의 편집 가능 여부다. 그 밖에는 null이다.</summary>
        public DeckEditAvailability Availability { get; }

        private DeckRuleState(DeckReadStatus status, long rosterRevision, RunDeckSnapshot deck, DeckEditAvailability availability)
        {
            Status = status;
            RosterRevision = rosterRevision;
            Deck = deck;
            Availability = availability;
        }

        public static DeckRuleState Ready(long rosterRevision, RunDeckSnapshot deck, DeckEditAvailability availability)
        {
            if (rosterRevision < 0) throw new ArgumentOutOfRangeException(nameof(rosterRevision));
            return new DeckRuleState(DeckReadStatus.Ready, rosterRevision,
                deck ?? throw new ArgumentNullException(nameof(deck)),
                availability ?? throw new ArgumentNullException(nameof(availability)));
        }

        /// <summary>준비 전이거나 원본이 읽는 중에 바뀐 상태다. 덱과 버전을 만들지 않는다.</summary>
        public static DeckRuleState Unavailable(DeckReadStatus status)
        {
            if (status == DeckReadStatus.Ready || !Enum.IsDefined(typeof(DeckReadStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            return new DeckRuleState(status, 0, null, null);
        }
    }
}
