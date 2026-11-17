using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>실제 렌즈값으로 두 끝점의 자세를 계산합니다. 입력·시간·Cinemachine을 참조하지 않습니다.</summary>
    internal sealed class CombatCameraZoomPosePath
    {
        private readonly Pose _zoomOut;
        private readonly Pose _zoomIn;
        private readonly Pose _reference;
        private readonly float _minSize;
        private readonly float _maxSize;

        internal CombatCameraZoomPosePath(Pose zoomOut, Pose zoomIn, float minSize, float maxSize)
        {
            ValidatePose(zoomOut, nameof(zoomOut));
            ValidatePose(zoomIn, nameof(zoomIn));
            if (!IsFinite(minSize) || !IsFinite(maxSize) || !(minSize > 0f) || !(maxSize > minSize))
                throw new ArgumentException("[CombatCameraZoomPose] 자세 줌은 0 < ZoomMin < ZoomMax여야 합니다.");
            _zoomOut = zoomOut;
            _zoomIn = zoomIn;
            _reference = zoomOut;
            _minSize = minSize;
            _maxSize = maxSize;
        }

        private CombatCameraZoomPosePath(Pose zoomOut, Pose zoomIn, float minSize, float maxSize, Pose reference)
            : this(zoomOut, zoomIn, minSize, maxSize)
        {
            ValidatePose(reference, nameof(reference));
            _reference = reference;
        }

        /// <summary>SO의 오프셋을 원래 카메라 구도에 적용합니다. 양 끝점 모두 같은 원래 구도를 기준으로 합니다.</summary>
        internal static CombatCameraZoomPosePath FromOffsets(Pose reference,
            Vector3 outPosition, Vector3 outRotation, Vector3 inPosition, Vector3 inRotation,
            float minSize, float maxSize)
        {
            ValidatePose(reference, nameof(reference));
            Pose zoomOut = new Pose(reference.position + reference.rotation * outPosition,
                reference.rotation * Quaternion.Euler(outRotation));
            Pose zoomIn = new Pose(reference.position + reference.rotation * inPosition,
                reference.rotation * Quaternion.Euler(inRotation));
            return new CombatCameraZoomPosePath(zoomOut, zoomIn, minSize, maxSize, reference);
        }

        /// <summary>실제 크기에서 진행률을 구합니다. 연출이 범위를 벗어나도 자세는 끝점에 고정합니다.</summary>
        internal float Progress(float actualSize)
        {
            if (!IsFinite(actualSize) || !(actualSize > 0f))
                throw new ArgumentOutOfRangeException(nameof(actualSize), "카메라 크기는 유한한 양수여야 합니다.");
            return Mathf.Clamp01((_maxSize - actualSize) / (_maxSize - _minSize));
        }

        /// <summary>직선 위치·최단 회전을 보간하고 줌아웃 화면 축의 팬을 더합니다. 누적 상태가 없습니다.</summary>
        internal Pose Evaluate(float actualSize, Vector2 offset)
        {
            if (!IsFinite(offset.x) || !IsFinite(offset.y))
                throw new ArgumentException("카메라 팬 오프셋은 유한해야 합니다.", nameof(offset));
            float t = Progress(actualSize);
            Vector3 pan = _zoomOut.rotation * new Vector3(offset.x, offset.y, 0f);
            return new Pose(Vector3.Lerp(_zoomOut.position, _zoomIn.position, t) + pan,
                Quaternion.Slerp(_zoomOut.rotation, _zoomIn.rotation, t));
        }

        /// <summary>처음 기준에서 계산한 자세를 현재 카메라 Transform의 이동·회전만큼 옮깁니다. 경로의 원본 끝점은 바꾸지 않습니다.</summary>
        internal Pose EvaluateAtReference(float actualSize, Vector2 offset, Pose currentReference)
        {
            ValidatePose(currentReference, nameof(currentReference));
            Pose pose = Evaluate(actualSize, offset);
            Quaternion rotationDelta = currentReference.rotation * Quaternion.Inverse(_reference.rotation);
            return new Pose(
                currentReference.position + rotationDelta * (pose.position - _reference.position),
                rotationDelta * pose.rotation);
        }

        /// <summary>
        /// 지금 기준에서 줌아웃 끝 자세(이동 포함)입니다. 줌 정도와 무관한 회전 중심을 이 자세의 화면 가운데 바닥 점으로 잡습니다(카메라 회전 계획 TP-5).
        /// </summary>
        internal Pose ZoomOutAt(Vector2 offset, Pose currentReference) => EvaluateAtReference(_maxSize, offset, currentReference);

        private static void ValidatePose(Pose pose, string name)
        {
            Vector3 p = pose.position;
            Quaternion q = pose.rotation;
            float length = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            if (!IsFinite(p.x) || !IsFinite(p.y) || !IsFinite(p.z)
                || !IsFinite(length) || Math.Abs(length - 1f) > 0.001f)
                throw new ArgumentException("[CombatCameraZoomPose] 위치는 유한하고 회전은 정규화된 Quaternion이어야 합니다.", name);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
