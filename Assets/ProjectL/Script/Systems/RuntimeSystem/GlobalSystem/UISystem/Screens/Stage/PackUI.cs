using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 낮의 가방 표시와 펼침 입력을 담당합니다. 재고와 장착은 소유하지 않습니다.
    /// 재고는 전투 표시가 그리기 때 넘긴 스냅숏만 그립니다(가방 설계 P1·P2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PackUI : UIBase, IUIInputReceiver, ICombatPackDisplay
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIPackClick = new("Combat.Action.UI.Pack.Click");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIPackHoverEnter = new("Combat.Action.UI.Pack.HoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIPackHoverExit = new("Combat.Action.UI.Pack.HoverExit");
#endif

        // 입력 슬롯 0은 펼침 토글이고, 아이템 칸은 1부터 칸 순서대로 씁니다.
        private const int FirstItemSlot = 1;

        [SerializeField] private Button toggleButton;
        [SerializeField] private PackExpansion expansion;
        [SerializeField] private GameObject descriptionPanel;
        [Tooltip("설명 패널 안에서 아이템 이름·설명을 보일 글자입니다(가방 설계 P2).")]
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject dragRect;
        [Tooltip("아이템을 끄는 동안 마우스를 따라다닐 아이콘입니다(가방 설계 P4). Pack 아래, 펼침 목록(마스크) 밖에 두고 기준점(Pivot)은 가운데로 둡니다. " +
            "보일 때 입력을 받지 않게 바뀝니다. 아이템 아이콘이 없으면 여기에 넣은 그림을 씁니다.")]
        [SerializeField] private Image dragIcon;

        private readonly List<PackInventoryLayout.Entry> _entries = new();
        private PackItemSlotView[] _slots = Array.Empty<PackItemSlotView>();
        private IConsumableItemVisualTableQuery _visuals;
        private ItemInventorySnapshot _shownInventory;
        private bool _warnedOverflow;
        // 아이템 장착 입력은 전투 View가 씬 연결 때 만든다. 씬 연결 전·해제 뒤에는 null이다(가방 설계 P4).
        private ICombatViewService _combat;
        private Sprite _dragDefaultSprite;
        private bool _dragPrepared;
        private IUISystemService _ui;
        private bool _shown;
        private bool _bound;
        public bool IsExpanded { get; private set; }

        public PackUI() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.Pack;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateReferences();
            _combat = resolver.GetService(SystemGroupsService.InGame).ViewGroup?.Combat;
            if (!_bound)
            {
                UIInputBinding.Attach(toggleButton, this, 0);
                for (int i = 0; i < _slots.Length; i++)
                {
                    Button button = _slots[i].Button;
                    UIInputBinding.Attach(button, this, FirstItemSlot + i, hasHover: true);
                    // 칸 드래그 시작은 손패 카드와 같이 Click으로 받는다. 이동·놓기는 전투 입력이 처리한다(가방 설계 P4).
                    var drag = button.GetComponent<DeckCardDragBinding>();
                    if (drag == null) drag = button.gameObject.AddComponent<DeckCardDragBinding>();
                    drag.Bind(this, FirstItemSlot + i);
                }
                _bound = true;
            }
            ResetPresentation();
        }

        public override void GetUIData(IServiceModule query) { }

        /// <summary>숨김 중에도 전투 View가 호출하므로 밤이 끝나면 다시 열 수 있습니다.</summary>
        internal void Refresh(in CombatInputFacts facts, bool suspended)
        {
            bool visible = !suspended && facts.HasBattle && facts.IsBattleDisplayed
                && !facts.HasCompletion && !CombatInputPolicy.IsTerminal(facts)
                && facts.StagePhase == StagePhase.Deployment;
            if (_shown == visible) return;
            if (_ui == null)
                throw new InvalidOperationException("[PackUI] UIId.Pack의 UISystem 연결이 필요합니다.");
            if (visible)
            {
                // 이번 버전에서는 아이템을 제공하지 않으므로 가방 자동 활성화를 보류합니다.
                // _ui.OpenUI(ID, true);
            }
            else _ui.CloseUI(ID);
        }

        public override void Open()
        {
            ValidateReferences();
            ResetPresentation();
            _shown = true;
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            ValidateReferences();
            // 가방이 닫히면(밤·결과 표시) 집은 아이템도 내려놓는다.
            _combat?.ItemInput?.Cancel();
            ResetPresentation();
            _shown = false;
            gameObject.SetActive(false);
        }

        void ICombatPackDisplay.ConfigureItems(IConsumableItemVisualTableQuery visuals)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            // 외형이 바뀌었으니 다음 재고는 같은 스냅숏이어도 다시 그린다.
            _shownInventory = null;
        }

        void ICombatPackDisplay.ShowInventory(ItemInventorySnapshot inventory)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            // Buffer는 재고가 바뀔 때만 새 스냅숏으로 바꾸므로 같은 객체면 그릴 것이 없다.
            if (ReferenceEquals(_shownInventory, inventory)) return;
            ValidateReferences();

            int overflow = PackInventoryLayout.Build(inventory, _slots.Length, _entries);
            if (overflow > 0 && !_warnedOverflow)
            {
                _warnedOverflow = true;
                Debug.LogWarning($"[PackUI] 가방 칸({_slots.Length})보다 아이템 종류가 많아 {overflow}종을 보이지 않습니다.");
            }
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i >= _entries.Count)
                {
                    _slots[i].Hide();
                    continue;
                }
                PackInventoryLayout.Entry entry = _entries[i];
                Sprite icon = _visuals != null && _visuals.TryGet(entry.ItemKey, out var visual) ? visual.Icon : null;
                _slots[i].Show(entry.ItemKey, icon, entry.Count);
            }
            _shownInventory = inventory;
            // 칸 내용이 바뀌었으므로 이전 칸의 설명은 닫는다.
            descriptionPanel.SetActive(false);
        }

        void IUIInputReceiver.Click(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIPackClickSample = _profileUIPackClick.Auto();
#endif

            if (slot >= FirstItemSlot)
            {
                SelectItem(slot - FirstItemSlot);
                return;
            }
            if (slot != 0 || !_shown || !isActiveAndEnabled || !toggleButton.IsInteractable()) return;
            IsExpanded = !IsExpanded;
            ClearTransientPresentation();
            expansion.SetExpanded(IsExpanded);
        }

        // 칸을 끌기 시작하거나 누르면 그 아이템을 집는다. 집을 수 있는지(국면·대기·재고)는 전투 입력이 판단한다.
        private void SelectItem(int index)
        {
            if (!_shown || !isActiveAndEnabled || index < 0 || index >= _slots.Length) return;
            int itemKey = _slots[index].ItemKey;
            ICombatItemInput input = _combat?.ItemInput;
            if (itemKey <= 0 || input == null || !input.CanSelect(itemKey)) return;
            descriptionPanel.SetActive(false);
            input.Select(itemKey);
        }

        void ICombatPackDisplay.ShowItemDrag(int itemKey)
        {
            PrepareDragIcon();
            Sprite icon = _visuals != null && _visuals.TryGet(itemKey, out var visual) ? visual.Icon : null;
            dragIcon.sprite = icon != null ? icon : _dragDefaultSprite;
            // 같은 Pack 안의 다른 그림보다 위에 그린다.
            dragIcon.transform.SetAsLastSibling();
            dragIcon.gameObject.SetActive(true);
        }

        void ICombatPackDisplay.MoveItemDrag(float screenX, float screenY)
        {
            if (!dragIcon.gameObject.activeSelf || !(dragIcon.transform.parent is RectTransform parent)) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(screenX, screenY), eventCamera, out Vector2 local))
            {
                Vector3 position = dragIcon.transform.localPosition;
                dragIcon.transform.localPosition = new Vector3(local.x, local.y, position.z);
            }
        }

        void ICombatPackDisplay.HideItemDrag() => dragIcon.gameObject.SetActive(false);

        // 아이콘은 포인터 바로 아래에 있으므로 UI 가림 판정에 걸리지 않게 입력을 받지 않도록 한 번 바꾼다.
        // 그러지 않으면 필드 위에서 놓아도 "UI 위에서 뗌"으로 취소된다.
        private void PrepareDragIcon()
        {
            if (_dragPrepared) return;
            foreach (Graphic graphic in dragIcon.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            _dragDefaultSprite = dragIcon.sprite;
            _dragPrepared = true;
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIPackHoverEnterSample = _profileUIPackHoverEnter.Auto();
#endif

            int index = slot - FirstItemSlot;
            if (!_shown || !isActiveAndEnabled || index < 0 || index >= _slots.Length) return;
            int itemKey = _slots[index].ItemKey;
            if (itemKey <= 0) return;
            descriptionText.text = _visuals != null && _visuals.TryGet(itemKey, out var visual)
                ? $"{visual.DisplayName}\n{visual.Description}"
                : itemKey.ToString();
            descriptionPanel.SetActive(true);
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIPackHoverExitSample = _profileUIPackHoverExit.Auto();
#endif

            if (slot >= FirstItemSlot) descriptionPanel.SetActive(false);
        }

        private void ResetPresentation()
        {
            IsExpanded = false;
            ClearTransientPresentation();
            expansion.SetExpanded(false, true);
            // 처음 그리기·숨김 때는 드래그가 없다(숨김은 먼저 드래그를 취소한다). 펼침 토글은 아이콘을 건드리지 않는다.
            dragIcon.gameObject.SetActive(false);
        }

        private void ClearTransientPresentation()
        {
            descriptionPanel.SetActive(false);
            dragRect.SetActive(false);
        }

        private void OnDisable()
        {
            // 바인딩 전 씬 수집의 비활성화는 정상 수명입니다.
            if (!_bound) return;
            _combat?.ItemInput?.Cancel();
            ResetPresentation();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _bound) ClearTransientPresentation();
        }

        private void ValidateReferences()
        {
            if (toggleButton == null || expansion == null || descriptionPanel == null || descriptionText == null || dragRect == null
                || dragIcon == null)
                throw new InvalidOperationException("[PackUI] UIId.Pack의 toggleButton, expansion, descriptionPanel, descriptionText, dragRect, dragIcon 참조가 필요합니다.");
            if (!toggleButton.transform.IsChildOf(transform) || !expansion.transform.IsChildOf(transform)
                || descriptionPanel == gameObject || !descriptionPanel.transform.IsChildOf(transform)
                || !descriptionText.transform.IsChildOf(descriptionPanel.transform)
                || dragRect == gameObject || !dragRect.transform.IsChildOf(transform)
                || dragIcon.gameObject == gameObject || !dragIcon.transform.IsChildOf(transform))
                throw new InvalidOperationException("[PackUI] UIId.Pack의 표시 요소는 Pack 내부 자식이어야 하고, 설명 글자는 설명 패널 안에 있어야 합니다.");
            // 펼침 목록 안에 두면 목록 마스크에 잘려 필드 위에서 보이지 않는다.
            if (dragIcon.transform.IsChildOf(expansion.transform))
                throw new InvalidOperationException("[PackUI] 드래그 아이콘(dragIcon)은 펼침 목록(PackExpansion) 밖에 두어야 합니다.");
            expansion.ValidateReferences();
            CollectSlots();
        }

        // 칸은 펼침 목록 안의 Item 프리팹들이다. 처음 한 번 모으고 계층 순서를 칸 순서로 쓴다.
        private void CollectSlots()
        {
            if (_slots.Length > 0) return;
            _slots = expansion.GetComponentsInChildren<PackItemSlotView>(true);
            if (_slots.Length == 0)
                throw new InvalidOperationException("[PackUI] 가방 목록에 PackItemSlotView 칸이 없습니다. Item 프리팹에 PackItemSlotView를 붙여 주세요.");
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].BindOwner(this);
                _slots[i].ValidateReferences();
            }
        }
    }
}
