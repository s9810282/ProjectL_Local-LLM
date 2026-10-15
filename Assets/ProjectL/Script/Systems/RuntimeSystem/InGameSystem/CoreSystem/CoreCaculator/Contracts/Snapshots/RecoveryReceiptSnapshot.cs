using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class RecoveryReceiptSnapshot
    {
        public RecoveryOptionId? AppliedOptionId { get; }
        public int SpentAmount { get; }
        public int HealedAmount { get; }

        public RecoveryReceiptSnapshot(RecoveryOptionId? appliedOptionId, int spentAmount, int healedAmount)
        {
            if (appliedOptionId.HasValue && appliedOptionId.Value.IsEmpty)
                throw new ArgumentException("An applied option cannot have an empty ID.", nameof(appliedOptionId));
            if (spentAmount < 0) throw new ArgumentOutOfRangeException(nameof(spentAmount));
            if (healedAmount < 0) throw new ArgumentOutOfRangeException(nameof(healedAmount));
            if (!appliedOptionId.HasValue && (spentAmount != 0 || healedAmount != 0))
                throw new ArgumentException("Amounts must be zero when no option was applied.");

            AppliedOptionId = appliedOptionId;
            SpentAmount = spentAmount;
            HealedAmount = healedAmount;
        }
    }
}
