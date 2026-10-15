using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface ITraitVisualTableQuery : IServiceModule
    {
        TraitVisualData GetTraitVisual(TraitKey key);
        bool TryGetTraitVisual(TraitKey key, out TraitVisualData data);
        IReadOnlyCollection<TraitVisualData> GetAllTraitVisuals();
    }

    /// <summary>유닛 특성 표시를 TraitKey 기준으로 제공합니다.</summary>
    internal sealed class TraitVisualTable : ITraitVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<TraitKey, TraitVisualData> _dataByKey = new();

        internal TraitVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not TraitVisualDataSO dataSO || dataSO.GetData() is not TraitVisualData data)
                    throw new InvalidOperationException("[TraitVisualTable] Requires non-null TraitVisualDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"[TraitVisualTable] Duplicate key: {data.Key}.");
            }
        }

        public TraitVisualData GetTraitVisual(TraitKey key)
        {
            if (_dataByKey.TryGetValue(key, out TraitVisualData data))
                return data;

            throw new KeyNotFoundException($"[TraitVisualTable] Trait visual not found. Key: {key}.");
        }

        public bool TryGetTraitVisual(TraitKey key, out TraitVisualData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<TraitVisualData> GetAllTraitVisuals() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ITraitVisualTableQuery>(this);
        }
    }
}
