using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>획득 가능 여부와 발급할 카드의 정의를 담는다. 보유 ID는 Buffer가 발급한다.</summary>
    public sealed class CharacterAcquisitionDecision
    {
        public bool Succeeded => FailureReason == RosterAcquireFailureReason.None;
        public RosterAcquireFailureReason FailureReason { get; }
        public IReadOnlyList<RosterAcquireRequest> Requests { get; }
        public MainCharacterPersistentState InitialMain { get; }

        private CharacterAcquisitionDecision(RosterAcquireFailureReason reason,
            RosterAcquireRequest[] requests, MainCharacterPersistentState initialMain)
        {
            FailureReason = reason;
            Requests = Array.AsReadOnly(requests);
            InitialMain = initialMain;
        }

        public static CharacterAcquisitionDecision Success(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain = null)
        {
            if (requests == null || requests.Count == 0)
                throw new ArgumentException("획득할 카드가 필요합니다.", nameof(requests));

            var copy = new RosterAcquireRequest[requests.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = requests[i] ?? throw new ArgumentException("획득 요청에 null을 넣을 수 없습니다.");

            return new CharacterAcquisitionDecision(RosterAcquireFailureReason.None, copy, initialMain);
        }

        public static CharacterAcquisitionDecision Failure(RosterAcquireFailureReason reason)
        {
            if (reason == RosterAcquireFailureReason.None ||
                Enum.IsDefined(typeof(RosterAcquireFailureReason), reason) == false)
                throw new ArgumentOutOfRangeException(nameof(reason));

            return new CharacterAcquisitionDecision(reason, Array.Empty<RosterAcquireRequest>(), null);
        }
    }
}
