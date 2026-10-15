using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Terrain.Core
{
    #region 서비스

    internal interface ITerrainTableQuery : IServiceModule
    {
        TerrainMasterData GetTerrain(int key);
        bool TryGetTerrain(int key, out TerrainMasterData data);
        IReadOnlyList<TerrainMasterData> GetAllTerrains();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class TerrainTable : ITerrainTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<TerrainMasterData> index;

        internal TerrainTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<TerrainMasterData>(source, data => data.Key);
        }

        public TerrainMasterData GetTerrain(int key) => index.Get(key);

        public bool TryGetTerrain(int key, out TerrainMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<TerrainMasterData> GetAllTerrains() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ITerrainTableQuery>(this);
        }
    }

    #endregion
}
