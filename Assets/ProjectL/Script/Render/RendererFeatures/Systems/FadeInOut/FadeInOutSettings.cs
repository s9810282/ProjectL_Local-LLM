using System;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Render.Shader.PostProcessing.FadeInOut
{
    public interface IFadeInOutSettingService : IServiceModule
    {
        void SetFadeValue(float value);
        void SetFadeTexture(Texture2D texture);
        void SetFadeColor(Color color);
    }

    [Serializable]
    public sealed class FadeInOutSettings : IFadeInOutSettingService
    {
        [Tooltip("0 shows the fade texture. 1 shows the scene.")]
        [Range(0f, 1f)] public float FadeValue = 1f;
        public Texture2D FadeTexture;
        [ColorUsage(true, true)] public Color FadeColor = Color.white;

        public void SetFadeValue(float value) => FadeValue = Mathf.Clamp01(value);
        public void SetFadeTexture(Texture2D texture) => FadeTexture = texture;
        public void SetFadeColor(Color color) => FadeColor = color;
    }
}
