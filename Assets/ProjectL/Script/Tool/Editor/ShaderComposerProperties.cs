using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectL.Tool.Editor
{
    // 사용자 선언과 입력 연결의 계약을 한 곳에서 검사한다.
    internal static class ShaderComposerProperties
    {
        internal static readonly ShaderComposerParameterKind[] Kinds =
        {
            ShaderComposerParameterKind.Number, ShaderComposerParameterKind.Range,
            ShaderComposerParameterKind.Color, ShaderComposerParameterKind.Vector2,
            ShaderComposerParameterKind.Vector4, ShaderComposerParameterKind.Texture
        };

        internal static readonly string[] Labels = { "Float", "Range", "Color", "Vector2", "Vector4", "Texture2D" };

        internal static bool Compatible(ShaderComposerParameterKind input, ShaderComposerParameterKind property)
        {
            switch (input)
            {
                case ShaderComposerParameterKind.Color:
                case ShaderComposerParameterKind.Texture:
                    return input == property;
                case ShaderComposerParameterKind.Vector2:
                    return property == ShaderComposerParameterKind.Vector2 || property == ShaderComposerParameterKind.Vector4;
                case ShaderComposerParameterKind.UV:
                case ShaderComposerParameterKind.Vector4:
                    return property == ShaderComposerParameterKind.Vector4;
                default:
                    return property == ShaderComposerParameterKind.Number || property == ShaderComposerParameterKind.Range;
            }
        }

        internal static IEnumerable<ShaderComposerValue> Inputs(ShaderComposerRecipe recipe)
        {
            return recipe.baseValues.Concat(recipe.lighting.SelectMany(light => light.values.Concat(light.blendValues)))
                .Concat(recipe.layers.SelectMany(layer => layer.effectValues.Concat(layer.maskValues)));
        }

        internal static ShaderComposerProperty Find(ShaderComposerRecipe recipe, string id)
        {
            ShaderComposerProperty property = recipe.properties.Find(item => item.id == id);
            if (property == null) throw new InvalidOperationException("참조한 프로퍼티가 없다. 입력의 참조를 다시 선택해야 한다.");
            return property;
        }

        internal static string Name(ShaderComposerRecipe recipe, ShaderComposerParameter parameter,
            ShaderComposerValue value, string localName)
        {
            if (string.IsNullOrEmpty(value.referenceId)) return localName;
            ShaderComposerProperty property = Find(recipe, value.referenceId);
            if (!Compatible(parameter.Kind, property.kind))
                throw new InvalidOperationException(parameter.Label + ": 참조 프로퍼티의 타입이 맞지 않는다.");
            return property.name;
        }

        internal static void Detach(ShaderComposerRecipe recipe, ShaderComposerProperty property)
        {
            foreach (ShaderComposerValue input in Inputs(recipe))
            {
                if (input.referenceId != property.id) continue;
                CopyDefault(input, property);
            }
        }

        private static void CopyDefault(ShaderComposerValue input, ShaderComposerProperty property)
        {
            input.number = property.value.number;
            input.color = property.value.color;
            input.vector = property.value.vector;
            input.textureGuid = property.value.textureGuid;
            input.referenceId = null;
        }

        internal static void DetachIncompatible(ShaderComposerRecipe recipe, ShaderComposerProperty property)
        {
            CheckModule(recipe, ShaderComposerCatalog.Base, recipe.baseValues, property);
            foreach (ShaderComposerLightingLayer light in recipe.lighting)
            {
                CheckModule(recipe, ShaderComposerCatalog.Find(ShaderComposerCatalog.Models, light.model), light.values, property);
                CheckModule(recipe, ShaderComposerCatalog.LightingBlend, light.blendValues, property);
            }
            foreach (ShaderComposerLayer layer in recipe.layers)
            {
                CheckModule(recipe, ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect), layer.effectValues, property);
                CheckModule(recipe, ShaderComposerCatalog.Find(ShaderComposerCatalog.Masks, layer.mask), layer.maskValues, property);
            }
        }

        private static void CheckModule(ShaderComposerRecipe recipe, ShaderComposerModule module,
            List<ShaderComposerValue> values, ShaderComposerProperty changed = null)
        {
            foreach (ShaderComposerParameter parameter in module.Parameters)
            {
                ShaderComposerValue value = values.Find(item => item.key == parameter.Key);
                if (string.IsNullOrEmpty(value.referenceId)) continue;
                ShaderComposerProperty property = Find(recipe, value.referenceId);
                if (changed != null)
                {
                    if (property != changed || Compatible(parameter.Kind, property.kind)) continue;
                    CopyDefault(value, property);
                }
                else if (!Compatible(parameter.Kind, property.kind))
                {
                    throw new InvalidOperationException(parameter.Label + ": 참조 프로퍼티의 타입이 맞지 않는다.");
                }
            }
        }

        internal static void Validate(ShaderComposerRecipe recipe)
        {
            var ids = new HashSet<string>();
            var names = new HashSet<string>();
            foreach (ShaderComposerProperty property in recipe.properties)
            {
                if (property == null || string.IsNullOrEmpty(property.id) || !ids.Add(property.id))
                    throw new InvalidOperationException("프로퍼티 ID가 비었거나 중복됐다.");
                if (string.IsNullOrEmpty(property.name) || property.name.Length > 80 ||
                    !Regex.IsMatch(property.name, @"^_[A-Za-z][A-Za-z0-9_]*$") || !names.Add(property.name))
                    throw new InvalidOperationException("프로퍼티 이름은 _로 시작하는 영문/숫자/_ 이름이어야 하고 중복할 수 없다.");
                if (property.name == "_Time" || property.name == "_SinTime" || property.name == "_CosTime" ||
                    property.name == "_LightDirection" || property.name == "_LightPosition" || property.name == "_WorldSpaceCameraPos")
                    throw new InvalidOperationException(property.name + ": Unity가 사용하는 이름이다.");
                if (string.IsNullOrWhiteSpace(property.label) || property.label.Length > 160 ||
                    property.label.Any(char.IsControl) || property.label.Contains("\"") || property.label.Contains("\\"))
                    throw new InvalidOperationException("프로퍼티 표시 이름에는 따옴표/역슬래시/줄바꿈을 사용할 수 없다.");
                if (!Kinds.Contains(property.kind) || property.value == null || !string.IsNullOrEmpty(property.value.referenceId))
                    throw new InvalidOperationException(property.label + ": 프로퍼티 타입/기본값이 잘못됐다.");
                if (property.kind == ShaderComposerParameterKind.Range &&
                    (float.IsNaN(property.minimum) || float.IsInfinity(property.minimum) ||
                    float.IsNaN(property.maximum) || float.IsInfinity(property.maximum) || property.minimum >= property.maximum))
                    throw new InvalidOperationException(property.label + ": Range 최댓값은 최솟값보다 커야 한다.");
                if (property.kind == ShaderComposerParameterKind.Range &&
                    (property.value.number < property.minimum || property.value.number > property.maximum))
                    throw new InvalidOperationException(property.label + ": 기본값은 Range 범위 안이어야 한다.");
            }
            CheckModule(recipe, ShaderComposerCatalog.Base, recipe.baseValues);
            foreach (ShaderComposerLightingLayer light in recipe.lighting)
            {
                CheckModule(recipe, ShaderComposerCatalog.Find(ShaderComposerCatalog.Models, light.model), light.values);
                CheckModule(recipe, ShaderComposerCatalog.LightingBlend, light.blendValues);
            }
            foreach (ShaderComposerLayer layer in recipe.layers)
            {
                CheckModule(recipe, ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect), layer.effectValues);
                CheckModule(recipe, ShaderComposerCatalog.Find(ShaderComposerCatalog.Masks, layer.mask), layer.maskValues);
            }
        }
    }
}
