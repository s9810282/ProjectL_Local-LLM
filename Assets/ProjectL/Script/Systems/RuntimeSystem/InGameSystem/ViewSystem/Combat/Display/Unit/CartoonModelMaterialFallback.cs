using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>기존 모델의 비 Cartoon 재질만 전투 표시용 Cartoon 재질로 바꿉니다.</summary>
    /// <remarks>
    /// Cartoon 슬롯은 공유 재질을 사용합니다. 이전 모델 재질의 복사본만 소유하고 파괴합니다.
    /// </remarks>
    internal sealed class CartoonModelMaterialFallback : IDisposable
    {
        private const string DefaultsPath = "Shared/Shaders/CartoonDissolveDefaults";
        private static Material _defaults;

        private readonly List<RendererState> _states = new();
        private bool _enabled;
        private bool _disposed;

        internal CartoonModelMaterialFallback(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            Shader cartoon = GetDefaults().shader;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                AddLegacyRenderer(renderer, cartoon);
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                AddLegacyRenderer(renderer, cartoon);
        }

        internal bool IsEnabled => _enabled;
        internal bool NeedsConversion => _states.Count != 0;

        private void AddLegacyRenderer(Renderer renderer, Shader cartoon)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == cartoon) continue;
                _states.Add(new RendererState(renderer));
                return;
            }
        }

        internal void SetEnabled(bool enabled)
        {
            if (_disposed || _enabled == enabled) return;

            if (enabled)
            {
                Material template = GetDefaults();
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

        internal static Material GetDefaults()
        {
            if (_defaults == null)
                _defaults = Resources.Load<Material>(DefaultsPath);
            if (_defaults == null || _defaults.shader == null)
                throw new InvalidOperationException("[CartoonModelMaterialFallback] Cartoon 기본 재질을 찾을 수 없습니다.");
            return _defaults;
        }

        private sealed class RendererState
        {
            private readonly Renderer _renderer;
            private Material[] _originals;
            private Material[] _converted;

            internal RendererState(Renderer renderer)
            {
                _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            }

            internal void Apply(Material template)
            {
                if (_renderer == null || _converted != null) return;

                _originals = _renderer.sharedMaterials;
                var converted = new Material[_originals.Length];
                try
                {
                    for (int i = 0; i < _originals.Length; i++)
                    {
                        Material source = _originals[i];
                        converted[i] = source == null || source.shader == template.shader ? source : Convert(source, template);
                    }
                    _renderer.sharedMaterials = converted;
                    _converted = converted;
                }
                catch
                {
                    DestroyConverted(converted);
                    _originals = null;
                    throw;
                }
            }

            internal void Restore()
            {
                if (_converted == null) return;
                if (_renderer != null) _renderer.sharedMaterials = _originals;
                DestroyConverted(_converted);
                _converted = null;
                _originals = null;
            }

            private void DestroyConverted(Material[] converted)
            {
                for (int i = 0; i < converted.Length; i++)
                {
                    if (converted[i] != _originals[i])
                        DestroyOwned(converted[i]);
                }
            }

            private static Material Convert(Material source, Material template)
            {
                if (source.shader == null)
                    throw new InvalidOperationException("[CartoonModelMaterialFallback] 모델 재질의 셰이더가 없습니다.");

                var converted = new Material(source)
                {
                    name = source.name + "_Cartoon",
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
