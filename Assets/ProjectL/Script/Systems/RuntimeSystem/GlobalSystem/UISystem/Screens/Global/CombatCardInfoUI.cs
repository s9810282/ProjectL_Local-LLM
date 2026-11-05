using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>Combat 덱 카드가 공유하는 상세창의 선택·고정·개폐·포커스 수명입니다.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatCardInfoUI : UIBase, IUnitCardInfoReceiver, IUIInputReceiver
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button changeButton;
        [SerializeField] private DeckSlotInfoStats stats = new();
        [SerializeField] private DeckSlotInfoExperience experience = new();
        [SerializeField] private DeckSlotInfoAbilities abilities = new();
        private UnitCardUI _source;
        private int _revision;
        private bool _prepared;
        private bool _ownsFocus;
        private GameObject _previousSelection;

        public CombatCardInfoUI() => Policy = UIOpenPolicy.Exclusive;
        public override UIId ID => UIId.CombatCardInfo;
        public override UIId ParentID => UIId.UnitCard;

        internal void Validate()
        {
            DeckSlotInfoReferences.RequireChild(closeButton, transform, nameof(closeButton));
            DeckSlotInfoReferences.RequireChild(changeButton, transform, nameof(changeButton));
            if (closeButton == changeButton || closeButton.targetGraphic == null ||
                !closeButton.targetGraphic.raycastTarget)
                throw DeckSlotInfoReferences.Error(transform, "closeButton에 독립적인 Raycast Target Graphic이 필요합니다.");
            if (stats == null || experience == null || abilities == null)
                throw DeckSlotInfoReferences.Error(transform, "stats·experience·abilities가 필요합니다.");
            stats.Validate(transform);
            experience.Validate(transform);
            abilities.Validate(transform);
        }

        public override void ExportUIService(IUIRegistry registry)
        {
            Validate();
            base.ExportUIService(registry);
        }

        private void Prepare()
        {
            if (_prepared) return;
            Validate();
            UIInputBinding.Attach(closeButton, this, 0);
            changeButton.gameObject.SetActive(false);
            _prepared = true;
        }

        void IUnitCardInfoReceiver.Toggle(UnitCardUI source)
        {
            if (_source == source) CloseInfo();
            else Show(source);
        }

        void IUnitCardInfoReceiver.Forget(UnitCardUI source)
        {
            if (_source == source) CloseInfo();
        }

        private IUISystemService RequireUI() => UISystemService ??
            throw new InvalidOperationException($"[CombatCardInfoUI] UIId.{ID}, '{name}': UISystem 바인딩이 필요합니다.");

        private void Show(UnitCardUI source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            IUISystemService ui = RequireUI();
            Prepare();
            UnitInfoContent content = source.ReadInfoContent();
            // 빈 카드와 이미 숨겨진 카드에서 지난 입력이 도착하는 것은 정상적인 수명 경계입니다.
            if (content == null || !source.isActiveAndEnabled) return;
            Render(content);
            _source = source;
            _revision = source.DisplayRevision;
            ui.OpenChildUI(ParentID, ID);
        }

        private void Render(UnitInfoContent content)
        {
            abilities.ValidateContent(content.Abilities);
            stats.Render(content);
            experience.RenderPending();
            abilities.Render(content.Abilities);
        }

        public override void Open()
        {
            Prepare();
            if (_source == null)
                throw new InvalidOperationException($"[CombatCardInfoUI] UIId.{ID}, '{name}': Open 전에 표시 대상이 필요합니다.");
            base.Open();
            if (!_ownsFocus && EventSystem.current != null)
            {
                _previousSelection = EventSystem.current.currentSelectedGameObject;
                _ownsFocus = true;
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            }
        }

        private void Update()
        {
            if (_source == null || !_source.isActiveAndEnabled)
            {
                CloseInfo();
                return;
            }
            if (_revision == _source.DisplayRevision) return;
            UnitInfoContent content = _source.ReadInfoContent();
            if (content == null) CloseInfo();
            else
            {
                Render(content);
                _revision = _source.DisplayRevision;
            }
        }

        private void CloseInfo()
        {
            if (ReferenceEquals(_source, null)) return;
            IUISystemService ui = RequireUI();
            _source = null;
            ui.CloseUI(ID);
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == 0 && isActiveAndEnabled) CloseInfo();
        }
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        protected override void OnUIHidden()
        {
            _source = null;
            _revision = 0;
            EventSystem events = EventSystem.current;
            if (_ownsFocus && events != null)
            {
                if (_previousSelection != null && _previousSelection.activeInHierarchy)
                    events.SetSelectedGameObject(_previousSelection);
                else if (events.currentSelectedGameObject != null &&
                    events.currentSelectedGameObject.transform.IsChildOf(transform))
                    events.SetSelectedGameObject(null);
            }
            _ownsFocus = false;
            _previousSelection = null;
        }
    }
}
