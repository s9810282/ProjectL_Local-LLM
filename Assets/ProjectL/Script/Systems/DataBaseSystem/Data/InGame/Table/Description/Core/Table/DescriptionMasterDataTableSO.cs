using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Description.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Description.Core.Table
{
    [ExcelAsset(ExcelName = "DescriptionMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/DescriptionTable/Core/Table", LogOnImport = true)]
    public sealed class DescriptionMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<DescriptionMasterData> DescriptionMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (DescriptionMaster == null)
                return Array.Empty<IDatabaseItem>();

            return DescriptionMaster;
        }
    }
}
