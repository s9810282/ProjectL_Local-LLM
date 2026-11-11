using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>등록된 화면 안에서 특성 하나를 표시합니다. 독립 UI 등록·선택·입력 수명을 갖지 않습니다.</summary>
    [Serializable]
    internal sealed class UnitAbilityTooltip
    {
        [SerializeField] private GameObject panel;
        // 아이콘 없이 제목과 설명만 표시하는 패널도 허용합니다.
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        internal GameObject Panel => panel;
        internal bool HasReferences => panel != null || icon != null || nameText != null || descriptionText != null;

        internal void Validate(Transform owner, string field)
        {
            UIBase ui = owner.GetComponentInParent<UIBase>(true);
            string context = $"[UnitAbilityTooltip] UIId.{ui?.ID ?? UIId.None}, '{owner.name}', {field}";
            if (panel == null || nameText == null || descriptionText == null)
                throw new InvalidOperationException($"{context}: panel·nameText·descriptionText 참조가 필요합니다.");
            if (panel.transform == owner || !panel.transform.IsChildOf(owner) ||
                (icon != null && !icon.transform.IsChildOf(panel.transform)) || !nameText.transform.IsChildOf(panel.transform) ||
                !descriptionText.transform.IsChildOf(panel.transform) || nameText == descriptionText)
                throw new InvalidOperationException($"{context}: 설명 패널은 입력 객체의 자식이어야 하며 표시 참조는 패널 내부에서 각각 연결해야 합니다.");
        }

        internal void Show(in UnitCardTraitContent content)
        {
            // 호출자는 구성 검증을 먼저 마칩니다. 내용 전체를 바꾼 뒤 표시합니다.
            if (icon != null) icon.sprite = content.Icon;
            nameText.text = content.Name;
            descriptionText.text = content.Description;
            if (!panel.activeSelf) panel.SetActive(true);
        }

        internal void Hide()
        {
            // 설명 기능이 없는 로비 카드와 초기 바인딩 전 정리도 같은 경로를 사용합니다.
            if (panel != null && panel.activeSelf) panel.SetActive(false);
        }
    }
}
