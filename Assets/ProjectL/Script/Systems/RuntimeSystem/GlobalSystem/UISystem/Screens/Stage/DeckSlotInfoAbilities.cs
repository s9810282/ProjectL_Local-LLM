using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>상세창의 병종·유닛 고유·추가 특성에 표시값을 전달합니다.</summary>
    [Serializable]
    internal sealed class DeckSlotInfoAbilities
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private DeckSlotCategoryAbility category = new();
        [SerializeField] private DeckSlotAbilitySection unit = new();
        [SerializeField] private DeckSlotAbilitySection growth = new();

        internal void Validate(Transform owner)
        {
            DeckSlotInfoReferences.RequireChild(scroll, owner, "abilities.scroll");
            DeckSlotInfoReferences.RequireChild(scroll.content, scroll.transform, "abilities.scroll.content");
            if (!scroll.enabled || !scroll.vertical || scroll.horizontal || scroll.viewport == null ||
                !scroll.content.IsChildOf(scroll.viewport) ||
                !scroll.viewport.TryGetComponent(out RectMask2D mask) || !mask.enabled ||
                !scroll.content.TryGetComponent(out VerticalLayoutGroup layout) || !layout.enabled ||
                !scroll.content.TryGetComponent(out ContentSizeFitter fitter) || !fitter.enabled ||
                fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                throw DeckSlotInfoReferences.Error(owner, "abilities.scroll에는 세로 스크롤·뷰포트 마스크·세로 자동 배치가 필요합니다.");
            if (unit == null || growth == null)
                throw DeckSlotInfoReferences.Error(owner, "abilities.unit·growth 영역이 필요합니다.");
            unit.Validate(owner, "abilities.unit");
            growth.Validate(owner, "abilities.growth");
            if (unit.Root.parent != scroll.content || growth.Root.parent != scroll.content || unit.Root == growth.Root)
                throw DeckSlotInfoReferences.Error(owner, "abilities.unit·growth는 스크롤 콘텐츠의 서로 다른 직계 자식이어야 합니다.");
            if (category == null)
                throw DeckSlotInfoReferences.Error(owner, "abilities.category 영역이 필요합니다.");
            category.Validate(owner);
        }

        internal void BindInput() => category.BindInput();

        internal void ValidateContent(UnitCardTraitContent[] contents)
        {
            if (contents == null) throw new ArgumentNullException(nameof(contents));
        }

        internal void Render(UnitCardTraitContent[] contents, IReadOnlyDictionary<TraitKey, int> ownedRanks = null)
        {
            ValidateContent(contents);
            category.Render(contents);
            unit.Render(contents, TraitOrigin.Unit, ownedRanks);
            growth.Render(contents, TraitOrigin.Growth, ownedRanks);

            // 세로 레이아웃은 같은 부모 아래의 형제 순서를 따릅니다.
            if (unit.Root.GetSiblingIndex() > growth.Root.GetSiblingIndex())
                unit.Root.SetSiblingIndex(growth.Root.GetSiblingIndex());
        }

        internal void ResetForSelection()
        {
            category.ClearHover();
            scroll.StopMovement();
            scroll.content.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>병종 특성 하나의 아이콘과 호버 설명을 소유합니다.</summary>
    [Serializable]
    internal sealed class DeckSlotCategoryAbility : IUIInputReceiver
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private UnitAbilityTooltip tooltip = new();

        private UIInputBinding _input;
        private UnitCardTraitContent _content;

        internal void Validate(Transform owner)
        {
            DeckSlotInfoReferences.RequireChild(button, owner, "abilities.category.button");
            DeckSlotInfoReferences.RequireChild(icon, button.transform, "abilities.category.icon");
            if (button.targetGraphic == null || button.targetGraphic == icon || !button.targetGraphic.raycastTarget ||
                !button.targetGraphic.transform.IsChildOf(button.transform))
                throw DeckSlotInfoReferences.Error(owner, "abilities.category.button에 아이콘과 별도의 Raycast Target Graphic이 필요합니다.");
            if (tooltip == null)
                throw DeckSlotInfoReferences.Error(owner, "abilities.category.tooltip 참조가 필요합니다.");
            tooltip.Validate(button.transform, "abilities.category.tooltip");
        }

        internal void BindInput()
        {
            tooltip.Hide();
            button.gameObject.SetActive(false);
            _input = UIInputBinding.Attach(button, this, 0, hasHover: true);
        }

        internal void Render(UnitCardTraitContent[] contents)
        {
            int index = Array.FindIndex(contents, content => content.Origin == TraitOrigin.Category);
            bool visible = index >= 0;
            _content = visible ? contents[index] : default;
            icon.sprite = _content.Icon;
            icon.enabled = _content.Icon != null;
            if (!visible) ClearHover();
            button.gameObject.SetActive(visible);
            if (tooltip.Panel.activeSelf) tooltip.Show(_content);
        }

        internal void ClearHover() => _input.OnHoverExit();

        // 병종 특성 설명의 입력은 호버로만 받습니다.
        void IUIInputReceiver.Click(int slot) { }
        void IUIInputReceiver.HoverEnter(int slot) => tooltip.Show(_content);
        void IUIInputReceiver.HoverExit(int slot) => tooltip.Hide();
    }

    /// <summary>한 출처의 카드 슬롯을 순서대로 채우고 재사용합니다.</summary>
    [Serializable]
    internal sealed class DeckSlotAbilitySection
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private DeckSlotAbilityContent template = new();
        private readonly List<DeckSlotAbilityContent> _slots = new();

        internal RectTransform Root => root;

        internal void Validate(Transform owner, string field)
        {
            DeckSlotInfoReferences.RequireChild(root, owner, field + ".root");
            if (template == null) throw DeckSlotInfoReferences.Error(owner, field + ".template이 필요합니다.");
            template.Validate(root, field + ".template");
            if (template.Root.parent.parent != root ||
                !template.Root.parent.TryGetComponent(out VerticalLayoutGroup layout) || !layout.enabled)
                throw DeckSlotInfoReferences.Error(owner, field + ".template은 영역 내부의 세로 목록에 연결해야 합니다.");
        }

        internal void Render(UnitCardTraitContent[] contents, TraitOrigin origin, IReadOnlyDictionary<TraitKey, int> ownedRanks)
        {
            if (_slots.Count == 0) _slots.Add(template);
            int count = 0;
            foreach (UnitCardTraitContent content in contents)
            {
                if (content.Origin != origin) continue;
                if (count == _slots.Count) _slots.Add(template.Clone(template.Root.parent));
                _slots[count++].Render(content, true, ownedRanks);
            }
            for (int i = count; i < _slots.Count; i++) _slots[i].Render(default, false);
            root.gameObject.SetActive(count > 0);
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
        [SerializeField] private Image levelBadge;
        [SerializeField] private TMP_Text levelText;

        internal RectTransform Root => root;

        internal void Validate(Transform owner, string field)
        {
            DeckSlotInfoReferences.RequireChild(root, owner, field + ".root");
            DeckSlotInfoReferences.RequireChild(icon, root, field + ".icon");
            DeckSlotInfoReferences.RequireChild(name, root, field + ".name");
            DeckSlotInfoReferences.RequireChild(description, root, field + ".description");
            DeckSlotInfoReferences.RequireChild(levelBadge, icon.transform, field + ".levelBadge");
            DeckSlotInfoReferences.RequireChild(levelText, levelBadge.transform, field + ".levelText");
            if (name == description || name == levelText || description == levelText)
                throw DeckSlotInfoReferences.Error(owner, field + ": 이름·설명·등급은 별도 텍스트여야 합니다.");
            if (levelBadge.raycastTarget || levelText.raycastTarget)
                throw DeckSlotInfoReferences.Error(owner, field + ".levelBadge·levelText의 Raycast Target은 꺼야 합니다.");
        }

        internal void Render(UnitCardTraitContent content, bool visible, IReadOnlyDictionary<TraitKey, int> ownedRanks = null)
        {
            icon.sprite = visible ? content.Icon : null;
            name.text = visible ? content.Name : string.Empty;
            description.text = visible ? content.Description : string.Empty;
            string label = visible ? content.FormatLevel(ownedRanks) : string.Empty;
            levelText.text = label;
            levelBadge.gameObject.SetActive(label.Length > 0);
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
                description = MapTransform(description.transform, root, clone).GetComponent<TMP_Text>(),
                levelBadge = MapTransform(levelBadge.transform, root, clone).GetComponent<Image>(),
                levelText = MapTransform(levelText.transform, root, clone).GetComponent<TMP_Text>()
            };
        }

        private static Transform MapTransform(Transform source, Transform original, Transform clone)
        {
            if (source == original) return clone;
            return MapTransform(source.parent, original, clone).GetChild(source.GetSiblingIndex());
        }
    }
}
