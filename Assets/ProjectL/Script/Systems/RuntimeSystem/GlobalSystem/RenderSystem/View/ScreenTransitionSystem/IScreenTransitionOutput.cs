using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>진행 상태와 카메라·스텐실 출력의 변경 경계를 분리합니다.</summary>
    internal interface IScreenTransitionOutput : IDisposable
    {
        void Begin(ScreenTransitionData data);
        void Apply(float amount);
        bool TryComplete();
    }

    internal interface IScreenTransitionOutputProvider
    {
        void BindServices(ICameraCatalogService cameras, IUnityCameraCatalogService outputs,
            IRenderFeatureSystemService features);
        void Init(ScreenTransitionInitData data);
        IScreenTransitionOutput Create();
        void Shutdown();
    }
}
