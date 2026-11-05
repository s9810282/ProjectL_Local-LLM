using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>미편성 유닛을 표시하고 공용 상세창 조회·드래그 입력을 소유 화면에 전달합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button), typeof(RectTransform))]
    public sealed class OwnedUnitSlot : UIWidgetBase, IUIInputReceiver, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const int InfoSlot = 0;
        private const int CardSlot = 2;

        [SerializeField] private Button infoButton;
        [SerializeField] private Image portrait;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject outline;

        private UIInputBinding _infoBinding;
        private UIInputBinding _cardBinding;
        private bool _prepared;
        private Button _button;
        private IDeckEditDragReceiver _dragReceiver;
        private bool _canDrag;
        private bool _dragging;
        private int _pointerId;
        private IDeckEditInteractionReceiver _interaction;

        internal void BindInteraction(IDeckEditInteractionReceiver receiver) =>
            _interaction = receiver ?? throw new ArgumentNullException(nameof(receiver));

        internal void SetChangeHighlighted(bool highlighted)
        {
            if (outline.activeSelf != highlighted) outline.SetActive(highlighted);
        }

        internal DeckEditCardModel Card { get; private set; }
        internal Sprite Portrait => portrait.sprite;

        internal void BindDragReceiver(IDeckEditDragReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            Prepare();
            EndDrag();
            _dragReceiver = receiver;
        }

        internal void Render(DeckEditCardModel card, string displayName, Sprite portraitSprite, Sprite categorySprite, bool canDrag)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            Prepare();
            if (Card == null || Card.OwnedId != card.OwnedId)
            {
                EndDrag();
                ClearInputHover();
            }
            Card = card;
            _canDrag = canDrag;
            if (!canDrag) EndDrag();
            nameText.text = string.IsNullOrWhiteSpace(displayName) ? card.CharacterKey.ToString() : displayName;
            portrait.sprite = portraitSprite;
            portrait.enabled = portraitSprite != null;
            categoryIcon.sprite = categorySprite;
            categoryIcon.enabled = categorySprite != null;
            _button.interactable = infoButton.interactable = true;
        }

        internal void Clear()
        {
            Prepare();
            EndDrag();
            Card = null;
            ClearInputHover();
            _canDrag = false;
            SetChangeHighlighted(false);
            _button.interactable = infoButton.interactable = false;
            nameText.text = string.Empty;
            portrait.sprite = categoryIcon.sprite = null;
            portrait.enabled = categoryIcon.enabled = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_dragging || !_canDrag || !isActiveAndEnabled || Card == null ||
                eventData.button != PointerEventData.InputButton.Left || !_button.IsInteractable()) return;
            if (_dragReceiver == null) throw ConfigurationError("드래그 수신자가 바인딩되지 않았습니다.");
            if (!_dragReceiver.BeginOwnedDrag(this, eventData)) return;
            eventData.eligibleForClick = false;
            _dragging = true;
            _pointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging && eventData.pointerId == _pointerId) _dragReceiver.MoveOwnedDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragging && eventData.pointerId == _pointerId) EndDrag();
        }

        private void EndDrag()
        {
            if (!_dragging) return;
            _dragging = false;
            _dragReceiver?.EndOwnedDrag(this);
        }

        private void Awake() => Prepare();

        public override void Open()
        {
            Prepare();
            base.Open();
        }

        private void ClearInputHover()
        {
            Prepare();
            _infoBinding.OnHoverExit();
            _cardBinding.OnHoverExit();
        }

        protected override void OnUIHidden()
        {
            // 최초 바인딩 전 비활성 상태에는 정리할 입력이 없습니다.
            if (!_prepared) return;
            EndDrag();
            ClearInputHover();
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled || Card == null) return;
            // 최초 바인딩 전에는 입력을 받지 않습니다.
            if (_interaction == null) return;
            if (slot == CardSlot && _interaction.ClickOwned(this)) return;
            if (slot == InfoSlot || slot == CardSlot)
                _interaction.ShowOwnedInfo(Card.OwnedId);
        }

        // 다른 카드의 호버는 고정 상태를 바꾸지 않습니다.
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        internal void Validate()
        {
            if (infoButton == null) throw ConfigurationError("infoButton 참조가 필요합니다.");
            if (infoButton.transform == transform || !infoButton.transform.IsChildOf(transform))
                throw ConfigurationError($"infoButton ('{infoButton.name}')은 슬롯 내부의 별도 버튼이어야 합니다.");
            if (GetComponent<Button>() == null) throw ConfigurationError("슬롯 본체에 Button이 필요합니다.");
            if (portrait == null || categoryIcon == null || nameText == null || outline == null)
                throw ConfigurationError("portrait·categoryIcon·nameText·outline 참조가 필요합니다.");
            if (outline == gameObject || !outline.transform.IsChildOf(transform))
                throw ConfigurationError("outline은 슬롯 내부의 별도 표시여야 합니다.");
            if (!portrait.transform.IsChildOf(transform) || !categoryIcon.transform.IsChildOf(transform) ||
                !nameText.transform.IsChildOf(transform))
                throw ConfigurationError("표시 참조는 슬롯 내부에 있어야 합니다.");
        }

        private void Prepare()
        {
            if (_prepared) return;
            Validate();
            _button = GetComponent<Button>();
            _infoBinding = UIInputBinding.Attach(infoButton, this, InfoSlot);
            _cardBinding = UIInputBinding.Attach(_button, this, CardSlot);
            _prepared = true;
        }

        private InvalidOperationException ConfigurationError(string reason) =>
            new($"[OwnedUnitSlot] UIId.{OwnerUI?.ID ?? UIId.DeckEdit}, '{name}': {reason}");
    }
}
