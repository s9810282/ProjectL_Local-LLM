using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>윤곽선과 연결선에서 공유하는 색상·폭 전환입니다. 목표가 바뀐 때의 표시값에서 이어집니다.</summary>
    internal sealed class StageHighlightTransition
    {
        private Color _fromColor;
        private Color _targetColor;
        private float _fromWidth;
        private float _targetWidth;
        private float _duration;
        private float _elapsed;

        internal StageHighlightTransition(Color color, float width)
        {
            Color = _targetColor = color;
            Width = _targetWidth = width;
        }

        internal Color Color { get; private set; }
        internal float Width { get; private set; }
        internal bool IsFinished => _elapsed >= _duration;

        internal void SetTarget(Color color, float width, float duration)
        {
            if (_targetColor.Equals(color) && _targetWidth.Equals(width)) return;
            _fromColor = Color;
            _fromWidth = Width;
            _targetColor = color;
            _targetWidth = width;
            _duration = duration;
            _elapsed = 0f;
        }

        internal bool Tick(float deltaTime)
        {
            Color previousColor = Color;
            float previousWidth = Width;
            _elapsed = Mathf.Min(_elapsed + Mathf.Max(0f, deltaTime), _duration);
            float t = _duration == 0f ? 1f : Mathf.SmoothStep(0f, 1f, _elapsed / _duration);
            Color = UnityEngine.Color.LerpUnclamped(_fromColor, _targetColor, t);
            Width = Mathf.LerpUnclamped(_fromWidth, _targetWidth, t);
            return !previousColor.Equals(Color) || !previousWidth.Equals(Width);
        }
    }
}
