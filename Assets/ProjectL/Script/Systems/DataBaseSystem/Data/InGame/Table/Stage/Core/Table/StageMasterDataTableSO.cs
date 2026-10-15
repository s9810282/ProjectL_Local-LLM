using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Core.Table
{
    [ExcelAsset(ExcelName = "StageMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/StageTable/Core/Table", LogOnImport = true)]
    public sealed class StageMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<StageMasterData> StageMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (StageMaster == null)
                return Array.Empty<IDatabaseItem>();

            return StageMaster;
        }
    }
}
