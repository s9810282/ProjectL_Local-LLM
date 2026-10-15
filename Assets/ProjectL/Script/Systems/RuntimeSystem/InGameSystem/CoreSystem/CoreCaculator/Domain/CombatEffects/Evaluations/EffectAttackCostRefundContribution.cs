using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 한 규칙이 요청한 환급액입니다. 실제 배분은 공통 evaluator가 상한을 적용해 결정합니다.
    /// </summary>
    internal readonly struct EffectAttackCostRefundContribution
    {
        public EffectOriginKey Origin { get; }
        public int RequestedRefund { get; }

        public EffectAttackCostRefundContribution(EffectOriginKey origin, int requestedRefund)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (requestedRefund < 0) throw new ArgumentOutOfRangeException(nameof(requestedRefund));

            Origin = origin;
            RequestedRefund = requestedRefund;
        }
    }
}
