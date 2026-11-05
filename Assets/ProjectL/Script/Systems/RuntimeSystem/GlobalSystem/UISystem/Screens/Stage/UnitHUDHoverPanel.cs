using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    [DisallowMultipleComponent]
    public sealed class UnitHUDHoverPanel : UIBase
    {
        public UnitHUDHoverPanel() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.UnitHUDHoverPanel;
        public override UIId ParentID => UIId.UnitHUDInfoPanel;
        internal override bool RegisterWithScene => false;
        [SerializeField] private TMP_Text unitName;
        [SerializeField] private TMP_Text aValue;
        [SerializeField] private TMP_Text sValue;
        [SerializeField] private GameObject shieldRoot;
        [SerializeField, Min(0f)] private float gap = 4f;
        private RectTransform _info;
        private RectTransform _rect;
        private Canvas _canvas;
        private readonly Vector3[] _corners = new Vector3[4];

        internal void Prepare(RectTransform info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (shieldRoot == null || shieldRoot.transform == transform || !shieldRoot.transform.IsChildOf(transform) ||
                sValue == null || !sValue.transform.IsChildOf(shieldRoot.transform) ||
                (unitName != null && unitName.transform.IsChildOf(shieldRoot.transform)) ||
                (aValue != null && aValue.transform.IsChildOf(shieldRoot.transform)))
                throw new InvalidOperationException($"[UnitHUDHoverPanel] UIId.{ID}, '{name}': sValue만 포함하는 패널 내부의 shieldRoot 참조가 필요합니다.");
            if (unitName == null || aValue == null || sValue == null || info.parent == null ||
                info.parent.parent == null || transform.parent != info.parent.parent ||
                !unitName.transform.IsChildOf(transform) || !aValue.transform.IsChildOf(transform) ||
                !sValue.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[UnitHUDHoverPanel] UIId.{ID}, '{name}': Name/Atk/aValue/Shield/sValue 연결과 Mask 외부 배치가 필요합니다.");
            RectTransform rect = transform as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (rect == null || canvas == null)
                throw new InvalidOperationException($"[UnitHUDHoverPanel] UIId.{ID}, '{name}': RectTransform과 상위 Canvas가 필요합니다.");
            _info = info;
            _rect = rect;
            _canvas = canvas;
        }

        internal void SetValues(string displayName, int attack, int? shield)
        {
            unitName.SetText(displayName);
            aValue.SetText("{0}", attack);
            // TODO: 별도 보호막 잔량 계약이 도입되면 유효 물리 방어력 대체 연결을 교체한다.
            shieldRoot.SetActive(shield.HasValue);
            if (shield.HasValue) sValue.SetText("{0}", shield.Value);
            else sValue.SetText(string.Empty);
        }

        public override void Open()
        {
            if (_info == null || _rect == null || _canvas == null)
                throw new InvalidOperationException($"[UnitHUDHoverPanel] UIId.{ID}, '{name}': InfoPanel/RectTransform/Canvas를 Prepare한 뒤 열어야 합니다.");
            RefreshPosition();
            base.Open();
        }

        private void LateUpdate() => RefreshPosition();

        internal void RefreshPosition()
        {
            if (_info == null) return; // 동적 바인딩 전에는 표시 위치를 계산하지 않는다.
            Camera camera = _canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.rootCanvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, _info.TransformPoint(_info.rect.center));
            bool upper = screen.y >= _canvas.rootCanvas.pixelRect.center.y;
            _info.GetWorldCorners(_corners);
            // 위쪽 절반(1·2사분면)은 우측 하단, 아래쪽 절반(3·4사분면)은 우측 상단.
            _rect.pivot = new Vector2(0f, upper ? 1f : 0f);
            Vector3 local = _rect.parent.InverseTransformPoint(_corners[upper ? 3 : 2]);
            local += new Vector3(gap, upper ? -gap : gap, 0f);
            _rect.localPosition = local;
        }
    }
}
