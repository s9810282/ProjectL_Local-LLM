using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    /// <summary>
    /// 대상 하나의 피해 배율을 정할 때 넘기는 공격자-대상 거리입니다.
    /// </summary>
    internal readonly struct TargetDistanceDamageEffectContext
    {
        /// <summary>
        /// 공격자 Anchor와 대상 칸 사이의 체비셰프 거리입니다. 1 이상입니다.
        /// </summary>
        public int Distance { get; }

        public TargetDistanceDamageEffectContext(int distance)
        {
            if (distance < 1)
                throw new ArgumentOutOfRangeException(nameof(distance));

            Distance = distance;
        }
    }
}
