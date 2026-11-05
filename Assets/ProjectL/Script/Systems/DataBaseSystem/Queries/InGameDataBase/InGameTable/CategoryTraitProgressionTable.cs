using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface ICategoryTraitProgressionTableQuery : IServiceModule
    {
        CategoryTraitProgressionData Get(CategoryKey key);
        bool TryGet(CategoryKey key, out CategoryTraitProgressionData data);
        IReadOnlyList<CategoryTraitProgressionData> GetAll();
    }
    internal sealed class CategoryTraitProgressionTable : ICategoryTraitProgressionTableQuery, IExportSystemServices
    {
        private readonly Dictionary<CategoryKey, CategoryTraitProgressionData> _byKey = new();
        private readonly IReadOnlyList<CategoryTraitProgressionData> _rows;

        internal CategoryTraitProgressionTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var item in source.GetAllData())
            {
                if (item is not CategoryTraitProgressionDataSO so || so.GetData() is not CategoryTraitProgressionData data)
                    throw new InvalidOperationException("CategoryTraitProgression 테이블의 항목 형식이 올바르지 않습니다.");
                if (!_byKey.TryAdd(data.Category, data))
                    throw new InvalidOperationException($"CategoryTraitProgression 키가 중복됩니다: {data.Category}.");
            }
            _rows = Array.AsReadOnly(_byKey.Values.OrderBy(r => r.Category).ToArray());
        }

        public CategoryTraitProgressionData Get(CategoryKey key) => _byKey.TryGetValue(key, out var data)
            ? data : throw new KeyNotFoundException($"CategoryTraitProgression 키를 찾을 수 없습니다: {key}.");
        public bool TryGet(CategoryKey key, out CategoryTraitProgressionData data) => _byKey.TryGetValue(key, out data);
        public IReadOnlyList<CategoryTraitProgressionData> GetAll() => _rows;
        public void ExportServices(ISystemWriteSink sink) => sink.AddService<ICategoryTraitProgressionTableQuery>(this);
    }
}
