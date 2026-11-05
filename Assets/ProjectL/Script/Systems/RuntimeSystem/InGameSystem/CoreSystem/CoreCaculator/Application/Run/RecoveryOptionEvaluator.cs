using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Run
{
    public static class RecoveryOptionEvaluator
    {
        public static RecoveryOptionState Evaluate(RecoveryOptionSpec option, HealthStateSnapshot health,
            int gold, bool hasApplied, bool isCompleted)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }
            if (gold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gold));
            }
            if (health.CurrentHealth <= 0 || health.MaxHealth < 1 || health.CurrentHealth > health.MaxHealth)
            {
                throw new InvalidOperationException("회복 판정에는 살아 있는 메인의 유효한 체력이 필요합니다.");
            }

            int healed = option.EffectKind == RecoveryEffectKind.Heal ? RecoveryHealCalculator.Calculate(option.Heal, health) : 0;
            int maxHealthIncrease = option.MaxHealthIncrease;
            // 성장할 수 없는 옵션 때문에 다른 선택이나 나가기가 막히지 않도록 효과 없음으로 투영한다.
            if (!hasApplied && !isCompleted && maxHealthIncrease > int.MaxValue - health.MaxHealth)
                maxHealthIncrease = 0;
            RecoveryFailureReason reason;
            if (isCompleted)
            {
                reason = RecoveryFailureReason.StaleEntry;
            }
            else if (hasApplied)
            {
                reason = RecoveryFailureReason.AlreadyResolved;
            }
            else if (healed == 0 && maxHealthIncrease == 0)
            {
                reason = RecoveryFailureReason.NoEffect;
            }
            else if (gold < option.Cost)
            {
                reason = RecoveryFailureReason.InsufficientFunds;
            }
            else
            {
                reason = RecoveryFailureReason.None;
            }
            return new RecoveryOptionState(option, healed, reason, maxHealthIncrease);
        }
    }
}
