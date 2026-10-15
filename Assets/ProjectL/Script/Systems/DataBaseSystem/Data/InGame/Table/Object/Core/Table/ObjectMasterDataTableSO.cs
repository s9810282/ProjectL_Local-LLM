using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Core.Table
{
    [ExcelAsset(ExcelName = "ObjectMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/ObjectTable/Core/Table", LogOnImport = true)]
    public sealed class ObjectMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<ObjectMasterData> ObjectMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (ObjectMaster == null)
                return Array.Empty<IDatabaseItem>();

            return ObjectMaster;
        }
    }
}
