using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item;
namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IConsumableItemTableQuery : IServiceModule
    {
        ConsumableItemData Get(int key);
        bool TryGet(int key, out ConsumableItemData data);
        IReadOnlyList<ConsumableItemData> GetAll();
    }

    internal sealed class ConsumableItemTable : IConsumableItemTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, ConsumableItemData> _byKey = new();
        private readonly IReadOnlyList<ConsumableItemData> _rows;

        internal ConsumableItemTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var item in source.GetAllData())
            {
                if (item is not ConsumableItemDataSO so || so.GetData() is not ConsumableItemData data)
                    throw new InvalidOperationException("아이템 정의가 없습니다.");
                if (!_byKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException("아이템 키가 중복됩니다.");
            }
            _rows = Array.AsReadOnly(_byKey.Values.OrderBy(x => x.Key).ToArray());
        }

        public ConsumableItemData Get(int key) => _byKey.TryGetValue(key, out var row)
            ? row : throw new KeyNotFoundException($"아이템 키를 찾을 수 없습니다: {key}");
        public bool TryGet(int key, out ConsumableItemData data) => _byKey.TryGetValue(key, out data);
        public IReadOnlyList<ConsumableItemData> GetAll() => _rows;
        public void ExportServices(ISystemWriteSink sink) => sink.AddService<IConsumableItemTableQuery>(this);
    }
}
