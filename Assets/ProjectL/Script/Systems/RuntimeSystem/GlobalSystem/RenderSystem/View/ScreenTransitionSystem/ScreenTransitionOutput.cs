using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>Scop과 함께 재사용하며 카메라 Handle과 스텐실 Scope를 소유합니다.</summary>
    internal sealed class ScreenTransitionOutput : IScreenTransitionOutput
    {
        private readonly ScreenTransitionOutputProvider _provider;
        private IUnityCameraCatalogHandle _main;
        private ICameraCatalogHandle _virtual;
        private IRenderFeatureScop _feature;
        private IStencilTransitionSettingService _stencil;
        private UnityCameraRenderState _mainState;
        private StencilTransitionState _stencilState;
        private int _originalPriority;
        private int _finalMask;
        private ulong _finalPresentation;
        private bool _hasMainState;
        private bool _hasStencilState;
        private bool _hasPriority;
        private bool _waitingForPresentation;
        private bool _committed;

        internal ScreenTransitionOutput(ScreenTransitionOutputProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public void Begin(ScreenTransitionData data)
        {
            ScreenTransitionInitData settings = _provider.Settings;
            _provider.Acquire(this);
            try
            {
                _main = _provider.Outputs.GetHandle(settings.MainCamera);
                _mainState = _main.Capture();
                _mainState.CullingMask |= settings.FixedLayer.value;
                _hasMainState = true;
                if (!_mainState.Enabled)
                    throw new InvalidOperationException("Main 카메라가 활성화되어 있어야 합니다.");

                if (data.TargetVirtureCamera != null)
                {
                    _virtual = _provider.Cameras.GetHandle(data.TargetVirtureCamera);
                    _originalPriority = _virtual.Priority;
                    _hasPriority = true;
                }
                _feature = _provider.Features.CreateScop();
                _stencil = _feature.UseRenderFeature(StencilTransitionFeature.Key);
                if (!_stencil.IsReady)
                    throw new InvalidOperationException("스텐실의 TargetRenderTexture·ComputeShader 설정을 확인해야 합니다.");
                _stencilState = _stencil.Capture();
                _hasStencilState = true;
                _finalMask = settings.FixedLayer.value | data.Layer.value;

                _main.SetCullingMask(_mainState.CullingMask);
                _stencil.SetOutputCameraName(settings.MainCamera);
                _stencil.SetCircleCenter(settings.CircleCenter);
                _stencil.SetTransitionWidth(settings.TransitionWidth);
                _stencil.SetLineColor(data.StencileMaskeColor);
                _stencil.SetCircleRadius(0f);
                _virtual?.RaisePriority();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Apply(float amount) => _stencil.SetCircleRadius(amount);

        public bool TryComplete()
        {
            if (!_waitingForPresentation)
            {
                _finalPresentation = _stencil.PresentationVersion;
                _waitingForPresentation = true;
                return false;
            }
            if (_stencil.PresentationVersion == _finalPresentation)
                return false;

            _main.SetCullingMask(_finalMask);
            _committed = true;
            RestoreStencil();
            _feature.ReleaseRenderFeature(StencilTransitionFeature.Key);
            return true;
        }

        public void Dispose()
        {
            try
            {
                if (!_committed)
                {
                    try
                    {
                        if (_hasMainState)
                            _main.Restore(_mainState);
                    }
                    finally
                    {
                        if (_hasPriority)
                            _virtual.SetPriority(_originalPriority);
                    }
                }
            }
            finally
            {
                try
                {
                    RestoreStencil();
                }
                finally
                {
                    try
                    {
                        _feature?.Dispose();
                    }
                    finally
                    {
                        try
                        {
                            _virtual?.Dispose();
                        }
                        finally
                        {
                            try
                            {
                                _main?.Dispose();
                            }
                            finally
                            {
                                Clear();
                            }
                        }
                    }
                }
            }
        }

        private void RestoreStencil()
        {
            if (!_hasStencilState)
                return;
            _stencil.Restore(_stencilState);
            _hasStencilState = false;
        }

        private void Clear()
        {
            _main = null;
            _virtual = null;
            _feature = null;
            _stencil = null;
            _mainState = default;
            _stencilState = default;
            _hasMainState = false;
            _hasPriority = false;
            _hasStencilState = false;
            _waitingForPresentation = false;
            _committed = false;
            _originalPriority = 0;
            _finalMask = 0;
            _finalPresentation = 0;
            _provider.Release(this);
        }
    }
}
