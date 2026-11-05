using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>특성 수에 맞는 영역을 고릅니다. 세 개를 넘는 특성도 복제 슬롯에 전달합니다.</summary>
    [Serializable]
    internal sealed class DeckSlotInfoAbilities
    {
        private const int ExpandedAreaSlotCount = 3;

        [SerializeField] private DeckSlotAbilityArea area2 = new();
        [SerializeField] private DeckSlotAbilityArea area3 = new();

        internal void Validate(Transform owner)
        {
            if (area2 == null || area3 == null) throw DeckSlotInfoReferences.Error(owner, "abilities.area2·area3가 필요합니다.");
            area2.Validate(owner, 2);
            area3.Validate(owner, ExpandedAreaSlotCount);
            if (area2.Root == area3.Root || area2.Root.IsChildOf(area3.Root) ||
                area3.Root.IsChildOf(area2.Root))
                throw DeckSlotInfoReferences.Error(owner, "abilities의 두 영역은 서로 분리돼야 합니다.");
        }

        internal void ValidateContent(UnitCardTraitContent[] contents)
        {
            if (contents == null) throw new ArgumentNullException(nameof(contents));
        }

        internal void Render(UnitCardTraitContent[] contents)
        {
            ValidateContent(contents);
            bool three = contents.Length >= ExpandedAreaSlotCount;
            area2.Render(three ? Array.Empty<UnitCardTraitContent>() : contents, !three);
            area3.Render(three ? contents : Array.Empty<UnitCardTraitContent>(), three);
        }
    }

    /// <summary>저작 슬롯의 공통 부모가 추가 슬롯도 소유합니다. 복제본은 마지막 저작 슬롯 뒤에서 재사용합니다.</summary>
    [Serializable]
    internal sealed class DeckSlotAbilityArea
    {
        [SerializeField] private DeckSlotAbilityContent[] slots = Array.Empty<DeckSlotAbilityContent>();
        private readonly List<DeckSlotAbilityContent> _extraSlots = new();

        // Validate에서 슬롯과 공통 부모를 먼저 확인한 뒤 사용합니다.
        internal Transform Root => slots[0].Root.parent;

        internal void Validate(Transform owner, int slotCount)
        {
            string field = $"abilities.area{slotCount}";
            if (slots == null || slots.Length != slotCount)
                throw DeckSlotInfoReferences.Error(owner, field + $": 직접 배치한 슬롯 {slotCount}개가 필요합니다.");
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) throw DeckSlotInfoReferences.Error(owner, $"{field}.slots[{i}]가 필요합니다.");
                slots[i].Validate(owner, $"{field}.slots[{i}]");
                for (int j = 0; j < i; j++)
                    if (slots[i].Root == slots[j].Root) throw DeckSlotInfoReferences.Error(owner, field + ": 중복 슬롯 참조입니다.");
            }
            DeckSlotInfoReferences.RequireChild(Root, owner, field + ".slots[0].root.parent");
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Root.parent != Root)
                    throw DeckSlotInfoReferences.Error(owner, $"{field}.slots[{i}].root는 같은 영역의 직계 자식이어야 합니다.");
            }
        }

        internal void Render(UnitCardTraitContent[] contents, bool visible)
        {
            EnsureCapacity(contents.Length);
            for (int i = 0; i < slots.Length; i++)
                slots[i].Render(i < contents.Length ? contents[i] : default, i < contents.Length);
            for (int i = 0; i < _extraSlots.Count; i++)
            {
                int contentIndex = slots.Length + i;
                _extraSlots[i].Render(contentIndex < contents.Length ? contents[contentIndex] : default,
                    contentIndex < contents.Length);
            }
            Root.gameObject.SetActive(visible);
        }

        private void EnsureCapacity(int count)
        {
            DeckSlotAbilityContent template = slots[slots.Length - 1];
            int required = count - slots.Length;
            while (_extraSlots.Count < required)
            {
                DeckSlotAbilityContent clone = template.Clone(Root);
                // 같은 위치에 겹치고 원래 3번 슬롯이 앞에 그려지도록 둡니다.
                clone.Root.SetSiblingIndex(template.Root.GetSiblingIndex());
                _extraSlots.Add(clone);
            }
        }
    }

    /// <summary>상세창 전용 특성 표시 슬롯입니다. 버튼 입력이나 별도 UI 등록을 갖지 않습니다.</summary>
    [Serializable]
    internal sealed class DeckSlotAbilityContent
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text name;
        [SerializeField] private TMP_Text description;

        internal RectTransform Root => root;

        internal void Validate(Transform owner, string field)
        {
            DeckSlotInfoReferences.RequireChild(root, owner, field + ".root");
            DeckSlotInfoReferences.RequireChild(icon, root, field + ".icon");
            DeckSlotInfoReferences.RequireChild(name, root, field + ".name");
            DeckSlotInfoReferences.RequireChild(description, root, field + ".description");
            if (name == description) throw DeckSlotInfoReferences.Error(owner, field + ": 이름·설명은 별도 텍스트여야 합니다.");
        }

        internal void Render(UnitCardTraitContent content, bool visible)
        {
            icon.sprite = visible ? content.Icon : null;
            name.text = visible ? content.Name : string.Empty;
            description.text = visible ? content.Description : string.Empty;
            root.gameObject.SetActive(visible);
        }

        /// <summary>필요 수가 늘 때만 복제합니다. 이름 검색 없이 원본의 자식 인덱스로 내부 참조를 대응합니다.</summary>
        internal DeckSlotAbilityContent Clone(Transform parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            Validate(parent, "복제 원본 슬롯");
            RectTransform clone = UnityEngine.Object.Instantiate(root, parent, false);
            clone.gameObject.SetActive(false);
            return new DeckSlotAbilityContent
            {
                root = clone,
                icon = MapTransform(icon.transform, root, clone).GetComponent<Image>(),
                name = MapTransform(name.transform, root, clone).GetComponent<TMP_Text>(),
                description = MapTransform(description.transform, root, clone).GetComponent<TMP_Text>()
            };
        }

        private static Transform MapTransform(Transform source, Transform original, Transform clone)
        {
            if (source == original) return clone;
            return MapTransform(source.parent, original, clone).GetChild(source.GetSiblingIndex());
        }
    }
}
