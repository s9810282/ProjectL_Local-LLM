using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>드래그 모델·프리뷰만 표시합니다. 보드 갱신 계약과 분리해 필요한 동작만 전달합니다.</summary>
    internal interface IArtifactDragPresenter
    {
        void BeginDrag(GameObject prefab, int instanceId, bool placed);
        void ClearPreview();
        void ShowPreview(ArtifactPlacementPreview preview, Rotation4 rotation, int width, int height);
        void EndDrag();
    }
}
