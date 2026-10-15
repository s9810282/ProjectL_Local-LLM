using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    /// <summary>
    /// 직전 성공 공격 이후 지불한 이동비에서 체크포인트 복귀 환급을 뺀 별도 공격 환급 기준이다.
    /// </summary>
    internal readonly struct AttackActionCostRefundEffectContext
    {
        public int AttackRefundEligibleMoveCost { get; }

        public AttackActionCostRefundEffectContext(int attackRefundEligibleMoveCost)
        {
            if (attackRefundEligibleMoveCost < 0)
                throw new ArgumentOutOfRangeException(nameof(attackRefundEligibleMoveCost));

            AttackRefundEligibleMoveCost = attackRefundEligibleMoveCost;
        }
    }
}
