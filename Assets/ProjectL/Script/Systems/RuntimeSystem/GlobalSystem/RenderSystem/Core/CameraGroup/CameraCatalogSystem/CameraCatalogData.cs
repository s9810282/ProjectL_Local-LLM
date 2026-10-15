using System;
using Cinemachine;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem
{
    /// <summary>Customize에 전달하는 전체 설정값. 생략한 필드를 유지하는 부분 업데이트가 아니다.</summary>
    [Serializable]
    public struct CameraCatalogData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public LensSettings Lens;
        public Transform Follow;
        public Transform LookAt;
        public int Priority;
        public bool Active;
        public bool Enabled;
        public CinemachineVirtualCameraBase.StandbyUpdateMode StandbyUpdate;
        public CinemachineVirtualCameraBase.TransitionParams Transitions;

        /// <summary>새 설정 작성 시 default(struct) 대신 사용하는 유효한 기본값.</summary>
        public static CameraCatalogData Default => new CameraCatalogData
        {
            Rotation = Quaternion.identity,
            Lens = LensSettings.Default,
            Active = true,
            Enabled = true,
            StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin
        };

        /// <summary>호출자가 가진 카메라의 현재 설정을 복사한다. 자동 복구용 상태가 아니다.</summary>
        public CameraCatalogData(CinemachineVirtualCamera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            Position = camera.transform.position;
            Rotation = camera.transform.rotation;
            Lens = camera.m_Lens;
            Follow = camera.Follow;
            LookAt = camera.LookAt;
            Priority = camera.Priority;
            Active = camera.gameObject.activeSelf;
            Enabled = camera.enabled;
            StandbyUpdate = camera.m_StandbyUpdate;
            Transitions = camera.m_Transitions;
        }
    }
}
