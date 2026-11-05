using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    public interface IUISliderReceiver
    {
        void ValueChanged(int slot, float value);
    }

    /// <summary>기본 Slider 입력과 표시를 유지하면서 값 변경을 명시적인 수신자에게 전달합니다.</summary>
    public sealed class UIValueSlider : Slider
    {
        [SerializeField] private TMP_Text percentage;
        private IUISliderReceiver _receiver;
        private Object _receiverObject;
        private int _slot;

        public void Bind(IUISliderReceiver receiver, int slot)
        {
            _receiver = receiver;
            _receiverObject = receiver as Object;
            _slot = slot;
        }

        protected override void Set(float input, bool sendCallback = true)
        {
            float calculated = Mathf.Clamp(input, minValue, maxValue);
            if (wholeNumbers) calculated = Mathf.Round(calculated);
            float percent = Mathf.InverseLerp(minValue, maxValue, calculated) * 100f;
            // 초기 표시·외부 갱신·사용자 입력 모두 텍스트를 먼저 갱신한 뒤 Slider에 적용한다.
            if (percentage != null) percentage.SetText("{0:0}%", percent);
            float previous = value;
            base.Set(calculated, sendCallback);
            if (!sendCallback || value == previous || !isActiveAndEnabled || !IsInteractable()
                || _receiver == null || (_receiver is Object && _receiverObject == null)) return;
            _receiver.ValueChanged(_slot, value);
        }
    }
}
