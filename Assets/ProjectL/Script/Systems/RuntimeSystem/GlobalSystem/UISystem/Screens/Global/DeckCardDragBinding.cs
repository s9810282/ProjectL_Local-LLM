using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>카드 드래그 시작을 배치 선택으로 전달합니다. 이동·놓기·집기 수명은 전투 입력 시스템이 처리합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class DeckCardDragBinding : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private IUIInputReceiver _receiver;
        private Object _receiverObject;
        private Button _button;
        private int _slot;
        private bool _dragging;
        private int _pointerId;

        internal void Bind(IUIInputReceiver receiver, int slot)
        {
            EndDrag();
            _receiver = receiver;
            _receiverObject = receiver as Object;
            _button = GetComponent<Button>();
            _slot = slot;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_dragging || eventData.button != PointerEventData.InputButton.Left || !isActiveAndEnabled ||
                _receiver == null || (_receiver is Object && _receiverObject == null) ||
                _button == null || !_button.IsActive() || !_button.IsInteractable()) return;

            // 카드 위로 돌아와 놓더라도 클릭 입력이 중복되지 않게 합니다.
            eventData.eligibleForClick = false;
            _dragging = true;
            _pointerId = eventData.pointerId;
            _receiver.Click(_slot);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // EventSystem이 드래그 시작을 인식하도록 구현합니다. 포인터 추적은 InputSystem 소유입니다.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && eventData.pointerId == _pointerId)
                EndDrag();
        }

        private void OnDisable() => EndDrag();

        private void EndDrag()
        {
            if (!_dragging) return;
            _dragging = false;
        }
    }
}
