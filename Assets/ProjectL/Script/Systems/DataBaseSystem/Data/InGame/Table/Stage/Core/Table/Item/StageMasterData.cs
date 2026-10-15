using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Core.Table.Item
{
    /// <summary>A serialized row from the StageMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class StageMasterData : IExcelData
    {
        [SerializeField] private int Stage_Key;
        [SerializeField] private string Stage_Name = string.Empty;
        [SerializeField] private string Stage_NameKOR = string.Empty;
        [SerializeField] private int Stage_RoleType;
        [SerializeField] private int Stage_Weather;
        [SerializeField] private int Stage_Event;
        [SerializeField] private int Stage_Level;
        [SerializeField] private int Stage_Clear;
        [SerializeField] private int Stage_Continuity;
        [SerializeField] private int Stage_ContinuityTime;
        [SerializeField] private float Stage_Weight;
        [SerializeField] private int Stage_WeightOption;
        [SerializeField] private int Stage_RewardItem;
        [SerializeField] private int Stage_RewardFame;
        [SerializeField] private int Stage_Description;

        public int Key => Stage_Key;
        public string Name => Stage_Name;
        public string NameKOR => Stage_NameKOR;
        public int RoleType => Stage_RoleType;
        public int Weather => Stage_Weather;
        public int Event => Stage_Event;
        public int Level => Stage_Level;
        public int Clear => Stage_Clear;
        public int Continuity => Stage_Continuity;
        public int ContinuityTime => Stage_ContinuityTime;
        public float Weight => Stage_Weight;
        public int WeightOption => Stage_WeightOption;
        public int RewardItem => Stage_RewardItem;
        public int RewardFame => Stage_RewardFame;
        public int Description => Stage_Description;
    }
}
