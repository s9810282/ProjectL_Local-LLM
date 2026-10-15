using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 보유 목록 슬롯 안에 아티팩트 모양을 그립니다. 입력은 슬롯 Button의 <c>UIInputBinding</c>이 전달합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArtifactPoolItemView : MonoBehaviour
    {
        private readonly List<Image> _shapeCells = new();
        private CustomPool<Image> _shapePool;

        /// <summary>받은 로컬 셀을 UI 아이콘으로 그립니다. 배치 규칙이나 회전 계산은 하지 않습니다.</summary>
        internal void DrawShape(IReadOnlyList<CellCoord> cells)
        {
            ClearShape();
            if (cells == null || cells.Count == 0) return;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var cell in cells)
            {
                minX = Math.Min(minX, cell.X); minY = Math.Min(minY, cell.Y);
                maxX = Math.Max(maxX, cell.X); maxY = Math.Max(maxY, cell.Y);
            }
            Rect area = ((RectTransform)transform).rect;
            float unit = Mathf.Min(area.width, area.height) * .55f / Math.Max(maxX - minX + 1, maxY - minY + 1);
            _shapePool ??= new CustomPool<Image>(new PoolContext<Image>(() =>
            {
                var obj = new GameObject("ShapeCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                obj.transform.SetParent(transform, false);
                var image = obj.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = new Color(.15f, .8f, .95f, 1);
                obj.SetActive(false);
                return image;
            }, 0));
            foreach (var cell in cells)
            {
                Image image = _shapePool.Get();
                _shapeCells.Add(image);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2((cell.X - (minX + maxX) * .5f) * unit,
                    (cell.Y - (minY + maxY) * .5f) * unit);
                rect.sizeDelta = Vector2.one * unit * .88f;
                image.gameObject.SetActive(true);
            }
        }

        /// <summary>그린 모양 셀을 모두 풀에 돌려줍니다. 여러 번 불러도 안전합니다.</summary>
        internal void ClearShape()
        {
            foreach (Image image in _shapeCells)
            {
                image.gameObject.SetActive(false);
                _shapePool.Return(image);
            }
            _shapeCells.Clear();
        }
    }
}
