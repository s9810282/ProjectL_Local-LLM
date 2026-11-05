using System;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    internal static class ShaderComposerCatalog
    {
        internal const int MaximumLayers = 8;
        internal const int MaximumLightingLayers = 4;

        private static ShaderComposerParameter Range(string key, string label, float value, float min, float max, bool integer = false)
        {
            return new ShaderComposerParameter(key, label, ShaderComposerParameterKind.Range)
            {
                DefaultNumber = value, Minimum = min, Maximum = max, Integer = integer
            };
        }

        private static ShaderComposerParameter Number(string key, string label, float value)
        {
            return new ShaderComposerParameter(key, label, ShaderComposerParameterKind.Number) { DefaultNumber = value };
        }

        private static ShaderComposerParameter Colour(string key, string label, Color value, bool hdr = false)
        {
            return new ShaderComposerParameter(key, label, ShaderComposerParameterKind.Color) { DefaultColor = value, Hdr = hdr };
        }

        private static ShaderComposerParameter Texture(string key, string label)
        {
            return new ShaderComposerParameter(key, label, ShaderComposerParameterKind.Texture);
        }

        private static ShaderComposerParameter Vector(string key, string label, Vector4 value, bool uv = false)
        {
            return new ShaderComposerParameter(key, label, uv ? ShaderComposerParameterKind.UV : ShaderComposerParameterKind.Vector2) { DefaultVector = value };
        }

        private static ShaderComposerParameter Choice(string key, string label, int value, params string[] choices)
        {
            return new ShaderComposerParameter(key, label, ShaderComposerParameterKind.Choice) { DefaultNumber = value, Choices = choices };
        }

        private static ShaderComposerParameter Invert()
        {
            return new ShaderComposerParameter("Invert", "반전", ShaderComposerParameterKind.Toggle);
        }

        internal static readonly ShaderComposerModule Base = new ShaderComposerModule("base", "기본 표면",
            new[]
            {
                Colour("BaseColor", "기본색", Color.white), Texture("BaseMap", "기본 텍스처"),
                Vector("BaseUV", "텍스처 타일링/이동", new Vector4(1, 1, 0, 0), true)
            }, "");

        internal static readonly ShaderComposerModule LightingBlend = new ShaderComposerModule("blend", "모델 혼합",
            new[] { Range("Weight", "혼합 가중치", 1, 0, 1) }, "");

        internal static readonly ShaderComposerModule[] Models =
        {
            new ShaderComposerModule("lit", "Lit (PBR)",
                new[] { Range("Metallic", "금속성", 0, 0, 1), Range("Smoothness", "매끄러움", 0.5f, 0, 1) },
                "return ProjectLComposerLit(context, albedo, @Metallic@, @Smoothness@);"),
            new ShaderComposerModule("pbrStep", "PBR Step (결과 밝기 단계)",
                new[]
                {
                    Range("Metallic", "금속성", 0, 0, 1), Range("Smoothness", "매끄러움", 0.5f, 0, 1),
                    Range("Steps", "밝기 단계 수", 6, 2, 20, true), Range("BrightnessRange", "기준 밝기 범위", 1, 0.1f, 8)
                },
                "return ProjectLComposerPbrStep(context, albedo, @Metallic@, @Smoothness@, @Steps@, @BrightnessRange@);"),
            new ShaderComposerModule("toon", "Toon (단계 명암)",
                new[]
                {
                    Range("Steps", "명암 단계", 4, 2, 20, true), Range("Dark", "어두운 밝기", 0.05f, 0, 1),
                    Range("Softness", "경계 부드러움", 0, 0, 0.1f), Range("Multiplier", "노멀 라이팅 배율", 1, 0, 8),
                    Range("Ambient", "환경광 강도", 0.15f, 0, 1), Range("Metallic", "금속성", 0, 0, 1),
                    Range("Smoothness", "매끄러움", 0.5f, 0, 1), Colour("SpecularColor", "반사광 색", Color.white, true),
                    Range("SpecularIntensity", "반사광 강도", 0, 0, 10)
                },
                "return ProjectLComposerToon(context, albedo, @Metallic@, @Smoothness@, @SpecularColor@.rgb, @SpecularIntensity@, @Steps@, @Dark@, @Softness@, @Multiplier@, @Ambient@);"),
            new ShaderComposerModule("nearestStep", "Step Lit (인접 조명색)",
                new[] { Range("Steps", "조명 표본 수", 6, 2, 20, true), Range("Ambient", "환경광 강도", 0.15f, 0, 1) },
                "return ProjectLComposerNearestStep(context, albedo, @Steps@, @Ambient@);"),
            new ShaderComposerModule("unlit", "Unlit (무조명)", Array.Empty<ShaderComposerParameter>(), "return albedo;"),
            new ShaderComposerModule("halfLambert", "Half Lambert (하프램버트)",
                new[] { Range("Power", "명암 곡선", 2, 0.1f, 8), Range("Ambient", "환경광 강도", 0.15f, 0, 1) },
                "return ProjectLComposerHalfLambert(context, albedo, @Power@, @Ambient@);"),
            new ShaderComposerModule("depthHalfLambert", "Half Lambert Depth Palette (깊이 보정 팔레트)",
                new[]
                {
                    Range("Power", "명암 곡선", 2, 0.1f, 8), Range("Ambient", "환경광 강도", 0.1f, 0, 1),
                    Range("NormalSmoothness", "노멀 스무스", 0, 0, 1),
                    Range("Steps", "팔레트 단계 수", 5, 2, 20, true), Range("DepthRadius", "주변 깊이 반경 (픽셀)", 4, 1, 16),
                    Range("DepthStrength", "깊이 보정 강도", 0.5f, 0, 2), Range("DepthLimit", "최대 비교 거리 (월드)", 0.25f, 0.01f, 4),
                    Colour("ShadeColor", "0 / 어두운 색", new Color(0.13f, 0.025f, 0.012f)),
                    Colour("LowerColor", "0.25 / 어두운 중간 색", new Color(0.4f, 0.07f, 0.02f)),
                    Colour("MiddleColor", "0.5 / 중간 색", new Color(0.75f, 0.2f, 0.045f)),
                    Colour("UpperColor", "0.75 / 밝은 중간 색", new Color(1, 0.45f, 0.1f)),
                    Colour("LightColor", "1 / 밝은 색", new Color(1, 0.75f, 0.36f))
                },
                "return ProjectLComposerDepthHalfLambert(context, albedo, @Power@, @Ambient@, @Steps@, @DepthRadius@, @DepthStrength@, @DepthLimit@, @ShadeColor@.rgb, @LowerColor@.rgb, @MiddleColor@.rgb, @UpperColor@.rgb, @LightColor@.rgb, @NormalSmoothness@);",
                requiresSceneDepth: true),
            new ShaderComposerModule("baseLit", "Base Lit (기존 베이스)",
                new[]
                {
                    Range("Steps", "명암 단계", 2, 2, 6, true), Range("Dark", "어두운 밝기", 0.05f, 0, 1),
                    Range("Softness", "경계 부드러움", 0, 0, 0.1f),
                    new ShaderComposerParameter("FlatNormals", "평면 명암 노멀", ShaderComposerParameterKind.Toggle) { DefaultNumber = 1 },
                    Range("Ambient", "환경광 강도", 0.15f, 0, 1), Range("Metallic", "금속성", 0, 0, 1),
                    Range("Smoothness", "매끄러움", 0.5f, 0, 1), Colour("SpecularColor", "반사광 색", Color.white, true),
                    Range("SpecularIntensity", "반사광 강도", 1, 0, 10), Colour("RimColor", "직접광 림 색", Color.white, true),
                    Range("RimIntensity", "직접광 림 강도", 1, 0, 5), Range("RimPower", "직접광 림 폭", 4, 0.5f, 8)
                },
                "return ProjectLComposerBaseLit(context, albedo, @Metallic@, @Smoothness@, @SpecularColor@.rgb, @SpecularIntensity@, @Steps@, @Dark@, @Softness@, @FlatNormals@, @Ambient@, @RimColor@.rgb, @RimIntensity@, @RimPower@);")
        };

        internal static readonly ShaderComposerModule[] Effects =
        {
            new ShaderComposerModule("tint", "색상 곱하기",
                new[] { Colour("Color", "곱할 색", Color.white), Range("Strength", "강도", 1, 0, 1) },
                "color *= lerp(1.0.xxx, @Color@.rgb, saturate(mask.x * @Strength@));",
                "float3 tint = lerp(1.0.xxx, @Color@.rgb, saturate(mask.x * @Strength@)); albedo *= tint; emission *= tint;"),
            new ShaderComposerModule("emission", "발광",
                new[] { Colour("Color", "발광 색", new Color(0, 0.6f, 1, 1), true), Range("Intensity", "발광 강도", 1, 0, 20) },
                "color += @Color@.rgb * @Intensity@ * mask.x;",
                "emission += @Color@.rgb * @Intensity@ * mask.x;"),
            new ShaderComposerModule("rim", "림 발광",
                new[]
                {
                    Colour("Color", "림 색", Color.white, true), Range("Intensity", "림 강도", 1, 0, 20),
                    Range("Power", "림 폭", 3, 0.1f, 12)
                },
                "color += @Color@.rgb * @Intensity@ * pow(1.0 - saturate(dot(context.normalWS, context.viewWS)), max(@Power@, 0.0001)) * mask.x;"),
            new ShaderComposerModule("dissolve", "디졸브",
                new[]
                {
                    Range("Amount", "디졸브 양", 0.35f, 0, 1), Range("EdgeWidth", "테두리 폭", 0.05f, 0, 0.5f),
                    Colour("EdgeColor", "테두리 색", new Color(1, 0.3f, 0.02f, 1), true),
                    Range("EdgeIntensity", "테두리 발광", 2, 0, 20)
                },
                "color = lerp(color, @EdgeColor@.rgb * @EdgeIntensity@, mask.z);",
                "albedo *= 1.0 - mask.z; emission *= 1.0 - mask.z; emission += @EdgeColor@.rgb * @EdgeIntensity@ * mask.z;", true)
        };

        internal static readonly ShaderComposerModule[] Masks =
        {
            new ShaderComposerModule("none", "없음 (전체 적용)", Array.Empty<ShaderComposerParameter>(), "1.0"),
            new ShaderComposerModule("texture", "텍스처",
                new[]
                {
                    Texture("Texture", "마스크 텍스처"), Vector("UV", "타일링/이동", new Vector4(1, 1, 0, 0), true),
                    Choice("Channel", "읽을 채널", 0, "R", "G", "B", "A"), Invert()
                },
                "SAMPLE_TEXTURE2D(@Texture@, sampler@Texture@, context.uv * @UV@.xy + @UV@.zw)[(int)clamp(@Channel@, 0, 3)]"),
            new ShaderComposerModule("noise", "노이즈",
                new[]
                {
                    Range("Scale", "노이즈 크기", 8, 0.1f, 100), Vector("Speed", "흐르는 속도", new Vector4(0.1f, 0, 0, 0)),
                    Range("Threshold", "기준값", 0.5f, 0, 1), Range("Softness", "부드러움", 0.1f, 0, 0.5f), Invert()
                },
                "ProjectLNoiseMask(ProjectLComposerNoise(context.uv * @Scale@ + _Time.y * @Speed@.xy), @Threshold@, @Softness@)"),
            new ShaderComposerModule("height", "높이",
                new[] { Choice("Axis", "축", 1, "X", "Y", "Z"), Number("Bottom", "시작 높이", -0.5f), Number("Top", "끝 높이", 0.5f), Invert() },
                "ProjectLComposerHeight(context.positionOS[(int)clamp(@Axis@, 0, 2)], @Bottom@, @Top@)"),
            new ShaderComposerModule("band", "밴드",
                new[]
                {
                    Choice("Axis", "축", 1, "X", "Y", "Z"), Number("Center", "중심", 0), Range("Thickness", "중앙 두께", 0.2f, 0, 10),
                    Range("LowerFade", "아래 페이드", 0.2f, 0, 10), Range("UpperFade", "위 페이드", 0.2f, 0, 10), Invert()
                },
                "ProjectLLinearBandMask(context.positionOS[(int)clamp(@Axis@, 0, 2)], @Center@, @Thickness@, @LowerFade@, @UpperFade@, 0, 0)"),
            new ShaderComposerModule("line", "라인 (UV)",
                new[]
                {
                    Choice("Mode", "모드", 0, "Repeat", "Single"),
                    Range("Angle", "선 방향 (도)", 0, -180, 180), Number("Offset", "위치 이동 (UV)", 0),
                    Range("Width", "선 폭 (UV)", 0.05f, 0, 1), Range("Spacing", "반복 중심 간격 (UV)", 0.25f, 0.001f, 2),
                    Range("Softness", "경계 부드러움 (UV)", 0, 0, 0.5f), Invert()
                },
                "ProjectLLineMask(context.uv, @Mode@, @Angle@, @Offset@, @Width@, @Spacing@, @Softness@)"),
            new ShaderComposerModule("light", "빛 (실시간 조명)",
                new[]
                {
                    Range("Threshold", "기준 밝기", 0.1f, 0, 8),
                    Range("Softness", "경계 부드러움", 0.1f, 0, 2), Invert()
                },
                "ProjectLLightMask(context.lightBrightness, @Threshold@, @Softness@)")
        };

        internal static ShaderComposerModule Find(ShaderComposerModule[] modules, string key)
        {
            ShaderComposerModule result = Array.Find(modules, module => module.Key == key);
            if (result == null) throw new InvalidOperationException("알 수 없는 셰이더 모듈: " + key);
            return result;
        }

        internal static void Normalize(ShaderComposerRecipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (recipe.version == 1)
            {
                recipe.lighting = new System.Collections.Generic.List<ShaderComposerLightingLayer>
                {
                    new ShaderComposerLightingLayer
                    {
                        model = recipe.model ?? "toon",
                        values = recipe.modelValues ?? new System.Collections.Generic.List<ShaderComposerValue>()
                    }
                };
                recipe.nextLightingId = 1;
                recipe.version = 2;
            }
            if (recipe.version != 2) throw new InvalidOperationException("지원하지 않는 조립 데이터 버전이다.");
            recipe.model = null;
            recipe.modelValues = null;
            if (recipe.properties == null) recipe.properties = new System.Collections.Generic.List<ShaderComposerProperty>();
            Base.Normalize(recipe.baseValues);
            if (recipe.lighting == null || recipe.lighting.Count == 0)
                throw new InvalidOperationException("라이트 모델을 하나 이상 추가해야 한다.");
            foreach (ShaderComposerLightingLayer light in recipe.lighting)
            {
                if (light == null) throw new InvalidOperationException("라이트 모델 데이터가 비어 있다.");
                if (light.model == "baseToon") light.model = "baseLit";
                Find(Models, light.model).Normalize(light.values);
                LightingBlend.Normalize(light.blendValues);
                recipe.nextLightingId = Math.Max(recipe.nextLightingId, light.id + 1);
            }
            foreach (ShaderComposerLayer layer in recipe.layers)
            {
                Find(Effects, layer.effect).Normalize(layer.effectValues);
                Find(Masks, layer.mask).Normalize(layer.maskValues);
                recipe.nextLayerId = Math.Max(recipe.nextLayerId, layer.id + 1);
            }
        }
    }
}
