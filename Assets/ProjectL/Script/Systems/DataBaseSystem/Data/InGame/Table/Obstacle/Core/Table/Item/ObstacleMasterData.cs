using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Core.Table.Item
{
    /// <summary>A serialized row from the ObstacleMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class ObstacleMasterData : IExcelData
    {
        [SerializeField] private int Obstacle_Key;
        [SerializeField] private string Obstacle_Name = string.Empty;
        [SerializeField] private string Obstacle_NameKOR = string.Empty;
        [SerializeField] private int Obstacle_HealthValue;
        [SerializeField] private int Obstacle_HitEffect;
        [SerializeField] private int Obstacle_DestroyEffect;
        [SerializeField] private int Obstacle_EffectApplicationPoint;
        [SerializeField] private int Obstacle_Description;

        public int Key => Obstacle_Key;
        public string Name => Obstacle_Name;
        public string NameKOR => Obstacle_NameKOR;
        public int HealthValue => Obstacle_HealthValue;
        public int HitEffect => Obstacle_HitEffect;
        public int DestroyEffect => Obstacle_DestroyEffect;
        public int EffectApplicationPoint => Obstacle_EffectApplicationPoint;
        public int Description => Obstacle_Description;
    }
}
