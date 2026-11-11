using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>사망 특성 하나의 기여입니다. 자폭 비율이 0이면 자폭하지 않습니다.</summary>
    internal readonly struct EffectDeathTraitContribution
    {
        public EffectOriginKey Origin { get; }
        public int SelfDestructPercent { get; }
        public bool Fear { get; }

        public EffectDeathTraitContribution(EffectOriginKey origin, int selfDestructPercent, bool fear)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (selfDestructPercent < 0) throw new ArgumentOutOfRangeException(nameof(selfDestructPercent));
            if (selfDestructPercent == 0 && !fear) throw new ArgumentException("사망 특성 기여에는 효과가 하나 이상 필요합니다.");

            Origin = origin;
            SelfDestructPercent = selfDestructPercent;
            Fear = fear;
        }
    }
}
