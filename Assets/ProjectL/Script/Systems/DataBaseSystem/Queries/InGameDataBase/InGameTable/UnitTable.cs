using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IUnitTableQuery : IServiceModule
    {
        UnitData GetUnit(EntityKey key);
        bool TryGetUnit(EntityKey key, out UnitData data);
        IReadOnlyCollection<UnitData> GetAllUnits();
    }

    /// <summary>모든 UnitData를 EntityKey 기준으로 제공합니다.</summary>
    internal sealed class UnitTable : IUnitTableQuery, IExportSystemServices
    {
        private readonly Dictionary<EntityKey, UnitData> _dataByKey = new();

        internal UnitTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not UnitDataSO dataSO || dataSO.GetData() is not UnitData data)
                    throw new InvalidOperationException("[UnitTable] UnitTable requires non-null UnitDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"[UnitTable] Duplicate unit key: {data.Key}.");
            }
        }

        public UnitData GetUnit(EntityKey key)
        {
            if (_dataByKey.TryGetValue(key, out UnitData data))
                return data;

            throw new KeyNotFoundException($"[UnitTable] Unit data not found. Key: {key}.");
        }

        public bool TryGetUnit(EntityKey key, out UnitData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<UnitData> GetAllUnits() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IUnitTableQuery>(this);
        }
    }
}
