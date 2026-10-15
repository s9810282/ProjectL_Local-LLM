using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    public interface IUIInputReceiver
    {
        void Click(int slot);
        void HoverEnter(int slot);
        void HoverExit(int slot);
    }

    /// <summary>Unity 입력을 구독 없이 명시적인 대상과 슬롯으로 전달합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class UIInputBinding : MonoBehaviour, IPointerClickHandler, ISubmitHandler,
        IPointerEnterHandler, IPointerExitHandler, UIHoverAction
    {
        private IUIInputReceiver _receiver;
        private Object _receiverObject;
        private Button _button;
        private int _slot;
        private bool _hovered;

        public static UIInputBinding Attach(Button button, IUIInputReceiver receiver, int slot)
        {
            if (button == null) return null;
            var binding = button.GetComponent<UIInputBinding>();
            if (binding == null) binding = button.gameObject.AddComponent<UIInputBinding>();
            binding.Bind(receiver, slot);
            return binding;
        }

        public void Bind(IUIInputReceiver receiver, int slot)
        {
            OnHoverExit();
            _button = GetComponent<Button>();
            _receiver = receiver;
            _receiverObject = receiver as Object;
            _slot = slot;
        }

        private bool ReceiverAvailable => _receiver != null &&
            (!(_receiver is Object) || _receiverObject != null);
        private bool CanInteract => isActiveAndEnabled && ReceiverAvailable &&
            _button != null && _button.IsActive() && _button.IsInteractable();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && CanInteract)
                _receiver.Click(_slot);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (CanInteract) _receiver.Click(_slot);
        }

        public void OnPointerEnter(PointerEventData eventData) => OnHoverEnter();
        public void OnPointerExit(PointerEventData eventData) => OnHoverExit();
        public void OnHoverEnter()
        {
            if (_hovered || !CanInteract) return;
            _hovered = true;
            _receiver.HoverEnter(_slot);
        }
        public void OnHoverExit()
        {
            if (!_hovered) return;
            _hovered = false;
            if (ReceiverAvailable) _receiver.HoverExit(_slot);
        }

        // Button.interactable 변경에는 알림이 없으므로 Hover 중에만 상태를 확인한다.
        protected virtual void LateUpdate()
        {
            if (_hovered && !CanInteract) OnHoverExit();
        }
        protected virtual void OnDisable() => OnHoverExit();
    }
}
