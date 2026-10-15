using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Core.Table
{
    [ExcelAsset(ExcelName = "ItemMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/ItemTable/Core/Table", LogOnImport = true)]
    public sealed class ItemMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<ItemMasterData> ItemMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (ItemMaster == null)
                return Array.Empty<IDatabaseItem>();

            return ItemMaster;
        }
    }
}
