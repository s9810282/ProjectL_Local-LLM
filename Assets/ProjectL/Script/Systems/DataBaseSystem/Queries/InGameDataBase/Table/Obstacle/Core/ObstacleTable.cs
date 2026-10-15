using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Obstacle.Core
{
    #region 서비스

    internal interface IObstacleTableQuery : IServiceModule
    {
        ObstacleMasterData GetObstacle(int key);
        bool TryGetObstacle(int key, out ObstacleMasterData data);
        IReadOnlyList<ObstacleMasterData> GetAllObstacles();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class ObstacleTable : IObstacleTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<ObstacleMasterData> index;

        internal ObstacleTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<ObstacleMasterData>(source, data => data.Key);
        }

        public ObstacleMasterData GetObstacle(int key) => index.Get(key);

        public bool TryGetObstacle(int key, out ObstacleMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<ObstacleMasterData> GetAllObstacles() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IObstacleTableQuery>(this);
        }
    }

    #endregion
}
