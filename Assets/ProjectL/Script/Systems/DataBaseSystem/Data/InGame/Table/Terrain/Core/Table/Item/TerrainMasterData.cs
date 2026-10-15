using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Core.Table.Item
{
    /// <summary>A serialized row from the TerrainMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class TerrainMasterData : IExcelData
    {
        [SerializeField] private int Terrain_Key;
        [SerializeField] private string Terrain_Name = string.Empty;
        [SerializeField] private string Terrain_NameKOR = string.Empty;
        [SerializeField] private int Terrain_Effect;
        [SerializeField] private int Terrain_Continuity;
        [SerializeField] private int Terrain_EffectTurn;
        [SerializeField] private int Terrain_Description;

        public int Key => Terrain_Key;
        public string Name => Terrain_Name;
        public string NameKOR => Terrain_NameKOR;
        public int Effect => Terrain_Effect;
        public int Continuity => Terrain_Continuity;
        public int EffectTurn => Terrain_EffectTurn;
        public int Description => Terrain_Description;
    }
}
