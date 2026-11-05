using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>TurnCost 내부의 배경과 Fill. 갱신 시간은 그룹이 전달하며 독립 UI로 등록하지 않는다.</summary>
    [DisallowMultipleComponent]
    public sealed class TurnCostSlot : UIWidgetBase
    {
        [SerializeField] private Image costImage;
        [SerializeField] private Image fillCost;
        [SerializeField, Min(0.05f)] private float previewPulseDuration = 1.2f;
        [SerializeField, Range(0f, 1f)] private float previewMinAlpha = 0.15f;
        [SerializeField, Min(0f)] private float consumeDuration = 0.25f;

        private bool _initialized;
        private bool _included;
        private bool _available;
        private bool _preview;
        private bool _consuming;
        private float _elapsed;
        private Color _fillColor;
        private Vector3 _fillScale;

        internal void ValidateReferences()
        {
            if (costImage == null || costImage.gameObject != gameObject)
                throw new InvalidOperationException($"[TurnCostSlot] UIId.TurnCost '{name}': 같은 객체의 costImage가 필요합니다.");
            if (fillCost == null || fillCost == costImage || !fillCost.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[TurnCostSlot] UIId.TurnCost '{name}': 별도 자식 Image인 fillCost가 필요합니다.");
            if (!IsFinite(previewPulseDuration) || previewPulseDuration < 0.05f ||
                !IsFinite(previewMinAlpha) || previewMinAlpha < 0f || previewMinAlpha > 1f ||
                !IsFinite(consumeDuration) || consumeDuration < 0f)
                throw new InvalidOperationException($"[TurnCostSlot] UIId.TurnCost '{name}': previewPulseDuration, previewMinAlpha 또는 consumeDuration 설정이 잘못되었습니다.");
        }

        internal void SetValue(bool included, bool available, bool snap)
        {
            Initialize();
            bool spend = _available && !available && included && !snap && isActiveAndEnabled;
            _included = included;
            _available = available;
            _preview = false;
            if (snap || available || !included) _consuming = false;
            if (spend)
            {
                _elapsed = 0f;
                _consuming = consumeDuration > 0f;
            }
            ApplyFill();
        }

        internal void SetPreview(bool preview)
        {
            preview &= _included && _available && isActiveAndEnabled;
            if (_preview == preview) return;
            _preview = preview;
            _elapsed = 0f;
            ApplyFill();
        }

        public override void Open()
        {
            ValidateReferences();
            // 그룹 Open은 이미 소모된 Fill을 복구하거나 최대치 밖 슬롯을 표시하지 않는다.
            if (_included) base.Open();
            else base.Hide();
        }

        internal void Advance(float deltaTime)
        {
            if (!isActiveAndEnabled || (!_preview && !_consuming)) return;
            if (_preview)
                _elapsed = Mathf.Repeat(_elapsed + deltaTime, previewPulseDuration);
            else
            {
                _elapsed = Mathf.Min(_elapsed + deltaTime, consumeDuration);
                if (_elapsed >= consumeDuration) _consuming = false;
            }
            ApplyFill();
        }

        internal void CompletePresentation()
        {
            // 씬 수집·바인딩 전에 비활성화되는 것은 정상이며 아직 참조를 사용할 필요가 없다.
            if (!_initialized) return;
            ValidateReferences();
            _preview = _consuming = false;
            _elapsed = 0f;
            ApplyFill();
        }

        protected override void OnUIHidden() => CompletePresentation();

        private void Initialize()
        {
            if (_initialized) return;
            ValidateReferences();
            _fillColor = fillCost.color;
            _fillScale = fillCost.rectTransform.localScale;
            costImage.raycastTarget = false;
            fillCost.raycastTarget = false;
            _initialized = true;
        }

        private void ApplyFill()
        {
            Color color = _fillColor;
            float scale = 1f;
            if (_preview)
                color.a *= Mathf.Lerp(previewMinAlpha, 1f,
                    (1f + Mathf.Cos(_elapsed / previewPulseDuration * Mathf.PI * 2f)) * 0.5f);
            else if (_consuming)
            {
                scale = 1f - Mathf.Clamp01(_elapsed / consumeDuration);
                color.a *= scale;
            }
            fillCost.color = color;
            fillCost.rectTransform.localScale = _fillScale * scale;
            fillCost.gameObject.SetActive(_included && (_available || _consuming));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
