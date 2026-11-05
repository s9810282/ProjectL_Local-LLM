using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 로비의 아티팩트 팝업 UI입니다. 닫기 버튼, 하단 보유 목록, 이름·효과·상태 문구를 맡습니다.
    /// 외부에서 전달한 표시값과 닫기 허용 상태를 사용하고 실제 열림·숨김을 동기 알립니다.
    /// </summary>
    /// <remarks>
    /// 루트는 항상 켜 두고 자식 Panel만 켜고 끕니다. 로비 시설 도착과 명시적 닫기는 UISystem을 거칩니다(FW-009).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ArtifactPopupUI : UIBase, IUIInputReceiver, IArtifactPopupDisplay, IArtifactPopupLifetime,
        IArtifactPopupScreenAreas
    {
        private const int CloseSlot = -2;
        private static readonly Color SlotColor = new Color(.08f, .20f, .27f, 1f);

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
        // 슬롯마다 모양 위젯을 만들 때 한 번 찾아 둡니다(로비 리팩토링 2차 설계 N-65). 프리팹에 직렬화되면 AddComponent 갈래를 걷습니다.
        private readonly List<ArtifactPoolItemView> _slotShapes = new();
        // 슬롯 이름에 쓴 (아티팩트 ID, 인스턴스 ID)입니다. 바뀔 때만 이름 문자열을 만듭니다(로비 리팩토링 2차 설계 N-38).
        private readonly List<(int ArtifactId, int InstanceId)> _slotNameKeys = new();
        private readonly List<ArtifactPoolItemContent> _slotContents = new();
        private IArtifactPopupCloseReceiver _closeReceiver;
        private bool _isOpen;
        private int _hoveredSlot = -1;
        private int _detailSlot = -1;

        public override UIId ID => UIId.Artifact;

        public bool IsOpen => _isOpen;
        /// <summary>실제 Open/Hide 상태 변경을 같은 호출 안에서 한 번 알립니다.</summary>
        public event Action<bool> OpenStateChanged;
        internal GameObject Panel => panel;

        private void Awake()
        {
            UIInputBinding.Attach(closeButton, this, CloseSlot);
            SetCanClose(false);
            if (panel != null && panel != gameObject) panel.SetActive(false);
        }

        /// <summary>부모 Canvas가 꺼지거나 씬이 해제될 때도 패널을 닫고 열림 해제를 알립니다(UIBase 꺼짐 훅).</summary>
        protected override void OnUIHidden() => Hide();

        /// <summary>현재 로비 연결의 명시적 닫기 수신자를 연결합니다.</summary>
        public void BindClose(IArtifactPopupCloseReceiver receiver) => _closeReceiver = receiver;

        public override void Open()
        {
            if (panel == null) throw new InvalidOperationException("[ArtifactPopupUI] Panel 참조가 필요합니다.");
            bool wasOpen = _isOpen;
            _isOpen = true;
            panel.SetActive(true);
            if (!wasOpen)
            {
                NotifyOpenStateChanged(true);
            }
        }

        public override void Hide()
        {
            bool wasOpen = _isOpen;
            _isOpen = false;
            try
            {
                // 씬 연결 때의 Hide는 열린 적이 없으므로 수명 알림이 없습니다.
                if (wasOpen)
                {
                    NotifyOpenStateChanged(false);
                }
            }
            finally
            {
                SetText(statusText, string.Empty);
                _hoveredSlot = -1;
                ClearSlotDetail();
                if (panel != null && panel != gameObject) panel.SetActive(false);
            }
        }

        private void NotifyOpenStateChanged(bool open)
        {
            Action<bool> changed = OpenStateChanged;
            if (changed == null) return;
            List<Exception> failures = null;
            // 앞선 관찰자 예외로 다른 수명 관찰자의 동기 정리가 빠지지 않도록 모두 호출합니다.
            foreach (Action<bool> observer in changed.GetInvocationList())
            {
                try
                {
                    observer(open);
                }
                catch (Exception error)
                {
                    (failures ??= new()).Add(error);
                }
            }
            if (failures != null) throw new AggregateException("[ArtifactPopupUI] 열림 상태 알림 중 실패했습니다.", failures);
        }

        /// <summary>표시 참조가 빠졌거나 Panel 배치가 구조와 다르면 예외를 던집니다.</summary>
        internal void Validate()
        {
            if (panel == null || closeButton == null || poolArea == null || slotPrefab == null ||
                statusText == null || artifactName == null || effectDescription == null)
                throw new InvalidOperationException("[ArtifactPopupUI] Panel·닫기 버튼·Pool Area·Slot Prefab·문구 참조가 모두 필요합니다.");
            if (panel == gameObject || !panel.transform.IsChildOf(transform))
                throw new InvalidOperationException("[ArtifactPopupUI] Panel은 팝업 UI 아래의 별도 자식이어야 합니다.");
            // 보유 목록·반환 영역·가림 판정은 화면 좌표를 카메라 없이 재므로 Overlay Canvas를 전제합니다(로비 리팩토링 2차 설계 N-10).
            Canvas canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("[ArtifactPopupUI] 아티팩트 팝업 UI는 Screen Space - Overlay Canvas 아래에 있어야 합니다.");
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == CloseSlot)
            {
                // 닫기 허용은 닫기 버튼의 interactable 하나가 원본입니다(N-22).
                if (_isOpen && closeButton.interactable) _closeReceiver?.CloseClicked();
            }
            else ShowSlotDetail(slot);
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
            if (!_isOpen || slot < 0 || slot >= _slotContents.Count) return;
            _hoveredSlot = slot;
            ShowSlotDetail(slot);
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
            if (slot < 0) return;
            if (_hoveredSlot == slot) _hoveredSlot = -1;
            if (_detailSlot == slot) ClearSlotDetail();
        }

        public void ShowStatus(string message) => SetText(statusText, message);

        /// <summary>
        /// 보드·드래그의 상세를 씁니다. 보유 목록 슬롯의 상세는 이 UI(호버·클릭) 한 경로가 갖으므로,
        /// 슬롯 상세가 떠 있는 동안에는 바꾸지 않습니다(로비 리팩토링 2차 설계 N-83, Q-R2c-1 A).
        /// </summary>
        public void ShowDetail(string displayName, string description)
        {
            if (_detailSlot >= 0) return;
            SetDetail(displayName, description);
        }

        /// <summary>포인터가 보유 목록 슬롯 위에 있는지입니다. EventSystem 호버 상태를 읽어 포인터 이동마다 슬롯을 다시 재지 않습니다.</summary>
        public bool IsPoolHovered => _isOpen && _hoveredSlot >= 0 && _hoveredSlot < _slotContents.Count;

        private void SetDetail(string displayName, string description)
        {
            SetText(artifactName, displayName);
            SetText(effectDescription, description);
        }

        private void ClearSlotDetail()
        {
            _detailSlot = -1;
            SetDetail(string.Empty, string.Empty);
        }

        /// <summary>연결 소유자가 닫기를 허용하는지를 닫기 버튼과 Click/Submit 수신에 반영합니다.</summary>
        public void SetCanClose(bool canClose)
        {
            if (closeButton != null) closeButton.interactable = canClose;
        }

        public bool TryPickPool(Vector2 screen, out int artifactId)
        {
            for (int i = 0; _isOpen && i < _slots.Count && i < _slotContents.Count; i++)
            {
                Button slot = _slots[i];
                if (!slot.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)slot.transform, screen, null)) continue;
                artifactId = _slotContents[i].ArtifactId;
                return true;
            }

            artifactId = 0;
            return false;
        }

        public bool IsReturnArea(Vector2 screen) => _isOpen && Contains(poolArea, screen);

        public bool IsBlockingArea(Vector2 screen)
        {
            if (!_isOpen) return false;
            // 열린 팝업 뒤에 가려진 열기 버튼은 검사하지 않습니다. 겹친 하단 목록의 회수가 취소되기 때문입니다.
            return (closeButton != null && Contains((RectTransform)closeButton.transform, screen)) ||
                Contains(artifactName != null ? artifactName.rectTransform : null, screen) ||
                Contains(effectDescription != null ? effectDescription.rectTransform : null, screen) ||
                Contains(statusText != null ? statusText.rectTransform : null, screen);
        }

        private void ShowSlotDetail(int slot)
        {
            if (!_isOpen || slot < 0 || slot >= _slotContents.Count) return;
            ArtifactPoolItemContent item = _slotContents[slot];
            _detailSlot = slot;
            SetDetail(item.DisplayName, item.Description);
        }

        /// <summary>호출자가 넘긴 목록 값만 복사해 그립니다. 목록 참조는 보관하지 않고 슬롯 Button은 재사용합니다.</summary>
        public void RenderPool(IReadOnlyList<ArtifactPoolItemContent> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            // 참조는 씬 연결(ArtifactSceneRoot.Validate) 때 한 번 검사합니다(N-22).
            _slotContents.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                if (i == _slots.Count)
                {
                    Button created = Instantiate(slotPrefab, poolArea, false);
                    _slots.Add(created);
                    _slotShapes.Add(CreateSlotShape(created));
                    _slotNameKeys.Add((int.MinValue, int.MinValue));
                    // 슬롯 번호와 수신자를 한 번 연결하고 현재 목록에서 ID를 읽습니다.
                    UIInputBinding.Attach(created, this, i, hasHover: true);
                }

                Button slot = _slots[i];
                _slotContents.Add(items[i]);
                var nameKey = (items[i].ArtifactId, items[i].InstanceId);
                if (_slotNameKeys[i] != nameKey)
                {
                    _slotNameKeys[i] = nameKey;
                    slot.name = $"ArtifactSlot_{nameKey.ArtifactId}_{nameKey.InstanceId}";
                }
                if (slot.image != null)
                {
                    // 전달받은 로컬 모양만 그립니다. 배치 규칙이나 별도 모양 DB는 없습니다.
                    slot.image.sprite = null;
                    slot.image.color = SlotColor;
                }
                _slotShapes[i].Open();
            }

            for (int i = items.Count; i < _slots.Count; i++)
            {
                _slotShapes[i].Hide();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(poolArea);
            for (int i = 0; i < items.Count; i++)
                _slotShapes[i].DrawShape(items[i].Cells);

            // 포인터가 정지한 채 목록 값만 바뀐 경우에도 현재 슬롯의 상세를 갱신합니다.
            if (_hoveredSlot >= 0 && _hoveredSlot < _slotContents.Count) ShowSlotDetail(_hoveredSlot);
            else
            {
                _hoveredSlot = -1;
                if (_detailSlot >= _slotContents.Count) ClearSlotDetail();
            }
        }

        private ArtifactPoolItemView CreateSlotShape(Button slot)
        {
            var shape = slot.GetComponent<ArtifactPoolItemView>();
            if (shape == null) shape = slot.gameObject.AddComponent<ArtifactPoolItemView>();
            shape.BindOwner(this);
            return shape;
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
