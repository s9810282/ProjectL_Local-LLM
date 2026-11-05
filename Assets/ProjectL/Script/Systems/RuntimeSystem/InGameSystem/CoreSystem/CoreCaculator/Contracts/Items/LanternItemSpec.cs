using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class LanternItemSpec : ItemEffectSpec
    {
        public int DefensePenalty { get; }

        public LanternItemSpec(int defensePenalty)
        {
            if (defensePenalty < 1) throw new ArgumentOutOfRangeException(nameof(defensePenalty));
            DefensePenalty = defensePenalty;
        }
    }
}
