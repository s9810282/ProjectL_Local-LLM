using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class HunterEffectStateSnapshot : IEffectStateSnapshot
    {
        public EntityId? TargetId { get; }
        public int SuccessfulRewards { get; }
        internal HunterEffectStateSnapshot(EntityId? targetId, int successfulRewards)
        {
            if (targetId.HasValue && targetId.Value.IsEmpty) throw new ArgumentException("대상 식별자는 비어 있을 수 없습니다.", nameof(targetId));
            if (successfulRewards < 0) throw new ArgumentOutOfRangeException(nameof(successfulRewards));
            TargetId = targetId;
            SuccessfulRewards = successfulRewards;
        }
        public bool ValueEquals(IEffectStateSnapshot other) => other is HunterEffectStateSnapshot state &&
            TargetId == state.TargetId && SuccessfulRewards == state.SuccessfulRewards;
    }
}
