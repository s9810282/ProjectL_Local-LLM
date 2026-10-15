using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface ITargetingMethodTableQuery : IServiceModule
    {
        TargetingMethodData GetTargetingMethod(int key);
        bool TryGetTargetingMethod(int key, out TargetingMethodData data);
        IReadOnlyCollection<TargetingMethodData> GetAllTargetingMethods();
    }

    /// <summary>검증된 공격 지정 방식을 MethodKey 기준으로 제공합니다.</summary>
    internal sealed class TargetingMethodTable : ITargetingMethodTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, TargetingMethodData> _dataByKey = new();

        internal TargetingMethodTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not TargetingMethodDataSO dataSO || dataSO.GetData() is not TargetingMethodData data)
                    throw new InvalidOperationException("[TargetingMethodTable] Requires non-null TargetingMethodDataSO items.");

                data.Validate();
                if (!_dataByKey.TryAdd(data.MethodKey, data))
                    throw new InvalidOperationException($"[TargetingMethodTable] Duplicate method key: {data.MethodKey}.");
            }
        }

        public TargetingMethodData GetTargetingMethod(int key)
        {
            if (_dataByKey.TryGetValue(key, out TargetingMethodData data))
                return data;

            throw new KeyNotFoundException($"[TargetingMethodTable] Targeting method not found. Key: {key}.");
        }

        public bool TryGetTargetingMethod(int key, out TargetingMethodData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<TargetingMethodData> GetAllTargetingMethods() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ITargetingMethodTableQuery>(this);
        }
    }
}
