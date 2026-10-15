using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.UpgradeStatistic.Core.Table.Item
{
    /// <summary>A serialized row from the UpgradeStatisticMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class UpgradeStatisticMasterData : IExcelData
    {
        [SerializeField] private int UpgradeStatistic_Key;
        [SerializeField] private int UpgradeStatistic_HealthValue;
        [SerializeField] private int UpgradeStatistic_AttackValue;
        [SerializeField] private int UpgradeStatistic_DefenseValue;
        [SerializeField] private int UpgradeStatistic_NeedExperiencePoint;
        [SerializeField] private int UpgradeStatistic_KillExperiencePoint;
        [SerializeField] private int UpgradeStatistic_KillGold;

        public int Key => UpgradeStatistic_Key;
        public int HealthValue => UpgradeStatistic_HealthValue;
        public int AttackValue => UpgradeStatistic_AttackValue;
        public int DefenseValue => UpgradeStatistic_DefenseValue;
        public int NeedExperiencePoint => UpgradeStatistic_NeedExperiencePoint;
        public int KillExperiencePoint => UpgradeStatistic_KillExperiencePoint;
        public int KillGold => UpgradeStatistic_KillGold;
    }
}
