using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>UI·모델 강조 영역을 합친 바깥에 흰 점선을 그립니다. 겹친 영역 안쪽의 선은 표시하지 않습니다.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [DisallowMultipleComponent]
    public sealed class TutorialFocusBorderGraphic : UnityEngine.UI.MaskableGraphic
    {
        // VertexHelper는 정점 65,000개 이상을 허용하지 않습니다. 점선 하나는 정점 4개입니다.
        private const int MaximumDashCount = 16249;
        private readonly List<Rect> _screenRects = new();
        private readonly List<Rect> _localRects = new();
        private readonly List<Rect> _dashPieces = new();
        private float _padding;
        private float _thickness = 2f;
        private float _dashLength = 10f;
        private float _dashGap = 6f;
        private Color _tint = Color.white;

        public override bool raycastTarget
        {
            get => false;
            set { }
        }

        internal void SetScreenRects(IReadOnlyList<Rect> rects, float padding, TutorialHighlightStyle style)
        {
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));
            if (style == null)
                throw new ArgumentNullException(nameof(style));
            style.Validate();

            float margin = Mathf.Max(0f, padding);
            ValidateVertexBudget(rects, margin, style.BorderThickness, style.DashLength + style.DashGap);
            bool changed = _screenRects.Count != rects.Count || _padding != margin
                || _thickness != style.BorderThickness || _dashLength != style.DashLength
                || _dashGap != style.DashGap || !_tint.Equals(style.UiBorderColor);
            _padding = margin;
            _thickness = style.BorderThickness;
            _dashLength = style.DashLength;
            _dashGap = style.DashGap;
            _tint = style.UiBorderColor;

            for (int i = 0; i < rects.Count; i++)
            {
                Rect local = TutorialFocusGeometry.ToLocal(rectTransform, rects[i], margin);
                if (i < _screenRects.Count)
                {
                    changed |= !_screenRects[i].Equals(rects[i]) || !_localRects[i].Equals(local);
                    _screenRects[i] = rects[i];
                    _localRects[i] = local;
                }
                else
                {
                    _screenRects.Add(rects[i]);
                    _localRects.Add(local);
                }
            }
            if (_screenRects.Count > rects.Count)
            {
                _screenRects.RemoveRange(rects.Count, _screenRects.Count - rects.Count);
                _localRects.RemoveRange(rects.Count, _localRects.Count - rects.Count);
            }
            if (changed)
                SetVerticesDirty();
        }

        internal void Clear()
        {
            _dashPieces.Clear();
            if (_screenRects.Count == 0)
                return;
            _screenRects.Clear();
            _localRects.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            // Canvas 크기 변경으로 메시가 다시 생성되는 경우에도 전체 예산을 먼저 확인합니다.
            ValidateVertexBudget(_screenRects, _padding, _thickness, _dashLength + _dashGap);
            // 모든 영역을 현재 배율로 먼저 갱신해야 뒤쪽 대상도 같은 좌표로 선을 가립니다.
            for (int i = 0; i < _screenRects.Count; i++)
                _localRects[i] = TutorialFocusGeometry.ToLocal(rectTransform, _screenRects[i], _padding);
            for (int i = 0; i < _localRects.Count; i++)
            {
                Rect rect = _localRects[i];
                if (rect.width <= 0f || rect.height <= 0f)
                    continue;
                AddEdge(vh, rect.xMin - _thickness, rect.xMax + _thickness, rect.yMin - _thickness, true);
                AddEdge(vh, rect.xMin - _thickness, rect.xMax + _thickness, rect.yMax, true);
                AddEdge(vh, rect.yMin, rect.yMax, rect.xMin - _thickness, false);
                AddEdge(vh, rect.yMin, rect.yMax, rect.xMax, false);
            }
        }

        private void AddEdge(VertexHelper vh, float start, float end, float cross, bool horizontal)
        {
            float stride = _dashLength + _dashGap;
            int count = (int)Math.Ceiling((double)(end - start) / stride);
            for (int i = 0; i < count; i++)
            {
                float from = start + i * stride;
                float to = Mathf.Min(from + _dashLength, end);
                Rect dash = horizontal
                    ? Rect.MinMaxRect(from, cross, to, cross + _thickness)
                    : Rect.MinMaxRect(cross, from, cross + _thickness, to);
                AddVisibleDash(vh, dash);
            }
        }

        private void AddVisibleDash(VertexHelper vh, Rect dash)
        {
            _dashPieces.Clear();
            _dashPieces.Add(dash);
            // 덮개와 동일한 밝은 영역의 합집합을 점선에서 뺍니다. 선의 일부만 겹쳐도 그 부분만 잘라냅니다.
            foreach (Rect hole in _localRects)
            {
                if (hole.width <= 0f || hole.height <= 0f) continue;
                for (int i = _dashPieces.Count - 1; i >= 0; i--)
                {
                    Rect piece = _dashPieces[i];
                    float left = Mathf.Max(piece.xMin, hole.xMin);
                    float right = Mathf.Min(piece.xMax, hole.xMax);
                    float bottom = Mathf.Max(piece.yMin, hole.yMin);
                    float top = Mathf.Min(piece.yMax, hole.yMax);
                    if (left >= right || bottom >= top) continue;
                    _dashPieces.RemoveAt(i);
                    // 겹친 사각형 밖의 네 조각은 서로 겹치지 않습니다.
                    AddPiece(piece.xMin, piece.yMin, piece.xMax, bottom);
                    AddPiece(piece.xMin, top, piece.xMax, piece.yMax);
                    AddPiece(piece.xMin, bottom, left, top);
                    AddPiece(right, bottom, piece.xMax, top);
                }
                if (_dashPieces.Count == 0) return;
            }
            // 잘림으로 조각 수가 늘 수 있어 실제 출력 정점 수도 따로 제한합니다.
            if (vh.currentVertCount / 4 + _dashPieces.Count > MaximumDashCount)
                throw new InvalidOperationException("[UIId.Tutorial] focusBorder의 겹침 제거 결과가 UI 메시 한도를 넘습니다. 점선 길이 또는 간격을 늘려 주세요.");
            foreach (Rect piece in _dashPieces)
            {
                int first = vh.currentVertCount;
                Color32 tint = _tint;
                vh.AddVert(new Vector3(piece.xMin, piece.yMin), tint, Vector2.zero);
                vh.AddVert(new Vector3(piece.xMin, piece.yMax), tint, Vector2.zero);
                vh.AddVert(new Vector3(piece.xMax, piece.yMax), tint, Vector2.zero);
                vh.AddVert(new Vector3(piece.xMax, piece.yMin), tint, Vector2.zero);
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first + 2, first + 3, first);
            }
        }

        private void AddPiece(float left, float bottom, float right, float top)
        {
            if (left >= right || bottom >= top) return;
            if (_dashPieces.Count >= MaximumDashCount)
                throw new InvalidOperationException("[UIId.Tutorial] focusBorder의 겹침 제거 조각이 UI 메시 한도를 넘습니다.");
            _dashPieces.Add(Rect.MinMaxRect(left, bottom, right, top));
        }

        private void ValidateVertexBudget(IReadOnlyList<Rect> rects, float padding, float thickness, float stride)
        {
            double count = 0;
            for (int i = 0; i < rects.Count; i++)
            {
                Rect rect = TutorialFocusGeometry.ToLocal(rectTransform, rects[i], padding);
                if (rect.width <= 0f || rect.height <= 0f)
                    continue;
                float horizontal = (rect.xMax + thickness) - (rect.xMin - thickness);
                count += 2 * Math.Ceiling((double)horizontal / stride)
                    + 2 * Math.Ceiling((double)(rect.yMax - rect.yMin) / stride);
                if (double.IsNaN(count) || double.IsInfinity(count) || count > MaximumDashCount)
                    throw new InvalidOperationException("[UIId.Tutorial] focusBorder의 강조 영역과 highlightStyle.dashLength/dashGap 조합이 UI 메시 한도를 넘습니다. 점선 길이 또는 간격을 늘려 주세요.");
            }
        }
    }
}
