using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>로비의 일반 UI에 전달받은 덱 표현 값만 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class LobbyDeckUI : UIBase, IUIInputReceiver, ILobbyDeckDisplay
    {
        /// <summary>닫기 버튼의 입력 슬롯 번호입니다. 덱 Presenter도 이 값으로 닫기를 구분합니다.</summary>
        internal const int CloseSlot = -1;

        [Tooltip("등록 루트와 구분된 자식 패널입니다. 표시만 열고 닫습니다.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform cardsRoot;
        [Tooltip("목록 생성에 사용하는 비활성 자식 원본입니다.")]
        [SerializeField] private LobbyDeckListItemView cardTemplate;
        [SerializeField] private UnitCardView details;
        [SerializeField] private Button closeButton;
        [SerializeField] private float cardSpacing = 112f;

        private readonly List<LobbyDeckListItemView> _items = new();
        private IUIInputReceiver _receiver;
        private UIInputBinding _closeBinding;
        private int _visibleCount;
        private int _version;
        private int _stateVersion;
        private bool _isOpen;
        private bool _acceptInput;
        private bool _hideInProgress;

        public override UIId ID => UIId.LobbyDeck;
        public bool IsOpen => _isOpen;
        public event Action<bool> OpenStateChanged;

        /// <summary>열려 있고 켜져 있는지입니다. 입력 수신 판정과 덱 Presenter가 이 답 하나를 씁니다(로비 리팩토링 2차 설계 N-45).</summary>
        public bool IsShown => this != null && _isOpen && isActiveAndEnabled;

        private bool CanReceive => _acceptInput && IsShown && _receiver != null;

        private void Awake()
        {
            // 상세 카드 소유 연결은 검사(Validate)가 아니라 준비 때 한 번 합니다(로비 리팩토링 2차 설계 N-64).
            if (details != null)
                details.BindOwner(this);
            EnsureCloseBinding();
            if (cardTemplate != null)
                cardTemplate.gameObject.SetActive(false);
            ShowDetails(null);
            if (panel != null && panel != gameObject)
                panel.SetActive(false);
            RefreshInputStates();
        }

        /// <summary>부모 Canvas가 꺼지거나 씬이 해제될 때도 패널을 닫고 열림 해제를 알립니다(UIBase 꺼짐 훅).</summary>
        protected override void OnUIHidden() => Hide();

        public override void Open()
        {
            if (_isOpen || _hideInProgress)
                return;
            // 구성 검사는 씬 연결(LobbyCanvasView.ValidateComposition) 때 한 번 합니다(로비 리팩토링 2차 설계 N-22).
            int version = ++_version;
            int stateVersion = ++_stateVersion;
            _isOpen = true;
            panel.SetActive(true);
            if (version != _version || !_isOpen)
                return;
            EnsureCloseBinding();
            if (version != _version || !_isOpen)
                return;
            _acceptInput = _receiver != null;
            RefreshInputStates();
            if (version == _version && _isOpen)
                NotifyOpenStateChanged(true, stateVersion);
        }

        public override void Hide()
        {
            if (_hideInProgress)
                return;
            bool wasOpen = _isOpen;
            _hideInProgress = true;
            try
            {
                // 입력 플래그를 내리기 전에 호버 해제를 수신자에게 한 번 보냅니다(로비 리팩토링 2차 설계 N-82).
                // 수신자 콜백이 예외를 던지거나 다시 들어와도 닫힘·입력 차단은 반드시 남깁니다.
                try
                {
                    ClearControlInput(_version);
                }
                finally
                {
                    _isOpen = false;
                    _acceptInput = false;
                    ++_version;
                }
                int stateVersion = wasOpen ? ++_stateVersion : _stateVersion;
                ShowDetails(null);
                if (panel != null && panel != gameObject)
                    panel.SetActive(false);
                if (wasOpen)
                    NotifyOpenStateChanged(false, stateVersion);
            }
            finally
            {
                _hideInProgress = false;
            }
        }

        /// <summary>창을 열지 않고 받은 목록을 표시합니다. 같은 화면의 목록 항목은 다시 사용합니다.</summary>
        public void SetModel(LobbyDeckDisplayModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            int version = ++_version;
            // 목록·입력 플래그를 바꾸기 전에 지난 목록의 호버 해제를 한 번 보냅니다(N-82).
            // 콜백이 새 호출로 다시 들어왔으면 그 호출의 상태를 덮지 않고 멈춥니다.
            bool current = false;
            try
            {
                current = ClearControlInput(version);
            }
            finally
            {
                if (version == _version)
                    _acceptInput = false;
            }
            if (!current)
                return;
            _visibleCount = model.Cards.Count;
            ShowDetails(null);

            for (int i = 0; i < _visibleCount; i++)
            {
                if (i == _items.Count)
                {
                    LobbyDeckListItemView created = Instantiate(cardTemplate, cardsRoot, false);
                    created.Bind(this, i);
                    _items.Add(created);
                }
                LobbyDeckListItemView item = _items[i];
                item.Render(model.Cards[i]);
                var rect = (RectTransform)item.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * cardSpacing);
                item.gameObject.SetActive(true);
                if (version != _version)
                    return;
            }

            for (int i = _visibleCount; i < _items.Count; i++)
            {
                _items[i].SetInputEnabled(false);
                _items[i].gameObject.SetActive(false);
                if (version != _version)
                    return;
            }

            EnsureCloseBinding();
            if (version != _version)
                return;
            _acceptInput = _isOpen && _receiver != null;
            RefreshInputStates();
        }

        /// <summary>소유한 카드 위젯에 이름·초상화만 전달합니다. 미제공 능력치와 부가 패널은 비웁니다.</summary>
        public void ShowDetails(LobbyDeckCardContent card)
        {
            if (details == null)
                return;
            details.SetInfoInputEnabled(false);
            if (card == null)
            {
                details.Clear();
                details.Hide();
                return;
            }
            if (!_isOpen)
                return;
            details.ShowCharacter(card.Key, card.DisplayName, card.Icon, level: null);
            details.ClearDetails();
            details.Open();
        }

        /// <summary>버튼은 화면을 직접 호출하고 화면은 유효한 수명에서만 명시적 수신자에게 전달합니다.</summary>
        public void BindInput(IUIInputReceiver receiver)
        {
            int version = ++_version;
            // 수신자를 바꾸기 전에 지금 수신자에게 호버 해제를 한 번 보냅니다(N-82).
            // 콜백이 새 호출로 다시 들어왔으면 그 호출이 둔 수신자를 지우지 않습니다.
            bool current = false;
            try
            {
                current = ClearControlInput(version);
            }
            finally
            {
                if (version == _version)
                {
                    _acceptInput = false;
                    _receiver = null;
                }
            }
            if (!current)
                return;
            _receiver = receiver;
            EnsureCloseBinding();
            if (version != _version)
                return;
            _acceptInput = _isOpen && receiver != null;
            RefreshInputStates();
        }

        internal void Validate()
        {
            if (panel == null || cardsRoot == null || cardTemplate == null || details == null || closeButton == null)
                throw new InvalidOperationException("[LobbyDeckUI] 패널·목록·원본·상세·닫기 참조가 필요합니다.");
            if (panel == gameObject || !panel.transform.IsChildOf(transform))
                throw new InvalidOperationException("[LobbyDeckUI] 패널은 등록 루트와 구분된 자식이어야 합니다.");
            if (!cardsRoot.IsChildOf(panel.transform) || !cardTemplate.transform.IsChildOf(panel.transform) ||
                !details.transform.IsChildOf(panel.transform) ||
                !closeButton.transform.IsChildOf(panel.transform))
                throw new InvalidOperationException("[LobbyDeckUI] 표시와 입력 참조는 패널 안에 있어야 합니다.");
            if (cardTemplate.gameObject.activeSelf || cardTemplate.transform == cardsRoot)
                throw new InvalidOperationException("[LobbyDeckUI] 목록 원본은 비활성 자식이어야 합니다.");
            foreach (UIBase component in details.GetComponents<UIBase>())
                if (!(component is UIWidgetBase))
                    throw new InvalidOperationException("[LobbyDeckUI] 상세 카드는 소유 위젯이어야 하며 별도 화면으로 등록하지 않습니다.");
            if (float.IsNaN(cardSpacing) || float.IsInfinity(cardSpacing) || cardSpacing <= 0f)
                throw new InvalidOperationException("[LobbyDeckUI] 카드 간격은 유한한 양수여야 합니다.");
            cardTemplate.Validate();
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!CanReceive)
                return;
            if (slot == CloseSlot)
            {
                // 닫기 버튼의 켜짐·상호작용 가능 여부는 UIInputBinding이 보고 부릅니다(N-22).
                _receiver.Click(slot);
            }
            else if (IsInteractiveSlot(slot))
                _receiver.Click(slot);
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
            if (CanReceive && IsInteractiveSlot(slot))
                _receiver.HoverEnter(slot);
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
            if (CanReceive && slot >= 0 && slot < _visibleCount && slot < _items.Count)
                _receiver.HoverExit(slot);
        }

        private bool IsInteractiveSlot(int slot) => slot >= 0 && slot < _visibleCount &&
            slot < _items.Count && _items[slot] != null && _items[slot].CanInteract;

        private void EnsureCloseBinding()
        {
            if (_closeBinding == null && closeButton != null)
                _closeBinding = UIInputBinding.Attach(closeButton, this, CloseSlot);
        }

        /// <summary>
        /// 닫기·목록 입력을 끄며 호버 해제를 보냅니다. 콜백 도중 다른 호출이 들어와 <paramref name="version"/>이
        /// 낡으면 남은 항목을 건드리지 않고 false를 돌려줍니다.
        /// </summary>
        private bool ClearControlInput(int version)
        {
            if (closeButton != null)
                closeButton.interactable = false;
            _closeBinding?.OnHoverExit();
            if (version != _version)
                return false;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == null)
                    continue;
                _items[i].SetInputEnabled(false);
                if (version != _version)
                    return false;
            }
            return true;
        }

        private void RefreshInputStates()
        {
            bool enabled = _acceptInput && _isOpen && _receiver != null;
            if (closeButton != null)
                closeButton.interactable = enabled;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null)
                    _items[i].SetInputEnabled(enabled && i < _visibleCount);
            }
        }

        private void NotifyOpenStateChanged(bool open, int stateVersion)
        {
            Action<bool> changed = OpenStateChanged;
            if (changed == null)
                return;
            List<Exception> failures = null;
            foreach (Action<bool> observer in changed.GetInvocationList())
            {
                if (stateVersion != _stateVersion || _isOpen != open)
                    break;
                try
                {
                    observer(open);
                }
                catch (Exception error)
                {
                    (failures ??= new()).Add(error);
                }
            }
            if (failures != null)
                throw new AggregateException("로비 덱 UI 상태 알림 중 실패했습니다.", failures);
        }
    }
}
