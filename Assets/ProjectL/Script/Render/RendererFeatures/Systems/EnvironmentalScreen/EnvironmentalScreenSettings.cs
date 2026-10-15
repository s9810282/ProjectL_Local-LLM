using System;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Render.Shader.PostProcessing.EnvironmentalScreenPost
{
    public enum EnvironmentalEffectType
    {
        None,
        Sandstorm,
        Rain,
        Darkness
    }

    [Serializable]
    public sealed class EnvironmentalLayerSettings
    {
        public Texture2D Texture;
        public Color Color = Color.white;

        [Range(0.0f, 1.0f)]
        public float Blending = 1.0f;

        [Header("Ellipse")]
        public Vector2 Radius = new Vector2(0.5f, 0.5f);

        [Min(0.0f)]
        public float EdgeSoftness = 0.02f;

        [Header("Texture")]
        [Min(0.0f)]
        public float Tiling = 1.0f;

        [Min(0.0f)]
        public float Speed = 0.0f;

        public Vector2 Direction = new Vector2(1.0f, 0.0f);
    }

    [Serializable]
    public sealed class EnvironmentalThemeSettings
    {
        [Header("Ellipse")]
        public Vector2 MaskCenter = new Vector2(0.5f, 0.5f);

        [Header("Layer 1")]
        public EnvironmentalLayerSettings Layer1 = new EnvironmentalLayerSettings();

        [Header("Layer 2")]
        public EnvironmentalLayerSettings Layer2 = new EnvironmentalLayerSettings();

        [Header("Layer 3")]
        public EnvironmentalLayerSettings Layer3 = new EnvironmentalLayerSettings();
    }

    public interface IEnvironmentalScreenSettingService : IServiceModule
    {
        void SetEffect(EnvironmentalEffectType effect);
        void SetSandstorm(EnvironmentalThemeSettings settings);
        void SetRain(EnvironmentalThemeSettings settings);
        void SetDarkness(EnvironmentalThemeSettings settings);
    }

    [Serializable]
    public sealed class EnvironmentalScreenSettings : IEnvironmentalScreenSettingService
    {
        [Header("Compute Shader")]
        public ComputeShader EnvironmentalShader;

        [Header("Effect Selection")]
        public EnvironmentalEffectType Effect = EnvironmentalEffectType.None;

        [Header("Sandstorm")]
        public EnvironmentalThemeSettings Sandstorm = new EnvironmentalThemeSettings();

        [Header("Rain")]
        public EnvironmentalThemeSettings Rain = new EnvironmentalThemeSettings();

        [Header("Darkness")]
        public EnvironmentalThemeSettings Darkness = new EnvironmentalThemeSettings();

        public void SetEffect(EnvironmentalEffectType effect) => Effect = effect;
        public void SetSandstorm(EnvironmentalThemeSettings settings) => Sandstorm = settings;
        public void SetRain(EnvironmentalThemeSettings settings) => Rain = settings;
        public void SetDarkness(EnvironmentalThemeSettings settings) => Darkness = settings;
    }
}
