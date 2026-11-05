using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>별도 버튼이 처리하지 않은 화면 배경 클릭으로 교체 모드를 취소합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckEditCancelBinding : MonoBehaviour, IPointerClickHandler
    {
        private IDeckEditInteractionReceiver _receiver;
        internal void Bind(IDeckEditInteractionReceiver receiver) =>
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isActiveAndEnabled && eventData.button == PointerEventData.InputButton.Left)
                _receiver?.CancelSlotChange(); // 화면 바인딩 전에는 입력을 받지 않습니다.
        }
    }
}
