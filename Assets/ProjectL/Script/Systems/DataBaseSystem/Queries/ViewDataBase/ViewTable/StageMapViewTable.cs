using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IStageMapViewTableQuery : IServiceModule
    {
        StageMapViewData Config { get; }
    }

    /// <summary>설정형 테이블입니다. 아이템이 정확히 1개여야 합니다.</summary>
    internal sealed class StageMapViewTable : IStageMapViewTableQuery, IExportSystemServices
    {
        public StageMapViewData Config { get; }

        internal StageMapViewTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageMapViewDataSO dataSO || dataSO.GetData() is not StageMapViewData data)
                    throw new InvalidOperationException("[StageMapViewTable] Requires non-null StageMapViewDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[StageMapViewTable] Exactly one item is required, but found more.");

                data.Validate();
                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[StageMapViewTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageMapViewTableQuery>(this);
        }
    }
}
