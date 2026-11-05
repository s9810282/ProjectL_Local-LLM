using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class TraitGrowthChoiceRequest
    {
        public RunId RunId { get; }
        public long OfferId { get; }
        public CharacterInstanceId OwnedId { get; }
        public int StepId { get; }
        public TraitKey TraitKey { get; }
        public TraitGrowthChoiceRequest(RunId runId, long offerId, CharacterInstanceId ownedId, int stepId, TraitKey traitKey)
        {
            if (runId.IsEmpty || offerId < 1 || ownedId.IsEmpty || stepId < 1 || traitKey == TraitKey.None ||
                !Enum.IsDefined(typeof(TraitKey), traitKey))
                throw new ArgumentException("선택 요청 식별자가 올바르지 않습니다.");
            RunId = runId;
            OfferId = offerId;
            OwnedId = ownedId;
            StepId = stepId;
            TraitKey = traitKey;
        }
        public bool SameValues(TraitGrowthChoiceRequest other) => other != null && RunId == other.RunId &&
            OfferId == other.OfferId && OwnedId == other.OwnedId && StepId == other.StepId && TraitKey == other.TraitKey;
    }
}
