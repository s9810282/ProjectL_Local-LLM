using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Core.Table
{
    [ExcelAsset(ExcelName = "TerrainMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/TerrainTable/Core/Table", LogOnImport = true)]
    public sealed class TerrainMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<TerrainMasterData> TerrainMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (TerrainMaster == null)
                return Array.Empty<IDatabaseItem>();

            return TerrainMaster;
        }
    }
}
