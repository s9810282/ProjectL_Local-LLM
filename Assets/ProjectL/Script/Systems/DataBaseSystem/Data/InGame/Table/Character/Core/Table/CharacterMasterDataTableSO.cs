using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Character.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Character.Core.Table
{
    [ExcelAsset(ExcelName = "CharacterMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/CharacterTable/Core/Table", LogOnImport = true)]
    public sealed class CharacterMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<CharacterMasterData> CharacterMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (CharacterMaster == null)
                return Array.Empty<IDatabaseItem>();

            return CharacterMaster;
        }
    }
}
