using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Chapter.Core.Table.Item
{
    /// <summary>A serialized row from the ChapterMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class ChapterMasterData : IExcelData
    {
        [SerializeField] private int Chapter_Key;
        [SerializeField] private string Chapter_Name = string.Empty;
        [SerializeField] private string Chapter_NameKOR = string.Empty;
        [SerializeField] private int Chapter_Length;
        [SerializeField] private int Chpater_StageWidth;
        [SerializeField] private string Chapter_StageList = string.Empty;
        [SerializeField] private int Chapter_Start;
        [SerializeField] private int Chapter_Point;
        [SerializeField] private int Chapter_PointStage;
        [SerializeField] private int Chapter_End;
        [SerializeField] private int Chapter_BattleStage;
        [SerializeField] private int Chatper_EventStage;
        [SerializeField] private int Chpater_NextChapter;
        [SerializeField] private int Chapter_Description;

        public int Key => Chapter_Key;
        public string Name => Chapter_Name;
        public string NameKOR => Chapter_NameKOR;
        public int Length => Chapter_Length;
        public int StageWidth => Chpater_StageWidth;
        public string StageList => Chapter_StageList;
        public int Start => Chapter_Start;
        public int Point => Chapter_Point;
        public int PointStage => Chapter_PointStage;
        public int End => Chapter_End;
        public int BattleStage => Chapter_BattleStage;
        public int EventStage => Chatper_EventStage;
        public int NextChapter => Chpater_NextChapter;
        public int Description => Chapter_Description;
    }
}
