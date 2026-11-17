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
        /// <summary>쓸기 전에 대상이 있던 거리입니다. 다른 공격에는 값이 없습니다.</summary>
        public int? PreSweepDistance { get; }

        public TargetDistanceDamageEffectContext(int distance, int? preSweepDistance = null)
        {
            if (distance < 1)
                throw new ArgumentOutOfRangeException(nameof(distance));

            if (preSweepDistance is < 1)
                throw new ArgumentOutOfRangeException(nameof(preSweepDistance));

            Distance = distance;
            PreSweepDistance = preSweepDistance;
        }
    }
}
