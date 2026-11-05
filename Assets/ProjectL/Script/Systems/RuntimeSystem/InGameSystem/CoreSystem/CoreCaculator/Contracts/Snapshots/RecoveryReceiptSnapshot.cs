using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class RecoveryReceiptSnapshot
    {
        public RecoveryOptionId? AppliedOptionId { get; }
        public int SpentAmount { get; }
        public int HealedAmount { get; }
        public int MaxHealthIncrease { get; }

        public RecoveryReceiptSnapshot(RecoveryOptionId? appliedOptionId, int spentAmount, int healedAmount, int maxHealthIncrease = 0)
        {
            if (appliedOptionId.HasValue && appliedOptionId.Value.IsEmpty)
                throw new ArgumentException("An applied option cannot have an empty ID.", nameof(appliedOptionId));
            if (spentAmount < 0) throw new ArgumentOutOfRangeException(nameof(spentAmount));
            if (healedAmount < 0) throw new ArgumentOutOfRangeException(nameof(healedAmount));
            if (maxHealthIncrease < 0) throw new ArgumentOutOfRangeException(nameof(maxHealthIncrease));
            if (healedAmount > 0 && maxHealthIncrease > 0)
                throw new ArgumentException("회복과 최대 체력 증가는 별도 구매여야 합니다.");
            if (!appliedOptionId.HasValue && (spentAmount != 0 || healedAmount != 0 || maxHealthIncrease != 0))
                throw new ArgumentException("Amounts must be zero when no option was applied.");

            AppliedOptionId = appliedOptionId;
            SpentAmount = spentAmount;
            HealedAmount = healedAmount;
            MaxHealthIncrease = maxHealthIncrease;
        }
    }
}
