using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IStageEconomyTableQuery : IServiceModule
    {
        StageEconomyData GetStageEconomy(int key);
        bool TryGetStageEconomy(int key, out StageEconomyData data);
        IReadOnlyCollection<StageEconomyData> GetAllStageEconomies();
    }

    /// <summary>모든 StageEconomyData를 EconomyKey 기준으로 제공합니다.</summary>
    internal sealed class StageEconomyTable : IStageEconomyTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, StageEconomyData> _dataByKey = new();

        internal StageEconomyTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageEconomyDataSO dataSO || dataSO.GetData() is not StageEconomyData data)
                    throw new InvalidOperationException("[StageEconomyTable] Requires non-null StageEconomyDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.EconomyKey, data))
                    throw new InvalidOperationException($"[StageEconomyTable] Duplicate economy key: {data.EconomyKey}.");
            }
        }

        public StageEconomyData GetStageEconomy(int key)
        {
            if (_dataByKey.TryGetValue(key, out StageEconomyData data))
                return data;

            throw new KeyNotFoundException($"[StageEconomyTable] Stage economy not found. Key: {key}.");
        }

        public bool TryGetStageEconomy(int key, out StageEconomyData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<StageEconomyData> GetAllStageEconomies() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageEconomyTableQuery>(this);
        }
    }
}
