using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.UpgradeStatistic.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.UpgradeStatistic.Core
{
    #region 서비스

    internal interface IUpgradeStatisticTableQuery : IServiceModule
    {
        UpgradeStatisticMasterData GetUpgradeStatistic(int key);
        bool TryGetUpgradeStatistic(int key, out UpgradeStatisticMasterData data);
        IReadOnlyList<UpgradeStatisticMasterData> GetAllUpgradeStatistics();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class UpgradeStatisticTable : IUpgradeStatisticTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<UpgradeStatisticMasterData> index;

        internal UpgradeStatisticTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<UpgradeStatisticMasterData>(source, data => data.Key);
        }

        public UpgradeStatisticMasterData GetUpgradeStatistic(int key) => index.Get(key);

        public bool TryGetUpgradeStatistic(int key, out UpgradeStatisticMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<UpgradeStatisticMasterData> GetAllUpgradeStatistics() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IUpgradeStatisticTableQuery>(this);
        }
    }

    #endregion
}
