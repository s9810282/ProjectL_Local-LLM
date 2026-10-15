using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IUnitVisualTableQuery : IServiceModule
    {
        UnitVisualData GetUnitVisual(EntityKey key);
        bool TryGetUnitVisual(EntityKey key, out UnitVisualData data);
        IReadOnlyCollection<UnitVisualData> GetAllUnitVisuals();
    }

    /// <summary>유닛·구조물 외형을 EntityKey 기준으로 제공합니다.</summary>
    internal sealed class UnitVisualTable : IUnitVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<EntityKey, UnitVisualData> _dataByKey = new();

        internal UnitVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not UnitVisualDataSO dataSO || dataSO.GetData() is not UnitVisualData data)
                    throw new InvalidOperationException("[UnitVisualTable] Requires non-null UnitVisualDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"[UnitVisualTable] Duplicate key: {data.Key}.");
            }
        }

        public UnitVisualData GetUnitVisual(EntityKey key)
        {
            if (_dataByKey.TryGetValue(key, out UnitVisualData data))
                return data;

            throw new KeyNotFoundException($"[UnitVisualTable] Unit visual not found. Key: {key}.");
        }

        public bool TryGetUnitVisual(EntityKey key, out UnitVisualData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<UnitVisualData> GetAllUnitVisuals() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IUnitVisualTableQuery>(this);
        }
    }
}
