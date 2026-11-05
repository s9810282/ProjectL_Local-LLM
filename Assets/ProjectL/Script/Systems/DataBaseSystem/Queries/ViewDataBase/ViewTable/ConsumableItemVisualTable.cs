using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ConsumableItem.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IConsumableItemVisualTableQuery : IServiceModule
    {
        ConsumableItemVisualData Get(int itemKey);
        bool TryGet(int itemKey, out ConsumableItemVisualData data);
        IReadOnlyCollection<ConsumableItemVisualData> GetAll();
    }

    /// <summary>소모 아이템 표시를 아이템 키 기준으로 제공합니다(가방 설계 P3).</summary>
    internal sealed class ConsumableItemVisualTable : IConsumableItemVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, ConsumableItemVisualData> _dataByKey = new();

        internal ConsumableItemVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not ConsumableItemVisualDataSO dataSO || dataSO.GetData() is not ConsumableItemVisualData data)
                    throw new InvalidOperationException("[ConsumableItemVisualTable] Requires non-null ConsumableItemVisualDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.ItemKey, data))
                    throw new InvalidOperationException($"[ConsumableItemVisualTable] Duplicate key: {data.ItemKey}.");
            }
        }

        public ConsumableItemVisualData Get(int itemKey)
        {
            if (_dataByKey.TryGetValue(itemKey, out ConsumableItemVisualData data))
                return data;

            throw new KeyNotFoundException($"[ConsumableItemVisualTable] Item visual not found. Key: {itemKey}.");
        }

        public bool TryGet(int itemKey, out ConsumableItemVisualData data) => _dataByKey.TryGetValue(itemKey, out data);

        public IReadOnlyCollection<ConsumableItemVisualData> GetAll() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IConsumableItemVisualTableQuery>(this);
        }
    }
}
