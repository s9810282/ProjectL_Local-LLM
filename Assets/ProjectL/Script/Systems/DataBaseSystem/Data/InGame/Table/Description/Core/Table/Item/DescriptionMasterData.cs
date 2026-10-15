using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Description.Core.Table.Item
{
    /// <summary>A serialized row from the DescriptionMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class DescriptionMasterData : IExcelData
    {
        [SerializeField] private int Description_Key;
        [SerializeField] private string Description_TextKOR = string.Empty;

        public int Key => Description_Key;
        public string TextKOR => Description_TextKOR;
    }
}
