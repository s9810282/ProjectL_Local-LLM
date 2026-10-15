using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Stage.Core
{
    #region 서비스

    internal interface IStageTableQuery : IServiceModule
    {
        StageMasterData GetStage(int key);
        bool TryGetStage(int key, out StageMasterData data);
        IReadOnlyList<StageMasterData> GetAllStages();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class StageTable : IStageTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<StageMasterData> index;

        internal StageTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<StageMasterData>(source, data => data.Key);
        }

        public StageMasterData GetStage(int key) => index.Get(key);

        public bool TryGetStage(int key, out StageMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<StageMasterData> GetAllStages() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageTableQuery>(this);
        }
    }

    #endregion
}
