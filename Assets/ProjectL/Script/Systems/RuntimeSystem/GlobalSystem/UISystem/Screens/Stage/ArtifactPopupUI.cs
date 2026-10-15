using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.ServiceKeys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// StageSelect의 아티팩트 팝업 UI입니다. 열기·닫기 버튼, 하단 보유 목록, 이름·효과·상태 문구를 맡습니다.
    /// 3D 보드·드래그·입력 Layer·Core 요청은 <see cref="ArtifactViewSystem"/>이 맡습니다.
    /// </summary>
    /// <remarks>
    /// 루트는 항상 켜 두고 자식 Panel만 켜고 끕니다(DeckButtonUI 구조). UISystem은 씬 연결 때 닫힌 UI의 Hide를 부르므로
    /// 루트를 끄면 열기 버튼까지 사라집니다. 열기·닫기는 UISystem을 거칩니다(FW-009).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ArtifactPopupUI : UIBase, IUIInputReceiver, IArtifactPopupScreen
    {
        private const int OpenSlot = -1;
        private const int CloseSlot = -2;
        private static readonly Color SlotColor = new Color(.08f, .20f, .27f, 1f);

        [Tooltip("StageSelect 화면의 아티팩트 열기 버튼입니다. Panel 밖, 이 오브젝트 아래에 둡니다.")]
        [SerializeField] private Button openButton;
        [Tooltip("열고 닫는 별도 자식 Panel입니다. 이 오브젝트 자체를 연결하면 안 됩니다.")]
        [SerializeField] private GameObject panel;
        [Tooltip("Panel 안의 닫기 버튼입니다.")]
        [SerializeField] private Button closeButton;
        [Tooltip("하단 보유 목록 영역입니다. 배치물을 여기에 놓으면 반환합니다.")]
        [SerializeField] private RectTransform poolArea;
        [Tooltip("목록에 복제할 슬롯 Button입니다. Transition은 None을 권장합니다.")]
        [SerializeField] private Button slotPrefab;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text artifactName;
        [SerializeField] private TMP_Text effectDescription;

        private readonly List<Button> _slots = new();
        private readonly List<int> _slotArtifactIds = new();
        private IUISystemService _ui;
        private IArtifactBufferReadSink _board;
        private IArtifactViewTableQuery _views;
        private IArtifactPopupHost _host;
        private ArtifactBufferSnapshot _drawn;
        private bool _isOpen;

        public override UIId ID => UIId.Artifact;

        internal bool IsOpen => _isOpen;
        internal GameObject Panel => panel;
        internal Button OpenButton => openButton;
        internal Button CloseButton => closeButton;
        internal int VisibleSlotCount
        {
            get
            {
                int count = 0;
                foreach (Button slot in _slots)
                    if (slot.gameObject.activeSelf) count++;
                return count;
            }
        }

        private void Awake()
        {
            UIInputBinding.Attach(openButton, this, OpenSlot);
            UIInputBinding.Attach(closeButton, this, CloseSlot);
            if (panel != null && panel != gameObject) panel.SetActive(false);
        }

        private void Update()
        {
            // ArtifactBuffer에는 변경 알림이 없습니다. 열려 있을 때만 스냅샷 참조를 비교합니다.
            if (_isOpen && _board != null && !ReferenceEquals(_board.Current, _drawn))
                RefreshPool();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _views = (query as IHasViewQuery)?.ViewQuery?.ArtifactViewTableQuery;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            // UIBase의 UISystem 참조는 private이므로 열기·닫기 요청용 서비스는 resolver에서 받습니다.
            _ui = resolver.GetService(SystemGroupsService.Global).UIServiceCommand;
            _board = resolver.GetService(BufferSystemKey.Buffer).ArtifactReadSink;
            _drawn = null;
        }

        public override void Open()
        {
            if (panel == null) throw new InvalidOperationException("[ArtifactPopupUI] Panel is required.");
            bool wasOpen = _isOpen;
            _isOpen = true;
            panel.SetActive(true);
            _drawn = null;
            RefreshPool();
            if (!wasOpen) _host?.PopupOpened();
        }

        public override void Hide()
        {
            bool wasOpen = _isOpen;
            _isOpen = false;
            try
            {
                // 씬 연결 때의 Hide는 열린 적이 없으므로 View를 부르지 않습니다.
                if (wasOpen) _host?.PopupHidden();
            }
            finally
            {
                _drawn = null;
                SetText(statusText, string.Empty);
                SetText(artifactName, string.Empty);
                SetText(effectDescription, string.Empty);
                if (panel != null && panel != gameObject) panel.SetActive(false);
            }
        }

        /// <summary>참조가 빠졌거나 Panel·열기 버튼 배치가 구조와 다르면 예외를 던집니다.</summary>
        internal void Validate()
        {
            if (openButton == null || panel == null || closeButton == null || poolArea == null || slotPrefab == null ||
                statusText == null || artifactName == null || effectDescription == null)
                throw new InvalidOperationException("[ArtifactPopupUI] Buttons, panel, pool area, slot prefab and texts are required.");
            if (panel == gameObject || !panel.transform.IsChildOf(transform))
                throw new InvalidOperationException("[ArtifactPopupUI] The panel must be a separate child of the popup UI.");
            if (openButton.transform.IsChildOf(panel.transform))
                throw new InvalidOperationException("[ArtifactPopupUI] The open button must stay outside the popup panel.");
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == OpenSlot)
            {
                if (!_isOpen && _ui != null && _host != null && _host.CanOpen) _ui.OpenUI(ID);
            }
            else if (slot == CloseSlot)
            {
                if (_isOpen && _ui != null) _ui.CloseUI(ID);
            }
            else ShowSlotDetail(slot);
        }

        void IUIInputReceiver.HoverEnter(int slot) => ShowSlotDetail(slot);

        void IUIInputReceiver.HoverExit(int slot) { }

        void IArtifactPopupScreen.Connect(IArtifactPopupHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        void IArtifactPopupScreen.Disconnect() => _host = null;

        void IArtifactPopupScreen.ShowStatus(string message) => SetText(statusText, message);

        void IArtifactPopupScreen.ShowDetail(int artifactId) => ShowDetail(artifactId);

        bool IArtifactPopupScreen.TryPickPool(Vector2 screen, out int artifactId)
        {
            for (int i = 0; i < _slots.Count && i < _slotArtifactIds.Count; i++)
            {
                Button slot = _slots[i];
                if (!slot.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)slot.transform, screen, null)) continue;
                artifactId = _slotArtifactIds[i];
                return true;
            }

            artifactId = 0;
            return false;
        }

        bool IArtifactPopupScreen.IsReturnArea(Vector2 screen) => Contains(poolArea, screen);

        bool IArtifactPopupScreen.IsBlockingArea(Vector2 screen)
        {
            // 열린 팝업 뒤에 가려진 열기 버튼은 검사하지 않습니다. 겹친 하단 목록의 회수가 취소되기 때문입니다.
            return (closeButton != null && Contains((RectTransform)closeButton.transform, screen)) ||
                Contains(artifactName != null ? artifactName.rectTransform : null, screen) ||
                Contains(effectDescription != null ? effectDescription.rectTransform : null, screen) ||
                Contains(statusText != null ? statusText.rectTransform : null, screen);
        }

        private void ShowSlotDetail(int slot)
        {
            if (!_isOpen || slot < 0 || slot >= _slotArtifactIds.Count) return;
            ShowDetail(_slotArtifactIds[slot]);
        }

        private void ShowDetail(int artifactId)
        {
            ArtifactViewData visual = null;
            if (_views != null) _views.TryGetArtifactView(artifactId, out visual);
            SetText(artifactName, visual?.DisplayName);
            SetText(effectDescription, visual?.Description);
        }

        /// <summary>Buffer의 보유 목록으로 슬롯을 다시 그립니다. 슬롯 Button은 재사용합니다.</summary>
        private void RefreshPool()
        {
            if (_board == null || poolArea == null || slotPrefab == null) return;
            ArtifactBufferSnapshot current = _board.Current;
            _drawn = current;
            _slotArtifactIds.Clear();
            IReadOnlyList<ArtifactData> items = current.IsInitialized ? current.PoolItems : Array.Empty<ArtifactData>();

            for (int i = 0; i < items.Count; i++)
            {
                if (i == _slots.Count)
                {
                    Button created = Instantiate(slotPrefab, poolArea, false);
                    _slots.Add(created);
                    // 슬롯 번호와 수신자를 한 번 연결하고 현재 목록에서 ID를 읽습니다.
                    UIInputBinding.Attach(created, this, i);
                }

                Button slot = _slots[i];
                _slotArtifactIds.Add(items[i].ArtifactId);
                slot.name = $"ArtifactSlot_{items[i].ArtifactId}_{items[i].InstanceId}";
                if (slot.image != null)
                {
                    // 목록의 모양은 Buffer의 로컬 셀로 그립니다. 별도 모양 데이터를 중복 저장하지 않습니다.
                    slot.image.sprite = null;
                    slot.image.color = SlotColor;
                }
                slot.gameObject.SetActive(true);
            }

            for (int i = items.Count; i < _slots.Count; i++)
            {
                SlotShape(_slots[i]).ClearShape();
                _slots[i].gameObject.SetActive(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(poolArea);
            for (int i = 0; i < items.Count; i++)
                SlotShape(_slots[i]).DrawShape(items[i].Cells);
        }

        private static ArtifactPoolItemView SlotShape(Button slot)
        {
            var shape = slot.GetComponent<ArtifactPoolItemView>();
            return shape != null ? shape : slot.gameObject.AddComponent<ArtifactPoolItemView>();
        }

        private static bool Contains(RectTransform rect, Vector2 screen)
        {
            return rect != null && rect.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }
    }
}
