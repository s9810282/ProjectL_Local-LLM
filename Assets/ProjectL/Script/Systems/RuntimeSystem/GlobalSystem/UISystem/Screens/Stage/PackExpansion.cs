using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>가방의 배경과 목록 마스크를 같은 하단 경계까지 펼친다.</summary>
    [DisallowMultipleComponent]
    public sealed class PackExpansion : UIWidgetBase
    {
        [SerializeField] private RectTransform panelImage;
        [SerializeField] private RectMask2D historyMask;
        [SerializeField] private CanvasGroup historyInput;
        [SerializeField] private ScrollRect scrollView;
        [SerializeField, Min(0f)] private float expandDuration = 0.25f;
        [SerializeField, Min(0f)] private float collapseDuration = 0.18f;

        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _panelParent;
        private Vector2 _closedPosition;
        private float _closedHeight;
        private float _closedBottom;
        private float _progress;
        private float _target;
        private float _start;
        private float _elapsed;
        private float _duration;
        private bool _initialized;

        /// <summary>현재 높이에서 펼침 방향을 바꾼다. 즉시 지정은 화면 종료·초기화에 사용한다.</summary>
        public void SetExpanded(bool expanded, bool immediate = false)
        {
            Initialize();
            float target = expanded ? 1f : 0f;
            if (!immediate && _target == target) return;

            scrollView.gameObject.SetActive(true);
            // ScrollRect가 Viewport를 구동하므로 완성된 레이아웃을 먼저 반영한다.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scrollView.transform);
            scrollView.StopMovement();
            _target = target;
            _start = _progress;
            _elapsed = 0f;
            _duration = (expanded ? expandDuration : collapseDuration) * Mathf.Abs(_target - _start);
            if (immediate || _duration <= 0f) _progress = _target;
            Apply();
        }

        private void Initialize()
        {
            if (_initialized) return;
            ValidateReferences();
            _panelParent = (RectTransform)panelImage.parent;
            _closedPosition = panelImage.anchoredPosition;
            _closedHeight = panelImage.rect.height;
            _closedBottom = panelImage.localPosition.y - panelImage.pivot.y * _closedHeight;
            _initialized = true;
        }

        internal void ValidateReferences()
        {
            if (panelImage == null || historyMask == null || historyInput == null ||
                scrollView == null || scrollView.viewport == null)
            {
                throw new InvalidOperationException("[PackExpansion] Panel Image, History Mask/Input, Scroll View/Viewport 참조가 필요합니다.");
            }
            if (!(panelImage.parent is RectTransform))
                throw new InvalidOperationException("[PackExpansion] Panel Image의 부모 RectTransform이 필요합니다.");
            if (scrollView.gameObject == gameObject || !scrollView.transform.IsChildOf(transform)
                || !historyMask.transform.IsChildOf(scrollView.transform)
                || historyInput.gameObject != scrollView.gameObject || scrollView.content == null)
                throw new InvalidOperationException("[PackExpansion] UIId.Pack의 별도 자식 Scroll View, Content, 내부 Mask와 Scroll View CanvasGroup이 필요합니다.");
        }

        private void LateUpdate() => Advance(Time.unscaledDeltaTime);

        private void Advance(float deltaTime)
        {
            if (!_initialized || _progress == _target) return;
            _elapsed += deltaTime;
            float time = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
            float eased = 1f - Mathf.Pow(1f - time, 3f);
            _progress = time >= 1f ? _target : Mathf.LerpUnclamped(_start, _target, eased);
            Apply();
        }

        private void Apply()
        {
            scrollView.viewport.GetWorldCorners(_corners);
            float bottom = float.PositiveInfinity;
            for (int i = 0; i < _corners.Length; i++)
                bottom = Mathf.Min(bottom, _panelParent.InverseTransformPoint(_corners[i]).y);
            float extension = Mathf.Max(0f, _closedBottom - bottom) * _progress;
            panelImage.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _closedHeight + extension);
            panelImage.anchoredPosition = _closedPosition - Vector2.up * (extension * (1f - panelImage.pivot.y));

            RectTransform maskTransform = historyMask.rectTransform;
            Vector3 edge = _panelParent.TransformPoint(new Vector3(0f, _closedBottom - extension, 0f));
            float maskBottom = maskTransform.InverseTransformPoint(edge).y;
            Vector4 padding = historyMask.padding;
            padding.y = Mathf.Clamp(maskBottom - maskTransform.rect.yMin, 0f, maskTransform.rect.height);
            historyMask.padding = padding;

            bool ready = _target == 1f && _progress == 1f;
            historyInput.blocksRaycasts = ready;
            historyInput.interactable = ready;
            scrollView.vertical = ready;
            if (_target == 0f && _progress == 0f) scrollView.gameObject.SetActive(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_initialized) Apply();
        }

        // 펼침/접힘은 SetExpanded로 제어한다. 같은 객체의 소유 화면을 Open/Hide하지 않는다.
        protected override void OnUIHidden()
        {
            if (_initialized) SetExpanded(false, true);
        }
    }
}
