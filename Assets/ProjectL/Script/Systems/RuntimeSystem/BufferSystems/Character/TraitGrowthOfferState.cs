using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    public interface ITraitGrowthStateReadSink : IServiceModule
    {
        TraitGrowthOffer ActiveOffer
        {
            get;
        }
    }
    public interface ITraitGrowthResultReadSink : IServiceModule
    {
        int HandledCommandNumber
        {
            get;
        }
        TraitGrowthProcessedResult LatestResult
        {
            get;
        }
    }
    internal interface ITraitGrowthStateStore : IServiceModule
    {
        TraitGrowthOffer PublishOffer(RunId run, CharacterInstanceId owned, int step, TraitGrowthKind kind, IReadOnlyList<TraitGrowthChoice> choices);
        void InstallResult(TraitGrowthProcessedResult result, bool clearOffer);
        void Clear();
    }
    internal sealed class TraitGrowthOfferState : ITraitGrowthStateReadSink, ITraitGrowthResultReadSink, ITraitGrowthStateStore
    {
        private long _lastOfferId;
        public TraitGrowthOffer ActiveOffer
        {
            get; private set;
        }
        public TraitGrowthProcessedResult LatestResult
        {
            get; private set;
        }
        public int HandledCommandNumber => LatestResult?.Number ?? 0;
        public TraitGrowthOffer PublishOffer(RunId run, CharacterInstanceId owned, int step, TraitGrowthKind kind, IReadOnlyList<TraitGrowthChoice> choices)
        {
            long id = checked(_lastOfferId + 1);
            var offer = new TraitGrowthOffer(run, id, owned, step, kind, choices);
            ActiveOffer = offer;
            _lastOfferId = id;
            return offer;
        }
        public void InstallResult(TraitGrowthProcessedResult result, bool clearOffer)
        {
            if (clearOffer)
                ActiveOffer = null;
            LatestResult = result;
        }
        public void Clear()
        {
            ActiveOffer = null;
            LatestResult = null;
        }
    }
}
