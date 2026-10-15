using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IEnemyLoadoutTableQuery : IServiceModule
    {
        EnemyLoadoutData GetEnemyLoadout(int key);
        bool TryGetEnemyLoadout(int key, out EnemyLoadoutData data);
        IReadOnlyCollection<EnemyLoadoutData> GetAllEnemyLoadouts();
    }

    /// <summary>모든 EnemyLoadoutData를 LoadoutKey 기준으로 제공합니다.</summary>
    internal sealed class EnemyLoadoutTable : IEnemyLoadoutTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, EnemyLoadoutData> _dataByKey = new();

        internal EnemyLoadoutTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not EnemyLoadoutDataSO dataSO || dataSO.GetData() is not EnemyLoadoutData data)
                    throw new InvalidOperationException("[EnemyLoadoutTable] EnemyLoadoutTable requires non-null EnemyLoadoutDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.LoadoutKey, data))
                    throw new InvalidOperationException($"[EnemyLoadoutTable] Duplicate LoadoutKey: {data.LoadoutKey}.");
            }
        }

        public EnemyLoadoutData GetEnemyLoadout(int key)
        {
            if (_dataByKey.TryGetValue(key, out EnemyLoadoutData data))
                return data;

            throw new KeyNotFoundException($"[EnemyLoadoutTable] Enemy loadout data not found. LoadoutKey: {key}.");
        }

        public bool TryGetEnemyLoadout(int key, out EnemyLoadoutData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<EnemyLoadoutData> GetAllEnemyLoadouts() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IEnemyLoadoutTableQuery>(this);
        }
    }
}
