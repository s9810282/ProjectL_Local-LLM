using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IStageFieldViewTableQuery : IServiceModule
    {
        StageFieldViewData Config { get; }
    }

    /// <summary>설정형 테이블입니다. 아이템이 정확히 1개여야 합니다.</summary>
    internal sealed class StageFieldViewTable : IStageFieldViewTableQuery, IExportSystemServices
    {
        public StageFieldViewData Config { get; }

        internal StageFieldViewTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageFieldViewDataSO dataSO || dataSO.GetData() is not StageFieldViewData data)
                    throw new InvalidOperationException("[StageFieldViewTable] Requires non-null StageFieldViewDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[StageFieldViewTable] Exactly one item is required, but found more.");

                data.Validate();
                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[StageFieldViewTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageFieldViewTableQuery>(this);
        }
    }
}
