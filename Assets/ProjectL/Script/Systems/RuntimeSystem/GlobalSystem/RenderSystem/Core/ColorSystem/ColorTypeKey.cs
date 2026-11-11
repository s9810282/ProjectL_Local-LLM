using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem 
{
    public readonly struct TypeSettingKey<TContext>
        where TContext : notnull
    {
        public string Name { get; }

        internal TypeSettingKey(string name)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class MaterialBaseTypeKey
    {
        // --- ���� ��(Color, float, int ��) Ÿ�� Ű ---
        public static readonly TypeSettingKey<Color> MainColor =
            new TypeSettingKey<Color>("_BaseColor");

        public static readonly TypeSettingKey<float> Alpha =
            new TypeSettingKey<float>("_Alpha");

        public static readonly TypeSettingKey<float> Metallic =
            new TypeSettingKey<float>("_Metallic");

        public static readonly TypeSettingKey<float> Smoothness =
            new TypeSettingKey<float>("_Smoothness");

        public static readonly TypeSettingKey<float> NormalScale =
            new TypeSettingKey<float>("_NormalScale");

        public static readonly TypeSettingKey<float> OcclusionStrength =
            new TypeSettingKey<float>("_OcclusionStrength");

    }

    public static class ColorShaderPropertyKey
    {
        public static readonly TypeSettingKey<float> AreaOutlineDashEnabled =
            new TypeSettingKey<float>("_DashEnabled");

        public static readonly TypeSettingKey<float> AreaOutlineDashCount =
            new TypeSettingKey<float>("_DashCount");

        public static readonly TypeSettingKey<float> AreaOutlineDashFill =
            new TypeSettingKey<float>("_DashFill");

        public static readonly TypeSettingKey<float> AreaOutlineFlowDirection =
            new TypeSettingKey<float>("_FlowDirection");

        public static readonly TypeSettingKey<float> AreaOutlineFlowSpeed =
            new TypeSettingKey<float>("_FlowSpeed");

        /// <summary>모델 재질의 사망 디졸브 진행값입니다.</summary>
        public static readonly TypeSettingKey<float> DissolveAmount =
            new TypeSettingKey<float>("_DissolveAmount");

        public static readonly TypeSettingKey<Texture> DissolveTexture =
            new TypeSettingKey<Texture>("_DissolveTex");

        /// <summary>외곽선 재질의 색입니다. Cartoon 셰이더의 Outline Pass와 SelectOutline 셰이더가 같은 이름을 씁니다.</summary>
        public static readonly TypeSettingKey<Color> OutlineColor =
            new TypeSettingKey<Color>("_OutlineColor");

        /// <summary>Cartoon 셰이더 Outline Pass의 월드 단위 외곽선 폭입니다.</summary>
        public static readonly TypeSettingKey<float> OutlineWidth =
            new TypeSettingKey<float>("_OutlineWidth");
    }
}
