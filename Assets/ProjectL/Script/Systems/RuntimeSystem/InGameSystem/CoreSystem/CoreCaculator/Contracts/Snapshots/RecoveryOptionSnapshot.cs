using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class RecoveryOptionSnapshot
    {
        public RecoveryOptionId Id { get; }
        public int Cost { get; }
        public int ProjectedHealAmount { get; }
        public bool CanSelect { get; }
        public RecoveryFailureReason UnavailableReason { get; }

        public RecoveryOptionSnapshot(RecoveryOptionId id, int cost, int projectedHealAmount,
            bool canSelect, RecoveryFailureReason unavailableReason)
        {
            if (id.IsEmpty) throw new ArgumentException(nameof(id));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (projectedHealAmount < 0) throw new ArgumentOutOfRangeException(nameof(projectedHealAmount));
            if (!Enum.IsDefined(typeof(RecoveryFailureReason), unavailableReason))
                throw new ArgumentOutOfRangeException(nameof(unavailableReason));
            if (canSelect != (unavailableReason == RecoveryFailureReason.None))
                throw new ArgumentException("Selection availability and its reason must agree.");

            Id = id;
            Cost = cost;
            ProjectedHealAmount = projectedHealAmount;
            CanSelect = canSelect;
            UnavailableReason = unavailableReason;
        }
    }
}
