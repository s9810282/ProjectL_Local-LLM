using System;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class TraitGrowthStepSpec
    {
        public int StepId { get; }
        public int UnlockLevel { get; }
        public TraitGrowthKind Kind { get; }
        public TraitGrowthStepSpec(int stepId, int unlockLevel, TraitGrowthKind kind)
        {
            if (stepId < 1) throw new ArgumentOutOfRangeException(nameof(stepId));
            if (unlockLevel < 2) throw new ArgumentOutOfRangeException(nameof(unlockLevel));
            if (kind != TraitGrowthKind.Acquire && kind != TraitGrowthKind.Enhance)
                throw new ArgumentOutOfRangeException(nameof(kind));
            StepId = stepId;
            UnlockLevel = unlockLevel;
            Kind = kind;
        }
    }
}
