using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    [Serializable]
    internal sealed class ShaderComposerValue
    {
        [SerializeField] internal string key;
        [SerializeField] internal float number;
        [SerializeField] internal Color color = Color.white;
        [SerializeField] internal Vector4 vector;
        [SerializeField] internal string textureGuid;
        [SerializeField] internal string referenceId;
    }

    [Serializable]
    internal sealed class ShaderComposerProperty
    {
        [SerializeField] internal string id;
        [SerializeField] internal string name;
        [SerializeField] internal string label = "새 프로퍼티";
        [SerializeField] internal ShaderComposerParameterKind kind;
        [SerializeField] internal float minimum;
        [SerializeField] internal float maximum = 1;
        [SerializeField] internal bool hdr;
        [SerializeField] internal bool expanded = true;
        [SerializeField] internal ShaderComposerValue value = new ShaderComposerValue();

        internal ShaderComposerParameter Parameter()
        {
            return new ShaderComposerParameter(name, label, kind)
            {
                Minimum = minimum, Maximum = maximum, Hdr = hdr
            };
        }
    }

    [Serializable]
    internal sealed class ShaderComposerLayer
    {
        [SerializeField] internal int id;
        [SerializeField] internal bool enabled = true;
        [SerializeField] internal bool expanded = true;
        [SerializeField] internal string effect = "tint";
        [SerializeField] internal string mask = "none";
        [SerializeField] internal List<ShaderComposerValue> effectValues = new List<ShaderComposerValue>();
        [SerializeField] internal List<ShaderComposerValue> maskValues = new List<ShaderComposerValue>();
    }

    [Serializable]
    internal sealed class ShaderComposerLightingLayer
    {
        [SerializeField] internal int id;
        [SerializeField] internal bool enabled = true;
        [SerializeField] internal bool expanded = true;
        [SerializeField] internal string model = "toon";
        [SerializeField] internal List<ShaderComposerValue> values = new List<ShaderComposerValue>();
        [SerializeField] internal List<ShaderComposerValue> blendValues = new List<ShaderComposerValue>();

        internal string Prefix => id == 0 ? "_Model_" : "_Model" + id + "_";
    }

    [Serializable]
    internal sealed class ShaderComposerRecipe
    {
        [SerializeField] internal int version = 2;
        [SerializeField] internal string shaderName = "ProjectL/Generated/NewShader";
        // v1 파일과 열린 창의 직렬화 상태를 읽을 때만 쓰고 이전 직후 비운다.
        [SerializeField] internal string model;
        [SerializeField] internal List<ShaderComposerValue> modelValues;
        [SerializeField] internal List<ShaderComposerValue> baseValues = new List<ShaderComposerValue>();
        [SerializeField] internal List<ShaderComposerLightingLayer> lighting = new List<ShaderComposerLightingLayer>
        {
            new ShaderComposerLightingLayer()
        };
        [SerializeField] internal int nextLightingId = 1;
        [SerializeField] internal List<ShaderComposerLayer> layers = new List<ShaderComposerLayer>();
        [SerializeField] internal List<ShaderComposerProperty> properties = new List<ShaderComposerProperty>();
        [SerializeField] internal int nextLayerId;
        [SerializeField] internal bool shadowPass = true;
        [SerializeField] internal bool depthPass = true;
        [SerializeField] internal bool normalsPass = true;
        [SerializeField] internal bool outlineNormalsPass = true;
        [SerializeField] internal bool metaPass = true;
        [SerializeField] internal bool unityPassNames;
        [SerializeField] internal bool disableFog;
    }

    internal enum ShaderComposerParameterKind
    {
        Number,
        Range,
        Toggle,
        Choice,
        Color,
        Texture,
        Vector2,
        UV,
        Vector4
    }

    internal sealed class ShaderComposerParameter
    {
        internal readonly string Key;
        internal readonly string Label;
        internal readonly ShaderComposerParameterKind Kind;
        internal float DefaultNumber;
        internal float Minimum;
        internal float Maximum;
        internal bool Integer;
        internal bool Hdr;
        internal Color DefaultColor = Color.white;
        internal Vector4 DefaultVector;
        internal string[] Choices;

        internal ShaderComposerParameter(string key, string label, ShaderComposerParameterKind kind)
        {
            Key = key;
            Label = label;
            Kind = kind;
        }

        internal ShaderComposerValue CreateValue()
        {
            return new ShaderComposerValue
            {
                key = Key,
                number = DefaultNumber,
                color = DefaultColor,
                vector = DefaultVector,
                textureGuid = string.Empty
            };
        }
    }

    internal sealed class ShaderComposerModule
    {
        internal readonly string Key;
        internal readonly string Label;
        internal readonly ShaderComposerParameter[] Parameters;
        internal readonly string Code;
        internal readonly string MetaCode;
        internal readonly bool Clips;
        internal readonly bool RequiresSceneDepth;

        internal ShaderComposerModule(string key, string label, ShaderComposerParameter[] parameters,
            string code, string metaCode = "", bool clips = false, bool requiresSceneDepth = false)
        {
            Key = key;
            Label = label;
            Parameters = parameters;
            Code = code;
            MetaCode = metaCode;
            Clips = clips;
            RequiresSceneDepth = requiresSceneDepth;
        }

        internal void Normalize(List<ShaderComposerValue> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            values.RemoveAll(value => value == null || Array.Find(Parameters, parameter => parameter.Key == value.key) == null);
            foreach (ShaderComposerParameter parameter in Parameters)
            {
                ShaderComposerValue value = values.Find(item => item.key == parameter.Key);
                if (value == null)
                {
                    values.Add(parameter.CreateValue());
                    continue;
                }
                if (parameter.Kind == ShaderComposerParameterKind.Range)
                    value.number = Mathf.Clamp(value.number, parameter.Minimum, parameter.Maximum);
                if (parameter.Kind == ShaderComposerParameterKind.Choice)
                    value.number = Mathf.Clamp(Mathf.Round(value.number), 0, parameter.Choices.Length - 1);
                if (parameter.Kind == ShaderComposerParameterKind.Toggle)
                    value.number = value.number >= 0.5f ? 1 : 0;
            }
        }
    }
}
