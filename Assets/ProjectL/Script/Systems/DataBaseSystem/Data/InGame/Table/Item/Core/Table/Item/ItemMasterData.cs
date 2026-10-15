using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Core.Table.Item
{
    /// <summary>A serialized row from the ItemMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class ItemMasterData : IExcelData
    {
        [SerializeField] private int Item_Key;
        [SerializeField] private string Item_Name = string.Empty;
        [SerializeField] private string Item_NameKOR = string.Empty;
        [SerializeField] private int Item_CostGold;
        [SerializeField] private int Item_Effect;
        [SerializeField] private int Item_EffectContinuity;
        [SerializeField] private int Item_EffectTurn;
        [SerializeField] private int Item_Trigger;
        [SerializeField] private int Item_Description;

        public int Key => Item_Key;
        public string Name => Item_Name;
        public string NameKOR => Item_NameKOR;
        public int CostGold => Item_CostGold;
        public int Effect => Item_Effect;
        public int EffectContinuity => Item_EffectContinuity;
        public int EffectTurn => Item_EffectTurn;
        public int Trigger => Item_Trigger;
        public int Description => Item_Description;
    }
}
