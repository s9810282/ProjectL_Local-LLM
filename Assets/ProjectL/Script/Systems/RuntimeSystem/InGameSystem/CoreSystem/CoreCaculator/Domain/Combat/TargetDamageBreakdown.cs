using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 한 대상에 대한 피해 단계별 내역입니다.
    /// 일반 공격은 가산 이후 스플래시 절삭, 상성, 방어, 피격 경감을 따른다. 기존 관통 경로는 감쇠 순서를 유지한다.
    /// Preview와 Commit이 같은 내역을 보고하도록 각 단계 값을 따로 보관합니다.
    /// </summary>
    public readonly struct TargetDamageBreakdown
    {
        /// <summary>상성 직전의 피해다. 일반 스플래시 비율 또는 기존 관통 감쇠가 반영되었으며 공통 원본 피해와 구분한다.</summary>
        public int DamageBeforeMatchup { get; }

        /// <summary>적용된 상성 배율의 분자입니다. 상성을 적용하지 않은 대상은 분모와 같습니다.</summary>
        public int MatchupNumerator { get; }

        /// <summary>적용된 상성 배율의 분모입니다.</summary>
        public int MatchupDenominator { get; }

        /// <summary>half-up 반올림까지 끝난 상성 적용 결과입니다.</summary>
        public int DamageAfterMatchup { get; }

        /// <summary>
        /// PhysicalDefense 단계를 통과한 뒤의 피해입니다.
        /// 실제 방어 공식은 아직 미정이므로 현재는 통과 값이며, 단계 위치만 고정합니다.
        /// </summary>
        public int DamageAfterPhysicalDefense { get; }

        /// <summary>Heavy 등 피격 경감이 실제로 깎은 양입니다.</summary>
        public int IncomingReduction { get; }

        /// <summary>모든 단계를 통과한 최종 피해입니다.</summary>
        public int FinalDamage { get; }
        /// <summary>기존 피격 경감 뒤의 비율 감소를 적용한 정확한 값이다. 최종 피해만 소수점을 버린다.</summary>
        public decimal DamageAfterReceivedPercentages { get; }

        internal TargetDamageBreakdown(
            int damageBeforeMatchup,
            int matchupNumerator,
            int matchupDenominator,
            int damageAfterMatchup,
            int damageAfterPhysicalDefense,
            int incomingReduction,
            int finalDamage, decimal? damageAfterReceivedPercentages = null)
        {
            if (damageBeforeMatchup < 0) throw new ArgumentOutOfRangeException(nameof(damageBeforeMatchup));
            if (matchupNumerator < 0) throw new ArgumentOutOfRangeException(nameof(matchupNumerator));
            if (matchupDenominator <= 0) throw new ArgumentOutOfRangeException(nameof(matchupDenominator));
            if (damageAfterMatchup < 0) throw new ArgumentOutOfRangeException(nameof(damageAfterMatchup));
            if (damageAfterPhysicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(damageAfterPhysicalDefense));
            if (incomingReduction < 0) throw new ArgumentOutOfRangeException(nameof(incomingReduction));
            if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));

            DamageBeforeMatchup = damageBeforeMatchup;
            MatchupNumerator = matchupNumerator;
            MatchupDenominator = matchupDenominator;
            DamageAfterMatchup = damageAfterMatchup;
            DamageAfterPhysicalDefense = damageAfterPhysicalDefense;
            IncomingReduction = incomingReduction;
            FinalDamage = finalDamage;
            DamageAfterReceivedPercentages = damageAfterReceivedPercentages ?? finalDamage;
        }

        /// <summary>상성과 경감이 모두 없는 대상의 통과 내역입니다.</summary>
        internal static TargetDamageBreakdown CreatePassthrough(int damage)
        {
            return new TargetDamageBreakdown(damage, 1, 1, damage, damage, 0, damage);
        }
    }
}
