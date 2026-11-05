using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>선택한 유닛만 동적 등록한다. 내용과 마스크 확장 연출은 이 화면이 소유한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UnitHUDInfoPanel : UIBase, IPointerEnterHandler, IPointerExitHandler, IUIWorldPointerPassThrough
    {
        public UnitHUDInfoPanel() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.UnitHUDInfoPanel;
        internal override bool RegisterWithScene => false;
        [SerializeField] private RectTransform maskRect;
        [SerializeField] private RectMask2D mask;
        [SerializeField] private Image background;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthSubFill;
        [SerializeField] private Image shieldFill;
        [SerializeField] private UnitHUDHoverPanel hoverPanel;
        [SerializeField, Min(0.01f)] private float expandDuration = 0.16f;
        private float _width;
        private float _elapsed;
        private bool _hovered;
        private bool _prepared;
        private bool _registered;

        internal UnitHUDHoverPanel HoverPanel => hoverPanel;
        // background는 이 객체의 Graphic으로 검증한다. 자식 Button/상태 아이콘은 통과 대상으로 만들지 않는다.
        bool IUIWorldPointerPassThrough.AllowsWorldPointerThrough => _prepared && isActiveAndEnabled;

        internal void Prepare()
        {
            if (_prepared) return;
            if (maskRect == null || mask == null || mask.transform != maskRect || transform.parent != maskRect ||
                background == null || background.transform != transform || healthFill == null ||
                healthSubFill == null || shieldFill == null || hoverPanel == null)
                throw new InvalidOperationException($"[UnitHUDInfoPanel] UIId.{ID}, '{name}': maskRect/mask/background/healthFill/healthSubFill/shieldFill/hoverPanel 연결이 필요합니다.");
            if (!healthFill.transform.IsChildOf(transform) ||
                !healthSubFill.transform.IsChildOf(transform) || !shieldFill.transform.IsChildOf(transform) || maskRect.parent == null ||
                hoverPanel.transform.parent != maskRect.parent)
                throw new InvalidOperationException($"[UnitHUDInfoPanel] UIId.{ID}, '{name}': healthFill/healthSubFill/shieldFill은 패널 내부, hoverPanel은 같은 HUD의 Mask 밖에 있어야 합니다.");
            if (shieldFill == healthFill || shieldFill == healthSubFill || shieldFill.sprite == null ||
                shieldFill.type != Image.Type.Filled || shieldFill.fillMethod != Image.FillMethod.Horizontal || shieldFill.fillOrigin != 0 ||
                shieldFill.transform.parent != healthFill.transform.parent || shieldFill.transform.parent != healthSubFill.transform.parent ||
                shieldFill.transform.GetSiblingIndex() >= healthSubFill.transform.GetSiblingIndex() ||
                healthSubFill.transform.GetSiblingIndex() >= healthFill.transform.GetSiblingIndex())
                throw new InvalidOperationException($"[UnitHUDInfoPanel] UIId.{ID}, '{name}': shieldFill은 Sprite가 연결된 왼쪽 시작 가로 Filled Image이며 같은 부모의 Shield → SubFill → Fill 순서가 필요합니다.");
            hoverPanel.Prepare((RectTransform)transform);
            _width = maskRect.rect.width;
            if (_width <= 0f)
                throw new InvalidOperationException($"[UnitHUDInfoPanel] UIId.{ID}, '{name}': InfoPanelMask 너비는 양수여야 합니다.");
            // Hover 수신은 유지한다. 필드 입력 가림은 IUIWorldPointerPassThrough 계약으로 별도 판정한다.
            background.raycastTarget = true;
            _prepared = true;
            // 동적 등록 전에는 데이터만 준비하고 두 화면 모두 숨긴다.
            Hide();
            hoverPanel.Hide();
        }

        internal void SetRegistered(bool registered)
        {
            _registered = registered;
            _hovered = false;
        }

        internal void SetUnit(string unitName, int attack, int? shield)
        {
            Prepare();
            hoverPanel.SetValues(unitName, attack, shield);
        }

        internal void CopyHealth(Image fill, Image subFill, Image shield)
        {
            if (!_prepared) return; // Awake/바인딩 전 Clear는 정상이다.
            if (fill == null) throw new ArgumentNullException(nameof(fill));
            if (subFill == null) throw new ArgumentNullException(nameof(subFill));
            if (shield == null) throw new ArgumentNullException(nameof(shield));
            healthFill.fillAmount = fill.fillAmount;
            healthFill.color = fill.color;
            healthSubFill.fillAmount = subFill.fillAmount;
            healthSubFill.color = subFill.color;
            shieldFill.fillAmount = shield.fillAmount;
            shieldFill.color = shield.color;
            shieldFill.enabled = shield.enabled;
        }

        public override void Open()
        {
            Prepare();
            bool wasOpen = gameObject.activeSelf;
            base.Open();
            if (wasOpen) return;
            _elapsed = 0f;
            maskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        }

        private void Update() => AnimateExpansion(Time.unscaledDeltaTime);

        internal void AnimateExpansion(float deltaTime)
        {
            if (!_prepared) return;
            _elapsed = Mathf.Min(_elapsed + Mathf.Max(0f, deltaTime), expandDuration);
            maskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                _width * Mathf.SmoothStep(0f, 1f, _elapsed / expandDuration));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_registered || !isActiveAndEnabled || _hovered) return;
            _hovered = true;
            hoverPanel.RefreshPosition();
            UISystemService.OpenChildUI(ID, hoverPanel.ID);
        }

        public void OnPointerExit(PointerEventData eventData) => ClearHover();

        internal void ClearHover()
        {
            if (!_hovered) return;
            _hovered = false;
            if (_registered) UISystemService.CloseUI(hoverPanel.ID);
        }

        protected override void OnUIHidden() => ClearHover();
    }
}
