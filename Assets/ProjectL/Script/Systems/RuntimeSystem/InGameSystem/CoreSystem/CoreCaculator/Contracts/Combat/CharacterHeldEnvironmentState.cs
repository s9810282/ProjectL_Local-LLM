using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>캐릭터가 보유한 환경 효과다. 위치·영혼·지속 능력치와 수명을 분리한다.</summary>
    public sealed class CharacterHeldEnvironmentState
    {
        public static CharacterHeldEnvironmentState Empty { get; } = new();
        public long? BadgeSourceId { get; }
        public bool BadgeAvailable { get; }
        public int SuppliesAttackBonus { get; }
        public HeldFlag Flag { get; }

        public CharacterHeldEnvironmentState(long? badgeSourceId = null, bool badgeAvailable = false,
            int suppliesAttackBonus = 0, HeldFlag flag = null)
        {
            if (badgeSourceId <= 0 || badgeAvailable && !badgeSourceId.HasValue)
                throw new ArgumentOutOfRangeException(nameof(badgeSourceId));
            if (suppliesAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(suppliesAttackBonus));
            BadgeSourceId = badgeSourceId;
            BadgeAvailable = badgeAvailable;
            SuppliesAttackBonus = suppliesAttackBonus;
            Flag = flag;
        }

        public bool SameValues(CharacterHeldEnvironmentState other) => other != null &&
            BadgeSourceId == other.BadgeSourceId && BadgeAvailable == other.BadgeAvailable &&
            SuppliesAttackBonus == other.SuppliesAttackBonus &&
            (Flag == null ? other.Flag == null : Flag.SameValues(other.Flag));
    }

    /// <summary>보유 중인 깃발의 기여분과 드롭 시 복원할 공개 범위다. 필드 위치·순서·활성 여부는 담지 않는다.</summary>
    public sealed class HeldFlag
    {
        public int PriorityBonus { get; }
        public IReadOnlyList<Owner> KnownTo { get; }

        public HeldFlag(int priorityBonus, IReadOnlyList<Owner> knownTo)
        {
            if (priorityBonus < 0) throw new ArgumentOutOfRangeException(nameof(priorityBonus));
            if (knownTo == null) throw new ArgumentNullException(nameof(knownTo));
            PriorityBonus = priorityBonus;
            KnownTo = Array.AsReadOnly(knownTo.OrderBy(owner => owner).ToArray());
        }

        public static HeldFlag From(FlagSourceFact source) =>
            new((source ?? throw new ArgumentNullException(nameof(source))).PriorityBonus, source.KnownTo);

        public bool SameValues(HeldFlag other) => other != null &&
            PriorityBonus == other.PriorityBonus && KnownTo.SequenceEqual(other.KnownTo);
    }
}
