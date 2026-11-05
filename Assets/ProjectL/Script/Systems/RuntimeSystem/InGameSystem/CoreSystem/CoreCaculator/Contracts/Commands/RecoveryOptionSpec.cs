using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class RecoveryOptionSpec
    {
        public RecoveryOptionId Id { get; }
        public int Cost { get; }
        /// <summary>체력 회복형의 입력이다. 최대 체력 증가형에서는 null이며 EffectKind로 구분한다.</summary>
        public RecoveryHealSpec Heal { get; }
        public RecoveryEffectKind EffectKind { get; }
        public int MaxHealthIncrease { get; }

        public RecoveryOptionSpec(RecoveryOptionId id, int cost, RecoveryHealSpec heal)
            : this(id, cost, heal ?? throw new ArgumentNullException(nameof(heal)), 0)
        {
        }

        public RecoveryOptionSpec(RecoveryOptionId id, int cost, int maxHealthIncrease)
            : this(id, cost, null, maxHealthIncrease)
        {
        }

        private RecoveryOptionSpec(RecoveryOptionId id, int cost, RecoveryHealSpec heal, int maxHealthIncrease)
        {
            if (id.IsEmpty) throw new ArgumentException(nameof(id));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (heal == null && maxHealthIncrease < 1) throw new ArgumentOutOfRangeException(nameof(maxHealthIncrease));

            Id = id;
            Cost = cost;
            Heal = heal;
            EffectKind = heal == null ? RecoveryEffectKind.MaxHealthGrowth : RecoveryEffectKind.Heal;
            MaxHealthIncrease = maxHealthIncrease;
        }
    }
}
