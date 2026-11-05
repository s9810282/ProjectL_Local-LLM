using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IRockfallVisualTableQuery : IServiceModule
    {
        RockfallVisualData Config { get; }
    }

    /// <summary>
    /// 설정형 테이블입니다. 아이템이 정확히 1개여야 합니다. 값이 잘못되면 낙석이 나오는 전투까지 기다리지 않고 DB를 만들 때 실패합니다.
    /// </summary>
    internal sealed class RockfallVisualTable : IRockfallVisualTableQuery, IExportSystemServices
    {
        public RockfallVisualData Config { get; }

        internal RockfallVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not RockfallVisualDataSO dataSO || dataSO.GetData() is not RockfallVisualData data)
                    throw new InvalidOperationException("[RockfallVisualTable] Requires non-null RockfallVisualDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[RockfallVisualTable] Exactly one item is required, but found more.");

                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[RockfallVisualTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRockfallVisualTableQuery>(this);
        }
    }
}
