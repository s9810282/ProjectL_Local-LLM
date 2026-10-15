using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Character.Core.Table.Item
{
    /// <summary>A serialized row from the CharacterMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class CharacterMasterData : IExcelData
    {
        [SerializeField] private int Character_Key;
        [SerializeField] private string Character_Name = string.Empty;
        [SerializeField] private string Character_NameKOR = string.Empty;
        [SerializeField] private int Character_Level;
        [SerializeField] private int Character_MaxLevel;
        [SerializeField] private int Character_RoleType;
        [SerializeField] private int Character_PositionType;
        [SerializeField] private int Character_Unlock;
        [SerializeField] private int Character_UnlockTrigger;
        [SerializeField] private int Character_HealthValue;
        [SerializeField] private int Character_AttackValue;
        [SerializeField] private int Character_DefenseValue;
        [SerializeField] private int Character_Priority;
        [SerializeField] private float Character_Delay;
        [SerializeField] private int Character_MaxCost;
        [SerializeField] private int Character_MoveMethod;
        [SerializeField] private int Character_MoveRange;
        [SerializeField] private int Character_MoveCost;
        [SerializeField] private int Character_AttackMethod;
        [SerializeField] private int Character_AttackRange;
        [SerializeField] private int Character_AttackCost;
        [SerializeField] private int Character_WeaponUpgradeEffect;
        [SerializeField] private int Character_UpgradeStatistic;
        [SerializeField] private string Character_State = string.Empty;
        [SerializeField] private int Character_Inventory;
        [SerializeField] private int Character_NeedExperiencePoint;
        [SerializeField] private int Character_KillExperiencePoint;
        [SerializeField] private int Character_KillGold;
        [SerializeField] private int Character_Description;

        public int Key => Character_Key;
        public string Name => Character_Name;
        public string NameKOR => Character_NameKOR;
        public int Level => Character_Level;
        public int MaxLevel => Character_MaxLevel;
        public int RoleType => Character_RoleType;
        public int PositionType => Character_PositionType;
        public int Unlock => Character_Unlock;
        public int UnlockTrigger => Character_UnlockTrigger;
        public int HealthValue => Character_HealthValue;
        public int AttackValue => Character_AttackValue;
        public int DefenseValue => Character_DefenseValue;
        public int Priority => Character_Priority;
        public float Delay => Character_Delay;
        public int MaxCost => Character_MaxCost;
        public int MoveMethod => Character_MoveMethod;
        public int MoveRange => Character_MoveRange;
        public int MoveCost => Character_MoveCost;
        public int AttackMethod => Character_AttackMethod;
        public int AttackRange => Character_AttackRange;
        public int AttackCost => Character_AttackCost;
        public int WeaponUpgradeEffect => Character_WeaponUpgradeEffect;
        public int UpgradeStatistic => Character_UpgradeStatistic;
        public string State => Character_State;
        public int Inventory => Character_Inventory;
        public int NeedExperiencePoint => Character_NeedExperiencePoint;
        public int KillExperiencePoint => Character_KillExperiencePoint;
        public int KillGold => Character_KillGold;
        public int Description => Character_Description;
    }
}
