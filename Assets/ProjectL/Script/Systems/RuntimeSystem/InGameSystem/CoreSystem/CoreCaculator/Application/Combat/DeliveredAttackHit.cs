using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 전달이 확정한 한 대상의 피격 입력이다. 일반 공격과 관통이 같은 모양으로 내보내므로 소비자는 전달 종류를 다시 판단하지 않는다.
    /// 공개 계약이 아니며, 피해량의 원본은 여전히 피해 파이프라인의 결과다.
    /// </summary>
    internal readonly struct DeliveredAttackHit
    {
        internal EntityId TargetId { get; }
        internal CombatDamageCause Cause { get; }
        internal AttackTargetRole Role { get; }
        /// <summary>거리 배율 적용 전 피해다. 일반 공격은 공통 피해, 관통은 조우 순번 감쇠를 적용한 피해다.</summary>
        internal int BaseDamage { get; }
        /// <summary>피해 파이프라인에 넘기는 적용 비율이다. 스플래시 대상만 방식의 스플래시 비율이고 나머지는 100이다.</summary>
        internal int AppliedPercent { get; }
        /// <summary>방향 방어·지형·보유 방어·사건 출처에 쓰는 물리 출발 칸이다. 스플래시는 주 칸, 나머지는 공격자 위치다.</summary>
        internal CellCoord? ImpactOrigin { get; }
        /// <summary>기본 피해의 일부라도 실제로 전달하는지 나타낸다. 스플래시 비율 0인 대상은 결과에는 남지만 피해를 받지 않는다.</summary>
        internal bool AppliesDamage => AppliedPercent > 0;
        /// <summary>보고 전용 대상과 실제 접촉을 구분한다. 피해 계산이 0이어도 실제 접촉은 남는다.</summary>
        internal bool Landed { get; }

        internal DeliveredAttackHit(EntityId targetId, AttackTargetRole role, int baseDamage, int appliedPercent,
            CellCoord? impactOrigin, CombatDamageCause cause = CombatDamageCause.NumericDamage, bool landed = true)
        {
            if (targetId.IsEmpty) throw new ArgumentException("피격 대상 ID가 필요합니다.", nameof(targetId));
            if (role == AttackTargetRole.None || !System.Enum.IsDefined(typeof(AttackTargetRole), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (appliedPercent < 0) throw new ArgumentOutOfRangeException(nameof(appliedPercent));
            if (!System.Enum.IsDefined(typeof(CombatDamageCause), cause)) throw new ArgumentOutOfRangeException(nameof(cause));
            Cause = cause;
            TargetId = targetId;
            Role = role;
            BaseDamage = baseDamage;
            AppliedPercent = appliedPercent;
            ImpactOrigin = impactOrigin;
            Landed = landed;
        }
    }
}
