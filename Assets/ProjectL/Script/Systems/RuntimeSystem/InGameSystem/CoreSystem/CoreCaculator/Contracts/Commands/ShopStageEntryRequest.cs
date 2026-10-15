using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class ShopStageEntryRequest
    {
        public RunId RunId { get; }
        public StageEntryId EntryId { get; }
        public long ExpectedRunRevision { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public HealthStateSnapshot Health { get; }
        public int OfferSeed { get; }

        public ShopStageEntryRequest(RunId runId, StageEntryId entryId, long expectedRunRevision,
            StageNodeId nodeId, StageKey stageKey, HealthStateSnapshot health, int offerSeed)
        {
            if (runId.IsEmpty) throw new ArgumentException(nameof(runId));
            if (entryId.IsEmpty) throw new ArgumentException(nameof(entryId));
            if (expectedRunRevision < 0) throw new ArgumentOutOfRangeException(nameof(expectedRunRevision));
            if (nodeId.IsEmpty) throw new ArgumentException(nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (health.MaxHealth < 1 || health.CurrentHealth < 1 || health.CurrentHealth > health.MaxHealth)
                throw new ArgumentException("Living main-character health is required.", nameof(health));

            RunId = runId;
            EntryId = entryId;
            ExpectedRunRevision = expectedRunRevision;
            NodeId = nodeId;
            StageKey = stageKey;
            Health = health;
            OfferSeed = offerSeed;
        }
    }
}
