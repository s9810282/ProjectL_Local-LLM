using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class WhiskeyItemSpec : ItemEffectSpec
    {
        public int TriggerHealthPercent { get; }
        public int HealPercent { get; }

        public WhiskeyItemSpec(int triggerHealthPercent, int healPercent)
        {
            if (triggerHealthPercent < 1 || triggerHealthPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(triggerHealthPercent));
            if (healPercent < 1 || healPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(healPercent));
            TriggerHealthPercent = triggerHealthPercent;
            HealPercent = healPercent;
        }
    }
}
