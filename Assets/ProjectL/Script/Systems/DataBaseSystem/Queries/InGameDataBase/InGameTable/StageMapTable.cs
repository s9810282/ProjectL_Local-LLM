using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    #region 서비스

    internal interface IStageMapTableQuery : IServiceModule
    {
        StageMapData GetStageMap(int mapKey);
        IReadOnlyCollection<StageMapData> GetAllStageMaps();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class StageMapTable : IStageMapTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, StageMapData> dataByKey = new();
        private readonly ReadOnlyCollection<StageMapData> allData;

        internal StageMapTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var entries = new List<StageMapData>();
            foreach (var item in source.GetAllData())
            {
                if (!(item is StageMapDataSO dataSO) || !(dataSO.GetData() is StageMapData data))
                    throw new InvalidOperationException("StageMapTable requires non-null StageMapDataSO items.");
                data.Validate();
                if (dataByKey.ContainsKey(data.MapKey))
                    throw new InvalidOperationException($"Duplicate MapKey: {data.MapKey}.");
                dataByKey.Add(data.MapKey, data);
                entries.Add(data);
            }
            allData = entries.AsReadOnly();
        }

        public StageMapData GetStageMap(int mapKey)
        {
            if (dataByKey.TryGetValue(mapKey, out var data)) return data;
            throw new KeyNotFoundException($"StageMap data not found: {mapKey}.");
        }

        public IReadOnlyCollection<StageMapData> GetAllStageMaps() => allData;
        public void ExportServices(ISystemWriteSink sink) => sink.AddService<IStageMapTableQuery>(this);
    }

    #endregion
}
