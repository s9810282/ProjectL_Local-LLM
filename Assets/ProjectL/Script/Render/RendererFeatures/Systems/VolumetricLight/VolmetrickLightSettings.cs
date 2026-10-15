using System;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Render.Shader.PostProcessing.VolmetrickLight
{
    public interface IVolmetrickLightSettingService : IServiceModule
    {
        void SetLightingRange(float range);
        void SetLightingSteps(int steps);
        void SetLightingIntensity(float intensity);
        void SetLightingIntensityMax(float intensity);
        void SetShadowSteps(int steps);
        void SetShadowIntensity(float intensity);
        void SetShadowIntensityMax(float intensity);
        void SetShadowsColor(Color color);
    }

    [Serializable]
    public sealed class VolmetrickLightSettings : IVolmetrickLightSettingService
    {
        public float LightingRange = 10.0f;
        public int LightingSteps = 100;
        public float LightingIntensity = 1.0f;
        public float LightingIntensityMax = 2f;
        public int ShadowSteps = 100;
        public float ShadowIntensity = 1.0f;
        public float ShadowIntensityMax = -2f;
        public Color ShadowsColor = Color.black;

        public void SetLightingRange(float range) => LightingRange = range;
        public void SetLightingSteps(int steps) => LightingSteps = steps;
        public void SetLightingIntensity(float intensity) => LightingIntensity = intensity;
        public void SetLightingIntensityMax(float intensity) => LightingIntensityMax = intensity;
        public void SetShadowSteps(int steps) => ShadowSteps = steps;
        public void SetShadowIntensity(float intensity) => ShadowIntensity = intensity;
        public void SetShadowIntensityMax(float intensity) => ShadowIntensityMax = intensity;
        public void SetShadowsColor(Color color) => ShadowsColor = color;
    }
}
