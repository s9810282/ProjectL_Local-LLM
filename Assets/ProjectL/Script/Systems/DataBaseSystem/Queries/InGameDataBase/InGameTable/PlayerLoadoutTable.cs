using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IPlayerLoadoutTableQuery : IServiceModule
    {
        PlayerLoadoutData GetPlayerLoadout(int key);
        bool TryGetPlayerLoadout(int key, out PlayerLoadoutData data);
        IReadOnlyCollection<PlayerLoadoutData> GetAllPlayerLoadouts();
    }

    /// <summary>모든 PlayerLoadoutData를 LoadoutKey 기준으로 제공합니다.</summary>
    internal sealed class PlayerLoadoutTable : IPlayerLoadoutTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, PlayerLoadoutData> _dataByKey = new();

        internal PlayerLoadoutTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not PlayerLoadoutDataSO dataSO || dataSO.GetData() is not PlayerLoadoutData data)
                    throw new InvalidOperationException("[PlayerLoadoutTable] PlayerLoadoutTable requires non-null PlayerLoadoutDataSO items.");

                data.Validate();
                // 초기 덱 번호의 모양은 저작 오류이므로 DB 생성에서 막습니다. 새 Run 시작은 Validate만 부릅니다(E2 인계 §3).
                data.ValidateInitialDeckIndices();

                if (!_dataByKey.TryAdd(data.LoadoutKey, data))
                    throw new InvalidOperationException($"[PlayerLoadoutTable] Duplicate LoadoutKey: {data.LoadoutKey}.");
            }
        }

        public PlayerLoadoutData GetPlayerLoadout(int key)
        {
            if (_dataByKey.TryGetValue(key, out PlayerLoadoutData data))
                return data;

            throw new KeyNotFoundException($"[PlayerLoadoutTable] Player loadout data not found. LoadoutKey: {key}.");
        }

        public bool TryGetPlayerLoadout(int key, out PlayerLoadoutData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<PlayerLoadoutData> GetAllPlayerLoadouts() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IPlayerLoadoutTableQuery>(this);
        }
    }
}
