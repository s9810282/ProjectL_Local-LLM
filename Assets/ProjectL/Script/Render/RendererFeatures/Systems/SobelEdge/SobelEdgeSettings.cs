using System;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Render.Shader.PostProcessing.SovelEdge
{
    public interface ISobelEdgeSettingService : IServiceModule
    {
        void SetEdgeIntensity(float intensity);
        void SetDepthThreshold(float threshold);
        void SetNormalThreshold(float threshold);
        void SetDepthSensitivity(float sensitivity);
        void SetZBuffer(float zBuffer);
        void SetColor(Color color);
    }

    [Serializable]
    public sealed class SobelEdgeSettings : ISobelEdgeSettingService
    {
        [Range(0.0f, 5.0f)]
        public float EdgeIntensity = 1.0f;

        [Range(0.0f, 1.0f)]
        public float DethThreshold = 0.1f;

        [Range(0.0f, 1.0f)]
        public float NormalThreshold = 0.1f;

        [Range(1.0f, 1000.0f)]
        public float DepthSensitivity = 100.0f;

        [Range(10.0f, 100.0f)]
        public float ZBuffer;

        public Color Color;

        public void SetEdgeIntensity(float intensity) => EdgeIntensity = intensity;
        public void SetDepthThreshold(float threshold) => DethThreshold = threshold;
        public void SetNormalThreshold(float threshold) => NormalThreshold = threshold;
        public void SetDepthSensitivity(float sensitivity) => DepthSensitivity = sensitivity;
        public void SetZBuffer(float zBuffer) => ZBuffer = zBuffer;
        public void SetColor(Color color) => Color = color;
    }
}
