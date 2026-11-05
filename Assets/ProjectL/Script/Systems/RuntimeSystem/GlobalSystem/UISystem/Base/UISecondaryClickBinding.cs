using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>오른쪽 클릭을 받는 UI 수신자입니다. 왼쪽 클릭·Hover는 <see cref="IUIInputReceiver"/>가 맡습니다.</summary>
    public interface IUISecondaryClickReceiver
    {
        void SecondaryClick(int slot);
    }

    /// <summary>
    /// Unity 오른쪽 클릭을 구독 없이 명시적인 대상과 슬롯으로 전달합니다(가방 설계 P6).
    /// <see cref="UIInputBinding"/>은 왼쪽 클릭만 받으며 바꾸지 않습니다. 같은 오브젝트에 함께 붙어도 서로의 버튼을 받지 않습니다.
    /// </summary>
    /// <remarks>
    /// Button의 눌림 가능 여부를 보지 않습니다. 받을지는 수신자가 슬롯 상태로 정합니다. 클릭을 받으려면 같은 오브젝트의 Graphic이 Raycast 대상이어야 합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class UISecondaryClickBinding : MonoBehaviour, IPointerClickHandler
    {
        private IUISecondaryClickReceiver _receiver;
        private Object _receiverObject;
        private int _slot;

        public static UISecondaryClickBinding Attach(GameObject target, IUISecondaryClickReceiver receiver, int slot)
        {
            if (target == null) return null;
            var binding = target.GetComponent<UISecondaryClickBinding>();
            if (binding == null) binding = target.AddComponent<UISecondaryClickBinding>();
            binding.Bind(receiver, slot);
            return binding;
        }

        public void Bind(IUISecondaryClickReceiver receiver, int slot)
        {
            _receiver = receiver;
            _receiverObject = receiver as Object;
            _slot = slot;
        }

        private bool ReceiverAvailable => _receiver != null &&
            (!(_receiver is Object) || _receiverObject != null);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right && isActiveAndEnabled && ReceiverAvailable)
                _receiver.SecondaryClick(_slot);
        }
    }
}
