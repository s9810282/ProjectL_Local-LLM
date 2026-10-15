using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IRecoveryOptionTableQuery : IServiceModule
    {
        RecoveryOptionData GetRecoveryOption(int key);
        bool TryGetRecoveryOption(int key, out RecoveryOptionData data);
        IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions();
    }

    /// <summary>모든 RecoveryOptionData를 OptionId 기준으로 제공합니다.</summary>
    internal sealed class RecoveryOptionTable : IRecoveryOptionTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, RecoveryOptionData> _dataByKey = new();

        internal RecoveryOptionTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not RecoveryOptionDataSO dataSO || dataSO.GetData() is not RecoveryOptionData data)
                    throw new InvalidOperationException("[RecoveryOptionTable] Requires non-null RecoveryOptionDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.OptionId, data))
                    throw new InvalidOperationException($"[RecoveryOptionTable] Duplicate option id: {data.OptionId}.");
            }
        }

        public RecoveryOptionData GetRecoveryOption(int key)
        {
            if (_dataByKey.TryGetValue(key, out RecoveryOptionData data))
                return data;

            throw new KeyNotFoundException($"[RecoveryOptionTable] Recovery option not found. Key: {key}.");
        }

        public bool TryGetRecoveryOption(int key, out RecoveryOptionData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRecoveryOptionTableQuery>(this);
        }
    }
}
