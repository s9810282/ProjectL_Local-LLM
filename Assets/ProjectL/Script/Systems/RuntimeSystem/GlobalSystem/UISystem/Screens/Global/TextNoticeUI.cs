using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>공용 텍스트 패널입니다. 명시적으로 전달받은 좌표로 HoverTarget을 이동합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class TextNoticeUI : GlobalUIBase, IUIInputReceiver
    {
        [SerializeField] private UIId panelId = UIId.Notice;
        [SerializeField] private TMP_Text message;
        [Tooltip("Hover 패널의 Name 텍스트입니다. Notice에는 연결하지 않습니다.")]
        [SerializeField] private TMP_Text title;
        [SerializeField] private RectTransform hoverTarget;
        [SerializeField] private Vector2 cursorOffset = new Vector2(16f, 16f);
        [Tooltip("Notice의 Y/N 버튼을 포함하는 확인 영역입니다. 확인 요청 중에만 표시합니다.")]
        [SerializeField] private GameObject confirmArea;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        private IUIConfirmationReceiver _confirmation;
        private IUISystemService _ui;
        private bool _buttonsBound;
        private GameObject _previousSelection;
        internal bool HasConfirmation => _confirmation != null;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        internal bool IsConfirmationFor(IUIConfirmationReceiver receiver) =>
            receiver != null && ReferenceEquals(_confirmation, receiver) && isActiveAndEnabled;

        internal void PrepareConfirmation(string text, IUIConfirmationReceiver receiver)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            ValidateBindings();
            if (panelId != UIId.Notice || confirmArea == null || yesButton == null || noButton == null || _ui == null)
                throw new InvalidOperationException($"[TextNoticeUI] '{name}' (UIId.Notice)의 확인 표시에는 confirmArea·Y/N Button·UISystem 연결이 필요합니다.");
            if (confirmArea == gameObject || !confirmArea.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[TextNoticeUI] '{name}' (UIId.Notice)의 confirmArea '{confirmArea.name}'는 패널의 자식이어야 합니다.");
            if (yesButton == noButton || yesButton.gameObject == confirmArea || noButton.gameObject == confirmArea ||
                !yesButton.transform.IsChildOf(confirmArea.transform) || !noButton.transform.IsChildOf(confirmArea.transform))
                throw new InvalidOperationException($"[TextNoticeUI] '{name}' (UIId.Notice)의 Y/N Button은 confirmArea '{confirmArea.name}' 내부의 서로 다른 버튼이어야 합니다.");
            if (_confirmation != null && !ReferenceEquals(_confirmation, receiver))
                throw new InvalidOperationException("[TextNoticeUI] UIId.Notice가 다른 확인 응답을 기다리고 있습니다.");
            if (!_buttonsBound)
            {
                UIInputBinding.Attach(yesButton, this, 0);
                UIInputBinding.Attach(noButton, this, 1);
                if (GetComponent<UIPointerClickBoundary>() == null) gameObject.AddComponent<UIPointerClickBoundary>();
                _buttonsBound = true;
            }
            _confirmation = receiver;
            message.text = text;
        }

        internal void ClearConfirmation()
        {
            var events = EventSystem.current;
            if (HasConfirmation && events != null && events.currentSelectedGameObject != null &&
                events.currentSelectedGameObject.transform.IsChildOf(transform))
                events.SetSelectedGameObject(_previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null);
            _previousSelection = null;
            _confirmation = null;
            SetConfirmationArea(false);
        }

        private void SetConfirmationArea(bool visible)
        {
            if (yesButton != null)
                yesButton.interactable = visible;
            if (noButton != null)
                noButton.interactable = visible;
            // Hover와 확인 기능을 쓰지 않는 텍스트 전용 구성은 확인 영역을 생략할 수 있습니다.
            if (confirmArea != null)
                confirmArea.SetActive(visible);
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled || !HasConfirmation || (slot != 0 && slot != 1)) return;
            IUIConfirmationReceiver receiver = _confirmation;
            _ui.CloseUI(ID);
            receiver.Respond(slot == 0);
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        private Canvas _canvas;
        private RectTransform _panel;
        private RectTransform _hoverParent;
        private readonly List<RectTransform> _boundsRects = new List<RectTransform>(8);
        private readonly Vector3[] _worldCorners = new Vector3[4];
        private Vector2 _screenPosition;
        private bool _hasScreenPosition;

        internal uint HoverHideVersion { get; private set; }

        public override UIId ID => panelId;
        public override bool OpenWithGlobalUI => false;

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateBindings();
        }

        public override void GetUIData(IServiceModule query) { }

        internal void SetText(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            ValidateBindings();
            ClearConfirmation();
            message.text = text;
        }

        internal void SetHoverText(string name, string description)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (panelId != UIId.Hover)
                throw new InvalidOperationException($"[TextNoticeUI] '{this.name}' ({panelId})는 Hover 패널이 아닙니다.");
            ValidateBindings();
            title.text = name;
            message.text = description;
        }

        public override void Open()
        {
            ValidateBindings();
            if (panelId == UIId.Hover) hoverTarget.gameObject.SetActive(true);
            SetConfirmationArea(HasConfirmation);
            gameObject.SetActive(true);
            if (HasConfirmation)
            {
                transform.SetAsLastSibling();
                if (EventSystem.current != null)
                {
                    _previousSelection = EventSystem.current.currentSelectedGameObject;
                    EventSystem.current.SetSelectedGameObject(noButton.gameObject);
                }
            }
            ApplyPosition();
        }

        public override void Hide()
        {
            ClearConfirmation();
            // 비활성 패널에 들어온 명시적 UnHover도 아직 처리하지 않은 포인터 입력을 무효화합니다.
            if (!isActiveAndEnabled) InvalidateHover();
            gameObject.SetActive(false);
            if (message != null) message.text = string.Empty;
            if (title != null) title.text = string.Empty;
        }

        private void OnDisable()
        {
            ClearConfirmation();
            InvalidateHover();
        }

        private void InvalidateHover()
        {
            _hasScreenPosition = false;
            if (panelId == UIId.Hover)
            {
                unchecked
                {
                    HoverHideVersion++;
                }
            }
        }

        // 입력은 읽지 않습니다. TMP·Canvas 레이아웃 갱신 뒤에도 전달받은 좌표로 위치를 보정합니다.
        private void LateUpdate() => ApplyPosition();

        internal void MoveTo(Vector2 screenPosition)
        {
            if (panelId != UIId.Hover || !isActiveAndEnabled) return;
            _screenPosition = screenPosition;
            _hasScreenPosition = true;
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (panelId != UIId.Hover || !isActiveAndEnabled || !_hasScreenPosition) return;
            ValidateBindings();
            Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hoverParent, _screenPosition, camera, out Vector2 point))
                return;

            hoverTarget.localPosition = new Vector3(point.x, point.y, hoverTarget.localPosition.z);
            Bounds bounds = CalculatePanelBounds();
            Vector2 delta = new Vector2(point.x + cursorOffset.x - bounds.min.x,
                point.y - cursorOffset.y - bounds.max.y);
            Rect area = _hoverParent.rect;
            delta.x = ClampDelta(delta.x, bounds.min.x, bounds.max.x, area.xMin, area.xMax);
            delta.y = ClampDelta(delta.y, bounds.min.y, bounds.max.y, area.yMin, area.yMax);
            hoverTarget.localPosition += new Vector3(delta.x, delta.y, 0f);
        }

        private Bounds CalculatePanelBounds()
        {
            // 기존 API와 같이 활성 자식 Rect도 포함합니다. 목록과 코너 좌표 배열은 재사용합니다.
            _panel.GetComponentsInChildren(false, _boundsRects);
            Matrix4x4 toLocal = _hoverParent.worldToLocalMatrix;
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < _boundsRects.Count; i++)
            {
                _boundsRects[i].GetWorldCorners(_worldCorners);
                for (int corner = 0; corner < _worldCorners.Length; corner++)
                {
                    Vector3 point = toLocal.MultiplyPoint3x4(_worldCorners[corner]);
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }
            var bounds = new Bounds(min, Vector3.zero);
            bounds.Encapsulate(max);
            return bounds;
        }

        private static float ClampDelta(float desired, float min, float max, float areaMin, float areaMax)
        {
            if (max - min > areaMax - areaMin) return areaMin - min;
            return Mathf.Clamp(desired, areaMin - min, areaMax - max);
        }

        private void ValidateBindings()
        {
            if (panelId != UIId.Notice && panelId != UIId.Hover)
                throw new InvalidOperationException("[TextNoticeUI] Notice 또는 Hover ID만 사용할 수 있습니다.");
            if (message == null) throw new InvalidOperationException("[TextNoticeUI] TMP Text를 연결해야 합니다.");
            if (panelId != UIId.Hover) return;
            if (title == null)
                throw new InvalidOperationException($"[TextNoticeUI] '{name}' ({panelId})의 title(Name) TMP Text를 연결해야 합니다.");
            if (_canvas != null) return;
            if (hoverTarget == null || !transform.IsChildOf(hoverTarget) || hoverTarget == transform)
                throw new InvalidOperationException("[TextNoticeUI] 패널을 감싸는 HoverTarget을 연결해야 합니다.");
            _panel = transform as RectTransform;
            _hoverParent = hoverTarget.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (_panel == null || _hoverParent == null || canvas == null)
                throw new InvalidOperationException("[TextNoticeUI] Canvas와 RectTransform 부모가 필요합니다.");
            _canvas = canvas.rootCanvas;
        }
    }
}
