using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>설치된 회복 옵션과 Core가 전달한 예상 회복량·선택 사유를 보관한다.</summary>
    public sealed class RecoveryOptionState
    {
        public RecoveryOptionSpec Option { get; }
        public int ProjectedHealAmount { get; }
        public int ProjectedMaxHealthIncrease { get; }
        public RecoveryFailureReason UnavailableReason { get; }
        public bool CanSelect => UnavailableReason == RecoveryFailureReason.None;

        public RecoveryOptionState(RecoveryOptionSpec option, int projectedHealAmount,
            RecoveryFailureReason unavailableReason, int projectedMaxHealthIncrease = 0)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }
            if (projectedHealAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(projectedHealAmount));
            }
            if (projectedMaxHealthIncrease < 0 ||
                projectedMaxHealthIncrease > 0 && (option.EffectKind != RecoveryEffectKind.MaxHealthGrowth || projectedMaxHealthIncrease != option.MaxHealthIncrease) ||
                projectedHealAmount > 0 && option.EffectKind != RecoveryEffectKind.Heal)
                throw new ArgumentException("효과 종류와 회복·최대 체력 예상 값이 일치하지 않습니다.");
            if (unavailableReason != RecoveryFailureReason.None &&
                unavailableReason != RecoveryFailureReason.InsufficientFunds &&
                unavailableReason != RecoveryFailureReason.NoEffect &&
                unavailableReason != RecoveryFailureReason.AlreadyResolved &&
                unavailableReason != RecoveryFailureReason.StaleEntry)
            {
                throw new ArgumentOutOfRangeException(nameof(unavailableReason));
            }
            if (projectedHealAmount == 0 && projectedMaxHealthIncrease == 0 && unavailableReason == RecoveryFailureReason.None)
            {
                throw new ArgumentException("효과가 없는 회복 옵션은 선택 가능할 수 없습니다.", nameof(unavailableReason));
            }

            Option = option;
            ProjectedHealAmount = projectedHealAmount;
            ProjectedMaxHealthIncrease = projectedMaxHealthIncrease;
            UnavailableReason = unavailableReason;
        }
    }
}
