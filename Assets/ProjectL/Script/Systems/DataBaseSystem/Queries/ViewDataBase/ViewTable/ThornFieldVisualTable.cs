using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ThornField.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IThornFieldVisualTableQuery : IServiceModule
    {
        ThornFieldVisualData Config { get; }
    }

    /// <summary>
    /// 설정형 테이블입니다. 아이템이 정확히 1개여야 하고 프리팹이 있어야 합니다(View 3라운드 W12-5).
    /// 프리팹이 없으면 가시밭이 나오는 전투까지 기다리지 않고 DB를 만들 때 실패합니다.
    /// </summary>
    internal sealed class ThornFieldVisualTable : IThornFieldVisualTableQuery, IExportSystemServices
    {
        public ThornFieldVisualData Config { get; }

        internal ThornFieldVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not ThornFieldVisualDataSO dataSO || dataSO.GetData() is not ThornFieldVisualData data)
                    throw new InvalidOperationException("[ThornFieldVisualTable] Requires non-null ThornFieldVisualDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[ThornFieldVisualTable] Exactly one item is required, but found more.");
                data.Validate();

                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[ThornFieldVisualTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IThornFieldVisualTableQuery>(this);
        }
    }
}
