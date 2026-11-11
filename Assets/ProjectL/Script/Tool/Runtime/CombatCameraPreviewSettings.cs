using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectL.Tool.Runtime
{
    /// <summary>공용 카메라 설정 SO에서 읽은 편집 시작 구도 값과 계산입니다.</summary>
    public readonly struct CombatCameraPreviewSettings
    {
        private CombatCameraPreviewSettings(bool useZoomPose, float zoomMin, float zoomMax,
            Vector3 zoomOutPosition, Vector3 zoomOutRotation)
        {
            UseZoomPose = useZoomPose;
            ZoomMin = zoomMin;
            ZoomMax = zoomMax;
            ZoomOutPosition = zoomOutPosition;
            ZoomOutRotation = zoomOutRotation;
        }

        public bool UseZoomPose { get; }
        public float ZoomMin { get; }
        public float ZoomMax { get; }
        public Vector3 ZoomOutPosition { get; }
        public Vector3 ZoomOutRotation { get; }

        public float InitialSize(float baseSize) => UseZoomPose ? ZoomMax : Mathf.Clamp(baseSize, ZoomMin, ZoomMax);

        public Pose StartPoseAt(Vector3 position, Quaternion rotation) => UseZoomPose
            ? new Pose(position + rotation * ZoomOutPosition, rotation * Quaternion.Euler(ZoomOutRotation))
            : new Pose(position, rotation);

#if UNITY_EDITOR
        /// <summary>SO 직렬화 필드 이름과 유효성 검사의 단일 편집 경계입니다.</summary>
        public static CombatCameraPreviewSettings Read(ScriptableObject source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new SerializedObject(source);
            SerializedProperty pose = values.FindProperty("useZoomPose");
            SerializedProperty zoomMin = values.FindProperty("zoomMin");
            SerializedProperty zoomMax = values.FindProperty("zoomMax");
            SerializedProperty zoomOutPosition = values.FindProperty("zoomOutPositionOffset");
            SerializedProperty zoomOutRotation = values.FindProperty("zoomOutRotationOffset");
            if (pose == null || zoomMin == null || zoomMax == null || zoomOutPosition == null || zoomOutRotation == null
                || !(zoomMin.floatValue > 0f) || !(zoomMax.floatValue >= zoomMin.floatValue))
                throw new InvalidOperationException("공용 카메라 설정의 줌 자세·범위를 읽을 수 없습니다.");
            return new CombatCameraPreviewSettings(pose.boolValue, zoomMin.floatValue, zoomMax.floatValue,
                zoomOutPosition.vector3Value, zoomOutRotation.vector3Value);
        }
#endif
    }
}
