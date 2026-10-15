using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.UpgradeStatistic.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.UpgradeStatistic.Core.Table
{
    [ExcelAsset(ExcelName = "UpgradeStatisticMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/UpgradeStatisticTable/Core/Table", LogOnImport = true)]
    public sealed class UpgradeStatisticMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<UpgradeStatisticMasterData> UpgradeStatisticMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (UpgradeStatisticMaster == null)
                return Array.Empty<IDatabaseItem>();

            return UpgradeStatisticMaster;
        }
    }
}
