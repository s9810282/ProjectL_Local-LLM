using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    internal interface IDeckCardHoverReceiver
    {
        void EnterCard(int slot);
        void ExitCard(int slot);
    }

    /// <summary>배치 Button이 비활성이어도 정보용 카드 Hover를 전달합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckCardHoverBinding : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private IDeckCardHoverReceiver _receiver;
        private Object _receiverObject;
        private int _slot;
        private bool _hovered;

        internal void Bind(IDeckCardHoverReceiver receiver, int slot)
        {
            ClearHover();
            _receiver = receiver;
            _receiverObject = receiver as Object;
            _slot = slot;
        }

        private bool Available => _receiver != null && (!(_receiver is Object) || _receiverObject != null);
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hovered || !isActiveAndEnabled || !Available) return;
            _hovered = true;
            _receiver.EnterCard(_slot);
        }

        public void OnPointerExit(PointerEventData eventData) => ClearHover();
        private void OnDisable() => ClearHover();

        private void ClearHover()
        {
            if (!_hovered) return;
            _hovered = false;
            if (Available) _receiver.ExitCard(_slot);
        }
    }
}
