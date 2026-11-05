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
    internal interface ITraitGrowthDefinitionTableQuery : IServiceModule
    {
        TraitGrowthDefinitionData Get(TraitKey key);
        bool TryGet(TraitKey key, out TraitGrowthDefinitionData data);
        IReadOnlyList<TraitGrowthDefinitionData> GetAll();
    }
    internal sealed class TraitGrowthDefinitionTable : ITraitGrowthDefinitionTableQuery, IExportSystemServices
    {
        private readonly Dictionary<TraitKey, TraitGrowthDefinitionData> _byKey = new();
        private readonly IReadOnlyList<TraitGrowthDefinitionData> _rows;

        internal TraitGrowthDefinitionTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var item in source.GetAllData())
            {
                if (item is not TraitGrowthDefinitionDataSO so || so.GetData() is not TraitGrowthDefinitionData data)
                    throw new InvalidOperationException("TraitGrowthDefinition 테이블의 항목 형식이 올바르지 않습니다.");
                if (!_byKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"TraitGrowthDefinition 키가 중복됩니다: {data.Key}.");
            }
            _rows = Array.AsReadOnly(_byKey.Values.OrderBy(r => r.Key).ToArray());
        }

        public TraitGrowthDefinitionData Get(TraitKey key) => _byKey.TryGetValue(key, out var data)
            ? data : throw new KeyNotFoundException($"TraitGrowthDefinition 키를 찾을 수 없습니다: {key}.");
        public bool TryGet(TraitKey key, out TraitGrowthDefinitionData data) => _byKey.TryGetValue(key, out data);
        public IReadOnlyList<TraitGrowthDefinitionData> GetAll() => _rows;
        public void ExportServices(ISystemWriteSink sink) => sink.AddService<ITraitGrowthDefinitionTableQuery>(this);
    }
}
