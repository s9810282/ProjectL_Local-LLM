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
    public sealed class DeckCardHoverBinding : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIDeckCardEnter = new("Combat.Action.UI.DeckCard.Enter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIDeckCardExit = new("Combat.Action.UI.DeckCard.Exit");
#endif

        private IDeckCardHoverReceiver _receiver;
        private Object _receiverObject;
        private int _slot;
        private bool _hovered;
        private RectTransform _card;
        private RectTransform _hitRect;
        private Rect _restRect;
        private Quaternion _restInverseRotation;

        internal void ConfigureHitArea(RectTransform card, RectTransform hitRect, Vector2 size, float angle)
        {
            if (card == null) throw new System.ArgumentNullException(nameof(card));
            if (hitRect == null) throw new System.ArgumentNullException(nameof(hitRect));
            _card = card;
            _hitRect = hitRect;
            _restRect = new Rect(-size.x * 0.5f, 0f, size.x, size.y);
            _restInverseRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        /// <summary>GraphicRaycaster의 기본 검사에 원위치 영역을 더해, 카드가 상승해도 포인터 이탈이 반복되지 않게 합니다.</summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // 부채꼴에 연결되기 전에는 기존 일반 Hover 바인딩으로 사용할 수 있습니다.
            if (_card == null || _hitRect == null) return true;
            if (!_card.gameObject.activeInHierarchy) return false;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_card, screenPoint, eventCamera, out Vector2 cardPoint) &&
                _card.rect.Contains(cardPoint)) return true;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_hitRect, screenPoint, eventCamera, out Vector2 hitPoint) &&
                _restRect.Contains((Vector2)(_restInverseRotation * hitPoint));
        }

        internal void ResetHover() => ClearHover();

        internal void Bind(IDeckCardHoverReceiver receiver, int slot)
        {
            if (receiver == null) throw new System.ArgumentNullException(nameof(receiver));
            ClearHover();
            _receiver = receiver;
            _receiverObject = receiver as Object;
            _slot = slot;
        }

        private bool Available => _receiver != null && (!(_receiver is Object) || _receiverObject != null);
        public void OnPointerEnter(PointerEventData eventData)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIDeckCardEnterSample = _profileUIDeckCardEnter.Auto();
#endif

            if (_hovered || !isActiveAndEnabled || !Available) return;
            _hovered = true;
            _receiver.EnterCard(_slot);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIDeckCardExitSample = _profileUIDeckCardExit.Auto();
#endif
            ClearHover();
        }
        private void OnDisable() => ClearHover();

        private void ClearHover()
        {
            if (!_hovered) return;
            _hovered = false;
            if (Available) _receiver.ExitCard(_slot);
        }
    }
}
