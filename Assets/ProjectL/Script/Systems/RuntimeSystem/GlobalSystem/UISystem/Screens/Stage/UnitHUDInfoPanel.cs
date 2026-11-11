using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>기존 상세 HUD 프리팹의 참조와 표시 데이터를 유지한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UnitHUDInfoPanel : UIBase
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
        private bool _prepared;

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
            if (maskRect.rect.width <= 0f)
                throw new InvalidOperationException($"[UnitHUDInfoPanel] UIId.{ID}, '{name}': InfoPanelMask 너비는 양수여야 합니다.");
            background.raycastTarget = false;
            _prepared = true;
            // 전투에서는 기본 HUD만 표시한다. 상세 프리팹은 비활성 상태로 준비한다.
            Hide();
            hoverPanel.Hide();
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
    }
}
