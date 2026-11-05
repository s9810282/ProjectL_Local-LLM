using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>피격 대상이 소유하는 표식 한 칸입니다. 출처가 사라져도 저장된 진영과 증가율을 사용합니다.</summary>
    public sealed class HuntersMarkState
    {
        public EntityId SourceId { get; }
        public Owner SourceOwner { get; }
        public int BonusDamagePercent { get; }

        public HuntersMarkState(EntityId sourceId, Owner sourceOwner, int bonusDamagePercent)
        {
            if (sourceId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(sourceId));
            if (sourceOwner != Owner.Player && sourceOwner != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(sourceOwner));
            if (bonusDamagePercent < 0) throw new ArgumentOutOfRangeException(nameof(bonusDamagePercent));
            SourceId = sourceId;
            SourceOwner = sourceOwner;
            BonusDamagePercent = bonusDamagePercent;
        }

        public bool SameValues(HuntersMarkState other) => other != null &&
            SourceId == other.SourceId && SourceOwner == other.SourceOwner && BonusDamagePercent == other.BonusDamagePercent;
    }
}
