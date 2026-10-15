using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 한 규칙이 기여한 피격 경감량입니다. 평가 자체는 상태를 바꾸지 않습니다.
    /// </summary>
    internal readonly struct EffectIncomingDamageContribution
    {
        public EffectOriginKey Origin { get; }
        public int FlatReduction { get; }

        public EffectIncomingDamageContribution(EffectOriginKey origin, int flatReduction)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (flatReduction < 0) throw new ArgumentOutOfRangeException(nameof(flatReduction));

            Origin = origin;
            FlatReduction = flatReduction;
        }
    }
}
