using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel
{
    /// <summary>기존 화면의 고정 배치를 종이 안전 영역에 같은 비율로 맞춥니다. 펼침 마스크 폭은 읽지 않습니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScrollContentFit : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private Vector2 referenceSize = new Vector2(1920f, 1080f);
        private RectTransform _rect;
        private Vector2 _lastSize = new Vector2(-1f, -1f);

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            if (viewport == null || viewport != transform.parent || referenceSize.x <= 0f || referenceSize.y <= 0f)
                throw new InvalidOperationException($"[ScrollContentFit] '{name}'의 직계 부모 viewport와 양수 기준 크기가 필요합니다.");
            Fit();
        }

        private void LateUpdate() => Fit();

        private void Fit()
        {
            if (_rect == null || viewport == null) return;
            Vector2 size = viewport.rect.size;
            if (size == _lastSize) return;
            _lastSize = size;
            float scale = Mathf.Max(0f, Mathf.Min(size.x / referenceSize.x, size.y / referenceSize.y));
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = referenceSize;
            _rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
