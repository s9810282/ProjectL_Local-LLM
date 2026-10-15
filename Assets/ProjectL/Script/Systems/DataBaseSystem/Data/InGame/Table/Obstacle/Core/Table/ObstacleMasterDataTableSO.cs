using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Core.Table
{
    [ExcelAsset(ExcelName = "ObstacleMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/ObstacleTable/Core/Table", LogOnImport = true)]
    public sealed class ObstacleMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<ObstacleMasterData> ObstacleMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (ObstacleMaster == null)
                return Array.Empty<IDatabaseItem>();

            return ObstacleMaster;
        }
    }
}
