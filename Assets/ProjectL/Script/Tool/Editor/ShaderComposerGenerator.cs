using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    internal sealed class ShaderComposerBinding
    {
        internal readonly string Name;
        internal readonly string Label;
        internal readonly ShaderComposerParameter Parameter;
        internal readonly ShaderComposerValue Value;

        internal ShaderComposerBinding(string name, string label, ShaderComposerParameter parameter, ShaderComposerValue value)
        {
            Name = name;
            Label = label;
            Parameter = parameter;
            Value = value;
        }
    }

    internal static class ShaderComposerGenerator
    {
        internal const string RecipeMarker = "// ProjectLShaderComposerRecipe: ";
        private const string IncludeRoot = "Assets/ProjectL/Script/Render/Shader/Core/Includes/Composer/";

        internal static ShaderComposerRecipe ReadRecipe(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            int start = source.IndexOf(RecipeMarker, StringComparison.Ordinal);
            if (start < 0) throw new InvalidOperationException("이 Shader에는 조립기 데이터가 없다.");
            start += RecipeMarker.Length;
            int end = source.IndexOf('\n', start);
            if (end < 0) end = source.Length;
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(source.Substring(start, end - start).Trim()));
            ShaderComposerRecipe recipe = JsonUtility.FromJson<ShaderComposerRecipe>(json);
            ShaderComposerCatalog.Normalize(recipe);
            Validate(recipe);
            return recipe;
        }

        internal static void Validate(ShaderComposerRecipe recipe)
        {
            ShaderComposerCatalog.Normalize(recipe);
            if (string.IsNullOrWhiteSpace(recipe.shaderName) ||
                !Regex.IsMatch(recipe.shaderName, @"^[\p{L}\p{N}_ /.-]+$") || recipe.shaderName.Length > 160)
                throw new InvalidOperationException("Shader 이름에는 문자·숫자·공백·/·_·-·.만 사용할 수 있다.");
            if (recipe.layers.Count > ShaderComposerCatalog.MaximumLayers)
                throw new InvalidOperationException("효과는 최대 " + ShaderComposerCatalog.MaximumLayers + "개까지 지원한다.");
            if (recipe.lighting.Count > ShaderComposerCatalog.MaximumLightingLayers)
                throw new InvalidOperationException("라이트 모델은 최대 " + ShaderComposerCatalog.MaximumLightingLayers + "개까지 지원한다.");
            var lightingIds = new HashSet<int>();
            bool hasLighting = false;
            foreach (ShaderComposerLightingLayer light in recipe.lighting)
            {
                if (light.id < 0 || !lightingIds.Add(light.id)) throw new InvalidOperationException("라이트 모델 ID가 잘못되었거나 중복됐다.");
                hasLighting |= light.enabled;
            }
            if (!hasLighting) throw new InvalidOperationException("라이트 모델을 하나 이상 활성화해야 한다.");
            var ids = new HashSet<int>();
            foreach (ShaderComposerLayer layer in recipe.layers)
            {
                if (layer.id < 0 || !ids.Add(layer.id)) throw new InvalidOperationException("효과 ID가 잘못되었거나 중복됐다.");
                if (layer.enabled && layer.mask == "light" && ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect).Clips)
                    throw new InvalidOperationException("빛 마스크는 디졸브에 사용할 수 없다. 그림자와 깊이의 일치를 위해 다른 마스크를 선택해야 한다.");
            }
            ShaderComposerProperties.Validate(recipe);
            List<ShaderComposerBinding> bindings = Bindings(recipe);
            // Forward 렌더 상태는 생성기가 소유한다. 사용자 프로퍼티와 중복 선언하지 않는다.
            var names = new HashSet<string> { "_SrcBlend", "_DstBlend", "_SrcBlendAlpha", "_DstBlendAlpha", "_ZWrite" };
            foreach (ShaderComposerBinding binding in bindings)
            {
                if (!names.Add(binding.Name)) throw new InvalidOperationException(binding.Name + ": 자동 프로퍼티와 이름이 겹친다.");
                ShaderComposerValue value = binding.Value;
                if (!Finite(value.number) || !Finite(value.vector.x) || !Finite(value.vector.y) ||
                    !Finite(value.vector.z) || !Finite(value.vector.w) || !Finite(value.color.r) ||
                    !Finite(value.color.g) || !Finite(value.color.b) || !Finite(value.color.a))
                    throw new InvalidOperationException(binding.Label + ": 유효한 숫자를 입력해야 한다.");
            }
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static List<ShaderComposerBinding> Bindings(ShaderComposerRecipe recipe)
        {
            var result = new List<ShaderComposerBinding>();
            foreach (ShaderComposerProperty property in recipe.properties)
                result.Add(new ShaderComposerBinding(property.name, property.label, property.Parameter(), property.value));
            AddBindings(result, ShaderComposerCatalog.Base, recipe.baseValues, "_", "");
            for (int i = 0; i < recipe.lighting.Count; i++)
            {
                ShaderComposerLightingLayer light = recipe.lighting[i];
                if (!light.enabled) continue;
                string label = "모델 " + (i + 1) + " / ";
                AddBindings(result, ShaderComposerCatalog.Find(ShaderComposerCatalog.Models, light.model), light.values, light.Prefix, label);
                AddBindings(result, ShaderComposerCatalog.LightingBlend, light.blendValues, light.Prefix, label);
            }
            for (int i = 0; i < recipe.layers.Count; i++)
            {
                ShaderComposerLayer layer = recipe.layers[i];
                if (!layer.enabled) continue;
                string label = "효과 " + (i + 1) + " / ";
                AddBindings(result, ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect), layer.effectValues,
                    "_Fx" + layer.id + "_", label);
                AddBindings(result, ShaderComposerCatalog.Find(ShaderComposerCatalog.Masks, layer.mask), layer.maskValues,
                    "_Mask" + layer.id + "_", label + "마스크 / ");
            }
            return result;
        }

        private static void AddBindings(List<ShaderComposerBinding> bindings, ShaderComposerModule module,
            List<ShaderComposerValue> values, string prefix, string label)
        {
            foreach (ShaderComposerParameter parameter in module.Parameters)
            {
                ShaderComposerValue value = values.Find(item => item.key == parameter.Key);
                if (string.IsNullOrEmpty(value.referenceId))
                    bindings.Add(new ShaderComposerBinding(prefix + parameter.Key, label + parameter.Label, parameter, value));
            }
        }

        private static string Number(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vector(Vector4 value)
        {
            return "(" + Number(value.x) + "," + Number(value.y) + "," + Number(value.z) + "," + Number(value.w) + ")";
        }

        private static string Resolve(string code, ShaderComposerModule module,
            List<ShaderComposerValue> values, string prefix, ShaderComposerRecipe recipe)
        {
            foreach (ShaderComposerParameter parameter in module.Parameters)
            {
                ShaderComposerValue value = values.Find(item => item.key == parameter.Key);
                code = code.Replace("@" + parameter.Key + "@",
                    ShaderComposerProperties.Name(recipe, parameter, value, prefix + parameter.Key));
            }
            if (code.Contains("@")) throw new InvalidOperationException("모듈 파라미터가 연결되지 않았다: " + module.Key);
            return code;
        }

        private static void Property(StringBuilder output, ShaderComposerBinding binding)
        {
            ShaderComposerParameter p = binding.Parameter;
            ShaderComposerValue value = binding.Value;
            string attributes = "";
            string type;
            string initial;
            switch (p.Kind)
            {
                case ShaderComposerParameterKind.Texture:
                    attributes = "[NoScaleOffset] "; type = "2D"; initial = "\"white\" {}"; break;
                case ShaderComposerParameterKind.Color:
                    attributes = p.Hdr ? "[HDR] " : "";
                    type = "Color"; initial = Vector(new Vector4(value.color.r, value.color.g, value.color.b, value.color.a)); break;
                case ShaderComposerParameterKind.Vector2:
                case ShaderComposerParameterKind.Vector4:
                case ShaderComposerParameterKind.UV:
                    type = "Vector"; initial = Vector(value.vector); break;
                case ShaderComposerParameterKind.Range:
                    type = "Range(" + Number(p.Minimum) + "," + Number(p.Maximum) + ")"; initial = Number(value.number); break;
                case ShaderComposerParameterKind.Toggle:
                    attributes = "[Toggle] "; type = "Float"; initial = Number(value.number); break;
                case ShaderComposerParameterKind.Choice:
                    var choices = new List<string>();
                    for (int i = 0; i < p.Choices.Length; i++) choices.Add(p.Choices[i] + "," + i);
                    attributes = "[Enum(" + string.Join(",", choices) + ")] "; type = "Float"; initial = Number(value.number); break;
                default:
                    type = "Float"; initial = Number(value.number); break;
            }
            output.AppendLine("        " + attributes + binding.Name + " (\"" + binding.Label + "\", " + type + ") = " + initial);
        }

        internal static string Generate(ShaderComposerRecipe recipe, string previewName = null)
        {
            Validate(recipe);
            List<ShaderComposerBinding> bindings = Bindings(recipe);
            var layers = recipe.layers.FindAll(layer => layer.enabled);
            bool clips = layers.Exists(layer => ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect).Clips);
            string baseColor = Resolve("@BaseColor@", ShaderComposerCatalog.Base, recipe.baseValues, "_", recipe);
            bool movementTransparency = baseColor == "_BaseColor" ||
                (baseColor == "_Color" && !bindings.Exists(binding => binding.Name == "_BaseColor"));
            var output = new StringBuilder();
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonUtility.ToJson(recipe)));
            output.AppendLine(RecipeMarker + data);
            output.AppendLine("Shader \"" + (previewName ?? recipe.shaderName.Trim()) + "\"");
            output.AppendLine("{");
            output.AppendLine("    Properties");
            output.AppendLine("    {");
            foreach (ShaderComposerBinding binding in bindings) Property(output, binding);
            // 기본값은 기존 불투명 렌더와 같다. 이동 미리보기는 복사 머티리얼의 값만 바꾼다.
            output.AppendLine("        [HideInInspector] _SrcBlend (\"Source Blend\", Float) = 1");
            output.AppendLine("        [HideInInspector] _DstBlend (\"Destination Blend\", Float) = 0");
            output.AppendLine("        [HideInInspector] _SrcBlendAlpha (\"Source Alpha Blend\", Float) = 1");
            output.AppendLine("        [HideInInspector] _DstBlendAlpha (\"Destination Alpha Blend\", Float) = 0");
            output.AppendLine("        [HideInInspector] _ZWrite (\"Depth Write\", Float) = 1");
            output.AppendLine("    }");
            output.AppendLine("    SubShader");
            output.AppendLine("    {");
            output.AppendLine("        Tags { \"RenderPipeline\"=\"UniversalPipeline\" \"RenderType\"=\"" + (clips ? "TransparentCutout" : "Opaque") + "\" \"Queue\"=\"" + (clips ? "AlphaTest" : "Geometry") + "\"" +
                (movementTransparency ? " \"ProjectLMovementTransparency\"=\"True\"" : "") + " }");
            output.AppendLine("        HLSLINCLUDE");
            if (recipe.disableFog) output.AppendLine("        #define PROJECTL_COMPOSER_DISABLE_FOG 1");
            if (recipe.lighting.Exists(light => light.enabled &&
                ShaderComposerCatalog.Find(ShaderComposerCatalog.Models, light.model).RequiresSceneDepth))
                output.AppendLine("        #define PROJECTL_COMPOSER_SCENE_DEPTH 1");
            if (layers.Exists(layer => layer.mask == "light"))
                output.AppendLine("        #define PROJECTL_COMPOSER_LIGHT_MASK 1");
            output.AppendLine("        #include \"" + IncludeRoot + "ComposerLighting.hlsl\"");
            output.AppendLine("        CBUFFER_START(UnityPerMaterial)");
            foreach (ShaderComposerBinding binding in bindings)
            {
                if (binding.Parameter.Kind == ShaderComposerParameterKind.Texture) continue;
                bool vector = binding.Parameter.Kind == ShaderComposerParameterKind.Color || binding.Parameter.Kind == ShaderComposerParameterKind.Vector2 ||
                    binding.Parameter.Kind == ShaderComposerParameterKind.Vector4 || binding.Parameter.Kind == ShaderComposerParameterKind.UV;
                output.AppendLine("            " + (vector ? "float4 " : "float ") + binding.Name + ";");
            }
            output.AppendLine("        CBUFFER_END");
            foreach (ShaderComposerBinding binding in bindings)
            {
                if (binding.Parameter.Kind != ShaderComposerParameterKind.Texture) continue;
                output.AppendLine("        TEXTURE2D(" + binding.Name + "); SAMPLER(sampler" + binding.Name + ");");
            }
            output.AppendLine("        half4 ProjectLComposerReadBase(ProjectLComposerContext context)");
            output.AppendLine("        { " + Resolve("return SAMPLE_TEXTURE2D(@BaseMap@, sampler@BaseMap@, context.uv * @BaseUV@.xy + @BaseUV@.zw) * @BaseColor@;",
                ShaderComposerCatalog.Base, recipe.baseValues, "_", recipe) + " }");
            output.AppendLine("        struct ProjectLComposerMasks");
            output.AppendLine("        {");
            if (layers.Count == 0) output.AppendLine("            float4 unused;");
            foreach (ShaderComposerLayer layer in layers) output.AppendLine("            float4 effect" + layer.id + ";");
            output.AppendLine("        };");
            output.AppendLine("        ProjectLComposerMasks ProjectLComposerEvaluateMasks(ProjectLComposerContext context)");
            output.AppendLine("        {");
            output.AppendLine("            ProjectLComposerMasks result = (ProjectLComposerMasks)0;");
            foreach (ShaderComposerLayer layer in layers)
            {
                ShaderComposerModule effect = ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect);
                ShaderComposerModule mask = ShaderComposerCatalog.Find(ShaderComposerCatalog.Masks, layer.mask);
                string maskPrefix = "_Mask" + layer.id + "_";
                string variable = "result.effect" + layer.id;
                output.AppendLine("            " + variable + ".x = saturate(" + Resolve(mask.Code, mask, layer.maskValues, maskPrefix, recipe) + ");");
                if (layer.mask != "none") output.AppendLine("            " + variable + ".x = lerp(" + variable + ".x, 1.0 - " + variable + ".x, step(0.5, " +
                    Resolve("@Invert@", mask, layer.maskValues, maskPrefix, recipe) + "));");
                if (effect.Clips)
                    output.AppendLine("            " + variable + ".yz = ProjectLDissolveMask(" + variable + ".x, " +
                        Resolve("@Amount@, @EdgeWidth@", effect, layer.effectValues, "_Fx" + layer.id + "_", recipe) + ");");
            }
            output.AppendLine("            return result;");
            output.AppendLine("        }");
            output.AppendLine("        void ProjectLComposerClip(ProjectLComposerMasks masks)");
            output.AppendLine("        {");
            foreach (ShaderComposerLayer layer in layers)
                if (ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect).Clips)
                    output.AppendLine("            clip(masks.effect" + layer.id + ".y - 0.5);");
            output.AppendLine("        }");
            foreach (ShaderComposerLightingLayer light in recipe.lighting)
            {
                if (!light.enabled) continue;
                ShaderComposerModule model = ShaderComposerCatalog.Find(ShaderComposerCatalog.Models, light.model);
                output.AppendLine("        half3 ProjectLComposerModel" + light.id + "(ProjectLComposerContext context, half3 albedo)");
                output.AppendLine("        { " + Resolve(model.Code, model, light.values, light.Prefix, recipe) + " }");
            }
            output.AppendLine("        half3 ProjectLComposerShade(ProjectLComposerContext context, half3 albedo)");
            output.AppendLine("        {");
            output.AppendLine("            float3 blended = 0; float totalWeight = 0;");
            foreach (ShaderComposerLightingLayer light in recipe.lighting)
            {
                if (!light.enabled) continue;
                string weight = Resolve("@Weight@", ShaderComposerCatalog.LightingBlend, light.blendValues, light.Prefix, recipe);
                output.AppendLine("            { float weight = max(" + weight + ", 0.0); if (weight > 0.0) { blended += ProjectLComposerModel" +
                    light.id + "(context, albedo) * weight; totalWeight += weight; } }");
            }
            output.AppendLine("            return totalWeight > 0.0 ? blended / max(totalWeight, 1.0e-20) : 0.0.xxx;");
            output.AppendLine("        }");
            output.AppendLine("        half3 ProjectLComposerApplyEffects(ProjectLComposerContext context, ProjectLComposerMasks masks, half3 color)");
            output.AppendLine("        {");
            foreach (ShaderComposerLayer layer in layers)
            {
                ShaderComposerModule effect = ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect);
                output.AppendLine("            { float4 mask = masks.effect" + layer.id + "; " + Resolve(effect.Code, effect, layer.effectValues, "_Fx" + layer.id + "_", recipe) + " }");
            }
            output.AppendLine("            return color;");
            output.AppendLine("        }");
            output.AppendLine("        void ProjectLComposerMetaSurface(ProjectLComposerContext context, ProjectLComposerMasks masks, half3 baseColor, out half3 albedo, out half3 emission)");
            output.AppendLine("        {");
            output.AppendLine("            albedo = baseColor; emission = 0;");
            foreach (ShaderComposerLayer layer in layers)
            {
                ShaderComposerModule effect = ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect);
                if (string.IsNullOrEmpty(effect.MetaCode) || layer.mask == "light") continue;
                output.AppendLine("            { float4 mask = masks.effect" + layer.id + "; " + Resolve(effect.MetaCode, effect, layer.effectValues, "_Fx" + layer.id + "_", recipe) + " }");
            }
            output.AppendLine("        }");
            output.AppendLine("        #include \"" + IncludeRoot + "ComposerPasses.hlsl\"");
            output.AppendLine("        ENDHLSL");
            Pass(output, recipe.unityPassNames ? "UniversalForward" : "FORWARD", recipe.unityPassNames ? "UniversalForward" : "UniversalForwardOnly", "ProjectLComposerVert", "ProjectLComposerForwardFrag", "", "forward");
            if (recipe.shadowPass) Pass(output, recipe.unityPassNames ? "ShadowCaster" : "SHADOWCASTER", "ShadowCaster", "ProjectLComposerShadowVert", "ProjectLComposerShadowFrag", "ColorMask 0", "shadow");
            if (recipe.depthPass) Pass(output, recipe.unityPassNames ? "DepthOnly" : "DEPTHONLY", "DepthOnly", "ProjectLComposerVert", "ProjectLComposerDepthFrag", "ColorMask R", "");
            if (recipe.normalsPass) Pass(output, recipe.unityPassNames ? "DepthNormals" : "DEPTHNORMALS", recipe.unityPassNames ? "DepthNormals" : "DepthNormalsOnly", "ProjectLComposerVert", "ProjectLComposerDepthNormalsFrag", "", "normals");
            if (recipe.outlineNormalsPass) Pass(output, recipe.unityPassNames ? "NormalOnly" : "NORMALONLY", "NormalOnly", "ProjectLComposerVert", "ProjectLComposerNormalFrag", "", "");
            if (recipe.metaPass) Pass(output, recipe.unityPassNames ? "Meta" : "META", "Meta", "ProjectLComposerMetaVert", "ProjectLComposerMetaFrag", "", "meta");
            output.AppendLine("    }");
            output.AppendLine("}");
            return output.ToString().Replace("\r\n", "\n");
        }

        private static void Pass(StringBuilder output, string name, string lightMode, string vertex, string fragment, string state, string type)
        {
            output.AppendLine("        Pass");
            output.AppendLine("        {");
            output.AppendLine("            Name \"" + name + "\" Tags { \"LightMode\"=\"" + lightMode + "\" }");
            output.AppendLine("            Cull " + (type == "meta" ? "Off" : "Back") +
                (type == "forward" ? " ZWrite [_ZWrite]" : " ZWrite On") + " ZTest LEqual" +
                (string.IsNullOrEmpty(state) ? "" : " " + state));
            if (type == "forward")
                output.AppendLine("            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]");
            output.AppendLine("            HLSLPROGRAM");
            output.AppendLine("            #pragma target 3.5");
            output.AppendLine("            #pragma vertex " + vertex);
            output.AppendLine("            #pragma fragment " + fragment);
            output.AppendLine("            #pragma multi_compile_instancing");
            if (type == "forward")
            {
                output.AppendLine("            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN");
                output.AppendLine("            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS");
                output.AppendLine("            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS");
                output.AppendLine("            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH");
                output.AppendLine("            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION");
                output.AppendLine("            #pragma multi_compile_fragment _ _LIGHT_COOKIES");
                output.AppendLine("            #pragma multi_compile_fragment _ _LIGHT_LAYERS");
                output.AppendLine("            #pragma multi_compile _ _FORWARD_PLUS");
                output.AppendLine("            #pragma multi_compile _ LIGHTMAP_ON");
                output.AppendLine("            #pragma multi_compile _ DYNAMICLIGHTMAP_ON");
                output.AppendLine("            #pragma multi_compile _ DIRLIGHTMAP_COMBINED");
                output.AppendLine("            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING");
                output.AppendLine("            #pragma multi_compile _ SHADOWS_SHADOWMASK");
                output.AppendLine("            #pragma multi_compile_fog");
            }
            else if (type == "shadow") output.AppendLine("            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW");
            else if (type == "normals")
            {
                output.AppendLine("            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT");
                output.AppendLine("            #include_with_pragmas \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl\"");
            }
            else if (type == "meta") output.AppendLine("            #pragma shader_feature EDITOR_VISUALIZATION");
            output.AppendLine("            ENDHLSL");
            output.AppendLine("        }");
        }
    }
}
