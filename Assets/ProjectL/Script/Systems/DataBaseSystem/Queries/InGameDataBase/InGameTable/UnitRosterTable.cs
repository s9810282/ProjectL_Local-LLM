using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IUnitRosterTableQuery : IServiceModule
    {
        UnitRosterData GetUnitRoster(int key);
        bool TryGetUnitRoster(int key, out UnitRosterData data);
        IReadOnlyCollection<UnitRosterData> GetAllUnitRosters();
    }

    /// <summary>모든 UnitRosterData를 RosterKey 기준으로 제공합니다.</summary>
    internal sealed class UnitRosterTable : IUnitRosterTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, UnitRosterData> _dataByKey = new();

        internal UnitRosterTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not UnitRosterDataSO dataSO || dataSO.GetData() is not UnitRosterData data)
                    throw new InvalidOperationException("[UnitRosterTable] UnitRosterTable requires non-null UnitRosterDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.RosterKey, data))
                    throw new InvalidOperationException($"[UnitRosterTable] Duplicate RosterKey: {data.RosterKey}.");
            }
        }

        public UnitRosterData GetUnitRoster(int key)
        {
            if (_dataByKey.TryGetValue(key, out UnitRosterData data))
                return data;

            throw new KeyNotFoundException($"[UnitRosterTable] Unit roster data not found. RosterKey: {key}.");
        }

        public bool TryGetUnitRoster(int key, out UnitRosterData data) => _dataByKey.TryGetValue(key, out data);

        public IReadOnlyCollection<UnitRosterData> GetAllUnitRosters() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IUnitRosterTableQuery>(this);
        }
    }
}
