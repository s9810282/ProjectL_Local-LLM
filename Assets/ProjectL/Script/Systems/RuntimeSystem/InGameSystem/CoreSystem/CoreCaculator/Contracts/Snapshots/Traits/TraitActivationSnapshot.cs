using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class TraitActivationSnapshot
    {
        public EntityId SourceUnitId { get; }
        public EntityId? TargetId { get; }
        public TraitKey TraitKey { get; }
        public TraitActivationKind TraitActivationKind { get; }
        public int Magnitude { get; }

        public TraitActivationSnapshot(
            EntityId sourceUnitId,
            EntityId? targetId,
            TraitKey traitKey,
            TraitActivationKind traitActivationKind,
            int magnitude)
        {
            if (sourceUnitId == EntityId.Empty)
                throw new ArgumentException(nameof(sourceUnitId));
            if (targetId.HasValue && targetId.Value == EntityId.Empty)
                throw new ArgumentException(nameof(targetId));
            if (traitKey == TraitKey.None || Enum.IsDefined(typeof(TraitKey), traitKey) == false)
                throw new ArgumentOutOfRangeException(nameof(traitKey));
            if (traitActivationKind == TraitActivationKind.None ||
                Enum.IsDefined(typeof(TraitActivationKind), traitActivationKind) == false)
                throw new ArgumentOutOfRangeException(nameof(traitActivationKind));
            if (magnitude < 0)
                throw new ArgumentOutOfRangeException(nameof(magnitude));

            SourceUnitId = sourceUnitId;
            TargetId = targetId;
            TraitKey = traitKey;
            TraitActivationKind = traitActivationKind;
            Magnitude = magnitude;
        }
    }
}
