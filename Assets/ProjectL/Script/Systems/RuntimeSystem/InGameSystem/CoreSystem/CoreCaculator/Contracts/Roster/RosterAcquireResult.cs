using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class RosterAcquireResult
    {
        public bool Succeeded => FailureReason == RosterAcquireFailureReason.None;
        public RosterAcquireFailureReason FailureReason { get; }
        /// <summary><see cref="RosterAcquireFailureReason.InvalidInitialDeck"/>일 때만 덱 규칙의 구체 사유다. 그 밖에는 None이다.</summary>
        public DeckFailureReason InitialDeckFailure { get; }
        public IReadOnlyList<OwnedCharacterRecord> Acquired { get; }

        private RosterAcquireResult(RosterAcquireFailureReason failureReason, DeckFailureReason initialDeckFailure,
            OwnedCharacterRecord[] acquired)
        {
            FailureReason = failureReason;
            InitialDeckFailure = initialDeckFailure;
            Acquired = Array.AsReadOnly(acquired);
        }

        public static RosterAcquireResult Success(IReadOnlyList<OwnedCharacterRecord> acquired)
        {
            if (acquired == null)
                throw new ArgumentNullException(nameof(acquired));
            var copy = new OwnedCharacterRecord[acquired.Count];
            var ids = new HashSet<CharacterInstanceId>();
            for (int i = 0; i < copy.Length; i++)
            {
                OwnedCharacterRecord record = acquired[i];
                if (record == null)
                    throw new ArgumentException("획득 결과에 null을 넣을 수 없습니다.", nameof(acquired));
                if (!ids.Add(record.Id))
                    throw new ArgumentException("획득 결과의 보유 ID는 중복될 수 없습니다.", nameof(acquired));
                copy[i] = record;
            }
            return new RosterAcquireResult(RosterAcquireFailureReason.None, DeckFailureReason.None, copy);
        }

        /// <summary>초기 덱 사유가 있는 실패는 <see cref="InvalidInitialDeck"/>로 만든다.</summary>
        public static RosterAcquireResult Failure(RosterAcquireFailureReason reason)
        {
            if (reason == RosterAcquireFailureReason.None || reason == RosterAcquireFailureReason.InvalidInitialDeck ||
                !Enum.IsDefined(typeof(RosterAcquireFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new RosterAcquireResult(reason, DeckFailureReason.None, Array.Empty<OwnedCharacterRecord>());
        }

        /// <summary>시작 로드아웃의 초기 덱 선택이 덱 규칙에서 거절된 결과다. 덱 규칙 사유를 그대로 보존한다.</summary>
        public static RosterAcquireResult InvalidInitialDeck(DeckFailureReason reason)
        {
            if (reason == DeckFailureReason.None || !Enum.IsDefined(typeof(DeckFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new RosterAcquireResult(RosterAcquireFailureReason.InvalidInitialDeck, reason,
                Array.Empty<OwnedCharacterRecord>());
        }
    }
}
