using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Enhancement.Item;
namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IEnhancementTableQuery : IServiceModule
    {
        EnhancementData Get(EnhancementKey key);
        bool TryGet(EnhancementKey key, out EnhancementData data);
        IReadOnlyList<EnhancementData> GetAll();
    }

    internal sealed class EnhancementTable : IEnhancementTableQuery, IExportSystemServices
    {
        private readonly Dictionary<EnhancementKey, EnhancementData> _byKey = new();
        private readonly IReadOnlyList<EnhancementData> _rows;

        internal EnhancementTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var item in source.GetAllData())
            {
                if (item is not EnhancementDataSO so || so.GetData() is not EnhancementData data)
                    throw new InvalidOperationException("강화 정의가 없습니다.");
                if (!_byKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException("강화 키가 중복됩니다.");
            }
            _rows = Array.AsReadOnly(_byKey.Values.OrderBy(x => x.Key).ToArray());
        }

        public EnhancementData Get(EnhancementKey key) => _byKey.TryGetValue(key, out var row)
            ? row : throw new KeyNotFoundException($"강화 키를 찾을 수 없습니다: {key}");
        public bool TryGet(EnhancementKey key, out EnhancementData data) => _byKey.TryGetValue(key, out data);
        public IReadOnlyList<EnhancementData> GetAll() => _rows;
        public void ExportServices(ISystemWriteSink sink) => sink.AddService<IEnhancementTableQuery>(this);
    }
}
