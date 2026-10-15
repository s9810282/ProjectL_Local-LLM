using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class ActionCostSettlementSnapshot
    {
        public int Before { get; }
        public int Spent { get; }
        public int Refunded { get; }
        public int Final { get; }

        public ActionCostSettlementSnapshot(int before, int spent, int refunded, int final)
        {
            if (before < 0) throw new ArgumentOutOfRangeException(nameof(before));
            if (spent < 0 || spent > before)
                throw new ArgumentOutOfRangeException(nameof(spent));
            if (refunded < 0) throw new ArgumentOutOfRangeException(nameof(refunded));
            if (final < 0) throw new ArgumentOutOfRangeException(nameof(final));
            if (checked(before - spent + refunded) != final)
                throw new ArgumentException("Final must equal Before - Spent + Refunded.", nameof(final));

            Before = before;
            Spent = spent;
            Refunded = refunded;
            Final = final;
        }
    }
}
