using System;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Render.Shader.PostProcessing.Stencil
{
    public interface IStencilSettingService : IServiceModule
    {
        void SetCircleCenter(Vector2 center);
        void SetCircleRadius(float radius);
        void SetTransitionWidth(float width);
        void SetBorderWidth(float width);
        void SetBorderColor(Color color);
    }

    [Serializable]
    public sealed class StencilSettings : IStencilSettingService
    {
        public RenderTexture TargetRenderTexture;
        [Tooltip("World XZ center of the circular boundary.")]
        public Vector2 CircleCenter;
        [Tooltip("Radius of the circular boundary in world units. The target render spreads outward as this grows.")]
        [Min(0f)] public float CircleRadius = 5f;
        [Tooltip("Maximum noisy transition width extending outward from CircleRadius, in world units.")]
        [Min(0f)] public float TargetRenderTextureThickness;
        [Min(0f)] public float LineThickness = 0.1f;
        [ColorUsage(true, true)] public Color LineColor = Color.white;
        public Texture2D LineNoiseTexture;
        [Tooltip("Polar noise tiling: X = repeats around the circle (rounded to a whole count, minimum 1), Y = repeats per radial world unit. Magnitudes are used.")]
        public Vector2 LineNoiseTiling = Vector2.one;
        [Tooltip("Radial noise speed in texture units per second. Positive flows outward from CircleCenter; negative flows inward; zero pauses.")]
        public float LineNoiseSpeed = 0.2f;
        [Tooltip("Requires a CSMain kernel.")]
        public ComputeShader BlendingShader;

        public void SetCircleCenter(Vector2 center) => CircleCenter = center;
        public void SetCircleRadius(float radius) => CircleRadius = Mathf.Max(0f, radius);
        public void SetTransitionWidth(float width) => TargetRenderTextureThickness = Mathf.Max(0f, width);
        public void SetBorderWidth(float width) => LineThickness = Mathf.Max(0f, width);
        public void SetBorderColor(Color color) => LineColor = color;
    }
}
