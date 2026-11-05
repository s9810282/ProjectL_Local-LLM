using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Render.RendererFeatures.Systems.Stencil;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;

namespace ProjectL.Render.Shader.PostProcessing.Stencil
{
    public interface IStencilSettingService : IServiceModule
    {
        void SetCircleCenter(Vector2 center);
        void SetCircleRadius(float radius);
        void SetTransitionWidth(float width);
        void SetLineKey(LineKey key);
    }

    [Serializable]
    public sealed class StencilSettings : IStencilSettingService, IStencilTransitionSettingService
    {
        public RenderTexture TargetRenderTexture;
        [Tooltip("World XZ center of the circular boundary.")]
        public Vector2 CircleCenter;
        [Tooltip("Radius of the circular boundary in world units. The target render spreads outward as this grows.")]
        [Min(0f)] public float CircleRadius = 5f;
        [Tooltip("Maximum noisy transition width extending outward from CircleRadius, in world units.")]
        [Min(0f)] public float TargetRenderTextureThickness;
        [SerializeField, Tooltip("Renderer Feature의 Line Presets에 등록한 챕터 키를 선택한다.")]
        private LineKey lineKey = LineKey.Capter1;
        [Tooltip("Requires a CSMain kernel.")]
        public ComputeShader BlendingShader;

        public LineKey LineKey => lineKey;

        [NonSerialized] private bool _hasLineColor;
        [NonSerialized] private Color _lineColor;
        [NonSerialized] private string _outputCameraName;
        [NonSerialized] private ulong _presentationVersion;

        RenderTexture IStencilTransitionSettingService.TargetRenderTexture => TargetRenderTexture;
        public string OutputCameraName => _outputCameraName;
        public bool IsReady => TargetRenderTexture != null && BlendingShader != null &&
            SystemInfo.supportsComputeShaders && BlendingShader.HasKernel("CSMain");
        public ulong PresentationVersion => _presentationVersion;
        internal void NotifyPresented() => _presentationVersion++;

        public Color ResolveLineColor(Color presetColor) => _hasLineColor ? _lineColor : presetColor;

        public StencilTransitionState Capture() => new()
        {
            Center = CircleCenter,
            Radius = CircleRadius,
            TransitionWidth = TargetRenderTextureThickness,
            HasLineColor = _hasLineColor,
            LineColor = _lineColor,
            OutputCameraName = _outputCameraName
        };

        public void Restore(StencilTransitionState state)
        {
            CircleCenter = state.Center;
            CircleRadius = state.Radius;
            TargetRenderTextureThickness = state.TransitionWidth;
            _hasLineColor = state.HasLineColor;
            _lineColor = state.LineColor;
            _outputCameraName = state.OutputCameraName;
        }

        public void SetLineColor(Color color)
        {
            _lineColor = color;
            _hasLineColor = true;
        }

        public void SetOutputCameraName(string objectName) => _outputCameraName = objectName;

        public void SetCircleCenter(Vector2 center) => CircleCenter = center;
        public void SetCircleRadius(float radius) => CircleRadius = Mathf.Max(0f, radius);
        public void SetTransitionWidth(float width) => TargetRenderTextureThickness = Mathf.Max(0f, width);
        public void SetLineKey(LineKey key)
        {
            if (!System.Enum.IsDefined(typeof(LineKey), key))
            {
                throw new ArgumentOutOfRangeException(nameof(key), key, "정의되지 않은 경계선 프리셋 키입니다.");
            }

            lineKey = key;
        }
    }
}
