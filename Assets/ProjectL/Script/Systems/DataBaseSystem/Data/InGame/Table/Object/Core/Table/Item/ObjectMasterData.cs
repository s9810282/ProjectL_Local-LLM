using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Core.Table.Item
{
    /// <summary>A serialized row from the ObjectMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class ObjectMasterData : IExcelData
    {
        [SerializeField] private int Object_Key;
        [SerializeField] private string Object_Name = string.Empty;
        [SerializeField] private string Object_NameKOR = string.Empty;
        [SerializeField] private int Object_Effect;
        [SerializeField] private int Object_EffectMethod;
        [SerializeField] private int Object_EffectContinuity;
        [SerializeField] private int Object_EffectTurn;
        [SerializeField] private int Object_Description;

        public int Key => Object_Key;
        public string Name => Object_Name;
        public string NameKOR => Object_NameKOR;
        public int Effect => Object_Effect;
        public int EffectMethod => Object_EffectMethod;
        public int EffectContinuity => Object_EffectContinuity;
        public int EffectTurn => Object_EffectTurn;
        public int Description => Object_Description;
    }
}
