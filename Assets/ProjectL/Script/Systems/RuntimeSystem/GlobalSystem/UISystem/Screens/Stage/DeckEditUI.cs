using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel;
using ProjectL.Systems.ServiceKeys.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// StageSelect의 덱 편집 팝업입니다. 받은 표시 값을 그리고 입력 의도만 넘깁니다.
    /// 덱 규칙·버전·확정 여부는 판단하지 않습니다(설계 RV2). 루트는 항상 켜 두고 열기 버튼은 Panel 밖에 둡니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeckEditUI : UIBase, IUIInputReceiver, IDeckEditDisplay, IDeckCombatDisplay,
        IDeckEditDragReceiver, IDeckEditInteractionReceiver, IScrollPopupDisplay
    {
        private const int OpenSlot = -1;
        private const int CloseSlot = -2;
        private const int ConfirmSlot = -3;
        private const int ReevaluateSlot = -4;
        private const int StartCombatSlot = -5;
        private const int FirstOwnedFilterSlot = -6;

        [Tooltip("임시 View 정책입니다. 켜면 메인 슬롯의 교체·삭제를 허용합니다. Open 시 적용하며 추후 Core 공개 권한으로 대체합니다.")]
        [SerializeField] private bool allowMainCharacterChange;

        /// <summary>열기 전에 호출 맥락에 맞게 설정합니다. 열린 상태에서 바꾸면 진행 중 편집 입력도 정리합니다.</summary>
        public bool AllowMainCharacterChange
        {
            get => allowMainCharacterChange;
            set
            {
                if (allowMainCharacterChange == value) return;
                allowMainCharacterChange = value;
                if (!_prepared) return;
                CancelDrag();
                CancelChangeMode();
            }
        }

        [Tooltip("열고 닫는 자식 Panel입니다. 루트는 항상 켜 둡니다.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private ScrollPanelPresentation scrollPresentation;
        [SerializeField] private RectTransform contentFrame;
        [Tooltip("Panel 밖의 열기 버튼입니다. Panel이 열려 있는 동안 숨깁니다.")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button startCombatButton;
        [SerializeField] private DeckCombatConfirmUI combatConfirmUI;
        [SerializeField] private Button reevaluateButton;
        [Tooltip("Main·Heavy·Rider·Cannon 순서의 고정 드롭 슬롯입니다.")]
        [SerializeField] private DeckEditSlotView[] deckSlots;
        [SerializeField] private DeckSlotInfoUI slotInfoPanel;
        [SerializeField] private ScrollRect ownedScrollView;
        [SerializeField] private OwnedUnitSlot ownedSlotPrefab;
        [SerializeField] private DeckOwnedFilter ownedFilter = new();
        [SerializeField, Min(0f)] private float ownedSlotSpacing = 50f;
        [SerializeField] private Vector2 ownedSlotPadding = new(25f, 25f);

        private readonly List<OwnedUnitSlot> _ownedSlots = new();
        private DeckEditDisplayModel _displayModel;
        private int _changingSlot = -1;
        private CharacterInstanceId _infoId;
        private IUnitVisualTableQuery _unitVisuals;
        private ITraitVisualTableQuery _traitVisuals;
        private ICategoryVisualTableQuery _categoryVisuals;
        private IDeckEditInput _input;
        private Action _open;
        private bool _prepared;
        private bool _isOpen;
        private bool _canEdit;
        private bool _canConfirm;
        private IDeckCombatInput _departure;
        private bool _canStart;
        private OwnedUnitSlot _dragSource;
        private CharacterInstanceId _draggedId;
        private int _dragPointerId;
        private Image _dragGhost;
        private RectTransform _dragArea;
        private Camera _dragCamera;

        public override UIId ID => UIId.DeckEdit;
        public override UIId ParentID => UIId.StageSelect;
        public bool IsOpen => _isOpen;
        public ScrollPanelPresentation ScrollPresentation => scrollPresentation != null ? scrollPresentation :
            throw new InvalidOperationException("[DeckEditUI] 두루마리 scrollPresentation 참조가 필요합니다.");
        public bool IsDepartureConfirmationOpen => combatConfirmUI.IsOpen;
        public int DeckSlotCount => deckSlots.Length;
        public int ChangingSlotIndex => _changingSlot;
        public event Action<bool> OpenStateChanged;

        /// <summary>런타임 씬 연결과 에디터 도구에서 덱 상세 패널의 정렬용 Canvas 구성을 확인합니다.</summary>
        /// <returns>별도 정렬을 사용하는 상세 패널의 Canvas입니다. 공통 Canvas를 쓰면 null입니다.</returns>
        public Canvas ValidateChildCanvases()
        {
            Canvas slotInfoCanvas = null;
            foreach (Canvas childCanvas in GetComponentsInChildren<Canvas>(true))
            {
                Transform canvasTransform = childCanvas.transform;
                if (slotInfoCanvas != null || panel == null || slotInfoPanel == null ||
                    canvasTransform == panel.transform || !canvasTransform.IsChildOf(panel.transform) ||
                    slotInfoPanel.transform.parent != canvasTransform || canvasTransform.childCount != 1)
                {
                    throw new InvalidOperationException(
                        $"[DeckEditUI] UIId.DeckSlotInfo의 Canvas '{childCanvas.name}'는 Panel 안에서 slotInfoPanel만 직접 감싸야 합니다.");
                }
                if (!childCanvas.overrideSorting || childCanvas.GetComponent<GraphicRaycaster>() == null)
                {
                    throw new InvalidOperationException(
                        $"[DeckEditUI] UIId.DeckSlotInfo의 Canvas '{childCanvas.name}'에 Override Sorting과 GraphicRaycaster가 필요합니다.");
                }

                slotInfoCanvas = childCanvas;
            }
            return slotInfoCanvas;
        }

        private void Awake() => Prepare();

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _unitVisuals = (query as IHasViewQuery)?.ViewQuery.UnitVisualTableQuery;
            _traitVisuals = (query as IHasViewQuery)?.ViewQuery.TraitVisualTableQuery;
            _categoryVisuals = (query as IHasViewQuery)?.ViewQuery.CategoryVisualTableQuery;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ScrollPresentation.BindAudio(resolver.GetService(SystemGroupsService.Global).AudioServiceCommand);
        }

        public void BindOpenRequest(Action open) => _open = open;

        public void BindInput(IDeckEditInput input)
        {
            CancelChangeMode();
            CancelDrag();
            _input = input;
        }

        public void BindDeparture(IDeckCombatInput input)
        {
            _departure = input;
            combatConfirmUI.BindInput(input);
        }

        public void RenderDeparture(bool visible, bool canStart)
        {
            CancelChangeMode();
            CancelDrag();
            // 출전 버튼·확인창의 연결과 위치는 씬 연결 때 Validate가 검사했습니다.
            _canStart = visible && canStart;
            confirmButton.gameObject.SetActive(!visible);
            closeButton.gameObject.SetActive(!visible);
            startCombatButton.gameObject.SetActive(visible);
            startCombatButton.interactable = _canStart;
        }

        public override void Open()
        {
            Prepare();
            CancelDrag();
            CancelChangeMode();
            bool wasOpen = _isOpen;
            if (!wasOpen)
            {
                ownedFilter.Reset();
                RenderOwned();
                ResetOwnedScroll();
            }
            _isOpen = true;
            panel.SetActive(true);
            if (!wasOpen) ScrollPresentation.PlayOpen();
            SetOpenButtonVisible(false);
            if (!wasOpen) OpenStateChanged?.Invoke(true);
        }

        public override void Hide()
        {
            CancelChangeMode();
            CancelDrag();
            CloseInfo();
            bool wasOpen = _isOpen;
            _isOpen = false;
            try
            {
                if (wasOpen) OpenStateChanged?.Invoke(false);
            }
            finally
            {
                // 닫힘 알림 안에서 다시 열린 화면을 이전 Hide가 덮지 않습니다.
                if (!_isOpen)
                {
                    if (scrollPresentation != null) scrollPresentation.ResetHidden();
                    if (panel != null && panel != gameObject) panel.SetActive(false);
                    SetOpenButtonVisible(true);
                }
            }
        }

        public void Render(DeckEditDisplayModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Deck.Count != DeckEditSlotLayout.Count)
                throw new ArgumentException("[DeckEditUI] UIId.DeckEdit: 표시는 고정 병과 슬롯 네 칸이어야 합니다.", nameof(model));
            Prepare();
            CancelDrag();
            CancelChangeMode();
            _canEdit = model.CanEdit;
            _displayModel = model;
            _canConfirm = model.CanConfirm;

            for (int i = 0; i < deckSlots.Length; i++)
            {
                DeckEditCardModel card = model.Deck[i];
                string displayName = null;
                Sprite icon = null;
                if (card != null) GetVisual(card, out displayName, out icon);
                deckSlots[i].Render(card, displayName, icon, CategoryIconAt(i), CanChangeSlot(i));
            }

            RenderOwned();
            RefreshInfo();

            confirmButton.interactable = _canConfirm;
            // 다시 평가하면 풀리는 상태(낡음·준비 안 됨)일 때만 보입니다(평가 C-06).
            reevaluateButton.gameObject.SetActive(model.CanReevaluate);
        }

        private void RenderOwned()
        {
            int visibleCount = 0;
            if (_displayModel != null)
            {
                for (int i = 0; i < _displayModel.Owned.Count; i++)
                {
                    DeckEditCardModel card = _displayModel.Owned[i];
                    if (!ownedFilter.Includes(card.Category)) continue;
                    EnsureOwnedCapacity(visibleCount + 1);
                    OwnedUnitSlot slot = _ownedSlots[visibleCount++];
                    GetVisual(card, out string displayName, out Sprite icon);
                    Sprite categoryIcon = null;
                    if (_categoryVisuals != null && _categoryVisuals.TryGetCategoryVisual(card.Category, out var category))
                        categoryIcon = category.Icon;
                    slot.Render(card, displayName, icon, categoryIcon, _canEdit);
                    slot.Open();
                }
            }
            for (int i = visibleCount; i < _ownedSlots.Count; i++)
            {
                _ownedSlots[i].Clear();
                _ownedSlots[i].Hide();
            }
            LayoutOwnedSlots(visibleCount);
        }

        public void Clear()
        {
            Prepare();
            _canEdit = false;
            _canConfirm = false;
            RenderDeparture(false, false);
            CancelDrag();
            CloseInfo();
            _displayModel = null;
            ownedFilter.Reset();
            for (int i = 0; i < deckSlots.Length; i++)
            {
                deckSlots[i].Render(null, null, null, CategoryIconAt(i), false);
            }
            for (int i = 0; i < _ownedSlots.Count; i++)
            {
                _ownedSlots[i].Clear();
                _ownedSlots[i].Hide();
            }
            LayoutOwnedSlots(0);
            ResetOwnedScroll();
            confirmButton.interactable = false;
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled) return;
            if (slot == OpenSlot)
            {
                if (!_isOpen) _open?.Invoke();
                return;
            }
            IDeckEditInput input = _input;
            if (!_isOpen || input == null) return;
            if (IsDepartureConfirmationOpen) return;
            int filterIndex = FirstOwnedFilterSlot - slot;
            if (filterIndex >= 0 && filterIndex < DeckOwnedFilter.Count)
            {
                if (ownedFilter.Select(filterIndex))
                {
                    CancelDrag();
                    CancelChangeMode();
                    RenderOwned();
                    ResetOwnedScroll();
                }
                return;
            }
            CancelChangeMode();
            if (slot == StartCombatSlot)
            {
                if (_canStart) _departure?.StartCombat();
                return;
            }
            if (slot == CloseSlot) input.Close();
            else if (slot == ConfirmSlot)
            {
                if (_canConfirm) input.Confirm();
            }
            else if (slot == ReevaluateSlot) input.Reevaluate();

        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
        }

        /// <summary>
        /// 필수 구성을 상태를 바꾸지 않고 검사합니다. 씬 연결 때 선택 패널 검사가 부르고(평가 C-26), 준비도 이것부터 부릅니다.
        /// 출전 버튼·확인창은 출전에서 늘 쓰므로 필수입니다.
        /// </summary>
        internal void Validate()
        {
            if (contentFrame == null || !contentFrame.IsChildOf(ScrollPresentation.ContentRoot))
                throw new InvalidOperationException("[DeckEditUI] contentFrame은 두루마리 ContentRoot 안에 있어야 합니다.");
            if (panel == null || openButton == null || closeButton == null || confirmButton == null ||
                reevaluateButton == null || startCombatButton == null || combatConfirmUI == null ||
                deckSlots == null || deckSlots.Length != DeckEditSlotLayout.Count)
                throw new InvalidOperationException(
                    "[DeckEditUI] Panel, 열기·닫기·확정·다시 평가·출전 버튼, 출전 확인창, 덱 칸 참조가 필요합니다.");
            ValidateOwnedList();
            if (ownedFilter == null)
                throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: ownedFilter 설정이 필요합니다.");
            ownedFilter.Validate(panel.transform);
            if (slotInfoPanel == null || !slotInfoPanel.transform.IsChildOf(panel.transform))
                throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: Panel 내부의 공용 slotInfoPanel 참조가 필요합니다.");
            slotInfoPanel.Validate();
            for (int i = 0; i < deckSlots.Length; i++)
            {
                if (deckSlots[i] == null || !deckSlots[i].transform.IsChildOf(panel.transform))
                    throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: deckSlots[{i}]는 Panel 내부 슬롯이어야 합니다.");
                for (int previous = 0; previous < i; previous++)
                    if (deckSlots[previous] == deckSlots[i])
                        throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: deckSlots[{previous}]와 [{i}]가 같은 슬롯입니다.");
                deckSlots[i].Validate();
            }
            Canvas containingCanvas = GetComponentInParent<Canvas>(true);
            if (containingCanvas == null || !combatConfirmUI.transform.IsChildOf(panel.transform) ||
                combatConfirmUI.GetComponentInParent<Canvas>(true) != containingCanvas)
                throw new InvalidOperationException("[DeckEditUI] combatConfirmUI는 Panel 아래에 있고 DeckEditUI와 같은 부모 Canvas를 사용해야 합니다.");
            combatConfirmUI.Validate();
        }

        private void Prepare()
        {
            if (_prepared) return;
            Validate();
            slotInfoPanel.BindInput(this);
            var cancelBinding = panel.GetComponent<DeckEditCancelBinding>();
            if (cancelBinding == null) cancelBinding = panel.AddComponent<DeckEditCancelBinding>();
            cancelBinding.Bind(this);
            UIInputBinding.Attach(openButton, this, OpenSlot);
            UIInputBinding.Attach(closeButton, this, CloseSlot);
            UIInputBinding.Attach(confirmButton, this, ConfirmSlot);
            UIInputBinding.Attach(startCombatButton, this, StartCombatSlot);
            UIInputBinding.Attach(reevaluateButton, this, ReevaluateSlot);
            ownedFilter.Bind(this, FirstOwnedFilterSlot);
            for (int i = 0; i < deckSlots.Length; i++)
            {
                deckSlots[i].BindOwner(this);
                deckSlots[i].Bind(this, this, i);
            }
            foreach (Transform child in ownedScrollView.content)
            {
                var slot = child.GetComponent<OwnedUnitSlot>();
                if (slot == null)
                    throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: Content의 '{child.name}'에 OwnedUnitSlot이 필요합니다.");
                AddOwnedSlot(slot);
            }
            _dragArea = contentFrame;
            var ghost = new GameObject("OwnedDragPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ghost.layer = gameObject.layer;
            ghost.transform.SetParent(contentFrame, false);
            _dragGhost = ghost.GetComponent<Image>();
            _dragGhost.raycastTarget = false;
            _dragGhost.preserveAspect = true;
            ghost.SetActive(false);
            if (panel != gameObject) panel.SetActive(false);
            _prepared = true;
            Clear();
            SetOpenButtonVisible(true);
        }

        private void ValidateOwnedList()
        {
            if (ownedScrollView == null || ownedScrollView.content == null || ownedScrollView.viewport == null || ownedSlotPrefab == null)
                throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: ownedScrollView·Content·Viewport·ownedSlotPrefab 참조가 필요합니다.");
            if (!ownedScrollView.transform.IsChildOf(panel.transform) ||
                !ownedScrollView.content.IsChildOf(ownedScrollView.viewport) ||
                !ownedScrollView.viewport.IsChildOf(ownedScrollView.transform))
                throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: Panel/OwnedScrollView/Viewport/Content 계층을 확인하세요.");
            if (ownedScrollView.viewport.TryGetComponent(out Mask mask) && mask.graphic == null)
                throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: Viewport '{ownedScrollView.viewport.name}'의 Mask에 같은 객체의 Graphic이 필요합니다.");
            if (float.IsNaN(ownedSlotSpacing) || float.IsInfinity(ownedSlotSpacing) || ownedSlotSpacing < 0f ||
                float.IsNaN(ownedSlotPadding.x) || float.IsInfinity(ownedSlotPadding.x) || ownedSlotPadding.x < 0f ||
                float.IsNaN(ownedSlotPadding.y) || float.IsInfinity(ownedSlotPadding.y) || ownedSlotPadding.y < 0f)
                throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: 슬롯 간격과 여백은 유한한 0 이상의 값이어야 합니다.");
            ownedSlotPrefab.Validate();
            var slotRect = ownedSlotPrefab.transform as RectTransform;
            if (slotRect == null || slotRect.rect.width <= 0f || slotRect.rect.height <= 0f)
                throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: ownedSlotPrefab에 크기가 양수인 RectTransform이 필요합니다.");
            foreach (Transform child in ownedScrollView.content)
            {
                var slot = child.GetComponent<OwnedUnitSlot>();
                if (slot == null)
                    throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: Content의 '{child.name}'에 OwnedUnitSlot이 필요합니다.");
                slot.Validate();
            }
        }

        private void AddOwnedSlot(OwnedUnitSlot slot)
        {
            slot.BindOwner(this);
            slot.BindDragReceiver(this);
            slot.BindInteraction(this);
            _ownedSlots.Add(slot);
        }

        private void EnsureOwnedCapacity(int count)
        {
            while (_ownedSlots.Count < count)
            {
                OwnedUnitSlot slot = Instantiate(ownedSlotPrefab, ownedScrollView.content, false);
                slot.name = $"OwnedUnitSlot{_ownedSlots.Count + 1}";
                AddOwnedSlot(slot);
            }
        }

        private void LayoutOwnedSlots(int count)
        {
            Vector2 size = ((RectTransform)ownedSlotPrefab.transform).rect.size;
            for (int i = 0; i < count; i++)
            {
                var rect = (RectTransform)_ownedSlots[i].transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.localScale = Vector3.one;
                rect.sizeDelta = size;
                rect.anchoredPosition = new Vector2(ownedSlotPadding.x + i * (size.x + ownedSlotSpacing), -ownedSlotPadding.y);
            }
            RectTransform content = ownedScrollView.content;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(ownedScrollView.viewport.rect.width,
                ownedSlotPadding.x * 2f + count * size.x + Mathf.Max(0, count - 1) * ownedSlotSpacing));
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(ownedScrollView.viewport.rect.height, size.y + ownedSlotPadding.y * 2f));
            Vector2 position = content.anchoredPosition;
            position.x = Mathf.Clamp(position.x, Mathf.Min(0f, ownedScrollView.viewport.rect.width - content.rect.width), 0f);
            position.y = 0f;
            content.anchoredPosition = position;
        }

        private void ResetOwnedScroll()
        {
            ownedScrollView.StopMovement();
            ownedScrollView.content.anchoredPosition = Vector2.zero;
        }

        private void GetVisual(DeckEditCardModel card, out string displayName, out Sprite icon)
        {
            displayName = null;
            icon = null;
            if (_unitVisuals == null || !_unitVisuals.TryGetUnitVisual(card.CharacterKey, out var visual)) return;
            displayName = visual.DisplayName;
            icon = visual.InfoIcon;
        }

        private Sprite CategoryIconAt(int index) =>
            _categoryVisuals != null && _categoryVisuals.TryGetCategoryVisual(DeckEditSlotLayout.CategoryAt(index), out var visual)
                ? visual.Icon : null;

        private bool CanInteract => isActiveAndEnabled && _isOpen && _input != null && !IsDepartureConfirmationOpen;

        // 허용 여부는 한 곳에서 합성하고 버튼·클릭 교체·드래그가 공유합니다. Core 규칙을 추정하지 않습니다.
        private bool CanChangeSlot(int index) => _canEdit &&
            (allowMainCharacterChange || DeckEditSlotLayout.CategoryAt(index) != CategoryKey.Main);

        void IDeckEditInteractionReceiver.BeginSlotChange(int index)
        {
            if (!CanInteract || !CanChangeSlot(index)) return;
            DeckEditSlotLayout.CategoryAt(index);
            if (_changingSlot == index) { CancelChangeMode(); return; }
            CancelDrag();
            _changingSlot = index;
            RefreshChangeVisuals();
        }

        void IDeckEditInteractionReceiver.CancelSlotChange() => CancelChangeMode();

        private void CancelChangeMode()
        {
            _changingSlot = -1;
            if (_prepared) RefreshChangeVisuals();
        }

        private void RefreshChangeVisuals()
        {
            for (int i = 0; i < deckSlots.Length; i++) deckSlots[i].SetChangeMode(i == _changingSlot, CanChangeSlot(i));
            foreach (OwnedUnitSlot slot in _ownedSlots)
                slot.SetChangeHighlighted(_changingSlot >= 0 && slot.Card != null && slot.gameObject.activeSelf &&
                    slot.Card.Category == DeckEditSlotLayout.CategoryAt(_changingSlot));
        }

        bool IDeckEditInteractionReceiver.ClickOwned(OwnedUnitSlot slot)
        {
            if (_changingSlot < 0) return false;
            int target = _changingSlot;
            bool matching = CanInteract && CanChangeSlot(target) && slot.Card != null &&
                slot.Card.Category == DeckEditSlotLayout.CategoryAt(target);
            CharacterInstanceId id = slot.Card?.OwnedId ?? CharacterInstanceId.Empty;
            CancelChangeMode();
            if (matching) _input.PlaceOwned(id, target);
            return true;
        }

        void IDeckEditInteractionReceiver.DeleteSlot(int index)
        {
            if (_changingSlot != index) return;
            bool canDelete = CanInteract && CanChangeSlot(index);
            DeckEditCardModel card = deckSlots[index].Card;
            CancelChangeMode();
            if (!canDelete || card == null) return;
            if (_infoId == card.OwnedId) CloseInfo();
            _input.RemoveSlot(index);
        }

        void IDeckEditInteractionReceiver.ShowSlotInfo(int index)
        {
            if (!CanInteract) return;
            ShowInfo(deckSlots[index].Card);
        }

        void IDeckEditInteractionReceiver.ShowOwnedInfo(CharacterInstanceId ownedId)
        {
            if (!CanInteract || _displayModel == null) return;
            foreach (DeckEditCardModel card in _displayModel.Owned)
                if (card.OwnedId == ownedId)
                {
                    ShowInfo(card);
                    return;
                }
            // 재표시 뒤 지난 슬롯에서 온 입력은 현재 보유 목록에 대상이 없으므로 무시합니다.
        }

        private void ShowInfo(DeckEditCardModel card)
        {
            if (card == null) return;
            IUISystemService ui = RequireUI();
            CancelChangeMode();
            CancelDrag();
            if (_infoId == card.OwnedId) { CloseInfo(); return; }
            GetVisual(card, out string displayName, out Sprite portrait);
            slotInfoPanel.Render(card, displayName, portrait, _traitVisuals);
            _infoId = card.OwnedId;
            ui.OpenUI(UIId.DeckSlotInfo);
        }

        void IDeckEditInteractionReceiver.CloseSlotInfo()
        {
            CancelChangeMode();
            CloseInfo();
        }

        void IDeckEditInteractionReceiver.SlotInfoHidden() => _infoId = CharacterInstanceId.Empty;

        private IUISystemService RequireUI() => UISystemService ??
            throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: 공용 SlotInfoPannel 개폐에 UISystem 바인딩이 필요합니다.");

        private void CloseInfo()
        {
            if (_infoId.IsEmpty) return;
            IUISystemService ui = RequireUI();
            _infoId = CharacterInstanceId.Empty;
            ui.CloseUI(UIId.DeckSlotInfo);
        }

        private void RefreshInfo()
        {
            if (_infoId.IsEmpty) return;
            DeckEditCardModel found = null;
            foreach (var card in _displayModel.Deck)
                if (card != null && card.OwnedId == _infoId) found = card;
            foreach (var card in _displayModel.Owned)
                if (card.OwnedId == _infoId) found = card;
            if (found == null) CloseInfo();
            else
            {
                GetVisual(found, out string displayName, out Sprite portrait);
                slotInfoPanel.Render(found, displayName, portrait, _traitVisuals);
            }
        }

        bool IDeckEditDragReceiver.BeginOwnedDrag(OwnedUnitSlot source, PointerEventData eventData)
        {
            if (!isActiveAndEnabled || !_isOpen || !_canEdit || _input == null || IsDepartureConfirmationOpen || _dragSource != null ||
                source.Card == null || source.Card.InDeck) return false;
            int target = DeckEditSlotLayout.IndexOf(source.Card.Category);
            if (target < 0 || !CanChangeSlot(target)) return false;
            CancelChangeMode();
            _dragSource = source;
            _draggedId = source.Card.OwnedId;
            _dragPointerId = eventData.pointerId;
            _dragCamera = eventData.pressEventCamera;
            _dragGhost.sprite = source.Portrait;
            _dragGhost.rectTransform.sizeDelta = ((RectTransform)source.transform).rect.size;
            _dragGhost.gameObject.SetActive(true);
            _dragGhost.transform.SetAsLastSibling();
            deckSlots[target].SetDropHighlighted(true);
            MoveDragPreview(eventData.position);
            return true;
        }

        void IDeckEditDragReceiver.MoveOwnedDrag(OwnedUnitSlot source, PointerEventData eventData)
        {
            if (_dragSource == source && eventData.pointerId == _dragPointerId) MoveDragPreview(eventData.position);
        }

        void IDeckEditDragReceiver.EndOwnedDrag(OwnedUnitSlot source)
        {
            if (_dragSource == source) CancelDrag();
        }

        void IDeckEditDragReceiver.DropOwned(int slotIndex, PointerEventData eventData)
        {
            if (_dragSource == null || eventData.pointerId != _dragPointerId ||
                eventData.button != PointerEventData.InputButton.Left || eventData.pointerDrag != _dragSource.gameObject ||
                !isActiveAndEnabled || !_isOpen || !_canEdit || _input == null || IsDepartureConfirmationOpen ||
                !CanChangeSlot(slotIndex) ||
                DeckEditSlotLayout.CategoryAt(slotIndex) != _dragSource.Card.Category) return;
            CharacterInstanceId id = _draggedId;
            CancelDrag();
            _input.PlaceOwned(id, slotIndex);
        }

        private void MoveDragPreview(Vector2 screenPosition)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragArea, screenPosition, _dragCamera, out Vector2 local))
                _dragGhost.rectTransform.localPosition = local;
        }

        private void CancelDrag()
        {
            _dragSource = null;
            _draggedId = CharacterInstanceId.Empty;
            if (_dragGhost != null) _dragGhost.gameObject.SetActive(false);
            if (_prepared)
                for (int i = 0; i < deckSlots.Length; i++) deckSlots[i].SetDropHighlighted(false);
        }

        protected override void OnUIHidden()
        {
            if (scrollPresentation != null) scrollPresentation.ResetHidden();
            CancelDrag();
            CancelChangeMode();
            _infoId = CharacterInstanceId.Empty;
        }

        private void SetOpenButtonVisible(bool visible)
        {
            if (openButton != null && openButton.gameObject.activeSelf != visible)
                openButton.gameObject.SetActive(visible);
        }
    }
}
