using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>옵션의 공용 확인창입니다. 실제 기능 실행은 명시적인 수신 대상이 맡습니다.</summary>
    [DisallowMultipleComponent]
    public sealed class ConfirmUI : UIBase, IUIInputReceiver
    {
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [Tooltip("문구를 표시할 TMP Text가 있으면 연결합니다.")]
        [SerializeField] private TMP_Text message;

        private IUISystemService _ui;
        private IConfirmReceiver _receiver;
        private GameObject _previousSelection;
        private bool _buttonsBound;
        private bool _hasRequest;
        private bool _isOpen;

        public override UIId ID => UIId.Confirm;
        public override UIId ParentID => UIId.Setting;
        public ConfirmAction CurrentAction { get; private set; }
        public bool IsOpen => _isOpen;

        private void Awake() => BindButtons();

        private void BindButtons()
        {
            if (_buttonsBound) return;
            if (yesButton == null || noButton == null)
                throw new InvalidOperationException("[ConfirmUI] Y·N Button을 연결해야 합니다.");
            Policy = UIOpenPolicy.Parallel;
            UIInputBinding.Attach(yesButton, this, 0);
            UIInputBinding.Attach(noButton, this, 1);
            if (GetComponent<UIPointerClickBoundary>() == null)
                gameObject.AddComponent<UIPointerClickBoundary>();
            _buttonsBound = true;
        }

        public void BindReceiver(IConfirmReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            if (_receiver != null && !ReferenceEquals(_receiver, receiver))
                throw new InvalidOperationException("[ConfirmUI] 확인 수신 대상이 이미 연결되어 있습니다.");
            _receiver = receiver;
        }

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
            BindButtons();
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        public void Show(ConfirmAction action)
        {
            if (_isOpen) return;
            if (!System.Enum.IsDefined(typeof(ConfirmAction), action))
                throw new ArgumentOutOfRangeException(nameof(action));
            // 진행 저장 흐름이 생기기 전에는 enum만 유지하고 확인창을 열지 않는다.
            if (action == ConfirmAction.SaveProgress) return;
            if (_ui == null || _receiver == null)
                throw new InvalidOperationException("[ConfirmUI] UI 서비스와 확인 수신 대상이 연결되어야 합니다.");
            CurrentAction = action;
            _hasRequest = true;
            try { _ui.OpenChildUI(ParentID, ID); }
            catch { _hasRequest = false; throw; }
        }

        public override void Open()
        {
            if (_isOpen) return;
            if (!_hasRequest) throw new InvalidOperationException("[ConfirmUI] Show로 확인 종류를 먼저 지정해야 합니다.");
            BindButtons();
            if (message != null) message.text = CurrentAction switch
            {
                ConfirmAction.SaveSettings => "변경한 설정을 저장하시겠습니까?",
                ConfirmAction.AbandonRun => "현재 전투를 포기하시겠습니까?",
                ConfirmAction.QuitGame => "게임을 종료하시겠습니까?",
                _ => throw new InvalidOperationException("[ConfirmUI] 아직 지원하지 않는 확인 종류입니다.")
            };
            _isOpen = true;
            transform.SetAsLastSibling();
            _previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            gameObject.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(noButton.gameObject);
        }

        public override void Hide()
        {
            ClearSelection();
            _isOpen = false;
            _hasRequest = false;
            gameObject.SetActive(false);
        }

        private void ClearSelection()
        {
            var events = EventSystem.current;
            if (_isOpen && events != null && events.currentSelectedGameObject != null
                && events.currentSelectedGameObject.transform.IsChildOf(transform))
                events.SetSelectedGameObject(_previousSelection != null && _previousSelection.activeInHierarchy
                    ? _previousSelection : null);
            _previousSelection = null;
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!_isOpen || (slot != 0 && slot != 1)) return;
            ConfirmAction action = CurrentAction;
            IConfirmReceiver receiver = _receiver;
            _ui.CloseUI(ID);
            receiver.Respond(action, slot == 0);
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        private void OnDisable()
        {
            ClearSelection();
            _isOpen = false;
            _hasRequest = false;
        }
    }
}
