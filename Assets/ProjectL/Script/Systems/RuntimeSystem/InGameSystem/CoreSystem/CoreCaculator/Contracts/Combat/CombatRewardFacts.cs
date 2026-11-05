using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>최초 전투 참여자와 이번 전투의 처치 골드 획득량입니다. 원본은 전투 상태 한 곳에 보관합니다.</summary>
    public sealed class CombatRewardFacts
    {
        public static CombatRewardFacts Empty { get; } = new CombatRewardFacts(false, Array.Empty<CharacterInstanceId>(), 0);
        public bool ParticipantsCaptured { get; }
        public IReadOnlyList<CharacterInstanceId> ClearParticipantIds { get; }
        public int KillGoldEarned { get; }

        public CombatRewardFacts(bool participantsCaptured, IReadOnlyList<CharacterInstanceId> clearParticipantIds, int killGoldEarned)
        {
            if (clearParticipantIds == null) throw new ArgumentNullException(nameof(clearParticipantIds));
            if (killGoldEarned < 0) throw new ArgumentOutOfRangeException(nameof(killGoldEarned));
            var copy = clearParticipantIds.ToArray();
            if ((!participantsCaptured && copy.Length != 0) || copy.Any(id => id.IsEmpty) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("참여자 ID는 확정 뒤에만 기록하며 빈 값이나 중복을 허용하지 않습니다.", nameof(clearParticipantIds));
            ParticipantsCaptured = participantsCaptured;
            ClearParticipantIds = Array.AsReadOnly(copy);
            KillGoldEarned = killGoldEarned;
        }

        public bool SameValues(CombatRewardFacts other) => other != null &&
            ParticipantsCaptured == other.ParticipantsCaptured && KillGoldEarned == other.KillGoldEarned &&
            ClearParticipantIds.SequenceEqual(other.ClearParticipantIds);
    }
}
