using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class RosterAcquireResult
    {
        public bool Succeeded => FailureReason == RosterAcquireFailureReason.None;
        public RosterAcquireFailureReason FailureReason { get; }
        public IReadOnlyList<OwnedCharacterRecord> Acquired { get; }

        private RosterAcquireResult(RosterAcquireFailureReason failureReason, OwnedCharacterRecord[] acquired)
        {
            FailureReason = failureReason;
            Acquired = Array.AsReadOnly(acquired);
        }

        public static RosterAcquireResult Success(IReadOnlyList<OwnedCharacterRecord> acquired)
        {
            if (acquired == null)
                throw new ArgumentNullException(nameof(acquired));
            var copy = new OwnedCharacterRecord[acquired.Count];
            var ids = new HashSet<CharacterInstanceId>();
            bool hasMain = false;
            for (int i = 0; i < copy.Length; i++)
            {
                OwnedCharacterRecord record = acquired[i];
                if (record == null)
                    throw new ArgumentException("획득 결과에 null을 넣을 수 없습니다.", nameof(acquired));
                if (!ids.Add(record.Id))
                    throw new ArgumentException("획득 결과의 보유 ID는 중복될 수 없습니다.", nameof(acquired));
                if (record.Role == CharacterOwnershipRole.Main)
                {
                    if (hasMain)
                        throw new ArgumentException("메인은 한 장만 획득할 수 있습니다.", nameof(acquired));
                    hasMain = true;
                }
                copy[i] = record;
            }
            return new RosterAcquireResult(RosterAcquireFailureReason.None, copy);
        }

        public static RosterAcquireResult Failure(RosterAcquireFailureReason reason)
        {
            if (reason == RosterAcquireFailureReason.None || !Enum.IsDefined(typeof(RosterAcquireFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new RosterAcquireResult(reason, Array.Empty<OwnedCharacterRecord>());
        }
    }
}
