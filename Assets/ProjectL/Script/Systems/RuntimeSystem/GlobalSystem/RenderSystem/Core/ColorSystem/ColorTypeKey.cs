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
    }
}
