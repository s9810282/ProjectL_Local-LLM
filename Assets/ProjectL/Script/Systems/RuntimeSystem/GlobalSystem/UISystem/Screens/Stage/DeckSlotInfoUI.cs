using System;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>덱 공용 상세창의 등록·입력·포커스 수명을 맡습니다. 표시 기능은 내부 위젯에 전달합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckSlotInfoUI : UIBase, IUIInputReceiver
    {
        private const int CloseSlot = 0;
        private const int PreviousSlot = 1;
        private const int NextSlot = 2;

        [SerializeField] private Button closeButton;
        [SerializeField] private Button changeButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private DeckSlotInfoStats stats = new();
        [SerializeField] private DeckSlotInfoExperience experience = new();
        [SerializeField] private DeckSlotInfoAbilities abilities = new();
        [SerializeField] private DeckSlotInfoRanges ranges = new();
        private IDeckSlotInfoReceiver _input;
        private bool _prepared;
        private bool _opened;
        private GameObject _previousSelection;

        public DeckSlotInfoUI() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.DeckSlotInfo;
        public override UIId ParentID => UIId.DeckEdit;

        internal void Validate()
        {
            DeckSlotInfoReferences.RequireChild(closeButton, transform, nameof(closeButton));
            DeckSlotInfoReferences.RequireChild(changeButton, transform, nameof(changeButton));
            if (closeButton == changeButton || closeButton.targetGraphic == null ||
                !closeButton.targetGraphic.raycastTarget ||
                !closeButton.targetGraphic.transform.IsChildOf(closeButton.transform))
                throw DeckSlotInfoReferences.Error(transform, "closeButton은 독립적인 버튼이며 내부의 Raycast Target Graphic이 필요합니다.");
            ValidateNavigationButton(leftButton, nameof(leftButton));
            ValidateNavigationButton(rightButton, nameof(rightButton));
            if (leftButton == rightButton)
                throw DeckSlotInfoReferences.Error(transform, "leftButton과 rightButton은 각각 별도의 버튼이어야 합니다.");
            if (stats == null || experience == null || abilities == null || ranges == null)
                throw DeckSlotInfoReferences.Error(transform, "stats·experience·abilities·ranges 표시 위젯이 필요합니다.");
            stats.Validate(transform);
            experience.Validate(transform);
            abilities.Validate(transform);
            ranges.Validate(transform);
        }

        private void ValidateNavigationButton(Button button, string fieldName)
        {
            DeckSlotInfoReferences.RequireChild(button, transform, fieldName);
            if (button == closeButton || button == changeButton || button.targetGraphic == null ||
                !button.targetGraphic.raycastTarget || !button.targetGraphic.transform.IsChildOf(button.transform))
                throw DeckSlotInfoReferences.Error(transform, $"{fieldName}은 독립적인 버튼이며 내부의 Raycast Target Graphic이 필요합니다.");
        }

        internal void BindInput(IDeckSlotInfoReceiver input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            Prepare();
            _input = input;
        }

        internal void Render(DeckEditCardModel card, string displayName, Sprite portrait, ITraitVisualTableQuery traits)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            Prepare();
            // 필수 특성 데이터를 먼저 변환하여 오류가 나면 일부 수치만 갱신하지 않습니다.
            UnitCardTraitContent[] contents = card.Stats == null ? Array.Empty<UnitCardTraitContent>() :
                UnitCardTraitContent.Create(traits, card.Stats.TraitKeys.Span, card.DescriptionSource);
            stats.Render(card, displayName, portrait);
            experience.Render(card.DescriptionSource?.Experience);
            abilities.Render(contents);
            abilities.ResetForSelection();
            ranges.Render(card.Stats?.MoveMethod, card.Stats?.MoveRange, card.DescriptionSource?.AttackPattern);
        }

        public override void Open()
        {
            Prepare();
            if (_input == null)
                throw DeckSlotInfoReferences.Error(transform, "Open 전에 상세창 입력 수신자 바인딩이 필요합니다.");
            if (!_opened)
                _previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            _opened = true;
            base.Open();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        private void Prepare()
        {
            if (_prepared) return;
            Validate();
            experience.RenderPending();
            // Phase 2B에서 편성 상태별 문구·교체/닫기 입력을 연결합니다.
            changeButton.gameObject.SetActive(false);
            UIInputBinding.Attach(closeButton, this, CloseSlot);
            UIInputBinding.Attach(leftButton, this, PreviousSlot);
            UIInputBinding.Attach(rightButton, this, NextSlot);
            abilities.BindInput();
            _prepared = true;
        }

        void IUIInputReceiver.Click(int slot)
        {
            // 바인딩 전 대기와 숨겨진 화면의 입력은 정상적으로 무시합니다.
            if (!isActiveAndEnabled || !_opened || _input == null) return;
            switch (slot)
            {
                case CloseSlot: _input.CloseSlotInfo(); break;
                case PreviousSlot: _input.MoveSlotInfo(-1); break;
                case NextSlot: _input.MoveSlotInfo(1); break;
            }
        }
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        protected override void OnUIHidden()
        {
            _opened = false;
            GameObject previous = _previousSelection;
            _previousSelection = null;
            EventSystem events = EventSystem.current;
            if (events != null)
            {
                if (previous != null && previous.activeInHierarchy) events.SetSelectedGameObject(previous);
                else if (events.currentSelectedGameObject != null &&
                    events.currentSelectedGameObject.transform.IsChildOf(transform)) events.SetSelectedGameObject(null);
            }
            _input?.SlotInfoHidden();
        }
    }
}
