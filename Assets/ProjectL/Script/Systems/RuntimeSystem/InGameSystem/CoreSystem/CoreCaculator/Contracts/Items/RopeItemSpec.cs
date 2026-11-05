using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class RopeItemSpec : ItemEffectSpec
    {
        public int PriorityPenalty { get; }

        public RopeItemSpec(int priorityPenalty)
        {
            if (priorityPenalty < 1) throw new ArgumentOutOfRangeException(nameof(priorityPenalty));
            PriorityPenalty = priorityPenalty;
        }
    }
}
