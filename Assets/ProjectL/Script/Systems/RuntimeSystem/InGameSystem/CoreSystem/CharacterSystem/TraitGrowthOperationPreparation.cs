using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal sealed class TraitGrowthOperationPreparation
    {
        internal RunId RunId { get; }
        internal long Revision { get; }
        internal OwnedCharacterRecord Record { get; }
        internal TraitGrowthOffer Offer { get; }
        internal TraitGrowthDecision Decision { get; }
        internal TraitGrowthOperationPreparation(RunId runId, long revision, TraitGrowthOffer offer, TraitGrowthDecision decision, OwnedCharacterRecord record = null)
        {
            RunId = runId;
            Revision = revision;
            Offer = offer;
            Decision = decision;
            Record = record;
        }
    }
}
