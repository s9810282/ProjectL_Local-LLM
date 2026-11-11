using System;
using Cinemachine;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>씬이 소유하는 전투 시점과 공용 조작 설정의 명시적인 연결입니다. 입력·효과·연결 상태는 소유하지 않습니다.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProjectL/Combat/Scene Camera Binding")]
    public sealed class CombatSceneCameraBinding : MonoBehaviour
    {
        [Tooltip("이 바인딩과 같은 객체 또는 자식에 있는 전투용 가상 카메라입니다. 배경 인스턴스 밖의 안정된 씬 루트에 배치합니다.")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [Tooltip("이동·줌·줌 자세에 사용하는 공용 설정입니다.")]
        [SerializeField] private CombatCameraSettingsSO settings;

        internal string CameraName => virtualCamera != null ? virtualCamera.gameObject.name
            : throw ConfigurationError("virtualCamera 참조가 비어 있습니다.");

        /// <summary>참조와 구성을 검사한 뒤, 이번 연결이 사용할 새 불변 설정을 만듭니다.</summary>
        internal CombatCameraSettingsData ReadSettings()
        {
            ValidateReferences();
            return settings.CreateData();
        }

        /// <summary>검사가 끝난 카메라의 연결 전 사실을 읽습니다. 기준값은 호출자가 한 번만 보관합니다.</summary>
        internal CombatCameraSnapshot Capture()
        {
            ValidateReferences();
            return CombatCameraSnapshot.Capture(virtualCamera);
        }

        /// <summary>씬 소유·활성 상태·투영·자세 writer·흔들림 수신 조건을 변경 전에 확인합니다.</summary>
        internal void ValidateReferences()
        {
            if (virtualCamera == null)
                throw ConfigurationError("virtualCamera 참조가 비어 있습니다.");
            if (settings == null)
                throw ConfigurationError("settings 참조가 비어 있습니다.");
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded
                || virtualCamera.gameObject.scene != gameObject.scene
                || !virtualCamera.transform.IsChildOf(transform))
                throw ConfigurationError("가상 카메라는 같은 로드된 씬의 이 바인딩 또는 자식에 있어야 합니다.");
            if (!isActiveAndEnabled || !virtualCamera.isActiveAndEnabled)
                throw ConfigurationError("바인딩과 가상 카메라는 활성 상태여야 합니다.");
            if (string.IsNullOrWhiteSpace(virtualCamera.gameObject.name))
                throw ConfigurationError("가상 카메라 이름이 비어 있습니다.");
            if (virtualCamera.m_Lens.ModeOverride != LensSettings.OverrideModes.Orthographic)
                throw ConfigurationError("가상 카메라 Lens Mode Override를 Orthographic으로 명시해야 합니다.");

            CombatCameraZoomPose.ValidateCamera(virtualCamera);
            var pose = virtualCamera.GetComponent<CombatCameraZoomPose>();
            if (pose != null && pose.IsBound)
                throw ConfigurationError("이전 표시의 자세 연결이 남아 있습니다.");
            var listener = virtualCamera.GetComponent<CinemachineImpulseListener>();
            if (listener == null || !listener.isActiveAndEnabled || listener.m_ChannelMask == 0)
                throw ConfigurationError("활성 ImpulseListener와 0이 아닌 수신 채널이 필요합니다.");
        }

        private InvalidOperationException ConfigurationError(string message) =>
            new InvalidOperationException($"[CombatSceneCameraBinding] '{name}': {message}");
    }
}
