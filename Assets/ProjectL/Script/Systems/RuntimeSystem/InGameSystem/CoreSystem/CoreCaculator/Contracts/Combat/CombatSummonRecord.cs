using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 전투 중 소환으로 만든 개체의 불변 출처다. CombatTurnFacts에 EnemyOrigins와 나란히 두고 전투 동안 추가만 한다.
    /// 생존 수는 이 기록과 캐릭터의 생존·영역에서 계산하며 따로 저장하지 않는다.
    /// </summary>
    public readonly struct CombatSummonRecord : IEquatable<CombatSummonRecord>
    {
        public EntityId SummonedId { get; }
        public EntityId SummonerId { get; }
        public TraitKey Ability { get; }

        public CombatSummonRecord(EntityId summonedId, EntityId summonerId, TraitKey ability)
        {
            if (summonedId.IsEmpty || summonerId.IsEmpty || summonedId == summonerId)
                throw new ArgumentException("소환 기록의 개체 ID가 잘못되었습니다.");
            if (ability == TraitKey.None || Enum.IsDefined(typeof(TraitKey), ability) == false)
                throw new ArgumentOutOfRangeException(nameof(ability));
            SummonedId = summonedId;
            SummonerId = summonerId;
            Ability = ability;
        }

        public bool Equals(CombatSummonRecord other) =>
            SummonedId == other.SummonedId && SummonerId == other.SummonerId && Ability == other.Ability;

        public override bool Equals(object obj) => obj is CombatSummonRecord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(SummonedId, SummonerId, Ability);
    }
}
