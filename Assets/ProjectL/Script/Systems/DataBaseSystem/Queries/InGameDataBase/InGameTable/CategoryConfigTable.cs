using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface ICategoryConfigTableQuery : IServiceModule
    {
        CategoryConfigData Config { get; }
    }

    /// <summary>설정형 테이블입니다. 아이템이 정확히 1개여야 합니다.</summary>
    internal sealed class CategoryConfigTable : ICategoryConfigTableQuery, IExportSystemServices
    {
        public CategoryConfigData Config { get; }

        internal CategoryConfigTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not CategoryConfigDataSO dataSO || dataSO.GetData() is not CategoryConfigData data)
                    throw new InvalidOperationException("[CategoryConfigTable] Requires non-null CategoryConfigDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[CategoryConfigTable] Exactly one item is required, but found more.");

                data.Validate();
                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[CategoryConfigTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICategoryConfigTableQuery>(this);
        }
    }
}
