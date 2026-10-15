using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 셀 하나의 표시 손잡이입니다. 위치·표시 여부와 강조 색만 다룹니다(옛 FieldCellHandle 이식).
    /// </summary>
    /// <remarks>
    /// 색은 Renderer마다 하나씩 받은 ColorScop으로만 바꿉니다(FW-011·설계 §13.3). ColorScop에는 덮어쓰기 해제가 없어
    /// 기본 색을 읽어 두었다가 다시 넣습니다. Scope 반환은 ColorSystem.Shutdown이 맡으므로 여기서 Dispose하지 않습니다.
    /// 새 Render의 키에는 BaseMap이 없어 옛 코드의 텍스처 끄기는 옮기지 않았습니다(색만 곱해집니다).
    /// </remarks>
    internal sealed class FieldCellHandle
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly GameObject _anchor;
        private readonly IColorHandle[] _colorScopes;
        private readonly Color[][] _defaultColors;
        private bool _isHighlighted;
        private Color _highlight;

        internal FieldCellHandle(GameObject anchor, Renderer[] renderers, IColorSystemService colors)
        {
            _anchor = anchor != null ? anchor : throw new ArgumentNullException(nameof(anchor));
            if (renderers == null)
                throw new ArgumentNullException(nameof(renderers));
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));
            if (renderers.Length == 0)
                throw new ArgumentException("셀에는 Renderer가 하나 이상 필요합니다.", nameof(renderers));

            _colorScopes = new IColorHandle[renderers.Length];
            _defaultColors = new Color[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    throw new ArgumentException("Renderer 배열에 null이 있습니다.", nameof(renderers));

                _colorScopes[i] = colors.CreateColorHandle(renderers[i])
                    ?? throw new InvalidOperationException("ColorSystem이 셀 Renderer의 Scope를 만들지 못했습니다.");
                _defaultColors[i] = ReadDefaultColors(renderers[i]);
            }
        }

        /// <summary>위치와 셀 크기 배율을 설정합니다. 풀에서 다시 빌려도 배율을 누적하지 않습니다.</summary>
        internal void ApplyLayout(Vector3 localPosition, Vector3 cellOffsetSize)
        {
            Transform anchor = _anchor.transform;
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = cellOffsetSize;
        }

        internal void SetVisible(bool visible)
        {
            if (_anchor.activeSelf != visible)
                _anchor.SetActive(visible);
        }

        internal void SetHighlight(Color color)
        {
            if (_isHighlighted && _highlight == color)
                return;

            for (int i = 0; i < _colorScopes.Length; i++)
            {
                for (int m = 0; m < _defaultColors[i].Length; m++)
                    _colorScopes[i].SetOverrideData(m, MaterialBaseTypeKey.MainColor, color);
            }

            _isHighlighted = true;
            _highlight = color;
        }

        internal void ClearHighlight()
        {
            if (!_isHighlighted)
                return;

            for (int i = 0; i < _colorScopes.Length; i++)
            {
                for (int m = 0; m < _defaultColors[i].Length; m++)
                    _colorScopes[i].SetOverrideData(m, MaterialBaseTypeKey.MainColor, _defaultColors[i][m]);
            }

            _isHighlighted = false;
        }

        /// <summary>풀로 돌아갈 때 강조를 지우고 숨깁니다.</summary>
        internal void ResetForPool()
        {
            ClearHighlight();
            SetVisible(false);
        }

        private static Color[] ReadDefaultColors(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            var colors = new Color[materials.Length];
            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];
                colors[m] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }

            return colors;
        }
    }
}
