using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>같은 덱 카드의 기본 정보를 전용 텍스트에 표시합니다. 규칙 값을 재계산하지 않습니다.</summary>
    [Serializable]
    internal sealed class DeckSlotInfoStats
    {
        [FormerlySerializedAs("categoryAndName")]
        [SerializeField] private TMP_Text category;
        [SerializeField] private TMP_Text unitName;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text level;
        [SerializeField] private TMP_Text health;
        [SerializeField] private TMP_Text attack;
        [SerializeField] private TMP_Text defense;
        [SerializeField] private TMP_Text moveDistance;
        [SerializeField] private TMP_Text attackDistance;

        internal void Validate(Transform owner)
        {
            DeckSlotInfoReferences.RequireChild(category, owner, "stats.category");
            DeckSlotInfoReferences.RequireChild(unitName, owner, "stats.unitName");
            DeckSlotInfoReferences.RequireChild(portrait, owner, "stats.portrait");
            // 이름의 부모를 PortraitArea로 삼고 초상화의 Mask 등 중간 컨테이너를 허용합니다.
            Transform portraitArea = unitName.transform.parent;
            if (portrait.transform == portraitArea || !portrait.transform.IsChildOf(portraitArea))
                throw DeckSlotInfoReferences.Error(owner,
                    $"stats.portrait '{portrait.name}'는 stats.unitName '{unitName.name}'의 부모 PortraitArea '{portraitArea.name}' 하위에 있어야 합니다.");
            DeckSlotInfoReferences.RequireChild(level, owner, "stats.level");
            DeckSlotInfoReferences.RequireChild(health, owner, "stats.health");
            DeckSlotInfoReferences.RequireChild(attack, owner, "stats.attack");
            DeckSlotInfoReferences.RequireChild(defense, owner, "stats.defense");
            DeckSlotInfoReferences.RequireChild(moveDistance, owner, "stats.moveDistance");
            DeckSlotInfoReferences.RequireChild(attackDistance, owner, "stats.attackDistance");
            TMP_Text[] fields = { category, unitName, level, health, attack, defense, moveDistance, attackDistance };
            for (int i = 0; i < fields.Length; i++)
                for (int j = 0; j < i; j++)
                    if (fields[i] == fields[j]) throw DeckSlotInfoReferences.Error(owner, $"stats 텍스트 참조가 중복됩니다: {fields[i].name}.");
        }

        internal void Render(DeckEditCardModel card, string displayName, Sprite portraitSprite)
        {
            RenderValues(card.Category.ToString(),
                string.IsNullOrWhiteSpace(displayName) ? card.CharacterKey.ToString() : displayName,
                portraitSprite, card.Level, card.Stats?.MaxHealth, card.Stats?.AttackPower,
                card.Stats?.PhysicalDefense, card.Stats?.MoveRange, card.Stats?.AttackRange);
        }

        internal void Render(UnitInfoContent content)
        {
            RenderValues(content.Category, content.Name, content.Portrait, content.Level, content.Health,
                content.Attack, content.Defense, content.MoveDistance, content.AttackDistance);
        }

        private void RenderValues(string categoryValue, string nameValue, Sprite portraitSprite, int? levelValue,
            int? healthValue, int? attackValue, int? defenseValue, int? moveValue, int? attackDistanceValue)
        {
            category.text = categoryValue;
            unitName.text = nameValue;
            // View DB의 정보 이미지 미입력은 기존 선택 계약입니다. 이전 유닛의 초상화를 남기지 않습니다.
            portrait.sprite = portraitSprite;
            portrait.enabled = portraitSprite != null;
            level.text = levelValue?.ToString() ?? "-";
            // 능력치 미게시 카드는 기존 모델 계약에 따라 대기로 표시합니다.
            health.text = healthValue?.ToString() ?? "-";
            attack.text = attackValue?.ToString() ?? "-";
            defense.text = defenseValue?.ToString() ?? "-";
            moveDistance.text = moveValue?.ToString() ?? "-";
            attackDistance.text = attackDistanceValue?.ToString() ?? "-";
        }
    }

    /// <summary>상세창 내부의 필수 저작 참조를 상태 변경 전에 확인합니다.</summary>
    internal static class DeckSlotInfoReferences
    {
        internal static void RequireChild(Component field, Transform owner, string fieldName)
        {
            if (field == null || field.transform == owner || !field.transform.IsChildOf(owner))
                throw Error(owner, $"{fieldName}은 화면 내부의 필수 참조입니다. 대상: {(field == null ? "없음" : field.name)}.");
        }

        internal static InvalidOperationException Error(Transform owner, string message)
        {
            UIBase ui = owner.GetComponentInParent<UIBase>(true);
            return new InvalidOperationException($"[UnitInfo] UIId.{ui?.ID ?? UIId.None}, '{owner.name}': {message}");
        }
    }
}
