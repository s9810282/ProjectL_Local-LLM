using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    /// <summary>특성 성장의 순수 판정 결과다. 성공한 변경도 저장은 호출자가 담당한다.</summary>
    public sealed class TraitGrowthDecision
    {
        public bool Succeeded => FailureReason == TraitGrowthFailureReason.None;
        public TraitGrowthFailureReason FailureReason { get; }
        public TraitGrowthChange Change { get; }

        private TraitGrowthDecision(TraitGrowthFailureReason reason, TraitGrowthChange change)
        {
            FailureReason = reason;
            Change = change;
        }

        public static TraitGrowthDecision Success(TraitGrowthChange change) =>
            new(TraitGrowthFailureReason.None, change ?? throw new ArgumentNullException(nameof(change)));

        public static TraitGrowthDecision Failure(TraitGrowthFailureReason reason)
        {
            if (reason == TraitGrowthFailureReason.None || !Enum.IsDefined(typeof(TraitGrowthFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new TraitGrowthDecision(reason, null);
        }
    }
}
