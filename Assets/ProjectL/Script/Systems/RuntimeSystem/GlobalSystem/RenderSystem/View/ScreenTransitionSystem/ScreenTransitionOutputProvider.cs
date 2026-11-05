using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>Init 설정과 하나인 메인 출력의 사용 소유권을 보관합니다.</summary>
    internal sealed class ScreenTransitionOutputProvider : IScreenTransitionOutputProvider
    {
        private ScreenTransitionInitData _settings;
        private ScreenTransitionOutput _owner;
        private bool _isInitialized;

        internal ICameraCatalogService Cameras { get; private set; }
        internal IUnityCameraCatalogService Outputs { get; private set; }
        internal IRenderFeatureSystemService Features { get; private set; }

        internal ScreenTransitionInitData Settings
        {
            get
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("화면 전환 시스템의 초기화가 완료되지 않았습니다.");
                return _settings;
            }
        }

        public void BindServices(ICameraCatalogService cameras, IUnityCameraCatalogService outputs,
            IRenderFeatureSystemService features)
        {
            Cameras = cameras ?? throw new ArgumentNullException(nameof(cameras));
            Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
            Features = features ?? throw new ArgumentNullException(nameof(features));
        }

        public void Init(ScreenTransitionInitData data)
        {
            if (_isInitialized)
                throw new InvalidOperationException("화면 전환 Init은 한 번만 호출합니다.");
            if (Cameras == null || Outputs == null || Features == null)
                throw new InvalidOperationException("화면 전환 서비스를 먼저 연결해야 합니다.");
            if (string.IsNullOrWhiteSpace(data.MainCamera))
                throw new ArgumentException("Main 카메라 이름이 필요합니다.", nameof(data));
            if (!IsFinite(data.CircleCenter.x) || !IsFinite(data.CircleCenter.y) ||
                !IsFinite(data.TransitionWidth) || data.TransitionWidth < 0f)
                throw new ArgumentException("원 중심은 유한해야 하고 경계 폭은 유한한 0 이상이어야 합니다.", nameof(data));

            _settings = data;
            _isInitialized = true;
        }

        public IScreenTransitionOutput Create() => new ScreenTransitionOutput(this);

        internal void Acquire(ScreenTransitionOutput output)
        {
            if (_owner != null && !ReferenceEquals(_owner, output))
                throw new InvalidOperationException("다른 Scop이 메인 화면을 사용 중입니다. 먼저 Dispose해야 합니다.");
            _owner = output;
        }

        internal void Release(ScreenTransitionOutput output)
        {
            if (ReferenceEquals(_owner, output))
                _owner = null;
        }

        public void Shutdown()
        {
            _owner = null;
            _isInitialized = false;
            _settings = default;
            Cameras = null;
            Outputs = null;
            Features = null;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
