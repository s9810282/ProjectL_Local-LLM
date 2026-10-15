using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectTargetDistanceDamageContribution
    {
        public EffectOriginKey Origin { get; }

        /// <summary>
        /// 이 효과가 요청하는 대상 피해 백분율입니다. 100이면 변화가 없습니다.
        /// </summary>
        public int Percent { get; }

        public EffectTargetDistanceDamageContribution(EffectOriginKey origin, int percent)
        {
            if (origin.IsValid == false)
                throw new ArgumentException(nameof(origin));
            if (percent < 0)
                throw new ArgumentOutOfRangeException(nameof(percent));

            Origin = origin;
            Percent = percent;
        }
    }
}
