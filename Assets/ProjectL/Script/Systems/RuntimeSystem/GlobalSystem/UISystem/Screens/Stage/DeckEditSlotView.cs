using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>저작한 DeckSlot의 표시·버튼·드롭 영역입니다. Parallel 내부 위젯으로 DeckEditUI에 소유됩니다.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckEditSlotView : UIWidgetBase, IUIInputReceiver, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private GameObject info;
        [SerializeField] private Image portrait;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text healthValue;
        [SerializeField] private TMP_Text attackValue;
        [SerializeField] private TMP_Text defenseValue;
        [SerializeField] private Button infoButton;
        [SerializeField] private Button changeButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text emptyText;
        private IDeckEditDragReceiver _drag;
        private IDeckEditInteractionReceiver _input;
        private int _index;
        private Color _normalColor;
        private bool _bound;
        // 입력 콜백에서 문자열을 새로 만들지 않도록 바인딩 때 표시 문구만 준비합니다.
        private string _occupiedLabel;
        private string _emptyLabel;
        private string _lockedEmptyLabel;
        private string _lockedOccupiedLabel;

        internal DeckEditCardModel Card { get; private set; }

        internal void Validate()
        {
            if (info == null || portrait == null || categoryIcon == null || nameText == null ||
                healthValue == null || attackValue == null || defenseValue == null || infoButton == null ||
                changeButton == null || deleteButton == null || background == null || emptyText == null)
                throw Error("Info·Portrait·Category·Name·HP/Attack/Defense Value·Info/Change/Delete Button·background·emptyText 참조가 필요합니다.");
            if (info == gameObject || !info.transform.IsChildOf(transform) || background.transform != transform ||
                !portrait.transform.IsChildOf(info.transform) || !nameText.transform.IsChildOf(info.transform) ||
                !healthValue.transform.IsChildOf(info.transform) ||
                !attackValue.transform.IsChildOf(info.transform) || !defenseValue.transform.IsChildOf(info.transform) ||
                !infoButton.transform.IsChildOf(info.transform) || !categoryIcon.transform.IsChildOf(transform) ||
                !emptyText.transform.IsChildOf(transform) || emptyText.transform.IsChildOf(info.transform) ||
                !changeButton.transform.IsChildOf(transform) || changeButton.transform.IsChildOf(info.transform) ||
                !deleteButton.transform.IsChildOf(transform) || deleteButton.transform.IsChildOf(info.transform))
                throw Error("표시 참조는 Info 내부에, Category·EmptyLabel·Change/Delete는 Info 밖의 슬롯 내부에 있어야 합니다.");
            if (infoButton == changeButton || infoButton == deleteButton || changeButton == deleteButton)
                throw Error("Info/Change/Delete Button은 서로 다른 버튼이어야 합니다.");
        }

        internal void Bind(IDeckEditDragReceiver drag, IDeckEditInteractionReceiver input, int index)
        {
            if (drag == null) throw new ArgumentNullException(nameof(drag));
            if (input == null) throw new ArgumentNullException(nameof(input));
            Validate();
            var category = DeckEditSlotLayout.CategoryAt(index);
            _drag = drag;
            _input = input;
            _index = index;
            _normalColor = background.color;
            UIInputBinding.Attach(infoButton, this, 0);
            UIInputBinding.Attach(changeButton, this, 1);
            UIInputBinding.Attach(deleteButton, this, 2);
            _occupiedLabel = category.ToString();
            _emptyLabel = _occupiedLabel + " - 비어 있음";
            _lockedEmptyLabel = _emptyLabel + " 변경 불가";
            _lockedOccupiedLabel = _occupiedLabel + " 변경 불가";
            _bound = true;
            SetChangeMode(false, false);
        }

        internal void Render(DeckEditCardModel card, string displayName, Sprite icon, Sprite categorySprite, bool canEdit)
        {
            Card = card;
            bool occupied = card != null;
            nameText.text = occupied ? (string.IsNullOrWhiteSpace(displayName) ? card.CharacterKey.ToString() : displayName) : string.Empty;
            healthValue.text = card?.Stats?.MaxHealth.ToString() ?? string.Empty;
            attackValue.text = card?.Stats?.AttackPower.ToString() ?? string.Empty;
            // 현 스냅샷의 방어 계약은 PhysicalDefense입니다. 별도 보호막 잔량을 추정하지 않습니다.
            defenseValue.text = card?.Stats?.PhysicalDefense.ToString() ?? string.Empty;
            portrait.sprite = icon;
            portrait.enabled = occupied && icon != null;
            categoryIcon.sprite = categorySprite;
            categoryIcon.enabled = categorySprite != null;
            infoButton.interactable = occupied;
            info.SetActive(occupied);
            SetChangeMode(false, canEdit);
        }

        internal void SetChangeMode(bool selected, bool canEdit)
        {
            EventSystem events = EventSystem.current;
            bool ownsSelection = events != null && (events.currentSelectedGameObject == changeButton.gameObject ||
                events.currentSelectedGameObject == deleteButton.gameObject);
            bool moveSelection = events != null && (selected
                ? events.currentSelectedGameObject == changeButton.gameObject
                : events.currentSelectedGameObject == deleteButton.gameObject);
            // 저작된 두 버튼이 같은 자리를 사용하므로 선택 중에는 Change를 Delete로 전환합니다.
            changeButton.interactable = canEdit;
            deleteButton.interactable = canEdit;
            changeButton.gameObject.SetActive(canEdit && !selected);
            deleteButton.gameObject.SetActive(canEdit && selected);
            emptyText.text = Card == null
                ? (canEdit ? _emptyLabel : _lockedEmptyLabel)
                : (canEdit ? _occupiedLabel : _lockedOccupiedLabel);
            emptyText.gameObject.SetActive(true);
            if (ownsSelection && !canEdit)
                events.SetSelectedGameObject(Card != null ? infoButton.gameObject : null);
            else if (moveSelection) events.SetSelectedGameObject(selected ? deleteButton.gameObject : changeButton.gameObject);
        }

        internal void SetDropHighlighted(bool highlighted) =>
            background.color = highlighted ? new Color(.2f, .8f, 1f, .85f) : _normalColor;

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled || !_bound) return;
            if (slot == 0 && Card != null) _input.ShowSlotInfo(_index);
            else if (slot == 1) _input.BeginSlotChange(_index);
            else if (slot == 2) _input.DeleteSlot(_index);
        }
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isActiveAndEnabled && _bound && eventData.button == PointerEventData.InputButton.Left) _input.CancelSlotChange();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (isActiveAndEnabled && _bound) _drag.DropOwned(_index, eventData);
        }

        protected override void OnUIHidden()
        {
            if (_bound) SetDropHighlighted(false);
        }

        private InvalidOperationException Error(string message) =>
            new($"[DeckEditSlotView] UIId.DeckEdit, '{name}': {message}");
    }
}
