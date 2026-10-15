using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class ArtifactActivationSnapshot
    {
        public EffectOriginKey Origin { get; }
        public EntityId TargetId { get; }
        public long RequestedAmount { get; }
        public long AppliedAmount { get; }

        public ArtifactActivationSnapshot(
            EffectOriginKey origin,
            EntityId targetId,
            long requestedAmount,
            long appliedAmount)
        {
            if (!origin.IsValid) throw new ArgumentException(nameof(origin));
            if (targetId.IsEmpty) throw new ArgumentException(nameof(targetId));
            if (requestedAmount <= 0) throw new ArgumentOutOfRangeException(nameof(requestedAmount));
            if (appliedAmount < 0 || appliedAmount > requestedAmount)
                throw new ArgumentOutOfRangeException(nameof(appliedAmount));

            Origin = origin;
            TargetId = targetId;
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
        }
    }
}
