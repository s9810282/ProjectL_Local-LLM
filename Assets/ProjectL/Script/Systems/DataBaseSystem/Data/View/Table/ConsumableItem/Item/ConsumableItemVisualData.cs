using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.ConsumableItem.Item
{
    /// <summary>
    /// 소모 아이템의 표시 이름, 아이콘, 설명, 발동 소리·큐입니다. 키는 InGame 소모 아이템과 같은 아이템 키입니다(가방 설계 P3).
    /// 조회마다 새로 만드는 읽기 전용 값입니다.
    /// </summary>
    internal sealed class ConsumableItemVisualData : IData
    {
        internal ConsumableItemVisualData(int itemKey, string displayName, Sprite icon, string description,
            int itemSoundPreset, CombatEventCues activationCues)
        {
            ItemKey = itemKey;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? itemKey.ToString() : displayName;
            Icon = icon;
            Description = description ?? string.Empty;
            ItemSoundPreset = itemSoundPreset;
            ActivationCues = activationCues ?? throw new ArgumentNullException(nameof(activationCues));
        }

        public int ItemKey { get; }
        public string DisplayName { get; }
        /// <summary>비어 있으면 null입니다.</summary>
        public Sprite Icon { get; }
        public string Description { get; }
        /// <summary>발동 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        public int ItemSoundPreset { get; }
        /// <summary>쓰인 순간의 큐입니다. 조회마다 새 Data와 함께 새로 만듭니다(이펙트 창구 확장 계획 P6).</summary>
        internal CombatEventCues ActivationCues { get; }

        internal void Validate()
        {
            if (ItemKey < 1)
                throw new InvalidOperationException($"[ConsumableItemVisualData] Item key must be positive. Key: {ItemKey}");
            CombatPresentationSettingsData.ValidateSoundPreset(ItemSoundPreset, $"itemSoundPreset (Key: {ItemKey})");
            CombatPresentationCueRules.Require(ActivationCues, CombatCueEvent.ItemActivated, $"[ConsumableItemVisualData] Key: {ItemKey}");
        }
    }
}
