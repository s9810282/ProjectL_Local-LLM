#nullable enable
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    /// <summary>재고나 편집 권한을 보장하지 않는 공간 미리보기 결과입니다.</summary>
    public sealed class ArtifactPlacementPreview
    {
        public ArtifactShapeSnapshot? Shape { get; }
        public CellCoord Anchor { get; }
        public ArtifactActionFailureReason FailureReason { get; }

        public ArtifactPlacementPreview(ArtifactShapeSnapshot? shape, CellCoord anchor, ArtifactActionFailureReason failureReason)
        {
            bool spatial;
            switch (failureReason)
            {
                case ArtifactActionFailureReason.None:
                case ArtifactActionFailureReason.OutOfBounds:
                case ArtifactActionFailureReason.CellOccupied:
                    spatial = true;
                    break;
                case ArtifactActionFailureReason.DefinitionNotFound:
                case ArtifactActionFailureReason.InstanceNotFound:
                    spatial = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failureReason));
            }
            if (spatial != (shape != null))
                throw new ArgumentException("Only spatial outcomes require a rotated shape.", nameof(shape));

            Shape = shape;
            Anchor = anchor;
            FailureReason = failureReason;
        }
    }
}
