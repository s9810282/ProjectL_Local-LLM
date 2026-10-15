using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public static class SplashDamageCalculator
    {
        /// <summary>동일한 원본 피해에서 각 추가 대상의 피해를 독립적으로 절삭 계산한다.</summary>
        public static int CalculateAdditional(int originalDamage, int splashPercent)
        {
            if (originalDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(originalDamage));
            if (splashPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(splashPercent));
            return checked((int)(((long)originalDamage * splashPercent) / 100L));
        }
    }
}
