using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem
{
    /// <summary>화면 전환이 사용하는 스텐실 설정의 저장·복원 값입니다.</summary>
    public struct StencilTransitionState
    {
        public Vector2 Center;
        public float Radius;
        public float TransitionWidth;
        public bool HasLineColor;
        public Color LineColor;
        public string OutputCameraName;
    }

    /// <summary>Systems에서 Render의 구체 구현을 참조하지 않고 스텐실을 제어하는 계약입니다.</summary>
    public interface IStencilTransitionSettingService : IServiceModule
    {
        bool IsReady { get; }
        ulong PresentationVersion { get; }
        RenderTexture TargetRenderTexture { get; }
        StencilTransitionState Capture();
        void Restore(StencilTransitionState state);
        void SetCircleCenter(Vector2 center);
        void SetCircleRadius(float radius);
        void SetTransitionWidth(float width);
        void SetLineColor(Color color);
        void SetOutputCameraName(string objectName);
    }

    public static class StencilTransitionFeature
    {
        public static readonly RenderFeatureKey<IStencilTransitionSettingService> Key =
            new(RenderFeatureType.StencilBlending);
    }
}
