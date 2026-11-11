using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>정렬을 시작한 순간의 기준 구도와 축 범위입니다. Position에는 offset이 포함되지 않습니다.</summary>
    internal readonly struct StageMapCameraFocusFrame
    {
        internal StageMapCameraFocusFrame(long generation, CameraCatalogData framing, Vector3 axis,
            float minimumOffset, float maximumOffset, float startOffset, float aspect)
        {
            Generation = generation;
            Framing = framing;
            Axis = axis;
            MinimumOffset = minimumOffset;
            MaximumOffset = maximumOffset;
            StartOffset = startOffset;
            Aspect = aspect;
        }

        internal long Generation { get; }
        internal CameraCatalogData Framing { get; }
        internal Vector3 Axis { get; }
        internal float MinimumOffset { get; }
        internal float MaximumOffset { get; }
        internal float StartOffset { get; }
        internal float Aspect { get; }
    }
}
