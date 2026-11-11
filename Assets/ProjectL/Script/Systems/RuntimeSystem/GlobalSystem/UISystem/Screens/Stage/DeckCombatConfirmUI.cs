using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>덱 Panel 아래에서 공통 Canvas를 사용하는 출전 확인창입니다. 부모 이력과 함께 닫힙니다.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckCombatConfirmUI : UIBase, IUIInputReceiver
    {
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button cancelButton;
        private IDeckCombatInput _input;
        private bool _prepared;
        private bool _open;
        private GameObject _previousSelection;

        public override UIId ID => UIId.DeckCombatConfirm;
        public override UIId ParentID => UIId.DeckEdit;
        public bool IsOpen => _open && isActiveAndEnabled;

        public void BindInput(IDeckCombatInput input) => _input = input;

        /// <summary>필수 참조를 상태를 바꾸지 않고 검사합니다. 씬 연결 때 덱 편집 화면 검사가 부릅니다(평가 C-26).</summary>
        internal void Validate()
        {
            if (acceptButton == null || cancelButton == null)
                throw new InvalidOperationException("[DeckCombatConfirmUI] acceptButton과 cancelButton이 필요합니다.");
        }

        public override void Open()
        {
            if (!_prepared)
            {
                Validate();
                UIInputBinding.Attach(acceptButton, this, 0);
                UIInputBinding.Attach(cancelButton, this, 1);
                acceptButton.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = cancelButton, selectOnRight = cancelButton, selectOnUp = cancelButton, selectOnDown = cancelButton };
                cancelButton.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = acceptButton, selectOnRight = acceptButton, selectOnUp = acceptButton, selectOnDown = acceptButton };
                _prepared = true;
            }
            _open = true;
            _previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            base.Open();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(acceptButton.gameObject);
        }

        public override void Hide()
        {
            _open = false;
            base.Hide();
            if (EventSystem.current != null && _previousSelection != null && _previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(_previousSelection);
            _previousSelection = null;
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!_open || !isActiveAndEnabled || _input == null) return;
            if (slot == 0) _input.AcceptDeparture();
            else if (slot == 1) _input.CancelDepartureConfirmation();
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }
    }
}
