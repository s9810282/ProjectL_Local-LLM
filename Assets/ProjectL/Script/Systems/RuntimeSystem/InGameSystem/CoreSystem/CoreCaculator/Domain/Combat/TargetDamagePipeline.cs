using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 확정된 대상별 피해 단계 순서를 한 곳에서 계산합니다.
    /// 기존 입력은 공격자 가산 및 해당 관통 감쇠 이후의 값이다. 일반 스플래시는 공통 피해에서 먼저 절삭한다.
    /// 상성 조회는 CategoryMatchupResolver가, 경감 기여는 CombatEffectEngine이 담당하며
    /// 이 계산기는 순서와 반올림만 책임집니다. 두 번째 피해 경로를 만들지 않습니다.
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
            CategoryKey attackerCategory, CategoryKey? defenderCategory, int incomingReduction)
        {
            if (originalDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(originalDamage));
            if (splashPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(splashPercent));
            if (role != AttackTargetRole.Primary && role != AttackTargetRole.Additional && role != AttackTargetRole.Designated)
                throw new ArgumentOutOfRangeException(nameof(role));

            int scaledDamage = role == AttackTargetRole.Additional
                ? SplashDamageCalculator.CalculateAdditional(originalDamage, splashPercent)
                : originalDamage;
            return Calculate(scaledDamage, attackerCategory, defenderCategory, incomingReduction);
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
        /// <param name="incomingReduction">
        /// 피격 경감 기여 합계입니다. Heavy 등 incoming 효과가 계산한 실제 값입니다.
        /// </param>
        public TargetDamageBreakdown Calculate(
            int damageBeforeMatchup,
            CategoryKey attackerCategory,
            CategoryKey? defenderCategory,
            int incomingReduction)
        {
            if (damageBeforeMatchup < 0)
                throw new ArgumentOutOfRangeException(nameof(damageBeforeMatchup));
            if (incomingReduction < 0)
                throw new ArgumentOutOfRangeException(nameof(incomingReduction));

            // 이 시점에는 일반 스플래시 비율 또는 기존 관통 감쇠가 이미 반영되어 있다.
            int numerator;
            int denominator;
            int damageAfterMatchup;

            if (defenderCategory.HasValue)
            {
                // 2단계: 대상별 Category 상성. half-up 반올림은 계산기가 담당한다.
                CategoryMatchupMultiplier multiplier =
                    _matchupResolver.GetMultiplier(attackerCategory, defenderCategory.Value);
                numerator = multiplier.Numerator;
                denominator = multiplier.Denominator;
                damageAfterMatchup = CategoryMatchupDamageCalculator.Apply(
                    damageBeforeMatchup, numerator, denominator);
            }
            else
            {
                // Category가 없는 대상은 상성을 적용하지 않고 통과시킨다.
                numerator = 1;
                denominator = 1;
                damageAfterMatchup = damageBeforeMatchup;
            }

            // 3단계: PhysicalDefense.
            // 실제 방어 공식이 프로젝트에 아직 없으므로 단계 위치만 고정하고 값을 통과시킨다.
            // 공식이 확정되면 이 한 곳만 교체한다.
            int damageAfterPhysicalDefense = damageAfterMatchup;

            // 4단계: Heavy 등 피격 경감. 경감량은 남은 피해를 넘지 않는다.
            int effectiveReduction = Math.Min(incomingReduction, damageAfterPhysicalDefense);
            int finalDamage = damageAfterPhysicalDefense - effectiveReduction;

            return new TargetDamageBreakdown(
                damageBeforeMatchup,
                numerator,
                denominator,
                damageAfterMatchup,
                damageAfterPhysicalDefense,
                effectiveReduction,
                finalDamage);
        }
    }
}
