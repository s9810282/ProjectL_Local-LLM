using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class HunterRuntimeState : TraitRuntimeState
    {
        public EntityId? TargetId { get; private set; }
        public int SuccessfulRewards { get; private set; }
        public HunterRuntimeState() : this(null, 0) { }
        public HunterRuntimeState(EntityId? targetId, int successfulRewards) : base(TraitKey.Hunter)
        {
            if (targetId.HasValue && targetId.Value.IsEmpty) throw new ArgumentException("대상 식별자는 비어 있을 수 없습니다.", nameof(targetId));
            if (successfulRewards < 0) throw new ArgumentOutOfRangeException(nameof(successfulRewards));
            TargetId = targetId;
            SuccessfulRewards = successfulRewards;
        }
        internal override TraitRuntimeState Clone() => new HunterRuntimeState(TargetId, SuccessfulRewards);
        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not HunterRuntimeState state) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
            TargetId = state.TargetId;
            SuccessfulRewards = state.SuccessfulRewards;
        }
    }
}
