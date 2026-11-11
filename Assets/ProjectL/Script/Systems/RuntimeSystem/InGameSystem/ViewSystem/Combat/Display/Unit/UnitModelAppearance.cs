using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 모델의 외형 층(이동 반투명·피격 플래시·디졸브·아웃라인)을 합쳐 쓰는 소유자입니다. 모델 슬롯의 투명 변형·색 손잡이를 소유하고,
    /// 층마다 현재 값을 기억해 한 번에 씁니다. 어느 층을 끝내도 남은 층을 다시 쓰고, 마지막 층이 끝날 때만 원래 재질/PropertyBlock을 명시적으로 복원합니다.
    /// </summary>
    /// <remarks>
    /// 색은 ColorSystem의 공개 손잡이(<see cref="IColorSystemService.CreateColorHandle"/>)로만 바꿉니다(FW-203). 손잡이 반환은 Renderer의 PropertyBlock 복원을
    /// 보장하지 않으므로, 원래 슬롯과 블록은 이 객체가 직접 되돌립니다(DR-004). 반투명 층이 있으면 투명 변형 재질을 쓰고, 플래시만 있으면 원래 재질에 색만 바꿉니다.
    /// 합친 색 = 원래 색을 플래시 색 쪽으로 세기만큼 섞은 값이고, 알파 = 원래 알파 × 투명도입니다(플래시가 없으면 옛 이동 반투명과 같은 값).
    /// 외곽선 = 바탕(강조 층이 있으면 그 외곽선, 없으면 재질 외곽선)을 플래시 외곽선 쪽으로 세기만큼 섞은 값입니다. 플래시 외곽선 폭이 0이면 바탕 그대로입니다.
    /// </remarks>
    internal sealed class UnitModelAppearance : IDisposable
    {
        private readonly List<RendererState> _states = new List<RendererState>();
        // 이동 반투명 층입니다. 1이면 층이 없습니다.
        private float _opacity = 1f;
        // 피격 플래시 층입니다(몸 색·외곽선). 세기 0이면 층이 없습니다.
        private CombatFlashLook _flash;
        private float _flashStrength;
        // 적 사망 디졸브 층입니다. 0이면 층이 없습니다.
        private float _dissolveAmount;
        private bool _hasOutline;
        private Color _outlineColor;
        private float _outlineWidth;
        private bool _applied;
        private bool _disposed;

        internal UnitModelAppearance(Renderer[] renderers, IColorSystemService colors)
        {
            if (renderers == null) throw new ArgumentNullException(nameof(renderers));
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            foreach (Renderer renderer in renderers)
                if (renderer != null)
                    _states.Add(new RendererState(renderer, colors));
        }

        /// <summary>이동 반투명 층을 씁니다. 1이면 그 층을 끝냅니다.</summary>
        internal void SetOpacity(float opacity)
        {
            if (_disposed) return;
            if (float.IsNaN(opacity) || float.IsInfinity(opacity) || opacity <= 0f || opacity > 1f)
                throw new ArgumentOutOfRangeException(nameof(opacity));
            _opacity = opacity;
            Apply();
        }

        /// <summary>이동 반투명 층을 끝냅니다. 다른 층이 남으면 그 층을 다시 씁니다.</summary>
        internal void ClearOpacity()
        {
            if (_disposed) return;
            _opacity = 1f;
            Apply();
        }

        /// <summary>피격 플래시 층(몸 색·외곽선)을 씁니다. 세기는 0~1이고, 0이면 그 층을 끝냅니다.</summary>
        internal void SetFlash(CombatFlashLook look, float strength)
        {
            if (_disposed) return;
            if (float.IsNaN(strength) || strength < 0f || strength > 1f)
                throw new ArgumentOutOfRangeException(nameof(strength));
            _flash = look;
            _flashStrength = strength;
            Apply();
        }

        /// <summary>피격 플래시 층을 끝냅니다. 다른 층이 남으면 그 층을 다시 씁니다.</summary>
        internal void ClearFlash()
        {
            if (_disposed) return;
            _flashStrength = 0f;
            Apply();
        }

        /// <summary>디졸브를 지원하는 모델의 사망 디졸브 진행값을 씁니다.</summary>
        internal void SetDissolve(float amount)
        {
            if (_disposed) return;
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f || amount > 1f)
                throw new ArgumentOutOfRangeException(nameof(amount));
            _dissolveAmount = amount;
            Apply();
        }

        /// <summary>사망 디졸브 층을 끝내고 원래 재질 값으로 되돌립니다.</summary>
        internal void ClearDissolve()
        {
            if (_disposed) return;
            _dissolveAmount = 0f;
            Apply();
        }

        /// <summary>기존 색 손잡이에 강조 층을 합칩니다. 미지원 재질은 적용 전에 구성 오류로 보고합니다.</summary>
        internal void SetOutline(Color color, float width)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitModelAppearance));
            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));
            bool found = false;
            foreach (RendererState state in _states)
                found |= state.RequireOutlineSupport();
            if (!found)
                throw new InvalidOperationException("[UnitModelAppearance] 아웃라인을 적용할 실제 모델 렌더러가 없습니다.");
            _outlineColor = color;
            _outlineWidth = width;
            _hasOutline = true;
            Apply();
        }

        /// <summary>강조 층만 끝내고 현재 투명도·플래시·디졸브 층을 유지합니다.</summary>
        internal void ClearOutline()
        {
            if (_disposed || !_hasOutline) return;
            _hasOutline = false;
            Apply();
        }

        /// <summary>모든 층을 끝내고 원래 외형으로 되돌립니다.</summary>
        internal void Restore()
        {
            if (_disposed) return;
            _opacity = 1f;
            _flashStrength = 0f;
            _dissolveAmount = 0f;
            _hasOutline = false;
            Apply();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _applied = false;
            RunAll(state => state.Dispose());
        }

        private void Apply()
        {
            if (_opacity >= 1f && _flashStrength <= 0f && _dissolveAmount <= 0f && !_hasOutline)
            {
                if (!_applied) return;
                _applied = false;
                RunAll(state => state.Restore());
                return;
            }
            _applied = true;
            try
            {
                foreach (RendererState state in _states)
                    state.Apply(_opacity, _flash, _flashStrength, _dissolveAmount, _hasOutline, _outlineColor, _outlineWidth);
            }
            catch (Exception error)
            {
                // 한 층의 적용이 실패하면 모든 층을 함께 지우고 원래대로 돌린다(옛 이동 반투명의 실패 정리와 같음).
                // 플래시 중 반투명 준비가 실패하면 플래시도 지워지지만, 플래시 조각이 다음 Tick에 다시 쓴다.
                try
                {
                    _opacity = 1f;
                    _flashStrength = 0f;
                    _dissolveAmount = 0f;
                    _hasOutline = false;
                    _applied = false;
                    RunAll(state => state.Restore());
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
        }

        private void RunAll(Action<RendererState> action)
        {
            List<Exception> errors = null;
            foreach (RendererState state in _states)
            {
                try
                {
                    action(state);
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            if (errors != null) throw new AggregateException("[UnitModelAppearance] 모델 표시 정리에 실패했습니다.", errors);
        }

        private sealed class RendererState : IDisposable
        {
            private static readonly int DissolveAmountId = Shader.PropertyToID(ColorShaderPropertyKey.DissolveAmount.Name);
            private static readonly int DissolveTextureId = Shader.PropertyToID(ColorShaderPropertyKey.DissolveTexture.Name);
            private static readonly int OutlineColorId = Shader.PropertyToID(ColorShaderPropertyKey.OutlineColor.Name);
            private static readonly int OutlineWidthId = Shader.PropertyToID(ColorShaderPropertyKey.OutlineWidth.Name);
            private readonly Renderer _renderer;
            private readonly IColorSystemService _colors;
            private readonly Material[] _originals;
            private readonly MaterialPropertyBlock[] _blocks;
            private readonly MaterialPropertyBlock _globalBlock;
            private readonly TypeSettingKey<Color>[] _keys;
            private readonly Color[] _baseColors;
            private readonly float?[] _baseDissolveAmounts;
            private readonly bool[] _needsDissolveTexture;
            private readonly bool _isModel;
            private readonly bool[] _supportsOutline;
            private readonly Color[] _baseOutlineColors;
            private readonly float[] _baseOutlineWidths;
            private Material[] _transparent;
            private IColorHandle _scope;
            private bool _transparentApplied;

            internal RendererState(Renderer renderer, IColorSystemService colors)
            {
                _renderer = renderer;
                _colors = colors;
                _originals = renderer.sharedMaterials;
                _globalBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(_globalBlock);
                _blocks = new MaterialPropertyBlock[_originals.Length];
                _keys = new TypeSettingKey<Color>[_originals.Length];
                _baseColors = new Color[_originals.Length];
                _baseDissolveAmounts = new float?[_originals.Length];
                _needsDissolveTexture = new bool[_originals.Length];
                _isModel = renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
                _supportsOutline = new bool[_originals.Length];
                _baseOutlineColors = new Color[_originals.Length];
                _baseOutlineWidths = new float[_originals.Length];
                for (int i = 0; i < _originals.Length; i++)
                {
                    Material material = _originals[i];
                    string property = material != null && material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    _keys[i] = new TypeSettingKey<Color>(property);
                    _blocks[i] = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(_blocks[i], i);
                    int id = Shader.PropertyToID(property);
                    _baseColors[i] = _blocks[i].HasColor(id) ? _blocks[i].GetColor(id)
                        : _blocks[i].isEmpty && _globalBlock.HasColor(id) ? _globalBlock.GetColor(id)
                        : material != null && material.HasProperty(id) ? material.GetColor(id) : Color.white;
                    // 디졸브와 아웃라인은 지원 여부가 다릅니다. 한쪽 미지원이 다른 층의 준비를 건너뛰지 않습니다.
                    if (_isModel && material != null && material.HasProperty(DissolveAmountId))
                    {
                        MaterialPropertyBlock originalBlock = _blocks[i].isEmpty ? _globalBlock : _blocks[i];
                        _baseDissolveAmounts[i] = originalBlock.HasFloat(DissolveAmountId)
                            ? originalBlock.GetFloat(DissolveAmountId)
                            : material.GetFloat(DissolveAmountId);
                        _needsDissolveTexture[i] = material.HasProperty(DissolveTextureId) &&
                            material.GetTexture(DissolveTextureId) == null && !originalBlock.HasTexture(DissolveTextureId);
                    }
                    _supportsOutline[i] = _isModel && material != null
                        && material.HasProperty(OutlineColorId) && material.HasProperty(OutlineWidthId);
                    if (_supportsOutline[i])
                    {
                        _baseOutlineColors[i] = _blocks[i].HasColor(OutlineColorId) ? _blocks[i].GetColor(OutlineColorId)
                            : _blocks[i].isEmpty && _globalBlock.HasColor(OutlineColorId) ? _globalBlock.GetColor(OutlineColorId)
                            : material.GetColor(OutlineColorId);
                        _baseOutlineWidths[i] = _blocks[i].HasFloat(OutlineWidthId) ? _blocks[i].GetFloat(OutlineWidthId)
                            : _blocks[i].isEmpty && _globalBlock.HasFloat(OutlineWidthId) ? _globalBlock.GetFloat(OutlineWidthId)
                            : material.GetFloat(OutlineWidthId);
                    }
                }
            }

            internal bool RequireOutlineSupport()
            {
                if (!_isModel || _renderer == null) return false;
                if (_originals.Length == 0)
                    throw new InvalidOperationException($"[UnitModelAppearance] 모델 '{_renderer.name}'에 아웃라인 재질 슬롯이 없습니다.");
                for (int i = 0; i < _originals.Length; i++)
                {
                    Material material = _originals[i];
                    if (!_supportsOutline[i] || material == null || material.FindPass("Outline") < 0
                        || !material.GetShaderPassEnabled("Outline"))
                        throw new InvalidOperationException($"[UnitModelAppearance] 모델 '{_renderer.name}'의 재질 슬롯 {i} ({material?.name})에 {ColorShaderPropertyKey.OutlineColor.Name}, {ColorShaderPropertyKey.OutlineWidth.Name} 및 활성 Outline Pass가 필요합니다.");
                }
                return true;
            }

            /// <summary>합친 층 값을 씁니다. 투명도 &lt; 1이면 투명 변형 재질, 아니면 원래 재질입니다. 재질은 바뀔 때만 바꿉니다.</summary>
            internal void Apply(float opacity, CombatFlashLook flash, float flashStrength, float dissolveAmount,
                bool hasOutline, Color outlineColor, float outlineWidth)
            {
                if (_renderer == null) return;
                // 강조만 남았을 때 파티클 등에는 새 색 손잡이를 만들지 않습니다.
                if (!_isModel && opacity >= 1f && flashStrength <= 0f && dissolveAmount <= 0f)
                {
                    Restore();
                    return;
                }
                bool transparent = opacity < 1f;
                if (transparent && _transparent == null)
                {
                    var prepared = new Material[_originals.Length];
                    try
                    {
                        for (int i = 0; i < _originals.Length; i++)
                            prepared[i] = CreateTransparent(_originals[i]);
                        _transparent = prepared;
                    }
                    finally
                    {
                        if (_transparent == null)
                            foreach (Material material in prepared) DestroyOwned(material);
                    }
                }
                if (_scope == null)
                {
                    // 슬롯 블록이 없으면 전체 Renderer 블록이 적용된다. 색 이외의 값도 손잡이에 보존한다.
                    for (int i = 0; i < _blocks.Length; i++)
                        if (_blocks[i].isEmpty && !_globalBlock.isEmpty) _renderer.SetPropertyBlock(_globalBlock, i);
                    _scope = _colors.CreateColorHandle(_renderer)
                        ?? throw new InvalidOperationException("[UnitModelAppearance] 모델 색 손잡이를 만들지 못했습니다.");
                }
                if (transparent != _transparentApplied)
                {
                    _renderer.sharedMaterials = transparent ? _transparent : _originals;
                    _transparentApplied = transparent;
                }
                for (int i = 0; i < _baseColors.Length; i++)
                {
                    Color baseColor = _baseColors[i];
                    Color color = flashStrength > 0f ? Color.Lerp(baseColor, flash.Body, flashStrength) : baseColor;
                    color.a = baseColor.a * opacity;
                    _scope.SetOverrideData(i, _keys[i], color);
                    if (_baseDissolveAmounts[i] is float baseDissolveAmount)
                    {
                        if (dissolveAmount > 0f && _needsDissolveTexture[i])
                        {
                            Texture texture = CartoonModelMaterialFallback.GetDefaults().GetTexture(DissolveTextureId);
                            if (texture == null)
                                throw new InvalidOperationException("[UnitModelAppearance] Cartoon 기본 디졸브 텍스처가 없습니다.");
                            _scope.SetOverrideData(i, ColorShaderPropertyKey.DissolveTexture, texture);
                            _needsDissolveTexture[i] = false;
                        }
                        _scope.SetOverrideData(i, ColorShaderPropertyKey.DissolveAmount,
                            dissolveAmount > 0f ? dissolveAmount : baseDissolveAmount);
                    }
                    if (_supportsOutline[i])
                    {
                        // 바탕은 강조 층이 있으면 그 외곽선, 없으면 재질 외곽선입니다. 피격 외곽선은 그 위에 세기만큼 섞어, 플래시가 끝나면 바탕으로 돌아옵니다.
                        Color lineColor = hasOutline ? outlineColor : _baseOutlineColors[i];
                        float lineWidth = hasOutline ? outlineWidth : _baseOutlineWidths[i];
                        if (flashStrength > 0f && flash.HasOutline)
                        {
                            lineColor = Color.Lerp(lineColor, flash.Outline, flashStrength);
                            lineWidth = Mathf.Lerp(lineWidth, flash.OutlineWidth, flashStrength);
                        }
                        _scope.SetOverrideData(i, ColorShaderPropertyKey.OutlineColor, lineColor);
                        _scope.SetOverrideData(i, ColorShaderPropertyKey.OutlineWidth, lineWidth);
                    }
                }
            }

            internal void Restore()
            {
                if (_renderer == null) return;
                // Dispose는 Renderer PropertyBlock 복원을 보장하지 않는다. 원래 슬롯과 블록을 직접 되돌린다.
                _renderer.sharedMaterials = _originals;
                _transparentApplied = false;
                for (int i = 0; i < _blocks.Length; i++)
                    _renderer.SetPropertyBlock(_blocks[i].isEmpty ? null : _blocks[i], i);
            }

            public void Dispose()
            {
                try
                {
                    Restore();
                }
                finally
                {
                    IColorHandle scope = _scope;
                    _scope = null;
                    try
                    {
                        scope?.Dispose();
                    }
                    finally
                    {
                        if (_transparent != null)
                        {
                            foreach (Material material in _transparent) DestroyOwned(material);
                            _transparent = null;
                        }
                    }
                }
            }
        }

        // Standard/URP 및 지원 계약을 선언한 셰이더의 복사본만 바꾼다. 공유 재질은 유지한다.
        private static Material CreateTransparent(Material source)
        {
            if (source == null || source.shader == null)
                throw new InvalidOperationException("[UnitModelAppearance] 모델 재질이 비어 있습니다.");
            string shader = source.shader.name;
            bool standard = shader == "Standard";
            bool urp = shader == "Universal Render Pipeline/Lit" || shader == "Universal Render Pipeline/Simple Lit" ||
                shader == "Universal Render Pipeline/Unlit";
            bool custom = string.Equals(source.GetTag("ProjectLMovementTransparency", false, string.Empty),
                "True", StringComparison.OrdinalIgnoreCase) &&
                (source.HasProperty("_BaseColor") || source.HasProperty("_Color")) &&
                source.HasProperty("_SrcBlend") && source.HasProperty("_DstBlend") &&
                source.HasProperty("_SrcBlendAlpha") && source.HasProperty("_DstBlendAlpha") && source.HasProperty("_ZWrite");
            if (!standard && !urp && !custom)
                throw new InvalidOperationException($"[UnitModelAppearance] 투명 변형을 지원하지 않는 셰이더입니다: {shader}.");
            var material = new Material(source) { name = source.name + "_MovementTransparent", hideFlags = HideFlags.DontSave };
            material.SetOverrideTag("RenderType", "Transparent");
            SetIfPresent(material, "_Mode", 2f);
            SetIfPresent(material, "_Surface", 1f);
            SetIfPresent(material, "_Blend", 0f);
            SetIfPresent(material, "_AlphaClip", 0f);
            SetIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetIfPresent(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetIfPresent(material, "_ZWrite", 0f);
            SetIfPresent(material, "_BlendModePreserveSpecular", 0f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            if (standard) material.EnableKeyword("_ALPHABLEND_ON");
            else if (urp) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("DepthNormals", false);
            if (custom)
            {
                // 조립기의 패스 이름 옵션 두 가지를 모두 지원한다.
                foreach (string pass in new[] { "SHADOWCASTER", "DEPTHONLY", "DEPTHNORMALS", "NormalOnly", "NORMALONLY" })
                    material.SetShaderPassEnabled(pass, false);
            }
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static void SetIfPresent(Material material, string name, float value)
        {
            if (material.HasProperty(name)) material.SetFloat(name, value);
        }

        internal static void DestroyOwned(UnityEngine.Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(item);
            else UnityEngine.Object.DestroyImmediate(item);
        }
    }
}
