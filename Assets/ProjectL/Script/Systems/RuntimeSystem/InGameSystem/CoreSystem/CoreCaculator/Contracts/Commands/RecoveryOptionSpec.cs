using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class RecoveryOptionSpec
    {
        public RecoveryOptionId Id { get; }
        public int Cost { get; }
        public RecoveryHealSpec Heal { get; }

        public RecoveryOptionSpec(RecoveryOptionId id, int cost, RecoveryHealSpec heal)
        {
            if (id.IsEmpty) throw new ArgumentException(nameof(id));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (heal == null) throw new ArgumentNullException(nameof(heal));

            Id = id;
            Cost = cost;
            Heal = heal;
        }
    }
}
