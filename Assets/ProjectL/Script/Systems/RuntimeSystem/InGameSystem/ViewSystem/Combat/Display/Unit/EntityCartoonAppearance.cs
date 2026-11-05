using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>전투 적 표시 인스턴스의 모델 재질만 EntityCartoon으로 바꿉니다.</summary>
    /// <remarks>
    /// 프리팹과 FBX의 공유 재질은 바꾸지 않습니다. 풀로 빌린 표시 하나가 가진 MeshRenderer와
    /// SkinnedMeshRenderer만 복제하고, 반환·파괴 전에 원래 재질을 되돌립니다. 파티클은 디졸브 대상이 아닙니다.
    /// </remarks>
    internal sealed class EntityCartoonAppearance : IDisposable
    {
        private const string TemplatePath = "Shared/Shaders/EntityCartoonTemplate";

        private readonly List<RendererState> _states = new();
        private bool _enabled;
        private bool _disposed;

        internal EntityCartoonAppearance(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                _states.Add(new RendererState(renderer));
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                _states.Add(new RendererState(renderer));
        }

        internal bool IsEnabled => _enabled;

        internal void SetEnabled(bool enabled)
        {
            if (_disposed || _enabled == enabled) return;

            if (enabled)
            {
                Material template = LoadTemplate();
                try
                {
                    foreach (RendererState state in _states) state.Apply(template);
                    _enabled = true;
                }
                catch
                {
                    foreach (RendererState state in _states) state.Restore();
                    throw;
                }
                return;
            }

            foreach (RendererState state in _states) state.Restore();
            _enabled = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (RendererState state in _states) state.Restore();
            _states.Clear();
            _enabled = false;
        }

        private static Material LoadTemplate()
        {
            Material template = Resources.Load<Material>(TemplatePath);
            if (template == null || template.shader == null)
                throw new InvalidOperationException("[EntityCartoonAppearance] EntityCartoon 템플릿 재질을 찾을 수 없습니다.");
            return template;
        }

        private sealed class RendererState
        {
            private readonly Renderer _renderer;
            private Material[] _originals;
            private Material[] _entityCartoons;

            internal RendererState(Renderer renderer)
            {
                _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            }

            internal void Apply(Material template)
            {
                if (_renderer == null || _entityCartoons != null) return;

                _originals = _renderer.sharedMaterials;
                var converted = new Material[_originals.Length];
                try
                {
                    for (int i = 0; i < _originals.Length; i++)
                        converted[i] = Convert(_originals[i], template);
                    _renderer.sharedMaterials = converted;
                    _entityCartoons = converted;
                }
                catch
                {
                    foreach (Material material in converted) DestroyOwned(material);
                    _originals = null;
                    throw;
                }
            }

            internal void Restore()
            {
                if (_entityCartoons == null) return;
                if (_renderer != null) _renderer.sharedMaterials = _originals;
                foreach (Material material in _entityCartoons) DestroyOwned(material);
                _entityCartoons = null;
                _originals = null;
            }

            private static Material Convert(Material source, Material template)
            {
                if (source == null) return null;
                if (source.shader == null)
                    throw new InvalidOperationException("[EntityCartoonAppearance] 모델 재질의 셰이더가 없습니다.");

                var converted = new Material(source)
                {
                    name = source.name + "_EntityCartoon",
                    hideFlags = HideFlags.DontSave
                };
                Texture baseMap = GetTexture(source, "_BaseMap", "_MainTex");
                Color baseColor = GetColor(source, "_BaseColor", "_Color");
                Vector2 scale = GetTextureScale(source, "_BaseMap", "_MainTex");
                Vector2 offset = GetTextureOffset(source, "_BaseMap", "_MainTex");
                converted.shader = template.shader;
                converted.SetTexture("_BaseMap", baseMap);
                converted.SetColor("_BaseColor", baseColor);
                converted.SetTextureScale("_BaseMap", scale);
                converted.SetTextureOffset("_BaseMap", offset);
                converted.SetTexture("_DissolveTex", template.GetTexture("_DissolveTex"));
                converted.SetFloat("_DissolveAmount", template.GetFloat("_DissolveAmount"));
                converted.SetColor("_DissolveEdgeColor", template.GetColor("_DissolveEdgeColor"));
                converted.SetFloat("_DissolveEdgeWidth", template.GetFloat("_DissolveEdgeWidth"));
                converted.SetFloat("_DissolveEdgeIntensity", template.GetFloat("_DissolveEdgeIntensity"));
                return converted;
            }

            private static Texture GetTexture(Material material, string primary, string fallback)
            {
                return material.HasProperty(primary) ? material.GetTexture(primary) :
                    material.HasProperty(fallback) ? material.GetTexture(fallback) : null;
            }

            private static Color GetColor(Material material, string primary, string fallback)
            {
                return material.HasProperty(primary) ? material.GetColor(primary) :
                    material.HasProperty(fallback) ? material.GetColor(fallback) : Color.white;
            }

            private static Vector2 GetTextureScale(Material material, string primary, string fallback)
            {
                return material.HasProperty(primary) ? material.GetTextureScale(primary) :
                    material.HasProperty(fallback) ? material.GetTextureScale(fallback) : Vector2.one;
            }

            private static Vector2 GetTextureOffset(Material material, string primary, string fallback)
            {
                return material.HasProperty(primary) ? material.GetTextureOffset(primary) :
                    material.HasProperty(fallback) ? material.GetTextureOffset(fallback) : Vector2.zero;
            }

            private static void DestroyOwned(Material material)
            {
                if (material == null) return;
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
