using System;
using Cinemachine;
using Cinemachine.PostFX;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>SO에서 계산한 끝점과 실제 렌즈값으로 기초 자세를 적용합니다. 표시 연결이 자동으로 추가합니다. 표시 연결 전에는 동작하지 않습니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    [AddComponentMenu("ProjectL/Combat/Camera Zoom Pose")]
    public sealed class CombatCameraZoomPose : CinemachineExtension
    {
        private CombatCameraZoomPosePath _path;
        private Vector2 _offset;

        internal bool IsBound => _path != null;

        /// <summary>자세 쓰기와 충돌하는 구성을 연결 전에 검사합니다. SO에 켜진 카메라만 검사합니다.</summary>
        internal static void ValidateCamera(CinemachineVirtualCamera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (!camera.m_Lens.Orthographic)
                throw ConfigurationError(camera, "Orthographic 카메라만 지원합니다. Lens Mode Override를 Orthographic으로 설정하세요.");
            if (camera.Follow != null || camera.LookAt != null || camera.ParentCamera != null)
                throw ConfigurationError(camera, "Follow·LookAt·상위 VirtualCamera와 자세 줌을 함께 사용할 수 없습니다.");
            CinemachineComponentBase[] pipeline = camera.GetComponentPipeline();
            for (int i = 0; pipeline != null && i < pipeline.Length; i++)
            {
                CinemachineComponentBase part = pipeline[i];
                // Follow 없는 Transposer처럼 활성화됐어도 동작하지 않는 단계는 충돌하지 않습니다.
                if (part != null && part.IsValid
                    && (part.Stage == CinemachineCore.Stage.Body || part.Stage == CinemachineCore.Stage.Aim))
                    throw ConfigurationError(camera, $"실제로 동작하는 Body·Aim과 자세 줌은 함께 사용할 수 없습니다: {part.GetType().Name}");
            }
            CinemachineExtension[] extensions = camera.GetComponents<CinemachineExtension>();
            for (int i = 0; i < extensions.Length; i++)
            {
                CinemachineExtension extension = extensions[i];
                // VolumeSettings는 후처리·초점만 적용하고 raw 자세를 변경하지 않습니다.
                if (extension.enabled && !(extension is CombatCameraZoomPose)
                    && !(extension is CinemachineImpulseListener) && !(extension is CinemachineVolumeSettings))
                    throw ConfigurationError(camera, $"자세 충돌 검증이 되지 않은 카메라 확장입니다: {extension.GetType().Name}");
            }
            var pose = camera.GetComponent<CombatCameraZoomPose>();
            if (pose != null && !pose.enabled)
                throw ConfigurationError(camera, "자세 어댑터가 비활성입니다. 제거하거나 활성화하세요.");
        }

        /// <summary>표시 연결이 끝점·범위를 전달합니다. 해제 전 중복 소유는 거절합니다.</summary>
        internal void Bind(CombatCameraZoomPosePath path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsBound) throw Error("이전 표시의 자세 연결이 해제되지 않았습니다.");
            _offset = Vector2.zero;
            _path = path;
        }

        internal void SetOffset(Vector2 offset) => _offset = offset;

        /// <summary>먼저 연결을 끊어 늦게 도는 pipeline이 자세를 다시 덮지 않게 합니다.</summary>
        internal void Unbind()
        {
            _path = null;
            _offset = Vector2.zero;
        }

        /// <summary>거리 감쇠와 카메라 공간 Impulse가 현재 자세를 사용하도록 pipeline 시작에도 동일한 기초 자세를 제공합니다.</summary>
        public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam,
            ref CameraState curState, float deltaTime)
        {
            ApplyPose(vcam, ref curState);
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize)
                ApplyPose(vcam, ref state);
        }

        private void ApplyPose(CinemachineVirtualCameraBase vcam, ref CameraState state)
        {
            if (_path == null || vcam != VirtualCamera)
                return;
            Pose pose = _path.Evaluate(state.Lens.OrthographicSize, _offset);
            state.RawPosition = pose.position;
            state.RawOrientation = pose.rotation;
            // ImpulseListener의 위치·회전 correction은 보존합니다.
        }

        private static InvalidOperationException ConfigurationError(CinemachineVirtualCamera camera, string message) =>
            new InvalidOperationException($"[CombatCameraZoomPose] '{camera.name}': {message}");

        private InvalidOperationException Error(string message) =>
            new InvalidOperationException($"[CombatCameraZoomPose] '{name}': {message}");
    }
}
