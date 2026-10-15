using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>선택·임시 회전과 후보 좌표만 소유합니다. 확정 상태·배치 규칙은 Core 소유입니다.</summary>
    internal sealed class ArtifactInteraction
    {
        internal bool IsDragging { get; private set; }
        internal bool IsPlaced { get; private set; }
        internal int ArtifactId { get; private set; }
        internal int InstanceId { get; private set; }
        internal CellCoord Anchor { get; private set; }
        internal Rotation4 Rotation { get; private set; }
        internal uint Revision { get; private set; }
        internal bool HasCandidate { get; private set; }
        private CellCoord _grabOffset;
        private bool _previewDirty;

        internal void BeginPool(int artifactId)
        {
            Cancel();
            IsDragging = true;
            ArtifactId = artifactId;
        }

        internal void BeginPlaced(int artifactId, int instanceId, CellCoord anchor, Rotation4 rotation, CellCoord grabbedCell)
        {
            BeginPool(artifactId);
            IsPlaced = true;
            InstanceId = instanceId;
            Rotation = rotation;
            _grabOffset = grabbedCell - anchor;
        }

        internal void SetCandidate(CellCoord pointerCell, bool usable)
        {
            if (!IsDragging) return;
            CellCoord anchor = pointerCell - _grabOffset;
            if (HasCandidate == usable && (!usable || Anchor == anchor)) return;
            HasCandidate = usable;
            Anchor = anchor;
            InvalidateCandidate();
        }

        internal void Rotate(int steps)
        {
            if (!IsDragging) return;
            // 회전은 Anchor 기준입니다. 모양 회전·충돌 계산은 Core가 수행합니다.
            Rotation = (Rotation4)(((int)Rotation + steps % 4 + 4) % 4);
            InvalidateCandidate();
        }

        internal void InvalidateCandidate()
        {
            Revision++;
            _previewDirty = true;
        }

        internal bool TryTakePreview(out ArtifactPreviewRequest request)
        {
            request = null;
            if (!IsDragging || !HasCandidate || !_previewDirty) return false;
            _previewDirty = false;
            request = IsPlaced
                ? ArtifactPreviewRequest.Move(InstanceId, Anchor, Rotation)
                : ArtifactPreviewRequest.Place(ArtifactId, Anchor, Rotation);
            return true;
        }

        internal ArtifactEditRequest Release(bool overBoard, bool overReturnArea)
        {
            if (!IsDragging) return null;
            ArtifactEditRequest request = null;
            if (overReturnArea && IsPlaced) request = ArtifactEditRequest.Return(InstanceId);
            else if (overBoard && HasCandidate)
                request = IsPlaced ? ArtifactEditRequest.Move(InstanceId, Anchor, Rotation)
                    : ArtifactEditRequest.Place(ArtifactId, Anchor, Rotation);
            Cancel();
            return request;
        }

        internal void Cancel()
        {
            IsDragging = false;
            IsPlaced = false;
            ArtifactId = InstanceId = 0;
            Anchor = _grabOffset = CellCoord.Zero;
            Rotation = Rotation4.R0;
            HasCandidate = _previewDirty = false;
            Revision++;
        }
    }
}
