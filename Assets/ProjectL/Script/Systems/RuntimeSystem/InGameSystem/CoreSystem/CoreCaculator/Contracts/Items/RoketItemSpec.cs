using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class RoketItemSpec : ItemEffectSpec
    {
        public int TriggerDamagePercent { get; }
        public int ReductionPercent { get; }

        public RoketItemSpec(int triggerDamagePercent, int reductionPercent)
        {
            if (triggerDamagePercent < 1 || triggerDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(triggerDamagePercent));
            if (reductionPercent < 1 || reductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(reductionPercent));
            TriggerDamagePercent = triggerDamagePercent;
            ReductionPercent = reductionPercent;
        }
    }
}
