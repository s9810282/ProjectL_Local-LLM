using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>소유 화면 내부의 특성 버튼과 아이콘 참조입니다. 입력 수신자는 소유 화면이 연결합니다.</summary>
    [Serializable]
    internal sealed class UnitAbilitySlot
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [Tooltip("공용 설명창의 표시 위치입니다. 전투 정보창에서는 아이콘 내부에 연결합니다.")]
        [SerializeField] private RectTransform tooltipAnchor;

        internal Button Button => button;
        internal RectTransform TooltipAnchor => tooltipAnchor;

        internal void ValidateTooltipAnchor(Transform owner, int index)
        {
            if (tooltipAnchor == null || tooltipAnchor == icon.transform || !tooltipAnchor.IsChildOf(icon.transform))
                throw new InvalidOperationException($"[UnitAbilitySlot] '{owner.name}': abilitySlots[{index}].tooltipAnchor는 아이콘 내부의 별도 기준점이어야 합니다.");
        }

        internal void Validate(Transform owner, int index, bool requireName = false)
        {
            if (button == null || icon == null)
                throw new InvalidOperationException($"[UnitAbilitySlot] '{owner.name}': abilitySlots[{index}].button·icon 참조가 필요합니다.");
            if (!button.transform.IsChildOf(owner) || button.transform == owner ||
                (icon.transform != button.transform && !icon.transform.IsChildOf(button.transform)))
                throw new InvalidOperationException($"[UnitAbilitySlot] '{owner.name}': abilitySlots[{index}]의 버튼은 화면 내부, 아이콘은 버튼 내부에 있어야 합니다.");
            // 이름을 표시하지 않는 기존 공용 카드는 이 참조를 생략합니다.
            if (requireName && nameText == null)
                throw new InvalidOperationException($"[UnitAbilitySlot] '{owner.name}': abilitySlots[{index}].nameText(AbilIcon/Name) 참조가 필요합니다.");
            if (nameText != null && (nameText.transform == icon.transform || !nameText.transform.IsChildOf(icon.transform)))
                throw new InvalidOperationException($"[UnitAbilitySlot] '{owner.name}': abilitySlots[{index}].nameText는 아이콘 내부의 별도 텍스트여야 합니다.");
        }

        internal void Render(Sprite sprite, bool visible, string abilityName = null)
        {
            // Sprite 미입력은 허용하지만 특성 부재와 구분합니다. 버튼의 입력 영역을 없애지 않습니다.
            if (icon.sprite != sprite) icon.sprite = sprite;
            if (nameText != null) nameText.text = visible ? abilityName ?? string.Empty : string.Empty;
            if (button.gameObject.activeSelf != visible) button.gameObject.SetActive(visible);
        }

        /// <summary>목록 갱신 때만 호출합니다. 아이콘의 자식 인덱스를 따라 복제본의 참조를 연결합니다.</summary>
        internal UnitAbilitySlot Clone(Transform parent)
        {
            Button clone = UnityEngine.Object.Instantiate(button, parent, false);
            clone.gameObject.SetActive(false);
            return new UnitAbilitySlot
            {
                button = clone,
                icon = MapTransform(icon.transform, button.transform, clone.transform).GetComponent<Image>(),
                nameText = nameText == null ? null :
                    MapTransform(nameText.transform, button.transform, clone.transform).GetComponent<TMP_Text>(),
                tooltipAnchor = tooltipAnchor == null ? null :
                    (RectTransform)MapTransform(tooltipAnchor, button.transform, clone.transform)
            };
        }

        private static Transform MapTransform(Transform source, Transform original, Transform clone)
        {
            if (source == original) return clone;
            return MapTransform(source.parent, original, clone).GetChild(source.GetSiblingIndex());
        }
    }
}
