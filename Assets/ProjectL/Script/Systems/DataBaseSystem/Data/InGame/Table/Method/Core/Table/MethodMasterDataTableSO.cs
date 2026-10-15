using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Method.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Method.Core.Table
{
    [ExcelAsset(ExcelName = "MethodMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/MethodTable/Core/Table", LogOnImport = true)]
    public sealed class MethodMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<MethodMasterData> MethodMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (MethodMaster == null)
                return Array.Empty<IDatabaseItem>();

            return MethodMaster;
        }
    }
}
