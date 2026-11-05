using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class TraitGrowthSelection
    {
        public int StepId { get; }
        public TraitKey TraitKey { get; }
        // None은 종류를 저장하지 않던 이전 이력이며, 검증된 이전 단계로 의미를 해석한다.
        public TraitGrowthKind Kind { get; }
        public TraitGrowthSelection(int stepId, TraitKey traitKey, TraitGrowthKind kind = TraitGrowthKind.None)
        {
            if (stepId < 1)
                throw new ArgumentOutOfRangeException(nameof(stepId));
            if (traitKey == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), traitKey))
                throw new ArgumentOutOfRangeException(nameof(traitKey));
            if (!Enum.IsDefined(typeof(TraitGrowthKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            StepId = stepId;
            TraitKey = traitKey;
            Kind = kind;
        }
        public bool SameValues(TraitGrowthSelection other) => other != null && StepId == other.StepId && TraitKey == other.TraitKey && Kind == other.Kind;
    }
}
