using System;
using Cinemachine;
using UnityEngine;

namespace ProjectL.Tool.Runtime
{
#if UNITY_EDITOR
    /// <summary>편집 씬에서 공용 카메라의 시작 렌즈·자세만 상태에 투영합니다. 프리팹 Transform과 렌즈를 쓰지 않습니다.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public sealed class CombatMapAuthoringCameraPose : CinemachineExtension
    {
        [SerializeField, HideInInspector] private ScriptableObject settings;
        private CombatCameraPreviewSettings _preview;
        private float _initialSize;
        private bool _attempted;
        private bool _ready;
        private string _problem;

        public string Problem => _problem;
        public ScriptableObject Settings => settings;

        public void Configure(ScriptableObject source)
        {
            settings = source;
            Refresh();
        }

        public void Refresh()
        {
            _attempted = true;
            _ready = false;
            _problem = null;
            try
            {
                var camera = GetComponent<CinemachineVirtualCamera>();
                if (camera == null || !camera.m_Lens.Orthographic || !(camera.m_Lens.OrthographicSize > 0f))
                    throw new InvalidOperationException("편집 씬의 공용 카메라는 양의 크기를 가진 직교 시점이어야 합니다.");
                _preview = CombatCameraPreviewSettings.Read(settings);
                _initialSize = _preview.InitialSize(camera.m_Lens.OrthographicSize);
                _ready = true;
            }
            catch (Exception exception)
            {
                _problem = exception.Message;
            }
        }

        public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime)
        {
            Apply(vcam, ref state);
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize)
                Apply(vcam, ref state);
        }

        private void Apply(CinemachineVirtualCameraBase vcam, ref CameraState state)
        {
            if (!_attempted) Refresh();
            if (!_ready || vcam != VirtualCamera) return;
            Pose pose = _preview.StartPoseAt(vcam.transform.position, vcam.transform.rotation);
            state.RawPosition = pose.position;
            state.RawOrientation = pose.rotation;
            LensSettings lens = state.Lens;
            lens.OrthographicSize = _initialSize;
            state.Lens = lens;
        }
    }
#endif
}
