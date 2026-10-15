using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Chapter.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Chapter.Core.Table
{
    [ExcelAsset(ExcelName = "ChapterMaster", AssetPath = "ProjectL/DataBaseSO/InGameGroup/ChapterTable/Core/Table", LogOnImport = true)]
    public sealed class ChapterMasterDataTableSO : DatabaseTable
    {
        // The field name must match the worksheet name for ExcelImporter.
        public List<ChapterMasterData> ChapterMaster = new();

        public override IEnumerable<IDatabaseItem> GetAllData()
        {
            if (ChapterMaster == null)
                return Array.Empty<IDatabaseItem>();

            return ChapterMaster;
        }
    }
}
