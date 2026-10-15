using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 상성 배율을 피해에 적용하는 순수 계산입니다.
    /// [사용자 확정] 비음수 피해에 half-up 반올림을 사용하며 banker's rounding을 쓰지 않습니다.
    /// </summary>
    public static class CategoryMatchupDamageCalculator
    {
        /// <summary>
        /// damage * (numerator / denominator)를 정수 안전하게 계산하고 half-up으로 반올림합니다.
        /// 0 피해는 0으로 유지되며 최소 피해 1을 여기서 추가하지 않습니다.
        /// </summary>
        public static int Apply(int damage, int numerator, int denominator)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (numerator < 0)
                throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(denominator));

            // 분모의 절반을 더한 뒤 내림하면 비음수 구간에서 half-up이 된다.
            long scaled = ((long)damage * numerator) + (denominator / 2);
            long result = scaled / denominator;

            if (result > int.MaxValue)
                throw new OverflowException("상성 적용 결과가 int 범위를 초과했습니다.");

            return (int)result;
        }
    }
}
