using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 작렬 부딪힘 피해 식의 유일한 원본이다(설계 2026-11-10 CI4·CI5). 언제 부딪히는지·누가 받는지는
    /// <see cref="CrashImpactCollision"/>가, 보호막·받는 피해 감소·사망은 공통 피해 입구가 맡는다.
    /// </summary>
    internal static class CrashImpactCollisionRules
    {
        /// <summary>부딪힌 종류에 맞는 비율이다. 캐릭터면 유닛 비율, 나머지(벽·구조물)는 벽 비율이다.</summary>
        internal static int PercentFor(EffectKnockbackCollision collision, AttackKnockbackCollisionKind kind) =>
            kind == AttackKnockbackCollisionKind.Character ? collision.UnitDamagePercent : collision.WallDamagePercent;

        /// <summary>날아간 대상 최대 체력 × 비율 ÷ 100, 내림이다.</summary>
        internal static int BaseDamage(int flyerMaxHealth, int percent)
        {
            if (flyerMaxHealth < 0) throw new ArgumentOutOfRangeException(nameof(flyerMaxHealth));
            if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            return (int)((long)flyerMaxHealth * percent / 100);
        }

        /// <summary>
        /// 받는 쪽의 실효 방어력을 공격과 같은 식으로 적용한다. 공격 상성은 넣지 않는다.
        /// 기준 피해가 1 이상이면 방어 뒤 최소 1이다.
        /// </summary>
        internal static int AfterDefense(int baseDamage, int physicalDefense)
        {
            int reduced = TargetDamagePipeline.ApplyPhysicalDefense(baseDamage, physicalDefense);
            return baseDamage > 0 ? Math.Max(1, reduced) : 0;
        }
    }
}
