using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IRailwayVisualTableQuery : IServiceModule
    {
        RailwayVisualData Config { get; }
    }

    /// <summary>설정형 테이블입니다. 아이템이 정확히 1개여야 합니다.</summary>
    internal sealed class RailwayVisualTable : IRailwayVisualTableQuery, IExportSystemServices
    {
        public RailwayVisualData Config { get; }

        internal RailwayVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not RailwayVisualDataSO dataSO || dataSO.GetData() is not RailwayVisualData data)
                    throw new InvalidOperationException("[RailwayVisualTable] Requires non-null RailwayVisualDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[RailwayVisualTable] Exactly one item is required, but found more.");

                data.Validate();
                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[RailwayVisualTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRailwayVisualTableQuery>(this);
        }
    }
}
