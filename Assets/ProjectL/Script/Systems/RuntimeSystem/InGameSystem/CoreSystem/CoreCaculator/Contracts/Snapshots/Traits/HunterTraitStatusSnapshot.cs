using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>잠긴 대상과 이번 스테이지 보상 횟수입니다. 누적 공격력은 보유 기록만 소유합니다.</summary>
    public sealed class HunterTraitStatusSnapshot : TraitStatusSnapshot
    {
        public EntityId? TargetId { get; }
        public int SuccessfulRewards { get; }
        public HunterTraitStatusSnapshot() : this(null, 0) { }
        public HunterTraitStatusSnapshot(EntityId? targetId, int successfulRewards) : base(TraitKey.Hunter)
        {
            if (targetId.HasValue && targetId.Value.IsEmpty) throw new ArgumentException("대상 식별자는 비어 있을 수 없습니다.", nameof(targetId));
            if (successfulRewards < 0) throw new ArgumentOutOfRangeException(nameof(successfulRewards));
            TargetId = targetId;
            SuccessfulRewards = successfulRewards;
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is HunterTraitStatusSnapshot state &&
            TargetId == state.TargetId && SuccessfulRewards == state.SuccessfulRewards;
    }
}
