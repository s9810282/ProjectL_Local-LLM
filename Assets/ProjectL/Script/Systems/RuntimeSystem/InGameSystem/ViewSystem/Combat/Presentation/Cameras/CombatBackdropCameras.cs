using System;
using Cinemachine;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>소유한 배경 인스턴스의 옛 카메라만 차단합니다. 원본 에셋과 배경 GameObject의 활성 상태는 바꾸지 않습니다.</summary>
    internal static class CombatBackdropCameras
    {
        /// <summary>카탈로그 수집 전에 선택·흔들림 수신을 끄고, 비활성 수집에서도 충돌하지 않는 이름을 붙입니다.</summary>
        internal static void Suppress(Transform backdrop)
        {
            if (backdrop == null)
                throw new ArgumentNullException(nameof(backdrop));
            CinemachineVirtualCameraBase[] cameras = backdrop.GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
            int instance = backdrop.GetInstanceID();
            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineVirtualCameraBase camera = cameras[i];
                camera.enabled = false;
                camera.gameObject.name = $"_CombatBackdrop_{instance}_{i}_{camera.gameObject.name}";
            }
            CinemachineImpulseListener[] listeners = backdrop.GetComponentsInChildren<CinemachineImpulseListener>(true);
            for (int i = 0; i < listeners.Length; i++)
                listeners[i].enabled = false;
        }
    }
}
