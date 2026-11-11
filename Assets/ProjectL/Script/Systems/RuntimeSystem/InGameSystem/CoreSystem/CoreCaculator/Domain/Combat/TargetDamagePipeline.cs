using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 대상별 스플래시·상성·방어력 단계를 계산합니다.
    /// 기존 입력은 공격자 가산 및 해당 관통 감쇠 이후의 값이다. 일반 스플래시는 공통 피해에서 먼저 절삭한다.
    /// 상성 조회는 CategoryMatchupResolver가, 경감 기여는 CombatEffectEngine이 담당하며
    /// 피격 경감과 최종 최소 피해는 호출자가 이어서 적용합니다.
    /// </summary>
    public sealed class TargetDamagePipeline
    {
        /// <summary>기존 피격 경감 뒤에 비율을 차례로 곱하고 마지막에만 소수점을 버린다.</summary>
        internal static TargetDamageBreakdown ApplyReceivedPercentages(TargetDamageBreakdown damage, params int[] remainingPercents)
        {
            if (remainingPercents == null) throw new ArgumentNullException(nameof(remainingPercents));
            decimal exact = damage.DamageAfterReceivedPercentages;
            foreach (int percent in remainingPercents)
            {
                if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(remainingPercents));
                exact = exact * percent / 100m;
            }
            return new TargetDamageBreakdown(damage.DamageBeforeMatchup, damage.MatchupNumerator,
                damage.MatchupDenominator, damage.DamageAfterMatchup, damage.DamageAfterPhysicalDefense,
                damage.IncomingReduction, (int)decimal.Truncate(exact), exact);
        }

        private readonly CategoryMatchupResolver _matchupResolver;

        public TargetDamagePipeline(CategoryMatchupResolver matchupResolver)
        {
            _matchupResolver = matchupResolver
                ?? throw new ArgumentNullException(nameof(matchupResolver));
        }

        /// <summary>공격자 가산과 효과 반영 후 확정된 공통 피해에 역할별 비율을 적용하고 기존 대상별 단계를 계산한다.</summary>
        public TargetDamageBreakdown Calculate(int originalDamage, AttackTargetRole role, int splashPercent,
            CategoryKey attackerCategory, CategoryKey? defenderCategory, int physicalDefense = 0)
        {
            if (originalDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(originalDamage));
            if (splashPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(splashPercent));
            if (role != AttackTargetRole.Primary && role != AttackTargetRole.Additional &&
                role != AttackTargetRole.Designated && role != AttackTargetRole.Effect)
                throw new ArgumentOutOfRangeException(nameof(role));

            int scaledDamage = role == AttackTargetRole.Additional
                ? SplashDamageCalculator.CalculateAdditional(originalDamage, splashPercent)
                : originalDamage;
            return Calculate(scaledDamage, attackerCategory, defenderCategory, physicalDefense);
        }

        /// <summary>
        /// 대상별 피해를 단계 순서대로 계산합니다.
        /// </summary>
        /// <param name="damageBeforeMatchup">상성 직전의 피해다. 일반 스플래시 비율이나 기존 관통 감쇠가 이미 반영된 값이다.</param>
        /// <param name="attackerCategory">공격자 Category입니다.</param>
        /// <param name="defenderCategory">
        /// 피격자 Category입니다. Category가 없는 대상(구조물 등)은 null이며 상성을 적용하지 않습니다.
        /// 가짜 Base로 대체하지 않습니다.
        /// </param>
        /// <param name="physicalDefense">피격자의 실효 방어력입니다. 1당 피해를 1% 줄입니다.</param>
        public TargetDamageBreakdown Calculate(
            int damageBeforeMatchup,
            CategoryKey attackerCategory,
            CategoryKey? defenderCategory,
            int physicalDefense = 0)
        {
            if (damageBeforeMatchup < 0)
                throw new ArgumentOutOfRangeException(nameof(damageBeforeMatchup));
            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));

            // 이 시점에는 일반 스플래시 비율 또는 기존 관통 감쇠가 이미 반영되어 있다.
            int numerator;
            int denominator;
            int damageAfterMatchup;

            if (defenderCategory.HasValue)
            {
                // 2단계: 대상별 Category 상성. half-up 반올림은 계산기가 담당한다.
                CategoryMatchupMultiplier multiplier = _matchupResolver.GetMultiplier(attackerCategory, defenderCategory.Value);

                numerator = multiplier.Numerator;

                denominator = multiplier.Denominator;

                damageAfterMatchup = CategoryMatchupDamageCalculator.Apply(damageBeforeMatchup, numerator, denominator);
            }
            else
            {
                // Category가 없는 대상은 상성을 적용하지 않고 통과시킨다.
                numerator = 1;
                denominator = 1;
                damageAfterMatchup = damageBeforeMatchup;
            }

            // 3단계: 방어력. Heavy가 실제 방어 후 피해를 받도록 여기서는 최소값을 보정하지 않는다.
            int damageAfterPhysicalDefense = ApplyPhysicalDefense(damageAfterMatchup, physicalDefense);

            return new TargetDamageBreakdown(
                damageBeforeMatchup,
                numerator,
                denominator,
                damageAfterMatchup,
                damageAfterPhysicalDefense,
                0,
                damageAfterPhysicalDefense);
        }

        /// <summary>
        /// 방어력 1당 피해 1% 감소의 유일한 식이다. 100 이상은 100%로 제한하고 소수점을 버린다(곱셈은 long).
        /// 최소값 보정은 하지 않는다. 공격 산식과 작렬 부딪힘 피해가 함께 쓴다.
        /// </summary>
        internal static int ApplyPhysicalDefense(int damage, int physicalDefense)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (physicalDefense < 0) throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            return (int)((long)damage * (100 - Math.Min(physicalDefense, 100)) / 100);
        }
    }
}
