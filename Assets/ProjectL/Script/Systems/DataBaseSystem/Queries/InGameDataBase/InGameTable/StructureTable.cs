using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IStructureTableQuery : IServiceModule
    {
        StructureData GetStructure(EntityKey key);
        bool TryGetStructure(EntityKey key, out StructureData data);
        IReadOnlyCollection<StructureData> GetAllStructures();
    }

    /// <summary>모든 StructureData를 EntityKey 기준으로 제공합니다.</summary>
    internal sealed class StructureTable : IStructureTableQuery, IExportSystemServices
    {
        private readonly Dictionary<EntityKey, StructureData> _dataByKey = new();

        internal StructureTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StructureDataSO dataSO || dataSO.GetData() is not StructureData data)
                    throw new InvalidOperationException("[StructureTable] StructureTable requires non-null StructureDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"[StructureTable] Duplicate structure key: {data.Key}.");
            }
        }

        public StructureData GetStructure(EntityKey key)
        {
            if (_dataByKey.TryGetValue(key, out StructureData data))
                return data;

            throw new KeyNotFoundException($"[StructureTable] Structure data not found. Key: {key}.");
        }

        public bool TryGetStructure(EntityKey key, out StructureData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<StructureData> GetAllStructures() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStructureTableQuery>(this);
        }
    }
}
