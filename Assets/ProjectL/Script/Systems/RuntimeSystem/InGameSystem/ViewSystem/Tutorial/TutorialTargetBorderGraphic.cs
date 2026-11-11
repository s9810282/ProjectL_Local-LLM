using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>실제 UI 하나하나의 가장자리를 따라 점선을 그립니다. 카드의 회전·크기도 그대로 따릅니다.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [DisallowMultipleComponent]
    public sealed class TutorialTargetBorderGraphic : UnityEngine.UI.MaskableGraphic
    {
        private const int MaximumDashCount = 16249;
        private readonly List<RectTransform> _targets = new();
        private readonly List<Quad> _quads = new();
        private readonly List<Quad> _pending = new();
        private readonly Vector3[] _worldCorners = new Vector3[4];
        private readonly Vector2[] _localCorners = new Vector2[4];
        private Color _tint;
        private float _thickness = 3f;
        private float _dashLength = 10f;
        private float _dashGap = 6f;
        private bool _blinking;
        private float _blinkStartedAt;
        private float _blinkPeriod;
        private float _blinkMinAlpha;

        public override bool raycastTarget
        {
            get => false;
            set { }
        }

        internal void SetTargets(IReadOnlyList<RectTransform> targets, TutorialHighlightStyle style)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (style == null) throw new ArgumentNullException(nameof(style));
            style.Validate();
            ReadQuads(targets);
            ValidateVertexBudget(style.DashLength + style.DashGap);
            bool changed = !_tint.Equals(style.TargetBorderColor) || _thickness != style.TargetBorderThickness
                || _dashLength != style.DashLength || _dashGap != style.DashGap || _quads.Count != _pending.Count;
            for (int i = 0; !changed && i < _quads.Count; i++)
                changed = !_quads[i].Matches(_pending[i]);

            _targets.Clear();
            foreach (RectTransform target in targets) _targets.Add(target);
            _tint = style.TargetBorderColor;
            _thickness = style.TargetBorderThickness;
            _dashLength = style.DashLength;
            _dashGap = style.DashGap;
            _quads.Clear();
            _quads.AddRange(_pending);
            if (_quads.Count == 0) ResetBlink();
            if (changed) SetVerticesDirty();
        }

        internal void SetBlink(bool enabled, TutorialHighlightStyle style, float now)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));
            style.Validate();
            if (!enabled || !style.BlinkEnabled || style.BlinkMinAlpha >= 1f || _quads.Count == 0)
            {
                ResetBlink();
                return;
            }
            if (!_blinking || _blinkPeriod != style.BlinkPeriod || _blinkMinAlpha != style.BlinkMinAlpha)
            {
                _blinking = true;
                _blinkStartedAt = now;
                _blinkPeriod = style.BlinkPeriod;
                _blinkMinAlpha = style.BlinkMinAlpha;
            }
            TickBlink(now);
        }

        internal void TickBlink(float now)
        {
            if (!_blinking) return;
            float phase = (now - _blinkStartedAt) / _blinkPeriod;
            float alpha = _blinkMinAlpha + (1f - _blinkMinAlpha) * (1f + Mathf.Cos(phase * 2f * Mathf.PI)) * 0.5f;
            // 색의 원래 알파에 곱하는 표시 배율만 갱신합니다. 메시·모델 대여는 다시 만들지 않습니다.
            canvasRenderer.SetAlpha(alpha);
        }

        private void ResetBlink()
        {
            _blinking = false;
            canvasRenderer.SetAlpha(1f);
        }

        protected override void OnDisable()
        {
            ResetBlink();
            base.OnDisable();
        }

        internal void Clear()
        {
            ResetBlink();
            bool hadGeometry = _quads.Count > 0;
            _targets.Clear();
            _quads.Clear();
            _pending.Clear();
            if (hadGeometry) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            // Canvas 배율 변경에 따른 메시 재생성에서도 현재 대상의 네 모서리를 다시 읽습니다.
            ReadQuads(_targets);
            ValidateVertexBudget(_dashLength + _dashGap);
            _quads.Clear();
            _quads.AddRange(_pending);
            foreach (Quad quad in _quads)
            {
                for (int edge = 0; edge < 4; edge++)
                    AddEdge(vh, quad.Corner(edge), quad.Corner((edge + 1) % 4), quad.Center);
            }
        }

        private void ReadQuads(IReadOnlyList<RectTransform> targets)
        {
            _pending.Clear();
            foreach (RectTransform target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy || target.rect.width <= 0f || target.rect.height <= 0f)
                    continue;
                Canvas canvas = target.GetComponentInParent<Canvas>();
                Canvas root = canvas != null ? canvas.rootCanvas : null;
                Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
                target.GetWorldCorners(_worldCorners);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, _worldCorners[i]);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out Vector2 point);
                    if (!IsFinite(point.x) || !IsFinite(point.y))
                        throw new InvalidOperationException($"[UIId.Tutorial] targetBorder 대상 '{target.name}'의 모서리 좌표가 유효하지 않습니다.");
                    _localCorners[i] = point;
                }
                _pending.Add(new Quad(_localCorners[0], _localCorners[1], _localCorners[2], _localCorners[3]));
            }
        }

        private void ValidateVertexBudget(float stride)
        {
            double count = 0;
            foreach (Quad quad in _pending)
            {
                for (int edge = 0; edge < 4; edge++)
                    count += Math.Ceiling(Vector2.Distance(quad.Corner(edge), quad.Corner((edge + 1) % 4)) / (double)stride);
                if (double.IsNaN(count) || double.IsInfinity(count) || count > MaximumDashCount)
                    throw new InvalidOperationException("[UIId.Tutorial] targetBorder가 UI 메시 한도를 넘습니다. highlightStyle.dashLength 또는 dashGap을 늘려 주세요.");
            }
        }

        private void AddEdge(VertexHelper vh, Vector2 from, Vector2 to, Vector2 center)
        {
            float length = Vector2.Distance(from, to);
            if (length <= 0f) return;
            Vector2 direction = (to - from) / length;
            Vector2 inward = new Vector2(-direction.y, direction.x);
            if (Vector2.Dot(inward, center - (from + to) * 0.5f) < 0f) inward = -inward;
            // 기존 흰 강조 영역과 겹치지 않게 실제 대상 가장자리의 안쪽으로 선 두께를 줍니다.
            Vector2 offset = inward * _thickness;
            float stride = _dashLength + _dashGap;
            int count = (int)Math.Ceiling(length / (double)stride);
            for (int i = 0; i < count; i++)
            {
                Vector2 start = from + direction * (i * stride);
                Vector2 end = from + direction * Mathf.Min(length, i * stride + _dashLength);
                int first = vh.currentVertCount;
                vh.AddVert(start, _tint, Vector2.zero);
                vh.AddVert(start + offset, _tint, Vector2.zero);
                vh.AddVert(end + offset, _tint, Vector2.zero);
                vh.AddVert(end, _tint, Vector2.zero);
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first + 2, first + 3, first);
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private readonly struct Quad
        {
            private readonly Vector2 _a;
            private readonly Vector2 _b;
            private readonly Vector2 _c;
            private readonly Vector2 _d;
            internal Vector2 Center => (_a + _b + _c + _d) * 0.25f;
            internal Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                _a = a;
                _b = b;
                _c = c;
                _d = d;
            }
            internal Vector2 Corner(int index) => index switch { 0 => _a, 1 => _b, 2 => _c, _ => _d };
            internal bool Matches(Quad other) => _a.Equals(other._a) && _b.Equals(other._b)
                && _c.Equals(other._c) && _d.Equals(other._d);
        }
    }
}
