using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Authoring;
using UnityEngine;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item
{
    [CreateAssetMenu(fileName = "NewConsumableItem", menuName = "ProjectL/Database/InGame/ConsumableItem")]
    internal sealed class ConsumableItemDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int key;
        internal int Key => key;
        [SerializeField, Min(0)] private int costGold;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeReference] private ItemEffectAuthoring effect;

        public override IData GetData()
        {
            // 지원하는 효과인지는 Core 규칙 등록부가 판단한다(정의 어댑터·전투 준비에서 검증).
            if (effect == null)
                throw new InvalidOperationException("아이템 효과가 필요합니다.");
            return new ConsumableItemData(key, costGold, displayName, description, effect.ToSpec());
        }
    }
}
