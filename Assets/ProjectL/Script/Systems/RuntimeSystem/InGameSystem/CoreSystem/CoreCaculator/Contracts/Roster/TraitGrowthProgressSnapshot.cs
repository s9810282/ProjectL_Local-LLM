using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public enum TraitGrowthProgressStatus
    {
        Unavailable, AwaitingAcknowledgement, ChoiceRequired, ReadyToContinue
    }
    public sealed class TraitGrowthProgressSnapshot
    {
        public RunId RunId { get; }
        public TraitGrowthProgressStatus Status { get; }
        public TraitGrowthOffer ActiveOffer { get; }
        public TraitGrowthProgressSnapshot(RunId runId, TraitGrowthProgressStatus status, TraitGrowthOffer activeOffer)
        {
            if (!Enum.IsDefined(typeof(TraitGrowthProgressStatus), status) || activeOffer != null && activeOffer.RunId != runId)
                throw new ArgumentException("성장 진행 문맥이 올바르지 않습니다.");
            RunId = runId;
            Status = status;
            ActiveOffer = activeOffer;
        }
    }
}
