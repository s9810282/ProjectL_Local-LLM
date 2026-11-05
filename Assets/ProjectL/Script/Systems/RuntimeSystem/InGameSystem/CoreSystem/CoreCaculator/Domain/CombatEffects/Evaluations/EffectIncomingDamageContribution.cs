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
        public StatusEffectKey? EffectKey => FlatReduction > 0 || RemainingDamagePercent < 100 ? StatusEffectKey.IncomingDamageDecrease : (StatusEffectKey?)null;
        public int FlatReduction { get; }
        public int RemainingDamagePercent { get; }

        public EffectIncomingDamageContribution(EffectOriginKey origin, int flatReduction, int remainingDamagePercent = 100)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (flatReduction < 0) throw new ArgumentOutOfRangeException(nameof(flatReduction));

            if (remainingDamagePercent < 0 || remainingDamagePercent > 100) throw new ArgumentOutOfRangeException(nameof(remainingDamagePercent));
            RemainingDamagePercent = remainingDamagePercent;
            Origin = origin;
            FlatReduction = flatReduction;
        }
    }
}
