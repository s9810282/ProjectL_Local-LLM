using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Category.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface ICategoryVisualTableQuery : IServiceModule
    {
        CategoryVisualData GetCategoryVisual(CategoryKey key);
        bool TryGetCategoryVisual(CategoryKey key, out CategoryVisualData data);
        IReadOnlyCollection<CategoryVisualData> GetAllCategoryVisuals();
    }

    /// <summary>병종 공통 아이콘을 CategoryKey 기준으로 제공합니다.</summary>
    internal sealed class CategoryVisualTable : ICategoryVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<CategoryKey, CategoryVisualData> _dataByKey = new();

        internal CategoryVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not CategoryVisualDataSO dataSO || dataSO.GetData() is not CategoryVisualData data)
                    throw new InvalidOperationException("[CategoryVisualTable] CategoryVisualDataSO 항목이 필요합니다.");
                data.Validate();
                if (!_dataByKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"[CategoryVisualTable] 병종 키가 중복됩니다. Key: {data.Key}.");
            }
        }

        public CategoryVisualData GetCategoryVisual(CategoryKey key) => _dataByKey.TryGetValue(key, out var data)
            ? data : throw new KeyNotFoundException($"[CategoryVisualTable] 병종 외형이 없습니다. Key: {key}.");

        public bool TryGetCategoryVisual(CategoryKey key, out CategoryVisualData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<CategoryVisualData> GetAllCategoryVisuals() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<ICategoryVisualTableQuery>(this);
        }
    }
}
