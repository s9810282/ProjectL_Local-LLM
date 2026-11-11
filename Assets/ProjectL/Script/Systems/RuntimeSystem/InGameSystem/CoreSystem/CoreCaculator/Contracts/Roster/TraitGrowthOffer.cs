using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class TraitGrowthOffer
    {
        public const int MaximumChoices = 3;

        public RunId RunId { get; }
        public long OfferId { get; }
        public CharacterInstanceId OwnedId { get; }
        public int StepId { get; }
        public TraitGrowthKind Kind { get; }
        public IReadOnlyList<TraitGrowthChoice> Choices { get; }
        public TraitGrowthOffer(RunId runId, long offerId, CharacterInstanceId ownedId, int stepId,
            TraitGrowthKind kind, IReadOnlyList<TraitGrowthChoice> choices)
        {
            if (runId.IsEmpty || offerId < 1 || ownedId.IsEmpty || stepId < 1)
                throw new ArgumentException("제안 식별자가 올바르지 않습니다.");
            var items = new TraitGrowthChoices(ownedId, choices).Items;
            if (items.Count < 1 || items.Count > MaximumChoices ||
                items.Any(c => c.Kind != kind))
                throw new ArgumentException("제안 종류와 후보 개수가 올바르지 않습니다.", nameof(choices));
            RunId = runId;
            OfferId = offerId;
            OwnedId = ownedId;
            StepId = stepId;
            Kind = kind;
            Choices = items;
        }
    }
}
